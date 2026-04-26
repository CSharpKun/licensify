using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using DotMake.CommandLine;
using Licensify.Commands;
using Licensify.Services;
using Licensify;

var rootCommand = Cli.Parse<RootCommand>().Bind<RootCommand>();

CliGlobalSettings globalSettings = new(rootCommand.Verbose, rootCommand.NoCache);

Cli.Ext.ConfigureServices(services =>
    services.AddSingleton<JsonSerializerOptions>(_ => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = LicensifyJsonSerializerContext.Default
    })
    .AddSingleton(globalSettings)
    .AddSingleton<ILicenseDatabase, JsonLicenseDatabase>()
    .AddHttpClient("spdx", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = new("https://spdx.org/licenses/");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        var clientInfo = new ProductInfoHeaderValue("Licensify", version);
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    }).Services.AddHttpClient("github", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = new("https://raw.githubusercontent.com/spdx/license-list-data/main/json/");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        var clientInfo = new ProductInfoHeaderValue("Licensify", version);
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    })
);

await Cli.RunAsync<RootCommand>();

