using DotMake.CommandLine;
using Licensify.Services;
using Spectre.Console;

namespace Licensify.Commands;

[CliCommand(
    Description = "Lists all licenses in a table."
)]
public class ListCommand(ILicenseDatabase database, CliGlobalSettings settings)
{
    public async Task RunAsync()
    {
        var manifest = await database.GetData<LicenseListManifest>("licenses.json", settings.SpdxRepo);

        if (manifest is null)
        {
            AnsiConsole.Markup("[bold red]Couldn't get list of licenses. Check your internet connection.[/]");
            return;
        }

        var table = new Table().RoundedBorder().Title("SPDX Licenses");
        
        var tableData = manifest.Licenses
            .Where(license => license.IsDeprecatedLicenseId is false)
            .OrderBy(license => license.Name);

        table.AddColumns(
            new TableColumn(nameof(LicenseListEntry.Name)), 
            new TableColumn(nameof(LicenseListEntry.LicenseId)).NoWrap(), 
            new TableColumn(nameof(LicenseListEntry.DetailsUrl)).NoWrap()
        );

        foreach (var entry in tableData)
        {
            table.AddRow(
                entry.Name,
                entry.LicenseId,
                entry.DetailsUrl.Replace(".json", ".html")
            );
        }

        AnsiConsole.Write(table);
    }
}