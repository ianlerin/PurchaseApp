using Microsoft.AspNetCore.Identity;
using Npgsql;
using Npgsql.Internal;
using PurchaseBlazorApp2.Components.Data;
using Radzen.Blazor.Markdown;
using ServiceStack;
using System.Data;
using System.Data.Common;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class PRRepository
    {
        private NpgsqlConnection Connection;
        public PRRepository()
        {
            Connection = GetConnection();
        }

        private NpgsqlConnection GetConnection()
        {
            // return new NpgsqlConnection($"Server=einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com;Port=5432; User Id=postgres; Password=password; Database=purchase");
            return new NpgsqlConnection($"Server=localhost;Port=5432; User Id=postgres; Password=password; Database=purchase");
        }

        private void InsertInfoOfBasicInfo<T>(T MainInfo, NpgsqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();
            try
            {
                foreach (var property in properties)
                {
                    if (property.Name == "SupportDocuments"|| property.Name == "ItemRequested"|| property.Name == "Approvals")
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

        public async Task<List<PurchaseRequisitionRecord>> GetRecordsForListAsync(List<string> requisitionNumbers = null)
        {
            List<PurchaseRequisitionRecord> ToReturn = new List<PurchaseRequisitionRecord>();
            try
            {
                await Connection.OpenAsync();

                string query = "SELECT requisitionnumber,requestor,prstatus,purpose FROM prtable";

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
                    query += $" WHERE requisitionnumber IN ({inClause})";
                }

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        PurchaseRequisitionRecord MainInfo = new PurchaseRequisitionRecord();
                        MainInfo.RequisitionNumber = reader["requisitionnumber"]?.ToString() ?? string.Empty;
                        MainInfo.Requestor = reader["requestor"]?.ToString() ?? string.Empty;
                        EPRStatus PRStatus;
                        Enum.TryParse(reader["prstatus"]?.ToString() ?? string.Empty, out PRStatus);
                        MainInfo.prstatus = PRStatus;
                        MainInfo.Purpose = reader["purpose"]?.ToString() ?? string.Empty;
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


        public async Task<List<PurchaseRequisitionRecord>> GetRecordsAsync(List<string> requisitionNumbers = null)
        {
            List<PurchaseRequisitionRecord> ToReturn = new List<PurchaseRequisitionRecord>();

            try
            {
                await Connection.OpenAsync();

                string query = "SELECT * FROM prtable";

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
                    query += $" WHERE requisitionnumber IN ({inClause})";
                }

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        PurchaseRequisitionRecord MainInfo = new PurchaseRequisitionRecord();
                        InsertInfoOfBasicInfo(MainInfo, reader);
                        ToReturn.Add(MainInfo);
                    }
                }
                var tasks = ToReturn.Select(async record =>
                {
                    var docsTask = GetImagesByRequisitionNumber(record.RequisitionNumber);
                    var approvalsTask = InsertApprovalByRequisitionNumber(record);
                    var itemsTask = GetRequestedItemByRequisitionNumber(record.RequisitionNumber);

                    await Task.WhenAll(docsTask, approvalsTask, itemsTask);

                    record.SupportDocuments = docsTask.Result;
                    record.Approvals = approvalsTask.Result;
                    record.ItemRequested = itemsTask.Result;
                });

                await Task.WhenAll(tasks);
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

        private async Task<List<ImageUploadInfo>> GetImagesByRequisitionNumber(string requisitionNumber, NpgsqlTransaction? externalTransaction = null)
        {
            var images = new List<ImageUploadInfo>();
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();
            try
            {
                await MyConnection.OpenAsync();
                var command = new NpgsqlCommand(
                    "SELECT imagebyte, photoformat FROM pr_image_table WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);
                command.Parameters.AddWithValue("@req", requisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var image = new ImageUploadInfo
                    {
                        Data = reader["imagebyte"] as byte[] ?? Array.Empty<byte>(),
                        DataFormat = reader["photoformat"]?.ToString() ?? string.Empty
                    };
                    images.Add(image);
                }

                return images;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetImagesByRequisitionNumber failed: {ex.Message}");
                return new List<ImageUploadInfo>();
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }
        }

        private async Task<List<RequestItemInfo>> GetRequestedItemByRequisitionNumber(string requisitionNumber, NpgsqlTransaction? externalTransaction = null)
        {
            var Items = new List<RequestItemInfo>();
            var MyConnection = GetConnection();
            try
            {
                await MyConnection.OpenAsync();

                var command = new NpgsqlCommand(
                    "SELECT itemrequested, unitprice,quantity,totalprice FROM pr_requestitem_table WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);
                command.Parameters.AddWithValue("@req", requisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    RequestItemInfo ItemInfo = new RequestItemInfo();
                    ItemInfo.RequestItem = reader["itemrequested"]?.ToString() ?? string.Empty;
                    ItemInfo.UnitPrice = reader["unitprice"] != DBNull.Value ? Convert.ToDecimal(reader["unitprice"]) : 0m;
                    ItemInfo.Quantity = reader["quantity"] != DBNull.Value ? Convert.ToDecimal(reader["quantity"]) : 0m;
                    ItemInfo.TotalPrice = reader["totalprice"] != DBNull.Value ? Convert.ToDecimal(reader["totalprice"]) : 0m;
                    Items.Add(ItemInfo);
                }

                return Items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetImagesByRequisitionNumber failed: {ex.Message}");
                return new List<RequestItemInfo>();
            }
            finally
            {
                await MyConnection.CloseAsync();

            }
        }



        public async Task<List<ApprovalInfo>> InsertApprovalByRequisitionNumber(PurchaseRequisitionRecord Record, NpgsqlTransaction? externalTransaction = null)
        {
            var RecordsFound = Record.Approvals;
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();
            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                var command = new NpgsqlCommand(
                    "SELECT username, isapproved,role FROM pr_approval_table WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);
                command.Parameters.AddWithValue("@req", Record.RequisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string RoleString = reader["role"]?.ToString() ?? string.Empty;
                    List<EDepartment> Departments = RoleString
     .Split(',', StringSplitOptions.RemoveEmptyEntries)
     .Select(role => Enum.Parse<EDepartment>(role))
     .ToList();
                    bool IsApproved = (bool)reader["isapproved"];
                    string username = reader["username"]?.ToString() ?? string.Empty;
                    foreach(var Approval in RecordsFound)
                    {
                        if (Approval.Departments.Count == Departments.Count &&
        !Approval.Departments.Except(Departments).Any() &&
        !Departments.Except(Approval.Departments).Any())
                        {
                            Approval.IsApproved = IsApproved;
                            Approval.UserName = username;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InsertApprovalByRequisitionNumber failed: {ex.Message}");
             
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }
            return RecordsFound;
        }

        private async Task<bool> InsertImage(PurchaseRequisitionRecord info, NpgsqlTransaction? externalTransaction = null)
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
                    "DELETE FROM pr_image_table WHERE requisitionnumber = @req",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new records
                foreach (ImageUploadInfo single in info.SupportDocuments)
                {
                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO pr_image_table (requisitionnumber, imagebyte, photoformat) VALUES (@req, @doc, @format)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
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


        private async Task<bool> InsertPRItems(PurchaseRequisitionRecord info, NpgsqlTransaction? externalTransaction = null)
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
                    "DELETE FROM pr_requestitem_table WHERE requisitionnumber = @req",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new records
                foreach (RequestItemInfo item in info.ItemRequested)
                {
                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO pr_requestitem_table (requisitionnumber, itemrequested,unitprice,quantity,totalprice) VALUES (@req, @itemrequested,@unitprice,@quantity,@totalprice)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                    insertCmd.Parameters.AddWithValue("@itemrequested", item.RequestItem);
                    insertCmd.Parameters.AddWithValue("@unitprice", item.UnitPrice);
                    insertCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                    insertCmd.Parameters.AddWithValue("@totalprice", item.TotalPrice);
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



        private async Task<bool> InsertApproval(PurchaseRequisitionRecord info, NpgsqlTransaction? externalTransaction = null)
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
                    "DELETE FROM pr_approval_table WHERE requisitionnumber = @req",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                await deleteCmd.ExecuteNonQueryAsync();



                // 2. Insert new records
                foreach (ApprovalInfo single in info.Approvals)
                {
                    string result = string.Join(",", single.Departments);
                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO pr_approval_table (requisitionnumber, username, isapproved,role) VALUES (@req, @username, @isapproved,@role)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                    insertCmd.Parameters.AddWithValue("@username", single.UserName);
                    insertCmd.Parameters.AddWithValue("@isapproved", single.IsApproved);
                    string Joined=string.Join(',', single.Departments);
                    insertCmd.Parameters.AddWithValue("@role", Joined);
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


        public async Task<bool> SubmitAsync(IEnumerable<PurchaseRequisitionRecord> InfoList)
        {
            bool bSuccess = false;
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
                                if (string.IsNullOrEmpty(Info.RequisitionNumber))
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
                                    SID = Info.RequisitionNumber;
                                }


                                string sqlCommand = "INSERT INTO prtable (";
                                string sqlValues = "VALUES (";
                                string sqlUpdate = "ON CONFLICT (requisitionnumber) DO UPDATE SET ";

                                var props = typeof(PurchaseRequisitionRecord).GetProperties();
                                foreach (var prop in props)
                                {
                                    string propName = prop.Name;

                                    if (propName == "RequisitionNumber" || propName == "SupportDocuments"|| propName == "Approvals"|| propName == "ItemRequested")
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
                                    else if (obj is EDepartment|| obj is ETask||obj is EPRStatus)
                                    {
                                        command.Parameters.AddWithValue("@" + propName, obj.ToString());
                                    }
                                   
                                    else
                                    {
                                        command.Parameters.AddWithValue("@" + propName, obj ?? DBNull.Value);
                                    }
                                }

                                sqlCommand += "requisitionnumber)";
                                sqlValues += "@requisitionnumber)";
                                sqlUpdate += "requisitionnumber = EXCLUDED.requisitionnumber";

                                command.Parameters.AddWithValue("@requisitionnumber", SID);
                                command.CommandText = sqlCommand + " " + sqlValues + " " + sqlUpdate;

                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    bSuccess = true;
                                }
                                Info.RequisitionNumber = SID;
                                bSuccess = await InsertImage(Info,transaction);
                                if (bSuccess)
                                {
                                    bSuccess=await InsertApproval(Info,transaction);
                                    bSuccess=await InsertPRItems(Info,transaction);
                                }

                            }
                        }
                    }

                    if (bSuccess)
                        await transaction.CommitAsync();
                    else
                        await transaction.RollbackAsync();
                }

                await Connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitAsync Exception: {ex.Message}");
                return false;
            }

            return bSuccess;
        }

    }
}

