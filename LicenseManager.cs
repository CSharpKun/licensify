using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace Licensify;

public interface ILicenseManager
{
    Task<int> ListSPDXLicenses(CancellationToken token);
    Task<int> ShowLicense(string? licenseId, CancellationToken token);
}

public class LicenseManager(JsonSerializerOptions options, ILogger<LicenseManager> logger, HttpClient client) : ILicenseManager
{
    private bool IsErrorEnabled { get; set; } = logger.IsEnabled(LogLevel.Error);
    private const string SPDX_LICENSES_LIST = "licenses.json";

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<int> ListSPDXLicenses(CancellationToken token)
    {
        var response = await client.GetAsync(SPDX_LICENSES_LIST, token).Spinner(Spinner.Known.Dots);

        if (!response.IsSuccessStatusCode)
        {
            if (IsErrorEnabled) logger.LogError("Failed to fetch SPDX licenses from {BaseUri}{Uri}", client.BaseAddress, SPDX_LICENSES_LIST);
            return 1;
        }

        var json = await response.Content.ReadAsStringAsync(token).Spinner(Spinner.Known.Dots);
        var manifest = JsonSerializer.Deserialize<LicenseListManifest>(json, options);

        if (manifest is null)
        {
            if (IsErrorEnabled) logger.LogError("Failed to deserialize SPDX licenses manifest");
            return 1;
        }

        var table = new Table().RoundedBorder().ShowRowSeparators().Title("SPDX Licenses");
        
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

        return 0;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<int> ShowLicense(string? licenseId, CancellationToken token)
    {
        if (licenseId is null) return 1;

        var response = await client.GetAsync(licenseId + ".json", token).Spinner(Spinner.Known.Dots);
        
        if (!response.IsSuccessStatusCode)
        {
            if (IsErrorEnabled) logger.LogError("Failed to fetch license {LicenseId}", licenseId);
            return 1;
        }

        var json = await response.Content.ReadAsStringAsync(token).Spinner(Spinner.Known.Dots);
        var entry = JsonSerializer.Deserialize<LicenseEntry>(json, options);

        if (entry is null)
        {
            if (IsErrorEnabled) logger.LogError("Failed to deserialize license {LicenseId}", licenseId);
            return 1;
        }    

        var renderList = new List<IRenderable>
        {
            new Panel(entry.LicenseText)
            {
                Border = BoxBorder.Double
            },
            new Markup($"Deprecated License Id: {GetStatusColorTag(entry.IsDeprecatedLicenseId, reverse: true) + entry.IsDeprecatedLicenseId}[/]"),
            new Markup($"Osi Approved: {GetStatusColorTag(entry.IsOsiApproved) + entry.IsOsiApproved}[/]")
        };

        foreach (var reference in entry.CrossRef)
        {
            renderList.Add(new Panel(
                new Rows(
                    new Markup($"[bold]Reference URL:[/] [link]{reference.Url}[/]"),
                    new Markup($"[bold]Live:[/] {GetStatusColorTag(reference.IsLive) + reference.IsLive}[/]"),
                    new Markup($"[bold]Valid:[/] {GetStatusColorTag(reference.IsValid) + reference.IsValid}[/]"),
                    new Markup($"[bold]Match:[/] \"{reference.Match}\""),
                    new Markup($"[bold]Timestamp:[/] {reference.Timestamp}")
                )
            ));
        }

        var panel = new Panel(new Rows(renderList))
        {
            Header = new PanelHeader($"[bold]{entry.Name}[/] ({entry.LicenseId})"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);      

        return 0;
    } 
    private static string GetStatusColorTag(bool condition, bool reverse = false) => condition ^ reverse ? "[green]" : "[red]";
}
