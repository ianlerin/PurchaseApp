using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Resource;
using System.Transactions;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class FinanceRepository
    {
        private NpgsqlConnection Connection;
        public FinanceRepository()
        {
            Connection = new NpgsqlConnection($"Server={StaticResources.ConnectionId()};Port=5432; User Id=postgres; Password=password; Database=purchase");
        }

        public async Task<bool> Submit(FinanceRecord info)
        {
            bool shouldCloseConnection = false;

            try
            {
                if (Connection.State != System.Data.ConnectionState.Open)
                {
                    await Connection.OpenAsync();
                    shouldCloseConnection = true;
                }

                using var transaction = await Connection.BeginTransactionAsync();

                try
                {
                    // 1. Upsert into finance_item
                    var upsertCmd = new NpgsqlCommand(@"
                INSERT INTO finance (requisitionnumber, paymentstatus)
                VALUES (@req, @status)
                ON CONFLICT (requisitionnumber)
                DO UPDATE SET 
                    paymentstatus = EXCLUDED.paymentstatus;",
                        Connection, transaction);

                    upsertCmd.Parameters.AddWithValue("@req", info.PO_ID ?? string.Empty);
                    upsertCmd.Parameters.AddWithValue("@status", info.PaymentStatus.ToString());
                    await upsertCmd.ExecuteNonQueryAsync();

                    // 2. Replace image records (delete old then insert new)
                    // You could also make pr_image_table upsertable if it has a unique key.
                    var imageResult = await InsertImage(info, transaction);
                    if (!imageResult)
                        throw new Exception("InsertImage failed — rolling back transaction.");

                    // 3. Commit transaction if all succeed
                    await transaction.CommitAsync();


                    PORepository PORepository = new PORepository();
                    await PORepository.UpdatePaymentStatus(info.PO_ID, info.PaymentStatus);
                    return true;
                }
                catch (Exception exInner)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception exOuter)
            {
                return false;
            }
            finally
            {
                if (shouldCloseConnection)
                    await Connection.CloseAsync();
            }
        }

        private async Task<bool> InsertImage(FinanceRecord info, NpgsqlTransaction? externalTransaction = null)
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

                // Loop through each finance update set
                foreach (var kvp in info.FinanceRecordLists)
                {
                    decimal updatePercent = kvp.Key;
                    FinanceRecordUpdate singleUpdate = kvp.Value;

                    // 1️⃣ Delete existing records for this requisition + updatepercent
                    using (var deleteCmd = new NpgsqlCommand(@"
                DELETE FROM finance_item 
                WHERE requisitionnumber = @req AND updatepercent = @updatepercent;",
                        Connection, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@req", info.PO_ID ?? string.Empty);
                        deleteCmd.Parameters.AddWithValue("@updatepercent", updatePercent);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // 2️⃣ Insert new records
                    if (singleUpdate.SupportDocuments.Count == 0)
                    {
                        // Insert an empty row
                        using (var insertCmd = new NpgsqlCommand(@"
        INSERT INTO finance_item 
            (requisitionnumber, imagebyte, adddate, updatepercent, photoformat)
        VALUES 
            (@req, @doc, @adddate, @updatepercent, @format);",
                            Connection, transaction))
                        {
                            insertCmd.Parameters.AddWithValue("@req", info.PO_ID ?? string.Empty);
                            insertCmd.Parameters.AddWithValue("@doc", Array.Empty<byte>());
                            insertCmd.Parameters.AddWithValue("@adddate", singleUpdate.AddDate);
                            insertCmd.Parameters.AddWithValue("@updatepercent", updatePercent);
                            insertCmd.Parameters.AddWithValue("@format", string.Empty);

                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        foreach (ImageUploadInfo single in singleUpdate.SupportDocuments)
                        {
                            using (var insertCmd = new NpgsqlCommand(@"
            INSERT INTO finance_item 
                (requisitionnumber, imagebyte, adddate, updatepercent, photoformat)
            VALUES 
                (@req, @doc, @adddate, @updatepercent, @format);",
                                Connection, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@req", info.PO_ID ?? string.Empty);
                                insertCmd.Parameters.AddWithValue("@doc", single.Data ?? Array.Empty<byte>());
                                insertCmd.Parameters.AddWithValue("@adddate", singleUpdate.AddDate);
                                insertCmd.Parameters.AddWithValue("@updatepercent", updatePercent);
                                insertCmd.Parameters.AddWithValue("@format", single.DataFormat ?? string.Empty);

                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
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
                    catch { /* ignore rollback errors */ }
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

        public async Task<FinanceRecord?> GetFinanceRecordByRequisitionNumber(string requisitionNumber)
        {
            bool shouldCloseConnection = false;
            var MyConnection = Connection;

            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                string query = @"
            SELECT paymentstatus 
            FROM finance
            WHERE requisitionnumber = @req
            LIMIT 1;";

                var command = new NpgsqlCommand(query, MyConnection);
                command.Parameters.AddWithValue("@req", requisitionNumber ?? string.Empty);

                string? paymentStatusText = null;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        paymentStatusText = reader["paymentstatus"]?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(paymentStatusText))
                {
                    Console.WriteLine($"Finance record not found for requisition: {requisitionNumber}");
                    return new FinanceRecord();
                }

                EPaymentStatus paymentStatus;
                if (!Enum.TryParse(paymentStatusText, ignoreCase: true, out paymentStatus))
                    paymentStatus = EPaymentStatus.PendingInvoice;

                var updates = await GetImagesByRequisitionNumber(requisitionNumber);

                var result = new FinanceRecord
                {
                    PO_ID = requisitionNumber,
                    PaymentStatus = paymentStatus,
                    FinanceRecordLists = updates
                };

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFinanceRecordByRequisitionNumber failed: {ex.Message}");
                return new FinanceRecord();
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }
        }

        private async Task<Dictionary<decimal, FinanceRecordUpdate>> GetImagesByRequisitionNumber(
     string requisitionNumber,
     NpgsqlTransaction? externalTransaction = null)
        {
            var result = new Dictionary<decimal, FinanceRecordUpdate>();
            bool shouldCloseConnection = false;
            var MyConnection = Connection;

            try
            {
                if (MyConnection.State != System.Data.ConnectionState.Open)
                {
                    await MyConnection.OpenAsync();
                    shouldCloseConnection = true;
                }

                var command = new NpgsqlCommand(
                    @"SELECT imagebyte, photoformat, adddate, updatepercent 
              FROM finance_item 
              WHERE requisitionnumber = @req",
                    MyConnection, externalTransaction);

                command.Parameters.AddWithValue("@req", requisitionNumber);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    decimal updatePercent = reader["updatepercent"] is decimal dec ? dec : 0m;
                    DateTime addDate = reader["adddate"] is DateTime dt ? dt : DateTime.Now;

                    byte[] bytes = reader["imagebyte"] as byte[] ?? Array.Empty<byte>();
                    string format = reader["photoformat"]?.ToString() ?? "";

                    // Always ensure this updatePercent exists
                    if (!result.TryGetValue(updatePercent, out var update))
                    {
                        update = new FinanceRecordUpdate
                        {
                            AddDate = addDate,
                            SupportDocuments = new List<ImageUploadInfo>()
                        };
                        result[updatePercent] = update;
                    }

                    // Only add image if real document exists
                    bool isPlaceholder = bytes.Length == 0 && format == "";
                    if (!isPlaceholder)
                    {
                        update.SupportDocuments.Add(new ImageUploadInfo
                        {
                            Data = bytes,
                            DataFormat = format
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetImagesByRequisitionNumber failed: {ex.Message}");
                return new Dictionary<decimal, FinanceRecordUpdate>();
            }
            finally
            {
                if (shouldCloseConnection)
                    await MyConnection.CloseAsync();
            }
        }

    }
}
