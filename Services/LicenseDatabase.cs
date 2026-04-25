using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Licensify;

public interface ILicenseDatabase
{
    Task<LicenseEntry?> GetLicense(string licenseId, CancellationToken token = default);
    Task<LicenseListManifest?> GetLicensesList(CancellationToken token = default);
}

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class JsonLicenseDatabase(HttpClient client, JsonSerializerOptions options, ILogger<JsonLicenseDatabase> logger) : ILicenseDatabase
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

    private bool IsErrorEnabled { get; } = logger.IsEnabled(LogLevel.Error);
    private const string SPDX_LICENSES_LIST = "licenses.json";

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
            logger.LogError(ex, "message");
        }

        if (!TryGetFromCache(LicensesCacheFile, out dict))
        {
            if (IsErrorEnabled) logger.LogError("d");
            dict = [];
        }

        if (license is not null)
        {
            dict?.Add(licenseId, license);
            WriteToCache(LicensesCacheFile, dict!);
            return license;
        }

        if ((dict?.TryGetValue(licenseId, out license) ?? false) && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Using local copy of license {License}", licenseId);
        }

        return license;
    }

    public async Task<LicenseListManifest?> GetLicensesList(CancellationToken token)
    {
        LicenseListManifest? manifest = default;

        try
        {
            manifest = await GetJsonRequest<LicenseListManifest>(SPDX_LICENSES_LIST, token);   
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "message");
        }

        if (manifest is not null)
        {
            WriteToCache(ManifestCacheFile, manifest);
            return manifest;
        }

        if (TryGetFromCache(ManifestCacheFile, out manifest) && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Using local copy of manifest: {Version}", manifest?.Version);
        }

        return manifest;
    }

    private bool TryGetFromCache<T>(string filePath, out T? result)
    {
        Directory.CreateDirectory(ApplicationFolder);
        var json = File.ReadAllText(filePath);

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options); 
            return true;
        }   
        catch (JsonException ex)
        {
            if (IsErrorEnabled) logger.LogError(ex, "message"); // todo
            result = default;
            return false;
        }   
    }

    private void WriteToCache(string filePath, object obj) => File.WriteAllText(filePath, JsonSerializer.Serialize(obj, options));

    private async Task<T> GetJsonRequest<T>(string url, CancellationToken token)
    {
        var response = await client.GetAsync(url, token);

        if (!response.IsSuccessStatusCode)
        {
            if (IsErrorEnabled) logger.LogError("Failed to fetch {Url}", url);
            throw new HttpRequestException();
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(token);
        var data = JsonSerializer.Deserialize<T>(responseJson, options);

        if (data is null)
        {
            if (IsErrorEnabled) logger.LogError("Failed to deserialize data from {Url} with contents {Contents}", url, responseJson);
            throw new JsonException();
        }

        return data;
    }
}