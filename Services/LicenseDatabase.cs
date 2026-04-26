using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Licensify.Commands;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Licensify.Services;

public interface ILicenseDatabase
{
    Task<LicenseEntry?> GetLicense(string licenseId, CancellationToken token = default);
    Task<LicenseListManifest?> GetLicensesList(CancellationToken token = default);
}

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class JsonLicenseDatabase(IHttpClientFactory httpFactory, JsonSerializerOptions options, RootCommand root) : ILicenseDatabase
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
    private readonly HttpClient _client = httpFactory.CreateClient("spdx");
    private readonly bool _verbose = true; //root.Verbose;

    public async Task<LicenseEntry?> GetLicense(string licenseId, CancellationToken token = default)
    {
        Dictionary<string, LicenseEntry>? dict = [];

        LicenseEntry? license = default;

        try
        {
            license = await GetJsonRequest<LicenseEntry>(licenseId + ".json", token);   
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.WriteException(ex);
        }

        if (!TryGetFromCache(LicensesCacheFile, out dict))
        {
            dict = [];
        }

        if (license is not null)
        {
            dict?.Add(licenseId, license);
            WriteToCache(LicensesCacheFile, dict!);
            return license;
        }

        var gotFromDict = dict?.TryGetValue(licenseId, out license) ?? false;

        if (gotFromDict && _verbose) AnsiConsole.Markup("[grey]Using local copy of the license {License}[/]", licenseId);

        return license;
    }

    public async Task<LicenseListManifest?> GetLicensesList(CancellationToken token)
    {
        var manifest = await GetJsonRequest<LicenseListManifest>(SPDX_LICENSES_LIST, token);   

        if (manifest is not null)
        {
            WriteToCache(ManifestCacheFile, manifest);
            return manifest;
        }

        var gotFromCache = TryGetFromCache(ManifestCacheFile, out manifest);

        if (gotFromCache && _verbose) AnsiConsole.Markup("[grey]Using local copy of the licenses' list[/]");

        return manifest;
    }

    private bool TryGetFromCache<T>(string filePath, out T? result)
    {
        Directory.CreateDirectory(ApplicationFolder);
        if (!File.Exists(filePath))
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

    private async Task<T?> GetJsonRequest<T>(string url, CancellationToken token)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await _client.GetAsync(url);    
        }
        catch (TaskCanceledException ex) when (!token.IsCancellationRequested)
        {
            if (_verbose)
            {
                AnsiConsole.Markup($"[red]Fetch to url {url} failed because of the timeout[/]");
                AnsiConsole.WriteException(ex);  
            } 
            return default;
        }

        if (response is null) return default;

        if (!response.IsSuccessStatusCode)
        {
            if (_verbose) AnsiConsole.Markup("[red]Failed to fetch {Url}[/]", url);
            return default;
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(token);
        var data = JsonSerializer.Deserialize<T>(responseJson, options);

        if (data is null && _verbose) AnsiConsole.Markup("[red]Failed to deserialize data from {Url} with contents {Contents}[/]", url, responseJson);

        return data;
    }
}