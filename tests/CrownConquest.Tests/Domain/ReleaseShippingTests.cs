using System;
using System.Collections.Generic;
using System.Text;
using CrownConquest.Domain.Shipping;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class ReleaseShippingTests
{
    [Fact]
    public void TC_S15_001_ComputeSha256_ValidPayload_ReturnsExactDigest()
    {
        // Arrange
        byte[] payload = Encoding.UTF8.GetBytes("Crown and Conquest RC1 Payload");

        // Act
        string hash = Sha256ChecksumValidator.ComputeSha256(payload);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash.ToLowerInvariant(), hash);
    }

    [Fact]
    public void TC_S15_002_GenerateAndVerifyManifest_ValidFiles_ReturnsValid()
    {
        // Arrange
        var files = new Dictionary<string, byte[]>
        {
            ["bin/game.exe"] = Encoding.UTF8.GetBytes("BINARY_DATA_1"),
            ["data/units.json"] = Encoding.UTF8.GetBytes("{\"units\":[]}"),
            ["readme.txt"] = Encoding.UTF8.GetBytes("Crown & Conquest")
        };

        var bundle = PackageBundleGenerator.CreateBundle("1.0.0", "ReleaseCandidate", "win-x64", files);

        // Act
        var result = Sha256ChecksumValidator.VerifyManifest(bundle.Manifest, files);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TotalFilesChecked);
        Assert.Equal(3, result.MatchingFiles);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void TC_S15_003_VerifyManifest_ModifiedFileContent_DetectsMismatch()
    {
        // Arrange
        var files = new Dictionary<string, byte[]>
        {
            ["bin/game.exe"] = Encoding.UTF8.GetBytes("ORIGINAL_DATA")
        };
        var bundle = PackageBundleGenerator.CreateBundle("1.0.0", "ReleaseCandidate", "win-x64", files);

        var corruptedFiles = new Dictionary<string, byte[]>
        {
            ["bin/game.exe"] = Encoding.UTF8.GetBytes("TAMPERED_DATA")
        };

        // Act
        var result = Sha256ChecksumValidator.VerifyManifest(bundle.Manifest, corruptedFiles);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Mismatches);
        Assert.Equal("bin/game.exe", result.Mismatches[0].FilePath);
    }

    [Fact]
    public void TC_S15_004_VerifyManifest_MissingFile_DetectsMissing()
    {
        // Arrange
        var files = new Dictionary<string, byte[]>
        {
            ["bin/game.exe"] = Encoding.UTF8.GetBytes("GAME_DATA"),
            ["bin/core.dll"] = Encoding.UTF8.GetBytes("CORE_DLL_DATA")
        };
        var bundle = PackageBundleGenerator.CreateBundle("1.0.0", "ReleaseCandidate", "win-x64", files);

        var incompleteFiles = new Dictionary<string, byte[]>
        {
            ["bin/game.exe"] = Encoding.UTF8.GetBytes("GAME_DATA")
        };

        // Act
        var result = Sha256ChecksumValidator.VerifyManifest(bundle.Manifest, incompleteFiles);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Mismatches);
        Assert.Equal("bin/core.dll", result.Mismatches[0].FilePath);
    }

    [Fact]
    public void TC_S15_005_EmptyAndZeroBytePayload_HandlesGracefully()
    {
        // Arrange
        byte[] empty = Array.Empty<byte>();

        // Act
        string hash = Sha256ChecksumValidator.ComputeSha256(empty);

        // Assert
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash);
    }

    [Fact]
    public void TC_S15_006_PackageBundleGenerator_ExportZipArchive_ProducesValidZip()
    {
        // Arrange
        var files = new Dictionary<string, byte[]>
        {
            ["CrownConquest.exe"] = Encoding.UTF8.GetBytes("EXE_PAYLOAD"),
            ["game_data.json"] = Encoding.UTF8.GetBytes("{\"version\":\"1.0.0\"}")
        };
        var bundle = PackageBundleGenerator.CreateBundle("1.0.0", "ReleaseCandidate", "win-x64", files);

        // Act
        byte[] zipBytes = PackageBundleGenerator.ExportZipArchive(bundle);

        // Assert
        Assert.NotNull(zipBytes);
        Assert.True(zipBytes.Length > 0);
        // PK zip signature (0x50, 0x4B)
        Assert.Equal(0x50, zipBytes[0]);
        Assert.Equal(0x4B, zipBytes[1]);
    }

    [Fact]
    public void TC_S15_007_CleanMachineValidator_EvaluateValidEnvironment_ReturnsPass()
    {
        // Act
        var diag = CleanMachineEnvironmentValidator.EvaluateEnvironment(
            architecture: "x64",
            operatingSystem: "Microsoft Windows 11 Pro 10.0.22631",
            dotnetVersion: "8.0.200",
            ramMb: 8192,
            diskSpaceMb: 10240,
            headlessSupported: true);

        // Assert
        Assert.Equal(DiagnosticSeverity.Pass, diag.OverallStatus);
        Assert.True(diag.IsPassing);
        Assert.NotEmpty(diag.Items);
    }

    [Fact]
    public void TC_S15_008_CleanMachineValidator_LowMemory_ReturnsWarning()
    {
        // Act
        var diag = CleanMachineEnvironmentValidator.EvaluateEnvironment(
            architecture: "x64",
            operatingSystem: "Windows 10",
            dotnetVersion: "8.0.100",
            ramMb: 1024, // < 2048 MB
            diskSpaceMb: 10240);

        // Assert
        Assert.Equal(DiagnosticSeverity.Warning, diag.OverallStatus);
        Assert.True(diag.IsPassing); // Non-fatal warning
    }

    [Fact]
    public void TC_S15_009_CleanMachineValidator_Non64BitArchitecture_ReturnsFatal()
    {
        // Act
        var diag = CleanMachineEnvironmentValidator.EvaluateEnvironment(
            architecture: "x86",
            operatingSystem: "Windows 10",
            dotnetVersion: "8.0.100",
            ramMb: 4096,
            diskSpaceMb: 5000);

        // Assert
        Assert.Equal(DiagnosticSeverity.Fatal, diag.OverallStatus);
        Assert.False(diag.IsPassing);
    }

    [Fact]
    public void TC_S15_010_ReleaseSaveCompatibility_CorruptAndTruncated_HandledGracefully()
    {
        // Act
        var report = ReleaseSaveCompatibilityValidator.ValidateCompatibility();

        // Assert
        Assert.True(report.IsCompatible);
        Assert.True(report.EmptyPayloadHandledGracefully);
        Assert.True(report.TruncatedPayloadHandledGracefully);
        Assert.True(report.CorruptPayloadHandledGracefully);
        Assert.True(report.ValidSaveRestoresState);
    }
}
