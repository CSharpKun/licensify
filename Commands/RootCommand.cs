using DotMake.CommandLine;

namespace Licensify.Commands;

[CliCommand(
    Children = [typeof(ListCommand), typeof(ShowCommand)], 
    Description = "SPDX Client that can automatically manage LICENSE files for any projects."
)]
public class RootCommand
{
    [CliOption(Description = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [CliOption(Description = "Force download and update for operation")]
    public bool NoCache { get; set; }

    /*
    [CliOption(
        Description = "Custom SPDX repository URL [github, spdx, (your url)]",
        ValidationPattern = @"^(?:github|spdx|(?:http|https):\/\/[^\s]+)$", ValidationMessage = "You must enter either Url, 'spdx' or 'github'"
    )] 
    public string SpdxRepo { get; set; } = "github"; */
}