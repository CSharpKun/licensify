using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Text.Json;
using Licensify;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(logging =>
    logging.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        options.UseUtcTimestamp = true;
        options.SingleLine = true;
        options.ColorBehavior = LoggerColorBehavior.Enabled;
    })
)
.AddSingleton<JsonSerializerOptions>(_ => new()
{ 
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    TypeInfoResolver = LicensifyJsonSerializerContext.Default
})
.AddSingleton<ILicenseManager, LicenseManager>()
.AddSingleton(provider =>
{
    HttpClient client = new()
    {
        BaseAddress = new Uri("https://spdx.org/licenses/"),
        Timeout = TimeSpan.FromSeconds(30)
    };
    client.DefaultRequestHeaders.Add("User-Agent", $"Licensify/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}");
    return client;    
});


using var host = builder.Build();
var services = host.Services;

RootCommand rootCommand = new("SPDX Client that can automatically manage LICENSE files.");

Command listCommand = new("list", "Lists all SPDX Licenses");
rootCommand.Subcommands.Add(listCommand);

listCommand.SetAction(services.GetRequiredService<ILicenseManager>().ListSPDXLicenses);

return await rootCommand.Parse(args).InvokeAsync();

