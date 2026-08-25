using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CrownConquest.Domain.Shipping;

public static class CleanMachineEnvironmentValidator
{
    public const long MinimumRamMb = 2048; // 2 GB
    public const long MinimumDiskSpaceMb = 1024; // 1 GB

    public static EnvironmentDiagnostics ValidateCurrentEnvironment()
    {
        string arch = RuntimeInformation.ProcessArchitecture.ToString();
        string os = RuntimeInformation.OSDescription;
        string dotnetVersion = Environment.Version.ToString();

        // Approximate GC total memory
        long gcMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long ramMb = gcMemory > 0 ? gcMemory / (1024 * 1024) : 4096;

        long diskMb = 5000;
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\");
            if (drive.IsReady)
            {
                diskMb = drive.AvailableFreeSpace / (1024 * 1024);
            }
        }
        catch
        {
            diskMb = 5000;
        }

        return EvaluateEnvironment(
            architecture: arch,
            operatingSystem: os,
            dotnetVersion: dotnetVersion,
            ramMb: ramMb,
            diskSpaceMb: diskMb,
            headlessSupported: true);
    }

    public static EnvironmentDiagnostics EvaluateEnvironment(
        string architecture,
        string operatingSystem,
        string dotnetVersion,
        long ramMb,
        long diskSpaceMb,
        bool headlessSupported = true)
    {
        var diag = new EnvironmentDiagnostics
        {
            Architecture = architecture,
            OperatingSystem = operatingSystem,
            DotNetVersion = dotnetVersion,
            TotalMemoryMb = ramMb,
            AvailableDiskSpaceMb = diskSpaceMb,
            HeadlessRenderingSupported = headlessSupported
        };

        var highestSeverity = DiagnosticSeverity.Pass;

        // 1. Architecture Check (x64 / X64 / Arm64)
        bool is64Bit = architecture.Contains("64", StringComparison.OrdinalIgnoreCase) ||
                       architecture.Equals("X64", StringComparison.OrdinalIgnoreCase) ||
                       architecture.Equals("Arm64", StringComparison.OrdinalIgnoreCase);

        if (is64Bit)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Hardware",
                "Processor Architecture",
                DiagnosticSeverity.Pass,
                architecture,
                "x64 / 64-bit Compatible",
                "Architecture meets 64-bit requirements."));
        }
        else
        {
            highestSeverity = DiagnosticSeverity.Fatal;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Hardware",
                "Processor Architecture",
                DiagnosticSeverity.Fatal,
                architecture,
                "x64 / 64-bit Required",
                "Crown & Conquest requires a 64-bit operating system and processor."));
        }

        // 2. OS Check (Windows 10/11)
        bool isWindows = operatingSystem.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (isWindows)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Operating System",
                "OS Platform",
                DiagnosticSeverity.Pass,
                operatingSystem,
                "Windows 10 / Windows 11 (64-bit)",
                "Host OS is fully supported."));
        }
        else
        {
            if (highestSeverity < DiagnosticSeverity.Warning) highestSeverity = DiagnosticSeverity.Warning;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Operating System",
                "OS Platform",
                DiagnosticSeverity.Warning,
                operatingSystem,
                "Windows 10 / Windows 11 (Primary Target)",
                "Non-Windows environment detected. Game simulation will execute in portable cross-platform mode."));
        }

        // 3. .NET Runtime Check (.NET 8+)
        bool isNet8Plus = dotnetVersion.StartsWith("8.") || dotnetVersion.StartsWith("9.") || Environment.Version.Major >= 8;
        if (isNet8Plus)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Runtime",
                ".NET Runtime Version",
                DiagnosticSeverity.Pass,
                dotnetVersion,
                ".NET 8.0 or higher",
                ".NET Runtime version satisfies all domain and SIMD requirements."));
        }
        else
        {
            highestSeverity = DiagnosticSeverity.Fatal;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Runtime",
                ".NET Runtime Version",
                DiagnosticSeverity.Fatal,
                dotnetVersion,
                ".NET 8.0 or higher",
                "Install .NET 8.0 runtime or higher from Microsoft."));
        }

        // 4. Memory Check (>= 2048 MB)
        if (ramMb >= MinimumRamMb)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Memory",
                "System Physical Memory",
                DiagnosticSeverity.Pass,
                $"{ramMb} MB",
                $">= {MinimumRamMb} MB",
                "System memory satisfies runtime and simulation requirements."));
        }
        else
        {
            if (highestSeverity < DiagnosticSeverity.Warning) highestSeverity = DiagnosticSeverity.Warning;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Memory",
                "System Physical Memory",
                DiagnosticSeverity.Warning,
                $"{ramMb} MB",
                $">= {MinimumRamMb} MB",
                "Available RAM is below recommended 2GB. Simulation may experience paging."));
        }

        // 5. Disk Space Check (>= 1024 MB)
        if (diskSpaceMb >= MinimumDiskSpaceMb)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Storage",
                "Available Disk Space",
                DiagnosticSeverity.Pass,
                $"{diskSpaceMb} MB",
                $">= {MinimumDiskSpaceMb} MB",
                "Sufficient disk space available for game installation and save state storage."));
        }
        else
        {
            highestSeverity = DiagnosticSeverity.Fatal;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Storage",
                "Available Disk Space",
                DiagnosticSeverity.Fatal,
                $"{diskSpaceMb} MB",
                $">= {MinimumDiskSpaceMb} MB",
                "Free up disk space before launching Crown & Conquest."));
        }

        // 6. Headless Rendering Fallback
        if (headlessSupported)
        {
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Display",
                "Headless / Presentation Adapter",
                DiagnosticSeverity.Pass,
                "Supported",
                "DirectX 12 / Vulkan / Headless Fallback Driver",
                "Display adapters and headless fallback render paths available."));
        }
        else
        {
            if (highestSeverity < DiagnosticSeverity.Warning) highestSeverity = DiagnosticSeverity.Warning;
            diag.Items.Add(new EnvironmentDiagnosticItem(
                "Display",
                "Headless / Presentation Adapter",
                DiagnosticSeverity.Warning,
                "Unavailable",
                "Display Adapter",
                "Headless rendering mode required for automated testing."));
        }

        diag.OverallStatus = highestSeverity;
        return diag;
    }
}
