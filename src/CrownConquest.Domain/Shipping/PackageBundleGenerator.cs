using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.Shipping;

public sealed record PackageBundle(
    ReleaseManifest Manifest,
    IReadOnlyDictionary<string, byte[]> Files,
    long TotalSizeBytes);

public static class PackageBundleGenerator
{
    public static PackageBundle CreateBundle(
        string version,
        string releaseChannel,
        string targetPlatform,
        IReadOnlyDictionary<string, byte[]> files,
        string commitHash = "HEAD",
        ReleaseBuildType buildType = ReleaseBuildType.ReleaseCandidate)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(files);

        var manifest = new ReleaseManifest
        {
            Version = version,
            ReleaseChannel = releaseChannel,
            TargetPlatform = targetPlatform,
            CommitHash = commitHash,
            Timestamp = DateTimeOffset.UtcNow,
            BuildType = buildType
        };

        long totalSize = 0;
        foreach (var (path, payload) in files)
        {
            string hash = Sha256ChecksumValidator.ComputeSha256(payload);
            manifest.Files.Add(new ReleaseFileEntry(
                path.Replace('\\', '/'),
                payload.Length,
                hash));
            totalSize += payload.Length;
        }

        return new PackageBundle(manifest, files, totalSize);
    }

    public static byte[] ExportZipArchive(PackageBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Write manifest.json
            string manifestJson = bundle.Manifest.SerializeToJson();
            var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
            using (var stream = manifestEntry.Open())
            {
                byte[] manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
                stream.Write(manifestBytes, 0, manifestBytes.Length);
            }

            // Write SHA256SUMS.txt
            var sbChecksums = new StringBuilder();
            foreach (var file in bundle.Manifest.Files)
            {
                sbChecksums.AppendLine($"{file.Sha256Hash}  {file.RelativePath}");
            }
            var checksumEntry = archive.CreateEntry("SHA256SUMS.txt", CompressionLevel.Optimal);
            using (var stream = checksumEntry.Open())
            {
                byte[] checksumBytes = Encoding.UTF8.GetBytes(sbChecksums.ToString());
                stream.Write(checksumBytes, 0, checksumBytes.Length);
            }

            // Write bundle files
            foreach (var (relPath, bytes) in bundle.Files)
            {
                var entry = archive.CreateEntry(relPath.Replace('\\', '/'), CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        return memoryStream.ToArray();
    }
}
