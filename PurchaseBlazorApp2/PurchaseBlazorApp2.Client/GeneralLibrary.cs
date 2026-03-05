using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;

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

        static public async Task<CompanyInfo> GetCurrentCompanyInfo(IJSRuntime JS)
        {
            CompanyInfo NullCompany = new CompanyInfo();
            string Email = "";
            var json = await JS.InvokeAsync<string>("getCookie", "SelectedCompany");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    CompanyInfo restoredCompany = System.Text.Json.JsonSerializer.Deserialize<CompanyInfo>(json);
                    return restoredCompany;
                }
                catch (Exception Ex)
                {

                }
            }
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


        static public Task<string> GetAccessTokenAsync(string accessToken,Uri uri, Dictionary<string, object>? context = null, CancellationToken token = default)
        {
            return Task.FromResult(accessToken);
        }
    }
}
