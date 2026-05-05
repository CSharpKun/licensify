using System.Text.Json;
using System.Text.Json.Serialization;
using Licensify.Services;
using YamlDotNet.Serialization;

namespace Licensify;

public record CliGlobalSettings(
    bool Verbose,
    bool ForceNoCache
);

public record LicenseListManifest(
    [property: JsonPropertyName("licenseListVersion")] string Version,
    IReadOnlyList<LicenseListEntry> Licenses
);

public record LicenseListEntry(
    string Reference,
    bool IsDeprecatedLicenseId,
    string DetailsUrl,
    int ReferenceNumber,
    string Name,
    string LicenseId,
    IReadOnlyList<string> SeeAlso,
    bool IsOsiApproved,
    bool IsFsfLibre
);

public record LicenseEntry(
    bool IsDeprecatedLicenseId,
    string LicenseText,
    string StandardLicenseTemplate,
    string Name,
    string LicenseId,
    IReadOnlyList<CrossRef> CrossRef,
    IReadOnlyList<string> SeeAlso,
    bool IsOsiApproved,
    string LicenseTextHtml
);

public record CrossRef(
    string Match,
    string Url,
    bool IsValid,
    bool IsLive,
    DateTime Timestamp,
    bool IsWayBackLink,
    int Order
);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LicenseListManifest))]
[JsonSerializable(typeof(LicenseListEntry))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(LicenseEntry))]
[JsonSerializable(typeof(CrossRef))]
public partial class LicensifyJsonSerializerContext : JsonSerializerContext;

[YamlStaticContext]
[YamlSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
public partial class LicensifyYamlContext : StaticContext;
