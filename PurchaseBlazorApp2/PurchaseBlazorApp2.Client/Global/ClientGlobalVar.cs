using Azure.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using PurchaseBlazorApp2.Components.Data;
using System.Collections.Generic;
using System.Text.Json;

namespace PurchaseBlazorApp2.Components.Global
{
    public class CredentialSubmitResponse
    {
        public bool bSuccess { get; set; }
        public UserName MyName { get; set; }= new UserName();
    }

    public class UserName
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public UserName(string _Name, string _Password)
        {
            Name = _Name;
            Password = _Password;   
        }
        public UserName()
        {

        }
        public EDepartment Role { get; set; }
    }

    public class ClientGlobalVar
    {
        public event Action? OnLoginStateChanged;
        public UserName UserName { get; set; }
        public bool IsLoggedIn { get; set; } = false;
        public Microsoft.AspNetCore.Components.WebAssembly.Authentication.AccessToken AccessToken { get; set; }
        public void SetToken(Microsoft.AspNetCore.Components.WebAssembly.Authentication.AccessToken _Token)
        {
            AccessToken = _Token;
        }
        public void SetUser(UserName ToSet)
        {
            if (UserName == null|| ToSet.Name!= UserName.Name)
            {
                UserName = ToSet;
                IsLoggedIn = true;
                OnLoginStateChanged?.Invoke();
            }
     

        }
        public void Logout()
        {
            UserName = new UserName();
            IsLoggedIn = false;
            StopTokenRefreshLoop();
             OnLoginStateChanged?.Invoke();
        }


        private Timer? _refreshTimer;

        public async Task StartTokenRefreshLoop(IAccessTokenProvider tokenProvider)
        {
            _refreshTimer = new Timer(async _ =>
            {
                var result = await tokenProvider.RequestAccessToken();

                if (result.TryGetToken(out var token))
                {
                    var expiresIn = token.Expires - DateTimeOffset.Now;
                    Console.WriteLine($"Token refreshed, expires in {expiresIn.TotalMinutes} min");
                }
                else
                {
                    Console.WriteLine("Failed to refresh token, user may need to login again.");
                }

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // check every 5 minutes
        }
        private void StopTokenRefreshLoop()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
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
