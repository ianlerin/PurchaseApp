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
            Connection = new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432; User Id=postgres; Password=password; Database=purchase_master");
        }

        public async Task<bool> RegisterAsync(UserName info)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"INSERT INTO credential (username, user_role) 
                    VALUES (@username, @user_role)", Connection);

                cmd.Parameters.AddWithValue("username", info.Name);
                cmd.Parameters.AddWithValue("user_role", info.Role.ToString());

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

        public async Task<List<CompanyInfo>> TryGetAllCompanyInfo(int userID)
        {
            List<int> idToFind = await TryGetAllCompanyID(userID);
            var companiesInfo = new List<CompanyInfo>();

            if (idToFind == null || idToFind.Count == 0)
                return companiesInfo;

            try
            {
                await using var connection = new NpgsqlConnection(Connection.ConnectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand(
              @"SELECT company_id AS ID, display_name AS Name
      FROM companies
      WHERE company_id = ANY(@ids)
      ORDER BY display_name", connection);

                cmd.Parameters.AddWithValue("ids", idToFind.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    companiesInfo.Add(new CompanyInfo
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching company info: " + ex.Message);
            }

            return companiesInfo;
        }

        public async Task<string?> TryGetDatabaseNameByCompanyId(int companyId)
        {

            try
            {
                await using var connection = new NpgsqlConnection(Connection.ConnectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    @"SELECT db_name 
              FROM companies 
              WHERE company_id = @company_id
              LIMIT 1", connection);

                cmd.Parameters.AddWithValue("company_id", companyId);

                var result = await cmd.ExecuteScalarAsync();

                return result?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching db_name: " + ex.Message);
                return null;
            }
        }
        public async Task<List<int>> TryGetAllCompanyID(int UserID)
        {
            var companies = new List<int>();

            try
            {
                await using var connection = new NpgsqlConnection(Connection.ConnectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    @"SELECT DISTINCT company_id
              FROM user_access
              WHERE user_id = @userId", connection);

                cmd.Parameters.AddWithValue("userId", UserID);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    companies.Add(reader.GetInt32(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching companies: " + ex.Message);
            }

            return companies;
        }


        public async Task<List<int>> TryGetAllRelevantUserID(DepartmentInfo Info)
        {
            var emailList = new List<int>();

            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = Connection;

                var parameters = new List<string>();
                for (int i = 0; i < Info.Departments.Count; i++)
                {
                    string paramName = $"user_role{i}";
                    parameters.Add($"@{paramName}");
                    cmd.Parameters.AddWithValue(paramName, Info.Departments[i].ToString()); 
                }

                cmd.CommandText = $@"
            SELECT DISTINCT user_id
            FROM user_access
            WHERE user_role IN ({string.Join(",", parameters)})
              AND company_id = @companyId
              AND user_id IS NOT NULL"; 

                cmd.Parameters.AddWithValue("companyId", Info.CompanyId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    emailList.Add(reader.GetInt32(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching RelevantUserID: " + ex.Message);
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return emailList;
        }

        public async Task<List<string>> TryGetAllProcurementEmail(DepartmentInfo Info)
        {
            var emailList = new List<string>();

            var userIDList = await TryGetAllRelevantUserID(Info);

            if (userIDList == null || userIDList.Count == 0)
                return emailList;

            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = Connection;

                // Build parameterized IN clause
                var parameters = new List<string>();
                for (int i = 0; i < userIDList.Count; i++)
                {
                    string paramName = $"id{i}";
                    parameters.Add($"@{paramName}");
                    cmd.Parameters.AddWithValue(paramName, userIDList[i]);
                }

                cmd.CommandText = $@"
            SELECT email
            FROM users
            WHERE user_id IN ({string.Join(",", parameters)})
              AND email IS NOT NULL
              AND email <> ''";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    emailList.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching emails: " + ex.Message);
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return emailList;
        }
        public async Task<bool> CheckIfUsernameExistsAsync(string username)
        {
            try
            {
                await Connection.OpenAsync();

                string query = @"
                            SELECT COUNT(*) 
                            FROM users
                            WHERE email = @username;";

                using var cmd = new NpgsqlCommand(query, Connection);
                cmd.Parameters.AddWithValue("@username", username);

                var result = await cmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking username existence: " + ex.Message);
                return false;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }
        public async Task<int> GetUserID(string userName)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"SELECT user_id FROM users WHERE email = @username LIMIT 1", Connection);

                cmd.Parameters.AddWithValue("username", userName);

                var result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching role: " + ex.Message);
                return -1;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }
        public async Task<EDepartment> TryGetRole(int userID, int companyID)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"SELECT user_role FROM user_access WHERE user_id = @username AND company_id=@companyid LIMIT 1", Connection);

                cmd.Parameters.AddWithValue("username", userID);
                cmd.Parameters.AddWithValue("companyid", companyID);
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

        public async Task<EHRRole> TryGetHRRole(int userID, int companyID)
        {
            try
            {
                await Connection.OpenAsync();

                using var cmd = new NpgsqlCommand(
                  @"SELECT hr_role FROM user_access WHERE user_id = @username AND company_id=@companyid LIMIT 1", Connection);

                cmd.Parameters.AddWithValue("username", userID);
                cmd.Parameters.AddWithValue("companyid", companyID);
                var result = await cmd.ExecuteScalarAsync();

                if (result != null && Enum.TryParse<EHRRole>(result.ToString(), out var department))
                {
                    return department;
                }

                return EHRRole.None;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching role: " + ex.Message);
                return EHRRole.None;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }
    }
}
