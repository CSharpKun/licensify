using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Spectre.Console;

namespace Licensify.Services;

public interface ILicenseDatabase
{
    public Task<T?> GetData<T>(string url, string clientName = "github", CancellationToken token = default) where T : class;
}

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class JsonLicenseDatabase(IHttpClientFactory httpFactory, JsonSerializerOptions options, CliGlobalSettings settings) : ILicenseDatabase
{
    private static string ApplicationFolder { get; } = 
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "licensify"
    );

    private static string DatabaseFile { get; } = 
    Path.Combine(
        ApplicationFolder,
        "database.json"
    );

    // private const string SPDX_LICENSES_LIST = "licenses.json";

    public async Task<T?> GetData<T>(string url, string clientName = "github", CancellationToken token = default) where T : class
    {
        var tName = typeof(T).Name;
        Dictionary<string, object>? cacheResult = [];

        if (!settings.ForceNoCache && TryGetFromCache(out cacheResult) && cacheResult?[tName] is T)
        {
            if (settings.Verbose) AnsiConsole.MarkupLine($"[grey]Using local copy of {tName}[/]");
            return cacheResult[tName] as T;
        }

        var fetchResult = await GetJsonRequest<T>(url, clientName, token);
        if (fetchResult is null) return fetchResult;

        cacheResult ??= [];
        cacheResult[tName] = fetchResult;
        WriteToCache(cacheResult);
        return fetchResult;
    }

    private bool TryGetFromCache<T>(out T? result)
    {
        Directory.CreateDirectory(ApplicationFolder);
        var isFileOld = File.GetLastWriteTime(DatabaseFile) < DateTime.Now - TimeSpan.FromHours(10);
        if (!File.Exists(DatabaseFile) || isFileOld)
        {
            result = default;
            return false;
        } 

        var json = File.ReadAllText(DatabaseFile);

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options); 
            return true;
        }   
        catch (JsonException)
        {
            File.Delete(DatabaseFile);
            result = default;
            return false;
        }   
    }

    private void WriteToCache(object obj) => File.WriteAllText(DatabaseFile, JsonSerializer.Serialize(obj, options));

    private async Task<T?> GetJsonRequest<T>(string url, string clientName, CancellationToken token)
    {
        var client = httpFactory.CreateClient(clientName);
        
        try
        {    
            return await client.GetFromJsonAsync<T>(url, options, token);
        }
        catch (TaskCanceledException ex) when (!token.IsCancellationRequested)
        {
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Fetch to url {url} failed because of the timeout[/]");
                AnsiConsole.WriteException(ex);  
            } 
        }
        catch (JsonException ex)
        {
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Failed to deserialize data from url {url}[/]");
                AnsiConsole.WriteException(ex); 
            }
        }

        return default;
    }
}