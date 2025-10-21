using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Data;

namespace Genesis.UserService
{
    public class UserHelperService
    {
        private readonly IJSRuntime JS;

        public UserHelperService(IJSRuntime jsRuntime)
        {
            JS = jsRuntime;
        }

        public async Task<EDepartment> GetCurrentDepartmentRoleAsync()
        {
            EDepartment role = EDepartment.NotSpecified;

            try
            {
                var json = await JS.InvokeAsync<string>("getCookie", "userKey");

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var restoredUser = JsonSerializer.Deserialize<UserName>(json);
                    if (restoredUser != null)
                        role = restoredUser.Role;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current department role: {ex.Message}");
            }

            return role;
        }
        public async Task<bool>GetIsProcurementAsync()
        {
            EDepartment MyDepartment = await GetCurrentDepartmentRoleAsync();
           bool bIsProcurementManager = (MyDepartment == EDepartment.ProcurementManager);
            return bIsProcurementManager;
        }
    }
}