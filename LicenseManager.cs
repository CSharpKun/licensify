using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Licensify;

public interface ILicenseManager
{
    Task<int> ListSPDXLicenses(CancellationToken token);
    Task<int> ShowLicense(string? licenseId, CancellationToken token);
}

public class LicenseManager(JsonSerializerOptions options, ILogger<LicenseManager> logger, HttpClient client) : ILicenseManager
{
    private const string SPDX_LICENSES_LIST = "licenses.json";

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<int> ListSPDXLicenses(CancellationToken token)
    {
        var response = await client.GetAsync(SPDX_LICENSES_LIST, token);
        var islogErrorEnabled = logger.IsEnabled(LogLevel.Error);

        if (!response.IsSuccessStatusCode)
        {
            if (islogErrorEnabled) logger.LogError("Failed to fetch SPDX licenses from {BaseUri}{Uri}", client.BaseAddress, SPDX_LICENSES_LIST);
            return 1;
        }

        var json = await response.Content.ReadAsStringAsync(token);
        var manifest = JsonSerializer.Deserialize<LicenseListManifest>(json, options);

        if (manifest is null)
        {
            if (islogErrorEnabled) logger.LogError("Failed to deserialize SPDX licenses manifest");
            return 1;
        }

        var table = new Table().RoundedBorder().Title("SPDX Licenses");
        
        var tableData = manifest.Licenses
            .Where(license => license.IsDeprecated is false)
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

    public async Task<int> ShowLicense(string? licenseId, CancellationToken token)
    {
        if (licenseId is null) return 1;
        return 0;
    }
}
