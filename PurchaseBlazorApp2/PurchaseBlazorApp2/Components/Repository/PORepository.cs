using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;
using PurchaseBlazorApp2.Resource;
using ServiceStack;
using System.Data;
using System.Data.Common;
using System.Transactions;



namespace PurchaseBlazorApp2.Components.Repository
{
    public class PORepository
    {
        private NpgsqlConnection Connection;
        public PORepository()
        {
            Connection = GetConnection();
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection($"Server={StaticResources.ConnectionId()};Port=5432; User Id=postgres; Password=password; Database=purchase");
        }

        public async Task<List<DateTime>> GetDeliveryDatesAsync(List<string> requisitionNumbers)
        {
            var results = new List<DateTime>();

            if (requisitionNumbers == null || requisitionNumbers.Count == 0)
                return results;

            try
            {
                await Connection.OpenAsync();

                string query = @"
            SELECT pr_id, deliverydate
            FROM potable
            WHERE pr_id = ANY(@ids);";

                var tempResults = new Dictionary<string, DateTime>();

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("ids", requisitionNumbers);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string prId = reader.GetString(0);
                            DateTime deliveryDate = reader.GetDateTime(1);
                            tempResults[prId] = deliveryDate;
                        }
                    }
                }

                // Always return in same order as requisitionNumbers
                foreach (var prId in requisitionNumbers)
                {
                    if (tempResults.TryGetValue(prId, out var date))
                        results.Add(date);
                    else
                        results.Add(DateTime.MinValue); // placeholder for missing -> UI can show empty
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDeliveryDatesAsync Exception: {ex.Message}");
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return results;
        }

        public async Task<List<PurchaseOrderRecord>> GetRecordsAsyncWithPR(List<string> requisitionNumbers = null)
        {
            List<PurchaseOrderRecord> ToReturn = new List<PurchaseOrderRecord>();

            try
            {
                await Connection.OpenAsync();

                string query = "SELECT * FROM potable";

                var command = new NpgsqlCommand();
                command.Connection = Connection;

                if (requisitionNumbers != null && requisitionNumbers.Count > 0)
                {
                    var paramNames = new List<string>();
                    for (int i = 0; i < requisitionNumbers.Count; i++)
                    {
                        string paramName = $"@id{i}";
                        paramNames.Add(paramName);
                        command.Parameters.AddWithValue(paramName, requisitionNumbers[i]);
                    }

                    string inClause = string.Join(", ", paramNames);
                    query += $" WHERE pr_id IN ({inClause})";
                }

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        PurchaseOrderRecord MainInfo = new PurchaseOrderRecord();
                        InsertInfoOfBasicInfo(MainInfo, reader);
                        MainInfo.ReceiveInfo = await GetReceiveInfo(MainInfo.PO_ID);
                        MainInfo.InvoiceInfo = await GetInvoiceInfo(MainInfo.PO_ID);
                        ToReturn.Add(MainInfo);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecordsAsync Exception: {ex.Message}");
                return ToReturn;
            }
            finally
            {
                await Connection.CloseAsync();
            }
            return ToReturn;
        }


        public async Task<List<PurchaseOrderRecord>> GetRecordsAsync(List<string> requisitionNumbers = null)
        {
            List<PurchaseOrderRecord> ToReturn = new List<PurchaseOrderRecord>();
            var PRRepository = new PRRepository();

            try
            {
                await Connection.OpenAsync();

                string query = "SELECT * FROM potable";

                var command = new NpgsqlCommand();
                command.Connection = Connection;

                if (requisitionNumbers != null && requisitionNumbers.Count > 0)
                {
                    var paramNames = new List<string>();
                    for (int i = 0; i < requisitionNumbers.Count; i++)
                    {
                        string paramName = $"@id{i}";
                        paramNames.Add(paramName);
                        command.Parameters.AddWithValue(paramName, requisitionNumbers[i]);
                    }

                    string inClause = string.Join(", ", paramNames);
                    query += $" WHERE po_id IN ({inClause})";
                }

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        PurchaseOrderRecord MainInfo = new PurchaseOrderRecord();
                        InsertInfoOfBasicInfo(MainInfo, reader);
                        MainInfo.ReceiveInfo= await GetReceiveInfo(MainInfo.PO_ID);
                        MainInfo.InvoiceInfo = await GetInvoiceInfo(MainInfo.PO_ID);

                        var PRRecords = await PRRepository.GetRecordsAsync(new List<string> { MainInfo.PR_ID });
                        if (PRRecords.Count > 0)
                        {
                            MainInfo._Approvals = PRRecords[0].Approvals;
                        }
                        ToReturn.Add(MainInfo);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecordsAsync Exception: {ex.Message}");
                return ToReturn;
            }
            finally
            {
                await Connection.CloseAsync();
            }
            return ToReturn;
        }

