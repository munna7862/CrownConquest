using System;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Accessibility Settings & Colorblind Palette
// ─────────────────────────────────────────────────

/// <summary>
/// Colorblind accessibility modes.
/// </summary>
public enum ColorblindMode
{
    Normal,
    Deuteranopia,  // Red-green (most common)
    Protanopia,    // Red weakness
    Tritanopia     // Blue-yellow
}

/// <summary>
/// RGBA color representation for palette remapping.
/// </summary>
public readonly record struct PaletteColor(byte R, byte G, byte B, byte A = 255);

/// <summary>
/// Accessibility settings model covering visual and UI adjustments.
/// </summary>
public sealed class AccessibilitySettings
{
    public ColorblindMode ColorblindMode { get; set; }
    public float UiScale { get; set; }
    public bool ShowTooltips { get; set; }
    public bool LargeFont { get; set; }
    public bool HighContrast { get; set; }
    public float ScreenShakeIntensity { get; set; }

    public AccessibilitySettings()
    {
        ColorblindMode = ColorblindMode.Normal;
        UiScale = 1.0f;
        ShowTooltips = true;
        LargeFont = false;
        HighContrast = false;
        ScreenShakeIntensity = 1.0f;
    }

    /// <summary>
    /// Gets the effective UI scale clamped to safe bounds.
    /// </summary>
    public float EffectiveUiScale => Math.Clamp(UiScale * (LargeFont ? 1.25f : 1.0f), 0.75f, 2.0f);
}

/// <summary>
/// Describes a tooltip to be displayed over a UI element.
/// </summary>
public readonly record struct TooltipDescriptor(
    string Title,
    string Description,
    string? Hotkey,
    string? ResourceCost,
    float DisplayDuration);

/// <summary>
/// Presenter that provides colorblind-safe faction palette colors
/// and tooltip generation.
/// </summary>
public sealed class AccessibilityPresenter
{
    private readonly AccessibilitySettings _settings;

    public AccessibilitySettings Settings => _settings;

    // Standard faction colors (Normal mode)
    private static readonly PaletteColor NormalPlayer1 = new(66, 133, 244);   // Blue
    private static readonly PaletteColor NormalPlayer2 = new(219, 68, 55);    // Red
    private static readonly PaletteColor NormalNeutral = new(128, 128, 128);  // Gray

    // Deuteranopia-safe colors
    private static readonly PaletteColor DeuteranoPlayer1 = new(0, 114, 178);    // Blue (safe)
    private static readonly PaletteColor DeuteranoPlayer2 = new(230, 159, 0);    // Orange (safe)
    private static readonly PaletteColor DeuteranoNeutral = new(128, 128, 128);

    // Protanopia-safe colors
    private static readonly PaletteColor ProtanoPlayer1 = new(0, 158, 115);      // Teal (safe)
    private static readonly PaletteColor ProtanoPlayer2 = new(204, 121, 167);    // Pink (safe)
    private static readonly PaletteColor ProtanoNeutral = new(128, 128, 128);

    // Tritanopia-safe colors
    private static readonly PaletteColor TritanoPlayer1 = new(86, 180, 233);     // Sky blue (safe)
    private static readonly PaletteColor TritanoPlayer2 = new(213, 94, 0);       // Vermillion (safe)
    private static readonly PaletteColor TritanoNeutral = new(128, 128, 128);

    public AccessibilityPresenter(AccessibilitySettings? settings = null)
    {
        _settings = settings ?? new AccessibilitySettings();
    }

    /// <summary>
    /// Gets the colorblind-safe palette color for a faction color index.
    /// </summary>
    public PaletteColor GetFactionColor(int factionColorIndex)
    {
        return (_settings.ColorblindMode, factionColorIndex) switch
        {
            (ColorblindMode.Normal, 0) => NormalPlayer1,
            (ColorblindMode.Normal, 1) => NormalPlayer2,
            (ColorblindMode.Normal, _) => NormalNeutral,

            (ColorblindMode.Deuteranopia, 0) => DeuteranoPlayer1,
            (ColorblindMode.Deuteranopia, 1) => DeuteranoPlayer2,
            (ColorblindMode.Deuteranopia, _) => DeuteranoNeutral,

            (ColorblindMode.Protanopia, 0) => ProtanoPlayer1,
            (ColorblindMode.Protanopia, 1) => ProtanoPlayer2,
            (ColorblindMode.Protanopia, _) => ProtanoNeutral,

            (ColorblindMode.Tritanopia, 0) => TritanoPlayer1,
            (ColorblindMode.Tritanopia, 1) => TritanoPlayer2,
            (ColorblindMode.Tritanopia, _) => TritanoNeutral,

            _ => NormalNeutral
        };
    }

    /// <summary>
    /// Verifies that two faction colors are visually distinct in the current colorblind mode.
    /// </summary>
    public bool AreFactionColorsDistinct(int factionIndex1, int factionIndex2)
    {
        if (factionIndex1 == factionIndex2) return false;
        var c1 = GetFactionColor(factionIndex1);
        var c2 = GetFactionColor(factionIndex2);

        // Euclidean color distance in RGB space
        int dr = c1.R - c2.R;
        int dg = c1.G - c2.G;
        int db = c1.B - c2.B;
        double distance = Math.Sqrt(dr * dr + dg * dg + db * db);

        // Minimum perceptual distance threshold for distinguishability
        return distance > 80.0;
    }

    /// <summary>
    /// Creates a tooltip descriptor for a UI element.
    /// </summary>
    public TooltipDescriptor CreateTooltip(
        string title,
        string description,
        string? hotkey = null,
        string? resourceCost = null)
    {
        if (!_settings.ShowTooltips)
        {
            return new TooltipDescriptor("", "", null, null, 0f);
        }

        return new TooltipDescriptor(
            Title: title,
            Description: description,
            Hotkey: hotkey,
            ResourceCost: resourceCost,
            DisplayDuration: 3.0f);
    }
}
