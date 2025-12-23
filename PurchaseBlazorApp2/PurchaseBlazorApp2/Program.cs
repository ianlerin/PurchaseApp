using Azure.Identity;
using Microsoft.Graph;
using PurchaseBlazorApp2.Client.Pages;
using PurchaseBlazorApp2.Components;
using PurchaseBlazorApp2.Components.Global;
using PurchaseBlazorApp2.Components.Helper;
using PurchaseBlazorApp2.Resource;
using PurchaseBlazorApp2.Service;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.EnableDetailedErrors = true;
    })
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddControllers();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<LoginCookieService>();
builder.Services.AddScoped<ClientStateStorage>();
builder.Services.AddScoped<ClientGlobalVar>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7129") 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton(provider =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var clientId = builder.Configuration["AzureAd:ClientId"];
    var clientSecret = builder.Configuration["AzureAd:ClientSecret"];

    var options = new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud };
    var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

    return new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
});

builder.Services.AddHostedService<ReminderEmailService>();
builder.Services.AddSingleton<IEPFTableService, EPFExcelConverter>();
builder.Services.AddScoped<EmailService>();
var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PurchaseBlazorApp2.Client._Imports).Assembly);

app.Run();