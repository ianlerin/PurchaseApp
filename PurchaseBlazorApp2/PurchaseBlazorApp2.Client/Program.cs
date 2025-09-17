using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using PurchaseBlazorApp2.Components.Global;
using Radzen;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.JSInterop;
using Microsoft.Graph.Models.Security;




var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ✅ Base HttpClient
builder.Services.AddScoped(sp =>
{

    NavigationManager navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
});


// ✅ MSAL Authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.Authentication.RedirectUri = "https://localhost:7129";
  options.ProviderOptions.Authentication.PostLogoutRedirectUri = "https://localhost:7129";
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/Team.ReadBasic.All");
});

// ✅ Graph Client + Token Provider
builder.Services.AddScoped<GraphServiceClient>(sp =>
{
    var blazorProvider = sp.GetRequiredService<
        Microsoft.AspNetCore.Components.WebAssembly.Authentication.IAccessTokenProvider>();

    var kiotaTokenProvider = new BlazorToKiotaTokenProvider(blazorProvider);

    var authProvider = new Microsoft.Kiota.Abstractions.Authentication.BaseBearerTokenAuthenticationProvider(kiotaTokenProvider);

    return new GraphServiceClient(authProvider);
});

// ✅ Radzen & Global Services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<ClientGlobalVar>();
builder.Services.AddScoped<ClientStateStorage>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
var host = builder.Build();

// Safe to use JSRuntime now
var js = host.Services.GetRequiredService<IJSRuntime>();
_ = js.InvokeVoidAsync("console.log", "Start building");

// Run the app
await host.RunAsync();

public class BlazorToKiotaTokenProvider
       : Microsoft.Kiota.Abstractions.Authentication.IAccessTokenProvider
{
    private readonly Microsoft.AspNetCore.Components.WebAssembly.Authentication.IAccessTokenProvider _blazorProvider;

    public BlazorToKiotaTokenProvider(
        Microsoft.AspNetCore.Components.WebAssembly.Authentication.IAccessTokenProvider blazorProvider)
    {
        _blazorProvider = blazorProvider
            ?? throw new ArgumentNullException(nameof(blazorProvider));
    }

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object> additionalAuthenticationContext = default,
        CancellationToken cancellationToken = default)
    {
        var tokenResult = await _blazorProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out var token))
            return token.Value;

        throw new InvalidOperationException("Unable to acquire access token for Microsoft Graph.");
    }



    /// <summary>
    /// Required by Kiota. This validator ensures that the auth provider
    /// only attaches tokens to known/allowed hosts.
    /// </summary>
    public Microsoft.Kiota.Abstractions.Authentication.AllowedHostsValidator AllowedHostsValidator { get; }
        = new Microsoft.Kiota.Abstractions.Authentication.AllowedHostsValidator(new[]
        {
                "graph.microsoft.com" // ✅ restrict token usage to Microsoft Graph
        });
}
