using DotMake.CommandLine;
using Licensify.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Licensify.Commands;

[CliCommand(
    Description = "Shows information about specified license.",
    Alias = "get"
)]
public class ShowCommand(ILicenseDatabase database)
{
    [CliArgument(Description = "License's short id.", Required = true)]
    public string LicenseId { get; set; } = null!;

    public async Task RunAsync()
    {
        var entry = await database.GetData<LicenseEntry>(LicenseId + ".json");  

        if (entry is null)
        {
            AnsiConsole.MarkupLine($"[bold red]Couldn't get {LicenseId} license. Check your internet connection.[/]");
            return;
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
    }

    private static string GetStatusColorTag(bool condition, bool reverse = false) => condition ^ reverse ? "[green]" : "[red]";
}