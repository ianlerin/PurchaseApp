using Npgsql;
using PurchaseBlazorApp2.Components.Global;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class CredentialRepo
    {
        private NpgsqlConnection Connection;
        public CredentialRepo()
        {
            Connection = new NpgsqlConnection($"Server=einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com;Port=5432; User Id=postgres; Password=password; Database=purchase");
        }
        public async Task<bool> TryLoginAsync(UserName info)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT 1 FROM credential WHERE username = @username AND password = @password", Connection);
                cmd.Parameters.AddWithValue("username", info.Name);
                cmd.Parameters.AddWithValue("password", info.Password);

                using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync(); // true if a row exists
            }
            catch (Exception ex)
            {
               Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }
    }
}
