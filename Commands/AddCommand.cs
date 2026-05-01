using DotMake.CommandLine;
using Licensify.Services;
using Spectre.Console;

namespace Licensify.Commands;

[CliCommand(
    Description = "Adds specified license to the specified project."
)]
public class AddCommand(ILicenseParser parser)
{
    [CliOption(Description = "Path to the repository.", Name = "repo")]
    public string RepositoryPath { get; set; } = "."; 

    [CliArgument(Description = "License's short id.", Required = true)]
    public string LicenseId { get; set; } = null!;

    public async Task RunAsync()
    {
        parser.GetOptionalParts = GetOptionalParts;
        parser.GetName = GetName;
        parser.GetSurname = GetSurname;
    }

    private bool GetOptionalParts(string optionalText)
    {
        return AnsiConsole.Ask("Would you like to add this optional part in your license: \"" + optionalText + "\"?", true);
    }

    private string GetName()
    {
        return AnsiConsole.Ask<string>("Enter your name for the license");
    }

    private string? GetSurname()
    {
        var surname = AnsiConsole.Ask<string?>("Enter your surname for the license (optional)", null);
        if (string.IsNullOrWhiteSpace(surname)) return null;
        return surname;
    }
}