using DotMake.CommandLine;
using Spectre.Console;

namespace Licensify.Commands;

[CliCommand(
    Description = "SPDX Client that can automatically manage LICENSE files."
)]
public class ListCommand(ILicenseDatabase database)
{
    public async Task RunAsync()
    {
        var manifest = await database.GetLicensesList();

        if (manifest is null)
        {
            AnsiConsole.Markup("ds");
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