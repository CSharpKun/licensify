using DotMake.CommandLine;

namespace Licensify.Commands;

[CliCommand(
    Children = [typeof(ListCommand), typeof(ShowCommand)], 
    Description = "SPDX Client that can automatically manage LICENSE files."
)]
public class RootCommand {}