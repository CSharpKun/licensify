using YamlDotNet.Serialization;

namespace Licensify.Services;

public class UserSettings
{
    public string? Name { get; set; } = null;
    public string? Surname { get; set; } = null;
}

public class SpdxSettings
{
    public string? Repo { get; set; } = "github";
}

public class RootSettings
{
    public UserSettings User { get; set; } = new();
    public SpdxSettings Spdx { get; set; } = new();
}

public interface ISettingsDatabase
{
    RootSettings Settings { get; }
    void ChangeSetting(string key, string value);
}

public class YamlSettingsDatabase : ISettingsDatabase
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    private static string ApplicationFolder { get; } =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "licensify"
    );

    private static string DatabaseFile { get; } =
    Path.Combine(
        ApplicationFolder,
        "settings.yaml"
    );

    public RootSettings Settings { get; set; }

    public YamlSettingsDatabase(IDeserializer deserializer, ISerializer serializer)
    {
        _deserializer = deserializer;
        _serializer = serializer;

        if (File.Exists(DatabaseFile))
        {
            var yaml = File.ReadAllText(DatabaseFile);
            Settings = _deserializer.Deserialize<RootSettings>(yaml);
            return;
        }
        Settings = new();
    }

    public void ChangeSetting(string key, string value)
    {
        var parts = key.Split('.');
        if (parts.Length != 2) return;

        switch (parts[0])
        {
            case "user":
                if (parts[1] == "name") Settings.User.Name = value;
                if (parts[1] == "surname") Settings.User.Surname = value;
                break;
            case "spdx":
                if (parts[1] == "repo") Settings.Spdx.Repo = value;
                break;
        }

        var yaml = _serializer.Serialize(Settings);
        File.WriteAllText(DatabaseFile, yaml);
    }
}