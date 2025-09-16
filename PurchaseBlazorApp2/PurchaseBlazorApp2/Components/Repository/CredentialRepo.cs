using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
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
            Connection = new NpgsqlConnection($"Server=localhost;Port=5432; User Id=postgres; Password=password; Database=purchase");
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
        /*
       public async Task<CredentialSubmitResponse> TryLoginAsync(UserName info)
       {
           var SubmitResponse = new CredentialSubmitResponse();

           try
           {
               // 1. Configure Azure.Identity credential
               var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
               {
                   TenantId = "d7b92891-b937-4702-ad5e-03e1f752dddf",  // Tenant (Directory) ID
                   ClientId = "d6e4c33e-072d-46cd-b8e2-4a8a256a96d9",  // Application (client) ID
                   RedirectUri = new Uri("http://localhost"), // must match Azure AD registration
                   LoginHint = info.Name
               });

               // 2. Define required scopes
               var scopes = new[] { "User.Read", "Team.ReadBasic.All" };

               // 3. Build Graph client directly using credential
               var graphClient = new GraphServiceClient(credential, scopes);

               // 4. Get signed-in user's profile
               var me = await graphClient.Me.GetAsync();

               if (me != null)
               {
                   SubmitResponse.bSuccess = true;
                   SubmitResponse.MyName.Name = me.DisplayName ?? info.Name;
                   SubmitResponse.MyName.Password = string.Empty; // OAuth removes need for password
                   //SubmitResponse.MyName.Role = EDepartment.; // default role

                   // 5. (Optional) Check Teams membership for role assignment
                   var joinedTeams = await graphClient.Me.JoinedTeams.GetAsync();

                   if (joinedTeams?.Value?.Any() == true)
                   {
                       if (joinedTeams.Value.Any(t => t.DisplayName.Contains("AdminTeam")))
                       {
                           SubmitResponse.MyName.Role = EDepartment.Admin;
                       }
                   }
               }
           }
           catch (Exception ex)
           {
               Console.WriteLine($"[AUTH ERROR] {ex.Message}");
           }

           return SubmitResponse;
       }
        */

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