        public async Task<InvoiceInfo?> GetInvoiceInfo(string poNumber)
        {
            InvoiceInfo? invoice = new InvoiceInfo();

            try
            {
                await using var connection = GetConnection();
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    "SELECT imagebyte, photoformat, paymentstatus " +
                    "FROM po_invoice_image_table WHERE requisitionnumber = @req",
                    connection);
                command.Parameters.AddWithValue("@req", poNumber);

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var statusText = reader["paymentstatus"]?.ToString() ?? "PendingInvoice";
                    EPaymentStatus status;

                    if (!Enum.TryParse(statusText, true, out status))
                        status = EPaymentStatus.PendingInvoice;

                    invoice = new InvoiceInfo
                    {
                        po_id = poNumber,
                        PaymentStatus = status,
                        SupportDocuments = new List<ImageUploadInfo>()
                    };
                    var image = new ImageUploadInfo
                    {
                        Data = reader["imagebyte"] as byte[] ?? Array.Empty<byte>(),
                        DataFormat = reader["photoformat"]?.ToString() ?? string.Empty
                    };

                    invoice.SupportDocuments.Add(image);
                }

                return invoice;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetInvoiceInfo failed for PO {poNumber}: {ex.Message}");
                return null;
            }
        }

        public async Task<ReceiveInfo> GetReceiveInfo(string poNumber)
        {
            ReceiveInfo? receive = new ReceiveInfo();

            await using var connection = GetConnection();
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                "SELECT imagebyte, photoformat, receive_date " +
                "FROM receive WHERE po_id = @req",
                connection);
            command.Parameters.AddWithValue("@req", poNumber);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (receive == null)
                {
                    receive = new ReceiveInfo
                    {
                        po_id = poNumber,
                        ReceiveDate = reader["receive_date"] == DBNull.Value
                            ? DateTime.Now
                            : (DateTime)reader["receive_date"],
                        SupportDocuments = new List<ImageUploadInfo>()
                    };
                }

                var image = new ImageUploadInfo
                {
                    Data = reader["imagebyte"] as byte[] ?? Array.Empty<byte>(),
                    DataFormat = reader["photoformat"]?.ToString() ?? string.Empty
                };
                receive.SupportDocuments.Add(image);
            }

            return receive;
        }


        private async Task<bool> InsertInvoiceInfo(string poId, string prId, InvoiceInfo info, NpgsqlTransaction? externalTransaction = null)
        {
            bool shouldCloseConnection = false;
            bool shouldDisposeTransaction = false;

            try
            {
                if (Connection.State != System.Data.ConnectionState.Open)
                {
                    await Connection.OpenAsync();
                    shouldCloseConnection = true;
                }

                var transaction = externalTransaction ?? await Connection.BeginTransactionAsync();
                if (externalTransaction == null)
                    shouldDisposeTransaction = true;

                // 1️⃣ Delete all existing invoice images for this PO
                using (var deleteCmd = new NpgsqlCommand(
                    @"DELETE FROM po_invoice_image_table 
              WHERE requisitionnumber = @requisitionnumber;",
                    Connection, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@requisitionnumber", poId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // 2️⃣ Insert all new invoice image records
                foreach (var single in info.SupportDocuments)
                {
                    using (var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO po_invoice_image_table 
                    (requisitionnumber, imagebyte, photoformat, paymentstatus)
                  VALUES (@requisitionnumber, @imagebyte, @photoformat, @paymentstatus);",
                        Connection, transaction))
                    {
                        insertCmd.Parameters.AddWithValue("@requisitionnumber", poId);
                        insertCmd.Parameters.AddWithValue("@imagebyte", single.Data ?? Array.Empty<byte>());
                        insertCmd.Parameters.AddWithValue("@photoformat", single.DataFormat ?? string.Empty);
                        insertCmd.Parameters.AddWithValue("@paymentstatus", info.PaymentStatus.ToString());

                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                // 3️⃣ Update PR payment status
                var prRepository = new PRRepository();
                await prRepository.UpdatePaymentStatus(prId, info.PaymentStatus);

                if (shouldDisposeTransaction)
                    await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                if (shouldDisposeTransaction)
                {
                    try
                    {
                        await externalTransaction.RollbackAsync();
                    }
                    catch { /* ignore rollback errors */ }
                }

                Console.WriteLine($"InsertInvoiceInfo (delete-insert) failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (shouldCloseConnection)
                    await Connection.CloseAsync();
            }
        }

        public async Task<bool> UpdatePaymentStatus(string RequisitionNumber,EPaymentStatus Status)
        {
            try
            {
                List<PurchaseOrderRecord> OrderRecords=await GetRecordsAsync(new List<string> { RequisitionNumber });
                if (OrderRecords.Count > 0)
                {
                    PurchaseOrderRecord OrderRecord = OrderRecords[0];
                    return await UpdatePaymentStatus(OrderRecord.PR_ID, OrderRecord.PO_ID, Status);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateDeliveryDateAsync Exception: {ex.Message}");
                return false;
            }
            finally
            {
                await Connection.CloseAsync();
            }
            return false;
        }

        public async Task<bool> UpdatePaymentStatus(string PR_ID,string PO_ID,EPaymentStatus Status)
        {
            try
            {
                await Connection.OpenAsync();

                string query = @"
            UPDATE po_invoice_image_table
            SET paymentstatus = @paymentstatus
            WHERE requisitionnumber = @requisitionnumber;";

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("@paymentstatus", Status.ToString());
                    command.Parameters.AddWithValue("@requisitionnumber", PO_ID);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    var prRepository = new PRRepository();
                    await prRepository.UpdatePaymentStatus(PR_ID, Status);
                    return rowsAffected > 0; // true if updated
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateDeliveryDateAsync Exception: {ex.Message}");
                return false;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        private async Task<bool> InsertImage(PurchaseOrderRecord info, NpgsqlTransaction? externalTransaction = null)
        {
            bool shouldCloseConnection = false;
            bool shouldDisposeTransaction = false;

            try
            {
                if (Connection.State != System.Data.ConnectionState.Open)
                {
                    await Connection.OpenAsync();
                    shouldCloseConnection = true;
                }

                var transaction = externalTransaction ?? await Connection.BeginTransactionAsync();
                if (externalTransaction == null)
                    shouldDisposeTransaction = true;

                // 1️⃣ Delete old records for this PO_ID
                var deleteCmd = new NpgsqlCommand(@"
            DELETE FROM receive 
            WHERE po_id = @po_id;",
                    Connection, transaction);

                deleteCmd.Parameters.AddWithValue("@po_id", info.PO_ID);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2️⃣ Insert new records
                foreach (var single in info.ReceiveInfo.SupportDocuments)
                {
                    var insertCmd = new NpgsqlCommand(@"
                INSERT INTO receive (po_id, imagebyte, photoformat, receive_date)
                VALUES (@po_id, @imagebyte, @photoformat, @receive_date);",
                        Connection, transaction);

                    insertCmd.Parameters.AddWithValue("@po_id", info.PO_ID);
                    insertCmd.Parameters.AddWithValue("@imagebyte", single.Data ?? Array.Empty<byte>());
                    insertCmd.Parameters.AddWithValue("@photoformat", single.DataFormat ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("@receive_date", info.ReceiveInfo.ReceiveDate);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                if (shouldDisposeTransaction)
                    await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                if (shouldDisposeTransaction)
                {
                    try
                    {
                        await externalTransaction.RollbackAsync();
                    }
                    catch { /* suppress rollback error */ }
                }

                Console.WriteLine($"InsertImage (delete-insert) failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (shouldCloseConnection)
                    await Connection.CloseAsync();
            }
        }
        private void InsertInfoOfBasicInfo<T>(T MainInfo, NpgsqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();
            try
            {
                foreach (var property in properties)
                {
                    if (property.Name == "ApprovalInfo"|| property.Name == "ReceiveInfo"|| property.Name == "InvoiceInfo")
                        continue;
                    

                    object obj = property.GetValue(MainInfo);
                    int RowNum = reader.GetOrdinal(property.Name);
                    if (RowNum >= 0)
                    {
                        if (obj is EPOStatus)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out EPOStatus Type);
                            property.SetValue(MainInfo, Type);
                        }

                        else if (obj is EDepartment)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out EDepartment Type);
                            property.SetValue(MainInfo, Type);
                        }
                        else if (obj is ETask)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out ETask Type);
                            property.SetValue(MainInfo, Type);
                        }
                        else if (obj is EPRStatus)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out EPRStatus Type);
                            property.SetValue(MainInfo, Type);
                        }
                        else if (obj is DateTime)
                        {
                            DateTime.TryParse(reader[property.Name].ToString(), out DateTime Date);
                            property.SetValue(MainInfo, Date);
                        }
                        else if (obj is decimal)
                        {
                            decimal.TryParse(reader[property.Name].ToString(), out decimal d);
                            property.SetValue(MainInfo, d);
                        }
                        else if (obj is int)
                        {
                            int.TryParse(reader[property.Name].ToString(), out int d);
                            property.SetValue(MainInfo, d);
                        }
                        else if (obj is bool)
                        {
                            bool.TryParse(reader[property.Name].ToString(), out bool d);
                            property.SetValue(MainInfo, d);
                        }
                        else
                        {
                            property.SetValue(MainInfo, reader[property.Name].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertInfoOfBasicInfo: " + ex.Message);
            }
        }

        public async Task<POSubmitResponse> SubmitAsync(IEnumerable<PurchaseOrderRecord> InfoList)
        {
            POSubmitResponse MySubmitResponse = new POSubmitResponse();
            try
            {
                await Connection.OpenAsync();
                await using (var transaction = await Connection.BeginTransactionAsync())
                {
                    long lastSequenceValue = 0;

                    await using (var command = new NpgsqlCommand())
                    {
                        command.Connection = Connection;
                        command.Transaction = transaction;

                        using (var infoEnumerator = InfoList.GetEnumerator())
                        {
                            while (infoEnumerator.MoveNext())
                            {

                                var Info = infoEnumerator.Current;
                                string SID = "";
                                if (string.IsNullOrEmpty(Info.PO_ID))
                                {
                                    await using (var Seqcommand = new NpgsqlCommand("SELECT last_value FROM po_table_id_seq;", Connection, transaction))
                                    {
                                        var result = await Seqcommand.ExecuteScalarAsync();
                                        lastSequenceValue = (long)result;
                                        SID = $"PO_{lastSequenceValue + 1}";
                                    }

                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    SID =Info.PO_ID;
                                }

                                string sqlCommand = "INSERT INTO potable (";
                                string sqlValues = "VALUES (";
                                string sqlUpdate = "ON CONFLICT (PO_ID) DO UPDATE SET ";

                                var props = typeof(PurchaseOrderRecord).GetProperties();
                                foreach (var prop in props)
                                {
                                    string propName = prop.Name;
                                    if (propName == "PO_ID"|| propName == "ApprovalInfo" || propName == "ReceiveInfo" || propName == "InvoiceInfo"|| propName == "_Approvals" || propName == "approvalstatus" || propName == "Rejectreason")
                                        continue;

                                    sqlCommand += propName + ",";
                                    sqlValues += "@" + propName + ",";
                                    sqlUpdate += propName + " = EXCLUDED." + propName + ",";

                                    object obj = prop.GetValue(Info);

                                    if (obj is DateTime)
                                    {
                                        DateTime.TryParse(obj.ToString(), out DateTime date);
                                        command.Parameters.AddWithValue("@" + propName, date);
                                    }

                                    if (obj is decimal)
                                    {
                                        decimal.TryParse(obj.ToString(), out decimal date);
                                        command.Parameters.AddWithValue("@" + propName, date);
                                    }
                                    if (obj is EPOStatus)
                                    { 
                                        command.Parameters.AddWithValue("@" + propName, obj.ToString());
                                    }

                                    else
                                    {
                                        command.Parameters.AddWithValue("@" + propName, obj ?? DBNull.Value);
                                    }
                                }
                

                                sqlCommand += "PO_ID)";
                                sqlValues += "@PO_ID)";
                                sqlUpdate += "PO_ID = EXCLUDED.PO_ID";

                                command.Parameters.AddWithValue("@PO_ID", SID);
                                command.CommandText = sqlCommand + " " + sqlValues + " " + sqlUpdate;

                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    MySubmitResponse.bSuccess = true;
                                    MySubmitResponse.IDs.Add(SID);
                                }
                                Info.PO_ID= SID;
                                await InsertImage(Info, transaction);
                                await InsertInvoiceInfo(Info.PO_ID, Info.PR_ID, Info.InvoiceInfo,transaction);
                                var prRepository = new PRRepository();
                                await prRepository.UpdatePOID(Info.PR_ID, Info.PO_ID);
                            }
                        }
                    }

                    if (MySubmitResponse.bSuccess)
                        await transaction.CommitAsync();

                    else
                        await transaction.RollbackAsync();
                }

                await Connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitAsync Exception: {ex.Message}");
                return MySubmitResponse;
            }

            return MySubmitResponse;
        }

    }
}
