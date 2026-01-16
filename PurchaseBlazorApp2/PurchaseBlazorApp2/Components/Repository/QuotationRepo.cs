using Npgsql;
using PurchaseBlazorApp2.Client.Pages.Quotation;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Resource;
using static PurchaseBlazorApp2.Client.Pages.Quotation.QuotationInfo;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class QuotationRepo
    {
        private NpgsqlConnection Connection;
        public QuotationRepo()
        {
            Connection = GetConnection();
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection($"Server={StaticResources.ConnectionId()};Port=5432; User Id=postgres; Password=password; Database=purchase");
        }


        private async Task<bool> InsertImage(QuotationRecord info, NpgsqlTransaction? externalTransaction = null)
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
                    "DELETE FROM quotation_image_table WHERE quotation_id = @req",
                    Connection, transaction);
                deleteCmd.Parameters.AddWithValue("@req", info.quotation_id);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new records
                for(int i = 0; i < info.SupportDocuments.Count();i++)
                {
                    string itemid = $"{info.quotation_id}+{i}";
                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO quotation_image_table (quotation_id, item_id,imagebyte, photoformat) VALUES (@req,@itemid, @doc, @format)",
                        Connection, transaction);
                    insertCmd.Parameters.AddWithValue("@req", info.quotation_id);
                    insertCmd.Parameters.AddWithValue("@itemid", itemid);

                    
                    insertCmd.Parameters.AddWithValue("@doc", info.SupportDocuments[i].Data ?? Array.Empty<byte>());
                    insertCmd.Parameters.AddWithValue("@format", info.SupportDocuments[i].DataFormat ?? string.Empty);
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



        public async Task<bool> SubmitAsync(IEnumerable<QuotationRecord> InfoList)
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
                                QuotationRecord Info = infoEnumerator.Current;
                                string SID = "";
                                if (string.IsNullOrEmpty(Info.quotation_id))
                                {
                                    await using (var Seqcommand = new NpgsqlCommand("SELECT last_value FROM quotation_sequence;", Connection, transaction))
                                    {
                                        var result = await Seqcommand.ExecuteScalarAsync();
                                        lastSequenceValue = (long)result;
                                        SID = $"Quotation_{lastSequenceValue + 1}";
                                    }

                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    SID = Info.quotation_id;
                                }


                                string sqlCommand = "INSERT INTO quotation (";
                                string sqlValues = "VALUES (";
                                string sqlUpdate = "ON CONFLICT (quotation_id) DO UPDATE SET ";

                                var props = typeof(QuotationRecord).GetProperties();
                                foreach (var prop in props)
                                {
                                    string propName = prop.Name;

                                    if (propName == "quotation_id" || propName == "SupportDocuments")
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
                                    else if (obj is EDepartment || obj is ETask || obj is EPRStatus)
                                    {
                                        command.Parameters.AddWithValue("@" + propName, obj.ToString());
                                    }

                                    else
                                    {
                                        command.Parameters.AddWithValue("@" + propName, obj ?? DBNull.Value);
                                    }
                                }

                                sqlCommand += "quotation_id)";
                                sqlValues += "@quotation_id)";
                                sqlUpdate += "quotation_id = EXCLUDED.quotation_id";

                                command.Parameters.AddWithValue("@quotation_id", SID);
                                command.CommandText = sqlCommand + " " + sqlValues + " " + sqlUpdate;

                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    bSuccess = true;
                                }
                                Info.quotation_id = SID;
                                bSuccess = await InsertImage(Info, transaction);
                              

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
        public async Task<List<QuotationRecord>> GetRecordsForListAsync(List<string> pr_ids = null)
        {
            List<QuotationRecord> ToReturn = new List<QuotationRecord>();
            try
            {
                await Connection.OpenAsync();

                string query = "SELECT quotation_id,pr_id,selectedid FROM quotation";

                var command = new NpgsqlCommand();
                command.Connection = Connection;

                if (pr_ids != null && pr_ids.Count > 0)
                {
                    var paramNames = new List<string>();
                    for (int i = 0; i < pr_ids.Count; i++)
                    {
                        string paramName = $"@id{i}";
                        paramNames.Add(paramName);
                        command.Parameters.AddWithValue(paramName, pr_ids[i]);
                    }

                    string inClause = string.Join(", ", paramNames);
                    query += $" WHERE pr_id IN ({inClause})";
                }

                command.CommandText = query;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        QuotationRecord MainInfo = new QuotationRecord();
                        MainInfo.quotation_id = reader["quotation_id"]?.ToString() ?? string.Empty;
                        MainInfo.pr_id = reader["pr_id"]?.ToString() ?? string.Empty;
                        byte[] SelectedId = reader["selectedid"] as byte[] ?? Array.Empty<byte>();
                        MainInfo.selectedid = SelectedId;
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
            for (int i = 0; i < ToReturn.Count; i++)
            {
                List<ImageUploadInfo>Infos=await GetImagesByRequisitionNumber(ToReturn[i].quotation_id);
                ToReturn[i].SupportDocuments = Infos;
            }

            return ToReturn;
        }

        private async Task<List<ImageUploadInfo>> GetImagesByRequisitionNumber(string quotationnumber, NpgsqlTransaction? externalTransaction = null)
        {
            var images = new List<ImageUploadInfo>();
            bool shouldCloseConnection = false;
            var MyConnection = GetConnection();
            try
            {
                await MyConnection.OpenAsync();
                var command = new NpgsqlCommand(
                    "SELECT imagebyte, photoformat FROM quotation_image_table WHERE quotation_id = @req",
                    MyConnection, externalTransaction);
                command.Parameters.AddWithValue("@req", quotationnumber);

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

    }
}
