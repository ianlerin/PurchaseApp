using Microsoft.AspNetCore.Identity;
using Microsoft.Graph.Models;
using Npgsql;
using Npgsql.Internal;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Resource;
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
            return new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432; User Id=postgres; Password=password; Database=purchase");
        }

        private void InsertInfoOfBasicInfo<T>(T MainInfo, NpgsqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();
            try
            {
                foreach (var property in properties)
                {
                    if (property.Name == "ApprovedItemRequested" || property.Name == "SupportDocuments"|| property.Name == "ItemRequested"|| property.Name == "Approvals")
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

                        else if (obj is EApprovalStatus)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out EApprovalStatus Type);
                            property.SetValue(MainInfo, Type);
                        }

                        else if (obj is EPaymentStatus)
                        {
                            Enum.TryParse(reader[property.Name].ToString(), out EPaymentStatus Type);
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
            var ToReturn = new List<PurchaseRequisitionRecord>();

            try
            {
                await Connection.OpenAsync();

                string query = @"
            SELECT requisitionnumber, requestdate, prstatus, approvalstatus, burgent, deliverydate, paymentstatus, po_id 
            FROM prtable
            WHERE prstatus <> 'Cancel'
              AND paymentstatus <> 'Paid'";

                using (var command = new NpgsqlCommand { Connection = Connection })
                {
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
                        query += $" AND requisitionnumber IN ({inClause})"; // Use AND instead of WHERE
                    }

                    command.CommandText = query;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var MainInfo = new PurchaseRequisitionRecord
                            {
                                RequisitionNumber = reader["requisitionnumber"]?.ToString() ?? string.Empty,
                                RequestDate = reader["requestdate"] != DBNull.Value ? (DateTime)reader["requestdate"] : default,
                                DeliveryDate = reader["deliverydate"] != DBNull.Value ? (DateTime)reader["deliverydate"] : default,
                                burgent = reader["burgent"] != DBNull.Value && (bool)reader["burgent"],
                                po_id = reader["po_id"]?.ToString() ?? string.Empty
                            };

                            if (Enum.TryParse(reader["prstatus"]?.ToString() ?? string.Empty, out EPRStatus prStatus))
                                MainInfo.prstatus = prStatus;

                            if (Enum.TryParse(reader["approvalstatus"]?.ToString() ?? string.Empty, out EApprovalStatus approvalStatus))
                                MainInfo.approvalstatus = approvalStatus;

                            if (Enum.TryParse(reader["paymentstatus"]?.ToString() ?? string.Empty, out EPaymentStatus paymentStatus))
                                MainInfo.paymentstatus = paymentStatus;

                            MainInfo.ItemRequested = await GetRequestedItemByRequisitionNumber(MainInfo.RequisitionNumber, "pr_requestitem_table");

                            ToReturn.Add(MainInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecordsForListAsync Exception: {ex.Message}");
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return ToReturn;
        }

        public async Task<List<PurchaseRequisitionRecord>> GetAllRecordsForListAsync()
        {
            var ToReturn = new List<PurchaseRequisitionRecord>();

            try
            {
                await Connection.OpenAsync();

                string query = @"
            SELECT requisitionnumber, requestdate, prstatus, approvalstatus, burgent, deliverydate, paymentstatus, po_id 
            FROM prtable
            WHERE prstatus <> 'Cancel'
              AND paymentstatus <> 'Paid'";

                var command = new NpgsqlCommand { Connection = Connection };

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var MainInfo = new PurchaseRequisitionRecord();

                        MainInfo.RequisitionNumber = reader["requisitionnumber"]?.ToString() ?? string.Empty;

                        // requestdate
                        if (reader["requestdate"] != DBNull.Value)
                            MainInfo.RequestDate = (DateTime)reader["requestdate"];

                        if (reader["deliverydate"] != DBNull.Value)
                            MainInfo.DeliveryDate = (DateTime)reader["deliverydate"];

                        // prstatus enum
                        if (Enum.TryParse(reader["prstatus"]?.ToString() ?? string.Empty, out EPRStatus prStatus))
                            MainInfo.prstatus = prStatus;

                        // approvalstatus enum
                        if (Enum.TryParse(reader["approvalstatus"]?.ToString() ?? string.Empty, out EApprovalStatus approvalStatus))
                            MainInfo.approvalstatus = approvalStatus;

                        if (Enum.TryParse(reader["paymentstatus"]?.ToString() ?? string.Empty, out EPaymentStatus paymentStatus))
                            MainInfo.paymentstatus = paymentStatus;


                        // burgent bool
                        MainInfo.burgent = reader["burgent"] != DBNull.Value && (bool)reader["burgent"];
                        MainInfo.po_id = reader["po_id"]?.ToString() ?? string.Empty;
                        MainInfo.ItemRequested = await GetRequestedItemByRequisitionNumber(MainInfo.RequisitionNumber, "pr_requestitem_table");
                        ToReturn.Add(MainInfo);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecordsForListAsync Exception: {ex.Message}");
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
            if(requisitionNumbers.Count==0)
            {
                return ToReturn;
            }
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
                    var itemsTask = GetRequestedItemByRequisitionNumber(record.RequisitionNumber, "pr_requestitem_table");
                    var approvedItemsTask = GetRequestedItemByRequisitionNumber(record.RequisitionNumber, "pr_approved_requestitem_table");
                    await Task.WhenAll(docsTask, approvalsTask, itemsTask);

                    record.SupportDocuments = docsTask.Result;
                    record.Approvals = approvalsTask.Result;
                    record.ItemRequested = itemsTask.Result;
                    record.ApprovedItemRequested = approvedItemsTask.Result;
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

        public async Task<List<RequestItemInfo>> GetRequestedItemByRequisitionNumber(string requisitionNumber,string requesttablename, NpgsqlTransaction? externalTransaction = null)
        {
            var Items = new List<RequestItemInfo>();
            var MyConnection = GetConnection();
            try
            {
                await MyConnection.OpenAsync();

                var command = new NpgsqlCommand(
                    $"SELECT itemrequested, unitprice,quantity,totalprice,currency FROM {requesttablename} WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);
                command.Parameters.AddWithValue("@req", requisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    RequestItemInfo ItemInfo = new RequestItemInfo();
                    ItemInfo.RequestItem = reader["itemrequested"]?.ToString() ?? string.Empty;
                    ItemInfo.Currency = reader["currency"]?.ToString() ?? string.Empty;
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


        public async Task<HashSet<string>> GetRequisitionNumbersByCreatedByAsync(string requestor)
        {
            var requisitionNumbers = new HashSet<string>();
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();

            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                using var command = new NpgsqlCommand(
                    @"SELECT requisitionnumber
              FROM prtable
              WHERE requestor = @requestor",
                    MyConnection);

                command.Parameters.AddWithValue("@requestor", requestor);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string reqNumber = reader["requisitionnumber"]?.ToString() ?? string.Empty;
                    requisitionNumbers.Add(reqNumber);
                  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRequisitionNumbersByDepartmentAsync failed: {ex.Message}");
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }

            return requisitionNumbers;
        }



        public async Task<HashSet<string>> GetRequisitionNumbersByDepartmentAsync(EDepartment department)
        {
            var requisitionNumbers = new HashSet<string>();
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();

            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                using var command = new NpgsqlCommand(
                    @"SELECT requisitionnumber, role
              FROM pr_approval_table
              WHERE approvestatus = @status",
                    MyConnection);

                command.Parameters.AddWithValue("@status", ESingleApprovalStatus.PendingAction.ToString());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string roleString = reader["role"]?.ToString() ?? string.Empty;

                    // Split roles and check if department matches
                    var departments = roleString
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(role => Enum.Parse<EDepartment>(role));

                    if (departments.Contains(department))
                    {
                        string reqNumber = reader["requisitionnumber"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(reqNumber))
                        {
                            requisitionNumbers.Add(reqNumber); // HashSet auto-ignores duplicates
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRequisitionNumbersByDepartmentAsync failed: {ex.Message}");
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }

            return requisitionNumbers;
        }

        public async Task<List<ApprovalInfo>> InsertApprovalByRequisitionNumber(
            PurchaseRequisitionRecord Record,
            NpgsqlTransaction? externalTransaction = null)
        {
            var Approvals = new List<ApprovalInfo>(); // Build a fresh list
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();

            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                using var command = new NpgsqlCommand(
                    "SELECT username, approvestatus, role FROM pr_approval_table WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);

                command.Parameters.AddWithValue("@req", Record.RequisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string RoleString = reader["role"]?.ToString() ?? string.Empty;

                    // Convert to List<EDepartment>
                    List<EDepartment> Departments = RoleString
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(role => Enum.Parse<EDepartment>(role))
                    .ToList();


                   ESingleApprovalStatus status = Enum.Parse<ESingleApprovalStatus>(reader["approvestatus"].ToString());
                    string username = reader["username"]?.ToString() ?? string.Empty;

                    // Create a new ApprovalInfo object and add to the list
                    var approvalInfo = new ApprovalInfo
                    {
                        Departments = Departments,
                        ApproveStatus = status,
                        UserName = username
                    };

                    Approvals.Add(approvalInfo);
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

            // Replace Record.Approvals entirely with new data
            Record.Approvals = Approvals;
            return Approvals;
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


        public async Task<bool> UpdatePOID(string requisitionNumber, string PO)
        {
            try
            {
                await Connection.OpenAsync();

                string query = @"
            UPDATE prtable
            SET po_id = @po_id
            WHERE requisitionnumber = @reqNo;";

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("@po_id", PO);
                    command.Parameters.AddWithValue("@reqNo", requisitionNumber);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
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
        public async Task<List<string>> GetRequisitionsFinance()
        {
            List<string> requisitionNumbers = new List<string>();

            try
            {
                await Connection.OpenAsync();

                string query = @"
            SELECT requisitionnumber
            FROM prtable
            WHERE paymentstatus = 'PendingPayment'
               OR paymentstatus = 'Paid';";

                using (var command = new NpgsqlCommand(query, Connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        requisitionNumbers.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRequisitionsWithPendingOrPaidAsync Exception: {ex.Message}");
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return requisitionNumbers;
        }

        public async Task<bool> UpdatePaymentStatus(string requisitionNumber, EPaymentStatus status)
        {
            try
            {
                await Connection.OpenAsync();

                string query = @"
            UPDATE prtable
            SET paymentstatus = @paymentstatus
            WHERE requisitionnumber = @reqNo;";

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("@paymentstatus", status.ToString());
                    command.Parameters.AddWithValue("@reqNo", requisitionNumber);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
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


        public async Task<bool> UpdateDeliveryDateAsync(string requisitionNumber, DateTime deliveryDate)
        {
            try
            {
                await Connection.OpenAsync();

                string query = @"
            UPDATE prtable
            SET deliverydate = @deliveryDate
            WHERE requisitionnumber = @reqNo;";

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("@deliveryDate", deliveryDate);
                    command.Parameters.AddWithValue("@reqNo", requisitionNumber);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
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

        private async Task<bool> InsertPRItems(PurchaseRequisitionRecord info, NpgsqlTransaction? externalTransaction = null)
        {
          
            bool bSuccess = await InsertPRItems(info.RequisitionNumber, info.ItemRequested, "pr_requestitem_table", externalTransaction);
            if(bSuccess)
            {
                 bSuccess = await InsertPRItems(info.RequisitionNumber, info.ApprovedItemRequested, "pr_approved_requestitem_table", externalTransaction);
            }
            
            return bSuccess;
        }

        private async Task<bool> InsertPRItems(string prID, List<RequestItemInfo> items,string requestTableName, NpgsqlTransaction? externalTransaction = null)
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
                    $"DELETE FROM {requestTableName} WHERE requisitionnumber = @req",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@req", prID);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new records
                foreach (RequestItemInfo item in items)
                {
                    var insertCmd = new NpgsqlCommand(
                        $"INSERT INTO {requestTableName} (requisitionnumber, itemrequested,unitprice,currency,quantity,totalprice) VALUES (@req, @itemrequested,@unitprice,@currency,@quantity,@totalprice)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", prID);
                    insertCmd.Parameters.AddWithValue("@itemrequested", item.RequestItem);
                    insertCmd.Parameters.AddWithValue("@currency", item.Currency);
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
                        "INSERT INTO pr_approval_table (requisitionnumber, username, approvestatus,role) VALUES (@req, @username, @approvestatus,@role)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.RequisitionNumber);
                    insertCmd.Parameters.AddWithValue("@username", single.UserName);
                    insertCmd.Parameters.AddWithValue("@approvestatus", single.ApproveStatus.ToString());
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

        public async Task<List<string>> GetPendingRemindersAsync(int daysAgo)
        {
            var results = new List<string>();

            try
            {
                await Connection.OpenAsync();

                string query = @"
            SELECT requisitionnumber
            FROM prtable
            WHERE prstatus = 'ApprovedRequests'
              AND updatedate < NOW() - @days * INTERVAL '1 day'
              AND bsentreminder = FALSE;";

                using (var command = new NpgsqlCommand(query, Connection))
                {
                    command.Parameters.AddWithValue("days", daysAgo);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPendingRemindersAsync Exception: {ex.Message}");
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return results;
        }


        public async Task<int> MarkRemindersAsSentAsync(List<string> requisitionNumbers)
        {
            if (requisitionNumbers == null || requisitionNumbers.Count == 0)
                return 0;

            int rowsAffected = 0;

            try
            {
                await Connection.OpenAsync();

                // Build parameterized IN clause
                var paramNames = new List<string>();
                var command = new NpgsqlCommand();
                command.Connection = Connection;

                for (int i = 0; i < requisitionNumbers.Count; i++)
                {
                    string paramName = $"@id{i}";
                    paramNames.Add(paramName);
                    command.Parameters.AddWithValue(paramName, requisitionNumbers[i]);
                }

                string inClause = string.Join(", ", paramNames);

                string query = $@"
            UPDATE prtable
            SET bsentreminder = TRUE
            WHERE requisitionnumber IN ({inClause});";

                command.CommandText = query;

                rowsAffected = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkRemindersAsSentAsync Exception: {ex.Message}");
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return rowsAffected;
        }

        public async Task<List<string>> SubmitAsync(IEnumerable<PurchaseRequisitionRecord> InfoList)
        {
            var submittedIds = new List<string>();

            try
            {
                await Connection.OpenAsync();
                await using (var transaction = await Connection.BeginTransactionAsync())
                {
                    await using (var command = new NpgsqlCommand())
                    {
                        command.Connection = Connection;
                        command.Transaction = transaction;

                        using (var infoEnumerator = InfoList.GetEnumerator())
                        {
                            while (infoEnumerator.MoveNext())
                            {
                                var Info = infoEnumerator.Current;
                                string SID;

                                // Generate new RequisitionNumber if missing
                                if (string.IsNullOrEmpty(Info.RequisitionNumber))
                                {
                                    await using (var Seqcommand = new NpgsqlCommand("SELECT last_value FROM prtable_id_seq;", Connection, transaction))
                                    {
                                        var result = await Seqcommand.ExecuteScalarAsync();
                                        long lastSequenceValue = (long)result;
                                        SID = $"PR_{lastSequenceValue + 1}";
                                    }
                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    SID = Info.RequisitionNumber;
                                }

                                // Build INSERT ... ON CONFLICT SQL
                                string sqlCommand = "INSERT INTO prtable (";
                                string sqlValues = "VALUES (";
                                string sqlUpdate = "ON CONFLICT (requisitionnumber) DO UPDATE SET ";

                                var props = typeof(PurchaseRequisitionRecord).GetProperties();
                                foreach (var prop in props)
                                {
                                    string propName = prop.Name;
                                    if (propName == "RequisitionNumber" || propName == "SupportDocuments" || propName == "Approvals" || propName == "ItemRequested" || propName == "ApprovedItemRequested")
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
                                    else if (obj is EDepartment || obj is ETask || obj is EPRStatus || obj is EPaymentStatus || obj is EApprovalStatus)
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
                                Info.RequisitionNumber = SID;

                                // Insert related data
                                bool bSuccess = await InsertImage(Info, transaction);
                                if (bSuccess)
                                {
                                    bSuccess = await InsertApproval(Info, transaction);
                                    bSuccess = await InsertPRItems(Info, transaction);
                                }

                                if (!bSuccess)
                                {
                                    await transaction.RollbackAsync();
                                    return new List<string>(); // empty if failed
                                }

                                submittedIds.Add(SID);
                            }
                        }
                    }

                    await transaction.CommitAsync();
                }

                await Connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitAsync Exception: {ex.Message}");
                return new List<string>();
            }

            return submittedIds;
        }

    }
}

