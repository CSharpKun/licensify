using DotMake.CommandLine;

namespace Licensify.Commands;

[CliCommand(
    Description = "Shows information about specified license.",
    Alias = "get"
)]
public class ShowCommand
{
    [CliArgument(Description = "License's short id.", Required = true)]
    public string LicenseId { get; set; } = null!;

    public async Task RunAsync()
    {
        
    }
}