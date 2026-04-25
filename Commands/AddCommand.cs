using DotMake.CommandLine;

namespace Licensify.Commands;

[CliCommand(
    Description = "Adds specified license to the specified project."
)]
public class AddCommand
{
    [CliOption(Description = "Path to the repository.", Name = "repo")]
    public string RepositoryPath { get; set; } = "."; 

    [CliArgument(Description = "License's short id.", Required = true)]
    public string LicenseId { get; set; } = null!;

    public async Task RunAsync()
    {
        
    }
}