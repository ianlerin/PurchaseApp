using Microsoft.AspNetCore.SignalR.Protocol;
using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using SharedDataType;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class SupplierRepository
    {
        private NpgsqlConnection Connection;
        public SupplierRepository()
        {
            Connection = GetConnection();
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection($"Server=localhost;Port=5432; User Id=postgres; Password=password; Database=purchase");
        }
        public async Task<List<SupplierRecord>> GetAllSuppliersAsync()
        {
            var suppliers = new List<SupplierRecord>();

            try
            {
                await Connection.OpenAsync();

                string query = "SELECT * FROM supplier";

                using (var cmd = new NpgsqlCommand(query, Connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var record = new SupplierRecord();
                        InsertInfoOfBasicInfo(record, reader);
                        suppliers.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllSuppliersAsync: " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return suppliers;
        }

        public async Task<SupplierRecord?> GetSupplierByIdAsync(string sid)
        {
            SupplierRecord? supplier = null;

            try
            {
                await Connection.OpenAsync();

                string query = "SELECT * FROM supplier WHERE sid = @sid";

                using (var cmd = new NpgsqlCommand(query, Connection))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            supplier = new SupplierRecord();
                            InsertInfoOfBasicInfo(supplier, reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetSupplierByIdAsync: " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return supplier;
        }


        private void InsertInfoOfBasicInfo<T>(T MainInfo, NpgsqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();
            try
            {
                foreach (var property in properties)
                {
                   
                    object obj = property.GetValue(MainInfo);
                    int RowNum = reader.GetOrdinal(property.Name);
                    if (RowNum >= 0)
                    {
                       
                        if (obj is DateTime)
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

        public async Task<bool> SubmitAsync(IEnumerable<SupplierRecord> records)
        {
            bool ownTransaction = false;

            
            try
            {
                Connection.Open();
                foreach (var record in records)
                {
                    string sqlCommand = "INSERT INTO supplier (";
                    string sqlValues = "VALUES (";
                    string sqlUpdate = "ON CONFLICT (sid) DO UPDATE SET ";

                    var props = typeof(SupplierRecord).GetProperties();
                    foreach (var prop in props)
                    {
                        string propName = prop.Name;

                        sqlCommand += propName + ",";
                        sqlValues += "@" + propName + ",";

                        if (propName != "SID") // don't update primary key
                        {
                            sqlUpdate += propName + " = EXCLUDED." + propName + ",";
                        }
                    }

                    sqlCommand = sqlCommand.TrimEnd(',') + ")";
                    sqlValues = sqlValues.TrimEnd(',') + ")";
                    sqlUpdate = sqlUpdate.TrimEnd(',');

                    string query = $"{sqlCommand} {sqlValues} {sqlUpdate};";

                    await using var command = new NpgsqlCommand(query, Connection);

                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(record) ?? DBNull.Value;
                        command.Parameters.AddWithValue("@" + prop.Name, value);
                    }

                    await command.ExecuteNonQueryAsync();
                }

              
                return true;
            }
            catch (Exception ex)
            {
               
                return false; // failure
            }
            finally
            {
                Connection.Close();
            }
        }

    }
}
