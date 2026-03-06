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
.AddSingleton<ILicenseDatabase, JsonLicenseDatabase>()
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

var licenseArgument = new Argument<string>("licenseId") { Description = "License's Id." };

var repoOption = new Option<string>("--repo", "-r")
    {
        Description = "Repository's path.",
        DefaultValueFactory = _ => "."
    };

var listCommand = new Command("list", "Lists all SPDX Licenses");
listCommand.SetAction((res, token) => licenseManager.ListLicenses(token));
commands.Add(listCommand);

var showCommand = new Command("show", "Shows information about specified license.");
showCommand.Arguments.Add(licenseArgument);
showCommand.Aliases.Add("get");
showCommand.SetAction((res, token) => licenseManager.ShowLicense(res.GetValue(licenseArgument), token));
commands.Add(showCommand);

var addCommand = new Command("add", "Adds specified license to the specified project.");
addCommand.Arguments.Add(licenseArgument);
addCommand.Options.Add(repoOption);
addCommand.SetAction((res, token) => licenseManager.AddLicense(res.GetValue(licenseArgument), res.GetValue(repoOption), token));
commands.Add(addCommand);

return await rootCommand.Parse(args).InvokeAsync();

