using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;

namespace PurchaseBlazorApp2.Client
{
    public class GeneralLibrary
    {
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
        static public Task<string> GetAccessTokenAsync(string accessToken,Uri uri, Dictionary<string, object>? context = null, CancellationToken token = default)
        {
            return Task.FromResult(accessToken);
        }
    }
}
