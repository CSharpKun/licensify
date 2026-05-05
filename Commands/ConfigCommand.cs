using DotMake.CommandLine;
using Licensify.Services;
using Spectre.Console;

namespace Licensify.Commands;

[CliCommand(
    Description = "Manages Config."
)]
public class ConfigCommand(IConfigService database)
{
    [CliArgument(
        Description = "Config Key",
        Required = true,
        ValidationPattern = @".*?\..{1,}"
    )]
    public string Key { get; set; } = null!;

    [CliArgument(
        Description = "Config Value",
        Required = false
    )]
    public string? Value { get; set; }

    public async Task RunAsync()
    {

    }
}