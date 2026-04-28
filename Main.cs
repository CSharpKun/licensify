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
var spdxRepoHttp = rootCommand.SpdxRepo.ToLower();

if (Uri.TryCreate(rootCommand.SpdxRepo, UriKind.Absolute, out Uri? result)) spdxRepoHttp = result.Host;

CliGlobalSettings globalSettings = new(rootCommand.Verbose, rootCommand.NoCache, spdxRepoHttp);

Cli.Ext.ConfigureServices(services =>
{
    services.AddSingleton<JsonSerializerOptions>(_ => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = LicensifyJsonSerializerContext.Default
    })
    .AddSingleton(globalSettings)
    .AddSingleton<ILicenseDatabase, JsonLicenseDatabase>();

    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
    var clientInfo = new ProductInfoHeaderValue("Licensify", version);

    services.AddHttpClient("spdx", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = new("https://spdx.org/licenses/");
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    });

    services.AddHttpClient("github", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = new("https://raw.githubusercontent.com/spdx/license-list-data/main/json/");
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    });

    if (result is null) return;

    services.AddHttpClient(result.Host, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = result;
        client.DefaultRequestHeaders.UserAgent.Add(clientInfo);
    });
});

await Cli.RunAsync<RootCommand>();

