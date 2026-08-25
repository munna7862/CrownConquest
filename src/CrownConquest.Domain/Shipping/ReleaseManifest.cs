using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrownConquest.Domain.Shipping;

public enum ReleaseBuildType
{
    Debug = 0,
    Release = 1,
    ReleaseCandidate = 2,
    Shipping = 3
}

public sealed record ReleaseFileEntry(
    string RelativePath,
    long SizeBytes,
    string Sha256Hash,
    string Component = "Core");

public sealed class ReleaseManifest
{
    public string ApplicationName { get; set; } = "Crown & Conquest";
    public string Version { get; set; } = "1.0.0";
    public string ReleaseChannel { get; set; } = "ReleaseCandidate";
    public string TargetPlatform { get; set; } = "win-x64";
    public string CommitHash { get; set; } = "HEAD";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ReleaseBuildType BuildType { get; set; } = ReleaseBuildType.ReleaseCandidate;
    public List<ReleaseFileEntry> Files { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string SerializeToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static ReleaseManifest? DeserializeFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<ReleaseManifest>(json, JsonOptions);
    }
}
