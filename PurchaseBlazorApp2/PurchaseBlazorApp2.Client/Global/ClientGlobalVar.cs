using PurchaseBlazorApp2.Components.Data;
using System.Text.Json;

namespace PurchaseBlazorApp2.Components.Global
{
    public struct UserName
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public UserName(string _Name, string _Password)
        {
            Name = _Name;
            Password = _Password;   
        }
        public EDepartment Role { get; set; }
    }

    public class ClientGlobalVar
    {
        public event Action? OnLoginStateChanged;
        public UserName UserName { get; set; }
        public bool IsLoggedIn { get; set; } = false;
        public void SetUser(UserName ToSet)
        {
            UserName = ToSet;
            IsLoggedIn = true;

         

            /*
            HttpContext.Response.Cookies.Append("UserInfo", json, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
            */
            OnLoginStateChanged?.Invoke();



        }
    }

    public class GlobalHelperFunctions
    {
        public bool CheckCanApprove(UserName UserInfo, PurchaseRequisitionRecord Record)
        {
           foreach(var ApprovalInfo in Record.Approvals)
            {
                if (ApprovalInfo.Departments.Contains(UserInfo.Role))
                {
                    if(string.IsNullOrEmpty(ApprovalInfo.UserName)||!string.IsNullOrEmpty(ApprovalInfo.UserName)&& ApprovalInfo.UserName== UserInfo.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }

}
