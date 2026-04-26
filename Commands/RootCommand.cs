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

    [CliOption(
        Description = "Custom SPDX repository URL",
        Group = "Repository"
    )]
    public Uri? CustomRepo { get; set; }

    [CliOption(
        Description = "Use SPDX official repo",
        Group = "Repository"
    )]
    public bool UseSPDX { get; set; }
}