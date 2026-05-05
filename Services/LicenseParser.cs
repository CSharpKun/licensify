using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Licensify.Services;

public interface ILicenseParser
{
    public Func<string, bool>? GetOptionalParts { get; set; }
    public Func<string>? GetName { get; set; }
    public Func<string?>? GetSurname { get; set; }
    public string Parse(string license);
}

public partial class LicenseParser(CliGlobalSettings globalFlags, IConfigService settings) : ILicenseParser
{
    public Func<string, bool>? GetOptionalParts { get; set; }
    public Func<string>? GetName { get; set; }
    public Func<string?>? GetSurname { get; set; }

    public string Parse(string license)
    {
        var localLicense = license;
        localLicense = OptionalParts().Replace(localLicense, ParseOptionalParts);
        localLicense = VariableParts().Replace(localLicense, ParseVariableParts);

        return localLicense;
    }

    private string ParseOptionalParts(Match match)
    {
        var optionalText = match.Groups[1].ToString();
        if (GetOptionalParts?.Invoke(optionalText) ?? true) return optionalText;
        return string.Empty;
    }

    private string ParseVariableParts(Match variableMatch)
    {
        var variable = variableMatch.Value;
        SpdxVariable? parsedVariable = new(variable, globalFlags);
        if (parsedVariable is null) return string.Empty;

        switch (parsedVariable.Name)
        {
            case "copyright":
                var name = settings.Settings["user.name"] ?? GetName?.Invoke() ?? throw new NullReferenceException();
                var surname = settings.Settings["user.surname"] ?? GetName?.Invoke() ?? "";
                var currentYear = DateTime.Now.Year;
                return string.Concat("Copyright (c) ", currentYear, " ", name, " ", surname, "  ");
        }

        return string.Empty;
    }

    [GeneratedRegex(@"<<beginOptional>>([\s\S]*?)<<endOptional>>", RegexOptions.IgnoreCase)]
    private static partial Regex OptionalParts();

    [GeneratedRegex(@"<<var.*?>>", RegexOptions.IgnoreCase)]
    private static partial Regex VariableParts();
}

public partial class SpdxVariable : INullable
{
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public SpdxVariable(string variable, CliGlobalSettings settings)
    {
        try
        {
            Name = CheckAndValidate(variable, NamePart());
            Original = CheckAndValidate(variable, OriginalPart());
            MatchScheme = new Regex(CheckAndValidate(variable, MatchPart()));
            Example = CheckAndValidate(variable, ExamplePart(), false);
        }
        catch (ArgumentException ex)
        {
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine("[red]Couldn't parse Spdx Variable[/]");
                AnsiConsole.WriteException(ex);
            }
            IsNull = true;
        }
    }

    private static string CheckAndValidate(string text, Regex regex, bool important = true)
    {
        var match = regex.Match(text);
        var isNotFound = !match.Success && important;
        if (isNotFound) throw new ArgumentException($"Variable did not contain '{regex.GetType().Name}' value", nameof(text));
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    public string Name { get; } = null!;
    public string Original { get; } = null!;
    public Regex MatchScheme { get; } = null!;
    public string? Example { get; }
    public bool IsNull { get; } = false;

    [GeneratedRegex(@"name=""(.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex NamePart();

    [GeneratedRegex(@"original=""(.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex OriginalPart();

    [GeneratedRegex(@"match=""(.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex MatchPart();

    [GeneratedRegex(@"example=""(.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex ExamplePart();
}