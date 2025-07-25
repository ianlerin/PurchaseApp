using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Global;
using PurchaseBlazorApp2.Components.Helper;
using PurchaseBlazorApp2.Components.Repository;
using ServiceStack;
using System.ComponentModel.DataAnnotations;

namespace PurchaseBlazorApp2.ViewModel
{

    public class LoginModel :ObservableObject
    {
        public event Action? OnLoginSuccess;
        private readonly GlobalVar globalVar;
        private readonly IJSRuntime JS;
        public LoginModel(GlobalVar _globalVar, IJSRuntime _JS)
        {
            globalVar = _globalVar;
            JS = _JS;

        }

        public async Task HandleLogin()
        {
            CredentialRepo repo = new CredentialRepo();
            UserName ToUse = new UserName(Username, Password);
            bool bSuccess = await repo.TryLoginAsync(ToUse);
         
            if(bSuccess)
            {
                string Key = System.Text.Json.JsonSerializer.Serialize(ToUse);
                if (JS is not null)
                {
                    //await JS.InvokeVoidAsync("setCookie", "userKey", Key, 7);
                }
                globalVar.SetUser(ToUse);
                //loginCookieService.SetCookie(ToUse);
               
                OnLoginSuccess?.Invoke();
            }
        }

        public string Username { get; set; } = "a";

        public string Password { get; set; } = "a";
    }
}
