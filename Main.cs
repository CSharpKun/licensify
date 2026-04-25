using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;
using Licensify;
using DotMake.CommandLine;
using Licensify.Commands;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);

Cli.Ext.ConfigureServices(services =>
    services.AddSingleton<JsonSerializerOptions>(_ => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = LicensifyJsonSerializerContext.Default
    })
    .AddSingleton<ILicenseDatabase, JsonLicenseDatabase>()
    .AddHttpClient("spdx", client =>
    {
        client.BaseAddress = new("https://spdx.org/licenses/");
        client.Timeout = TimeSpan.FromSeconds(30);
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        var clientInfo = new ProductInfoHeaderValue("Licensify", version);
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    })
);

await Cli.RunAsync<RootCommand>();

