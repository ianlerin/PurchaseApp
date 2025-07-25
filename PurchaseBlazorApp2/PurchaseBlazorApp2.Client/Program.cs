using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.NetworkInformation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

await builder.Build().RunAsync();
