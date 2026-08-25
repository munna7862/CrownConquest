using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Shipping;

public enum DiagnosticSeverity
{
    Pass = 0,
    Warning = 1,
    Fatal = 2
}

public sealed record EnvironmentDiagnosticItem(
    string Category,
    string CheckName,
    DiagnosticSeverity Severity,
    string ObservedValue,
    string RequiredSpecification,
    string Recommendation);

public sealed class EnvironmentDiagnostics
{
    public DiagnosticSeverity OverallStatus { get; set; } = DiagnosticSeverity.Pass;
    public string Architecture { get; set; } = "x64";
    public string OperatingSystem { get; set; } = string.Empty;
    public string DotNetVersion { get; set; } = string.Empty;
    public long TotalMemoryMb { get; set; }
    public long AvailableDiskSpaceMb { get; set; }
    public bool HeadlessRenderingSupported { get; set; }
    public List<EnvironmentDiagnosticItem> Items { get; set; } = new();

    public bool IsPassing => OverallStatus != DiagnosticSeverity.Fatal;
}
