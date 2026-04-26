using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Spectre.Console;

namespace Licensify.Services;

public interface ILicenseDatabase
{
    Task<LicenseEntry?> GetLicense(string licenseId, CancellationToken token = default);
    Task<LicenseListManifest?> GetLicensesList(CancellationToken token = default);
}

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class JsonLicenseDatabase(IHttpClientFactory httpFactory, JsonSerializerOptions options, CliGlobalSettings settings) : ILicenseDatabase
{
    private static string ApplicationFolder { get; } = 
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Assembly.GetEntryAssembly()?.GetName()?.Name ?? "licensify"
    );

    private static string LicensesCacheFile { get; } =
        Path.Combine(
            ApplicationFolder,
            "licensesCache.json"
        );

    private static string ManifestCacheFile { get; } =
        Path.Combine(
           ApplicationFolder,
            "manifestCache.json"
        );

    private const string SPDX_LICENSES_LIST = "licenses.json";

    public async Task<LicenseEntry?> GetLicense(string licenseId, CancellationToken token = default)
    {
        if (!TryGetFromCache(LicensesCacheFile, out Dictionary<string, LicenseEntry>? dict)) dict = [];

        LicenseEntry? license = null;

        var gotFromDict = dict?.TryGetValue(licenseId, out license) ?? false;

        if (gotFromDict && !settings.ForceNoCache)
        {
            if (settings.Verbose) AnsiConsole.MarkupLine($"[grey]Using local copy of the license {licenseId}[/]");
            return license;
        } 

        try
        {
            license = await GetJsonRequest<LicenseEntry>(licenseId + ".json", "github", token);   
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.WriteException(ex);
        }

        if (license is not null && (dict?.TryGetValue(licenseId, out var _) ?? false))
        {
            dict[licenseId] = license;
            WriteToCache(LicensesCacheFile, dict!);
        }

        return license;
    }

    public async Task<LicenseListManifest?> GetLicensesList(CancellationToken token)
    {
        var gotFromCache = TryGetFromCache(ManifestCacheFile, out LicenseListManifest? manifest);

        if (gotFromCache && !settings.ForceNoCache) 
        {
            if (settings.Verbose) AnsiConsole.MarkupLine("[grey]Using a local copy of the license list[/]");
            return manifest;
        }
        
        manifest = await GetJsonRequest<LicenseListManifest>(SPDX_LICENSES_LIST, "github", token);   

        if (manifest is not null) WriteToCache(ManifestCacheFile, manifest);
        
        return manifest;
    }

    private bool TryGetFromCache<T>(string filePath, out T? result)
    {
        Directory.CreateDirectory(ApplicationFolder);
        var isFileOld = File.GetLastWriteTime(filePath) < DateTime.Now - TimeSpan.FromHours(10);
        if (!File.Exists(filePath) || isFileOld)
        {
            result = default;
            return false;
        } 

        var json = File.ReadAllText(filePath);

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options); 
            return true;
        }   
        catch (JsonException)
        {
            File.Delete(filePath);
            result = default;
            return false;
        }   
    }

    private void WriteToCache(string filePath, object obj) => File.WriteAllText(filePath, JsonSerializer.Serialize(obj, options));

    private async Task<T?> GetJsonRequest<T>(string url, string clientName, CancellationToken token)
    {
        HttpResponseMessage? response = null;
        var client = httpFactory.CreateClient(clientName);
        try
        {
            response = await client.GetAsync(url, token);    
        }
        catch (TaskCanceledException ex) when (!token.IsCancellationRequested)
        {
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Fetch to url {url} failed because of the timeout[/]");
                AnsiConsole.WriteException(ex);  
            } 
            return default;
        }

        if (response is null) return default;

        if (!response.IsSuccessStatusCode)
        {
            if (settings.Verbose) AnsiConsole.MarkupLine("[red]Failed to fetch {Url}[/]", url);
            return default;
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(token);
        var data = JsonSerializer.Deserialize<T>(responseJson, options);

        if (data is null && settings.Verbose) AnsiConsole.MarkupLine("[red]Failed to deserialize data from {Url} with contents {Contents}[/]", url, responseJson);

        return data;
    }
}