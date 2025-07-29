using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;
using ServiceStack;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class CredentialRepo
    {
        private NpgsqlConnection Connection;
        public CredentialRepo()
        {
            Connection = new NpgsqlConnection($"Server=einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com;Port=5432; User Id=postgres; Password=password; Database=purchase");
        }

        public async Task<bool> RegisterAsync(UserName info)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"INSERT INTO credential (username, password, role) 
              VALUES (@username, @password, @role)", Connection);

                cmd.Parameters.AddWithValue("username", info.Name);
                cmd.Parameters.AddWithValue("password", info.Password);
                cmd.Parameters.AddWithValue("role", info.Role.ToString());

                int affectedRows = await cmd.ExecuteNonQueryAsync();

                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Registration failed: " + ex.Message);
                return false;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }


        public async Task<CredentialSubmitResponse> TryLoginAsync(UserName info)
        {
            CredentialSubmitResponse SubmitResponse = new CredentialSubmitResponse();
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT username, password, role  FROM credential WHERE username = @username AND password = @password", Connection);
                cmd.Parameters.AddWithValue("username", info.Name);
                cmd.Parameters.AddWithValue("password", info.Password);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    SubmitResponse.bSuccess = true;
                    SubmitResponse.MyName.Name = reader["username"]?.ToString() ?? string.Empty;
                    SubmitResponse.MyName.Password = reader["password"]?.ToString() ?? string.Empty;
                    EDepartment MyRole;

                    Enum.TryParse(reader["role"]?.ToString() ?? string.Empty, out MyRole);

                    SubmitResponse.MyName.Role = MyRole;
                }
                return SubmitResponse; // true if a row exists
            }
            catch (Exception ex)
            {
               Console.WriteLine(ex.Message);
                return SubmitResponse;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }
    }
}
