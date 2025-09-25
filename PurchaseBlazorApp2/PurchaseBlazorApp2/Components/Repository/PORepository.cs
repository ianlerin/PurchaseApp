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



        private void InsertInfoOfBasicInfo<T>(T MainInfo, NpgsqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();
            try
            {
                foreach (var property in properties)
                {
                    if (property.Name == "ApprovalInfo" )
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

                                    if (propName == "PO_ID"|| propName == "ApprovalInfo")
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
