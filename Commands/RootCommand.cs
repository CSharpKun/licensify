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
}