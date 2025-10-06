using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;
using ServiceStack;
using System.Data;



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
            return new NpgsqlConnection($"Server=localhost;Port=5432; User Id=postgres; Password=password; Database=purchase");
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

                var transaction = externalTransaction;
                if (transaction == null)
                {
                    transaction = await Connection.BeginTransactionAsync();
                    shouldDisposeTransaction = true;
                }

                // 1. Delete existing record
                var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM receive WHERE po_id = @po_id",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@po_id", info.PO_ID);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new records
                foreach (ImageUploadInfo single in info.ReceiveInfo.SupportDocuments)
                {
                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO receive (po_id, imagebyte, photoformat,receive_date) VALUES (@req, @doc, @format,@date)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.PO_ID);
                    insertCmd.Parameters.AddWithValue("@date", info.ReceiveInfo.ReceiveDate);
                    insertCmd.Parameters.AddWithValue("@doc", single.Data ?? Array.Empty<byte>());
                    insertCmd.Parameters.AddWithValue("@format", single.DataFormat ?? string.Empty);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                if (shouldDisposeTransaction)
                    await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                if (externalTransaction == null)
                {
                    try
                    {
                        await Connection?.BeginTransaction()?.RollbackAsync();
                    }
                    catch { /* suppress rollback error */ }
                }

                Console.WriteLine($"InsertImage failed: {ex.Message}");
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
                    if (property.Name == "ApprovalInfo"|| property.Name == "ReceiveInfo")
                        continue;
                    

                    object obj = property.GetValue(MainInfo);
                    int RowNum = reader.GetOrdinal(property.Name);
                    if (RowNum >= 0)
                    {
                        if (obj is EDepartment)
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

                                    if (propName == "PO_ID"|| propName == "ApprovalInfo" || propName == "ReceiveInfo")
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
