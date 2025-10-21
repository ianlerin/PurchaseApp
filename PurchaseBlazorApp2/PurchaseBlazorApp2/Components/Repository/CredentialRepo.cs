using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using ServiceStack;
using PurchaseBlazorApp2.Resource;

namespace PurchaseBlazorApp2.Components.Repository
{


    public class CredentialRepo
    {
        private NpgsqlConnection Connection;
        public CredentialRepo()
        {
            Connection = new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432; User Id=postgres; Password=password; Database=purchase");
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
           var SubmitResponse = new CredentialSubmitResponse();

           try
           {
               // 1. Configure Azure.Identity credential
               var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
               {
                   TenantId = "85e055dd-d7b9-4a2b-be46-f5bb151440d0",  // Tenant (Directory) ID
                   ClientId = "ec2d75da-33ea-45d3-9e84-bd67e831a610",  // Application (client) ID
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

        public async Task<List<string>> TryGetAllProcurementEmail(List<EDepartment> departments)
        {
            var emailList = new List<string>();

            try
            {
                await Connection.OpenAsync();

                // Build parameterized IN clause
                var parameters = new List<string>();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = Connection;

                for (int i = 0; i < departments.Count; i++)
                {
                    string paramName = $"role{i}";
                    parameters.Add($"@{paramName}");
                    cmd.Parameters.AddWithValue(paramName, departments[i].ToString());
                }

                cmd.CommandText = $@"
            SELECT username 
            FROM credential
            WHERE role IN ({string.Join(",", parameters)}) 
            AND username IS NOT NULL 
            AND username <> ''";

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    emailList.Add(reader.GetString(0)); // username = email
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching procurement emails: " + ex.Message);
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return emailList;
        }


        public async Task<EDepartment> TryGetRole(string userName)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"SELECT role FROM credential WHERE username = @username LIMIT 1", Connection);

                cmd.Parameters.AddWithValue("username", userName);

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && Enum.TryParse<EDepartment>(result.ToString(), out var department))
                {
                    return department;
                }

                return EDepartment.NotSpecified;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching role: " + ex.Message);
                return EDepartment.NotSpecified;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }


    }
}
