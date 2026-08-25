using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Shipping;

public sealed record ChecksumMismatch(
    string FilePath,
    string ExpectedHash,
    string ActualHash,
    string Reason);

public sealed record ChecksumVerificationResult(
    bool IsValid,
    int TotalFilesChecked,
    int MatchingFiles,
    IReadOnlyList<ChecksumMismatch> Mismatches)
{
    public static ChecksumVerificationResult Success(int totalFiles) =>
        new(true, totalFiles, totalFiles, Array.Empty<ChecksumMismatch>());

    public static ChecksumVerificationResult Failure(int totalFiles, int matching, IReadOnlyList<ChecksumMismatch> mismatches) =>
        new(false, totalFiles, matching, mismatches);
}

public static class Sha256ChecksumValidator
{
    public static string ComputeSha256(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeSha256(ReadOnlySpan<byte> data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeSha256(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return ComputeSha256(bytes);
    }

    public static string ComputeSha256(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static ChecksumVerificationResult VerifyManifest(
        ReleaseManifest manifest,
        IReadOnlyDictionary<string, byte[]> filePayloads)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(filePayloads);

        var mismatches = new List<ChecksumMismatch>();
        int matched = 0;

        foreach (var entry in manifest.Files)
        {
            if (!filePayloads.TryGetValue(entry.RelativePath, out var payload))
            {
                mismatches.Add(new ChecksumMismatch(
                    entry.RelativePath,
                    entry.Sha256Hash,
                    string.Empty,
                    "File missing from distribution payload"));
                continue;
            }

            if (payload.Length != entry.SizeBytes)
            {
                string computedHash = ComputeSha256(payload);
                mismatches.Add(new ChecksumMismatch(
                    entry.RelativePath,
                    entry.Sha256Hash,
                    computedHash,
                    $"Size mismatch: Expected {entry.SizeBytes} bytes, Actual {payload.Length} bytes"));
                continue;
            }

            string actualHash = ComputeSha256(payload);
            if (!string.Equals(entry.Sha256Hash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add(new ChecksumMismatch(
                    entry.RelativePath,
                    entry.Sha256Hash,
                    actualHash,
                    "SHA-256 hash mismatch"));
            }
            else
            {
                matched++;
            }
        }

        bool isValid = mismatches.Count == 0 && manifest.Files.Count > 0;
        return isValid
            ? ChecksumVerificationResult.Success(manifest.Files.Count)
            : ChecksumVerificationResult.Failure(manifest.Files.Count, matched, mismatches);
    }
}
