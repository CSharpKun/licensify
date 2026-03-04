using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.CommandLine;
using System.Reflection;
using Licensify;

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
    var client = new HttpClient()
    {
        BaseAddress = new Uri("https://spdx.org/licenses/"),
        Timeout = TimeSpan.FromSeconds(30)
    };
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
    client.DefaultRequestHeaders.Add("User-Agent", $"Licensify/{version ?? "0.0.0"}");
    return client;    
});


using var host = builder.Build();
var licenseManager = host.Services.GetRequiredService<ILicenseManager>();

var rootCommand = new RootCommand("SPDX Client that can automatically manage LICENSE files.");
var commands = rootCommand.Subcommands;

var listCommand = new Command("list", "Lists all SPDX Licenses");
listCommand.SetAction((res, token) => licenseManager.ListSPDXLicenses(token));
commands.Add(listCommand);

var showCommand = new Command("show", "Shows information about specified license.");
var licenseArgument = new Argument<string>("licenseId") { Description = "License's Id." };
showCommand.Arguments.Add(licenseArgument);
showCommand.SetAction((res, token) => licenseManager.ShowLicense(res.GetValue(licenseArgument), token));
commands.Add(showCommand);

return await rootCommand.Parse(args).InvokeAsync();

