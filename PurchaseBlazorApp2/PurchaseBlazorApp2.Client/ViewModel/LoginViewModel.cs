using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;



namespace PurchaseBlazorApp2.ViewModel
{

    public class LoginModel :ObservableObject
    {
        public event Action? OnLoginSuccess;
        private readonly ClientGlobalVar globalVar;
        private readonly IJSRuntime JS;
        private readonly HttpClient Http;
        public LoginModel(ClientGlobalVar _globalVar, IJSRuntime _JS)
        {
            globalVar = _globalVar;
            JS = _JS;

        }

        public async Task HandleLogin()
        {
            Console.WriteLine("HandleLogin");
         
        }

        public string Username { get; set; } = "a";

        public string Password { get; set; } = "a";
    }
}
