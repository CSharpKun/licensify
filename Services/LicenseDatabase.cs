using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

namespace Licensify.Services;

public interface ILicenseDatabase
{
    public Task<T?> GetData<T>(string name, CancellationToken token = default) where T : class;
}

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class JsonLicenseDatabase(IHttpClientFactory httpFactory, JsonSerializerOptions options, CliGlobalSettings globalFlags, IConfigService settings) : ILicenseDatabase
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

    public async Task<T?> GetData<T>(string name, CancellationToken token = default) where T : class
    {
        var tName = typeof(T).Name + "_" + name;
        Dictionary<string, JsonElement>? cacheResult = [];

        if (!globalFlags.ForceNoCache && TryGetFromCache(out cacheResult) && (cacheResult?.TryGetValue(tName, out var json) ?? false))
        {
            var deserialized = json.Deserialize<T>(options);
            if (deserialized is not null)
            {
                if (globalFlags.Verbose) AnsiConsole.MarkupLine($"[grey]Using local copy of {tName}[/]");
                return deserialized;    
            }
            if (globalFlags.Verbose) AnsiConsole.MarkupLine($"[grey]Couldn't deserialize local copy of {tName}[/]");
        }

        var repo = settings.Settings["spdx.repo"] ?? "github";

        var url = repo == "github" && name != "licenses.json" ? "details/" + name : name;

        var fetchResult = await GetJsonRequest<T>(url, repo, token);
        if (fetchResult is null) return fetchResult;

        cacheResult ??= [];
        cacheResult[tName] = JsonSerializer.SerializeToElement(fetchResult, options);
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
            if (result is null && globalFlags.Verbose) AnsiConsole.MarkupLine("[grey]Couldn't parse cache JSON[/]");
            return true;
        }   
        catch (JsonException ex)
        {
            if (globalFlags.Verbose) 
            { 
                AnsiConsole.MarkupLine("[grey]Couldn't parse cache JSON[/]");
                AnsiConsole.WriteException(ex);
            }
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
            if (globalFlags.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Fetch to url {url} failed because of the timeout[/]");
                AnsiConsole.WriteException(ex);  
            } 
        }
        catch (HttpRequestException ex)
        {
            if (globalFlags.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch data from url {url}[/]");
                AnsiConsole.WriteException(ex); 
            }
        }
        catch (JsonException ex)
        {
            if (globalFlags.Verbose)
            {
                AnsiConsole.MarkupLine($"[red]Failed to deserialize data from url {url}[/]");
                AnsiConsole.WriteException(ex); 
            }
        }

        return default;
    }
}