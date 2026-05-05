using Spectre.Console;
using YamlDotNet.Serialization;

namespace Licensify.Services;

public interface IConfigService
{
    Dictionary<string, string> Settings { get; }
    void UpdateSettings();
}

public class YamlConfigService : IConfigService
{
    public Dictionary<string, string> Settings { get; set; } = [];

    private static string ApplicationFolder { get; } =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "licensify"
    );

    private static string ConfigFile { get; } =
    Path.Combine(
        ApplicationFolder,
        "settings.yaml"
    );

    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;
    private readonly CliGlobalSettings _globalConfig;

    public YamlConfigService(IDeserializer deserializer, ISerializer serializer, CliGlobalSettings globalConfig)
    {
        _deserializer = deserializer;
        _serializer = serializer;
        _globalConfig = globalConfig;

        if (!File.Exists(ConfigFile))
        {
            Settings = [];
            InitializeSettings();
            return;
        }

        var yaml = File.ReadAllText(ConfigFile);
        var settings = _deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml) ?? [];
        foreach (var outerKwp in settings) foreach (var innerKwp in outerKwp.Value) 
            {
                var keys = outerKwp.Key.Split(' ');
                string? finalKey;
                if (keys.Length is 1) finalKey = keys[0] + "." + innerKwp.Key;
                else if (keys.Length is 2)
                {
                    var rawSubgroup = keys[1];
                    var start = rawSubgroup.IndexOf('"');
                    var end = rawSubgroup.LastIndexOf('"');
                    var subgroup = rawSubgroup[(start+1)..end];
                    finalKey = keys[0] + '.' + subgroup + '.' + innerKwp.Key;
                }
                else
                {
                    if (_globalConfig.Verbose) AnsiConsole.MarkupLine(
                        $"[b, red]Couldn't parse Key-Value Pair because of incorrect formatting: array's length is {keys.Length}");
                    continue;
                }
                Settings[finalKey] = innerKwp.Value;
            }
    }

    public void UpdateSettings()
    {
        Dictionary<string, Dictionary<string, string>> saveDict = [];
        foreach (var kwp in Settings)
        {
            var keys = kwp.Key.Split('.');
            
            string outerKey;
            string innerKey;
            
            if (keys.Length is 2) 
            {
                outerKey = keys[0];
                innerKey = keys[1];
            }
            else if (keys.Length is 3) 
            {
                outerKey = keys[0] + $" \"{keys[1]}\"";
                innerKey = keys[2];
            } 
            else 
            {
                if (_globalConfig.Verbose) AnsiConsole.MarkupLine(
                    $"[b, red]Couldn't parse Key-Value Pair because of incorrect formatting: array's length is {keys.Length}");
                continue;
            } 
            
            if (!saveDict.ContainsKey(outerKey)) saveDict[outerKey] = []; 

            saveDict[outerKey][innerKey] = kwp.Value;
        }
        File.WriteAllText(ConfigFile, _serializer.Serialize(saveDict));
    }

    private static void InitializeSettings() => File.WriteAllText(ConfigFile, string.Empty);
}