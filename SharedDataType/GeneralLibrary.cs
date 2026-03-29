using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PurchaseBlazorApp2.Client.Service;
using PurchaseBlazorApp2.Components.Data;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;


namespace PurchaseBlazorApp2.Client
{
    public class GeneralLibrary
    {
        static public async Task<string> GetCurrentUserEmail(IJSRuntime JS)
        {
            string Email = "";
            var json = await JS.InvokeAsync<string>("getCookie", "userKey");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    UserName restoredUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(json);
                    if (restoredUser != null)
                    {
                        Email = restoredUser.Email;
                    }
                }
                catch (Exception Ex)
                {

                }
            }
            return Email;
        }
        static public async Task<int> GetCurrentUserID(IJSRuntime JS)
        {
            int UserID = 0;
            var json = await JS.InvokeAsync<string>("getCookie", "userKey");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    UserName restoredUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(json);
                    if (restoredUser != null)
                    {
                        UserID = restoredUser.ID;
                    }
                }
                catch (Exception Ex)
                {

                }
            }
            return UserID;
        }
        static public async Task<CompanyInfo> GetCurrentCompanyInfo(IJSRuntime JS)
        {
            //await JS.InvokeVoidAsync("alert", $"GetCurrentCompanyInfo");
            CompanyInfo NullCompany = new CompanyInfo();
            string Email = "";
            var json = await JS.InvokeAsync<string>("getCookie", "SelectedCompany");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    CompanyInfo restoredCompany = System.Text.Json.JsonSerializer.Deserialize<CompanyInfo>(json);

                    //await JS.InvokeVoidAsync("alert", $"restoredCompany:{restoredCompany.Name}");
                    return restoredCompany;
                }
                catch (Exception Ex)
                {
                    //await JS.InvokeVoidAsync("alert", $"Exception{Ex}");
                }
            }

           // await JS.InvokeVoidAsync("alert", $"null company");
            return NullCompany;
        }

        static public async Task<EDepartment> GetCurrentDepartmentRole(IJSRuntime JS)
        {
            EDepartment role = EDepartment.NotSpecified;
            var json = await JS.InvokeAsync<string>("getCookie", "userKey");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    UserName restoredUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(json);
                    if (restoredUser != null)
                    {
                        role = restoredUser.Role;
                    }
                }
                catch (Exception Ex)
                {

                }
            }
            return role;
        }
        static public async Task<EHRRole> GetCurrentHRRole(IJSRuntime JS)
        {
            EHRRole role = EHRRole.None;
            var json = await JS.InvokeAsync<string>("getCookie", "userKey");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    UserName restoredUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(json);
                    if (restoredUser != null)
                    {
                        role = restoredUser.HRRole;
                    }
                }
                catch (Exception Ex)
                {

                }
            }
            return role;
        }

        static public async Task<UserName?> GetCurrentUser(IJSRuntime JS)
        {
            UserName restoredUser = new UserName();
            var json = await JS.InvokeAsync<string>("getCookie", "userKey");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    restoredUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(json);

                }
                catch (Exception Ex)
                {

                }
            }
            return restoredUser;
        }

        static public async Task LoadCompanyInfo(CompanyInfo SelectedCompany, NavigationManager NavigationManager, HttpClient Http, IJSRuntime JS)
        {

            if (JS != null)
            {
                UserName MyUser = new UserName();
                string userKey = await JS.InvokeAsync<string>("getCookie", "userKey");
                if (!string.IsNullOrWhiteSpace(userKey))
                {
                    MyUser = System.Text.Json.JsonSerializer.Deserialize<UserName>(userKey);
                }
                GetRoleRequest Request = new GetRoleRequest();
                Request.UserID = MyUser.ID;
                Request.CompanyId = SelectedCompany.ID;
                var roleTask = Http.PostAsJsonAsync(
                  NavigationManager.ToAbsoluteUri("api/login/getrole"),
                  Request
              );
                var hrroleTask = Http.PostAsJsonAsync(
                   NavigationManager.ToAbsoluteUri("api/login/gethrrole"),
                   Request
               );

                await Task.WhenAll(hrroleTask, roleTask);
                // --- ROLE ---
                EDepartment departmentRole = EDepartment.NotSpecified;
                try
                {
                    var roleResponse = roleTask.Result;
                    departmentRole = await roleResponse.Content.ReadFromJsonAsync<EDepartment>();
                }
                catch (Exception ex)
                {
                    await JS.InvokeVoidAsync("console.error", $"Error retrieving role: {ex.Message}");
                }

                EHRRole HRRole = EHRRole.None;
                try
                {
                    var hrroleResponse = hrroleTask.Result;
                    HRRole = await hrroleResponse.Content.ReadFromJsonAsync<EHRRole>();
                }
                catch (Exception ex)
                {
                    await JS.InvokeVoidAsync("console.error", $"Error retrieving role: {ex.Message}");
                }
                MyUser.Role = departmentRole;
                MyUser.Department = departmentRole.ToString();
                MyUser.HRRole = HRRole;
                if (JS != null)
                {
                    string jsonUser = System.Text.Json.JsonSerializer.Serialize(MyUser);
                    await JS.InvokeVoidAsync("setCookie", "userKey", jsonUser, 1);
                }

                string SelectedCompanyUser = System.Text.Json.JsonSerializer.Serialize(SelectedCompany);
                await JS.InvokeVoidAsync("setCookie", "SelectedCompany", SelectedCompanyUser, 1);
            }

        }

        static public Task<string> GetAccessTokenAsync(string accessToken,Uri uri, Dictionary<string, object>? context = null, CancellationToken token = default)
        {
            return Task.FromResult(accessToken);
        }
    }
}
