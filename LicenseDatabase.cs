using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;

namespace Licensify;

public interface ILicenseDatabase
{
    Task<LicenseEntry> GetLicense(string licenseId, CancellationToken token);
    Task<LicenseListManifest> GetLicensesList(CancellationToken token);
}

public class JsonLicenseDatabase(HttpClient client, JsonSerializerOptions options, ILogger<JsonLicenseDatabase> logger) : ILicenseDatabase
{
    private static string ApplicationFolder { get; } = 
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Assembly.GetEntryAssembly()?.GetName()?.Name ?? "licensify"
    );

    private static FileInfo CacheFile { get; } = new(
        Path.Combine(
            ApplicationFolder,
            "cache.json"
        )
    );

    private bool IsErrorEnabled { get; } = logger.IsEnabled(LogLevel.Error);

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
    [UnconditionalSuppressMessage("AOT", "IL3050")]
    public async Task<LicenseEntry> GetLicense(string licenseId, CancellationToken token)
    {
        Directory.CreateDirectory(ApplicationFolder);
        using var file = CacheFile.Open(FileMode.OpenOrCreate);

        Dictionary<string, LicenseEntry> dict = [];

        try
        {
            dict = await JsonSerializer.DeserializeAsync<Dictionary<string, LicenseEntry>>(file, options, token) ?? []; 
        }   
        catch (JsonException ex)
        {
            if (IsErrorEnabled) logger.LogError(ex, "message"); // todo
        }

        if (dict.TryGetValue(licenseId, out var localLicense)) 
        {
            if (logger.IsEnabled(LogLevel.Information)) logger.LogInformation("Used local copy of license {LicenseId}", licenseId);
            return localLicense; 
        }   

        var response = await client.GetAsync(licenseId + ".json", token);

        if (!response.IsSuccessStatusCode)
        {
            if (IsErrorEnabled) logger.LogError("Failed to fetch license {LicenseId}", licenseId);
            throw new HttpRequestException();
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(token);
        var remoteLicense = JsonSerializer.Deserialize<LicenseEntry>(responseJson, options);

        if (remoteLicense is null)
        {
            if (IsErrorEnabled) logger.LogError("Failed to deserialize license {LicenseId}", licenseId);
            throw new JsonException();
        }

        dict.Add(licenseId, remoteLicense);
        var encodedFileJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dict, options));

        file.SetLength(0);
        file.Write(encodedFileJson, 0, encodedFileJson.Length);   

        return remoteLicense;
    }

    public async Task<LicenseListManifest> GetLicensesList(CancellationToken token)
    {
        return new(null, null);
    }
}