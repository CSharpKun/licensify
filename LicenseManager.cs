using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Rendering;

namespace Licensify;

public interface ILicenseManager
{
    Task<int> ListLicenses(CancellationToken token);
    Task<int> ShowLicense(string? licenseId, CancellationToken token);
    Task<int> AddLicense(string? licenseId, string? repoPath, CancellationToken token);
}

public class LicenseManager(JsonSerializerOptions options, ILogger<LicenseManager> logger, ILicenseDatabase database, HttpClient client) : ILicenseManager
{
    private bool IsErrorEnabled { get; } = logger.IsEnabled(LogLevel.Error);
    

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<int> ListLicenses(CancellationToken token)
    {
        var manifest = await database.GetLicensesList(token);

        if (manifest is null)
        {
            if (IsErrorEnabled) logger.LogError("ds");
            AnsiConsole.Markup("ds");
            return 1;
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

        return 0;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<int> ShowLicense(string? licenseId, CancellationToken token)
    {
        if (licenseId is null) return 1;

        LicenseEntry? entry = null;

        try
        {
            entry = await database.GetLicense(licenseId, token);  
        } 
        catch (HttpRequestException)
        {
            AnsiConsole.Markup($"Couldn't find license");
            return 1;
        }
        catch (JsonException)
        {
            AnsiConsole.Markup($"Couldn't parse license");
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "message"); // todo
            AnsiConsole.WriteException(ex);
            return 1;
        }

        if (entry is null)
        {
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

    public async Task<int> AddLicense(string? licenseId, string? repoPath, CancellationToken token)
    {
        
        return 0;
    }

    private static string GetStatusColorTag(bool condition, bool reverse = false) => condition ^ reverse ? "[green]" : "[red]";
}
