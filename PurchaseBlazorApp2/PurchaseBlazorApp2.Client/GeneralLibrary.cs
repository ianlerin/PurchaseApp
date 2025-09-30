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
    }
}
