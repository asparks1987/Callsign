using System.Drawing;

namespace Callsign.UI;

public static class CallsignVisualStyle
{
    public const string TargetName = "macOS Voice Control";
    public const string SurfacePrinciples = "compact, high-contrast, translucent, non-activating, accessible, visible-status";
    public const string EvidenceMarker = "contrast>=4.5:1; opacity=0.86-0.99; radius=20-26px; Segoe UI; stop-visible";
    public const int CompactRadius = 20;
    public const int ComfortableRadius = 24;
    public const int LargeSurfaceRadius = 26;
    public const double MinimumOverlayOpacity = 0.86;
    public const double MaximumSurfaceOpacity = 0.99;
    public const double MinimumTextContrastRatio = 4.5;

    public static readonly Color SurfaceBackground = Color.FromArgb(248, 250, 253);
    public static readonly Color ElevatedSurfaceBackground = Color.FromArgb(252, 252, 253);
    public static readonly Color PrimaryText = Color.FromArgb(15, 23, 42);
    public static readonly Color SecondaryText = Color.FromArgb(71, 85, 105);
    public static readonly Color Accent = Color.FromArgb(30, 64, 175);
    public static readonly Color OverlayText = Color.White;
    public static readonly Color OverlayPanel = Color.FromArgb(218, 6, 10, 20);
    public const string PreferredFontFamilyName = "Segoe UI";

    public static string DescribeSurface(string surfaceName) =>
        $"{TargetName} {surfaceName}: {SurfacePrinciples}; {EvidenceMarker}.";

    public static double ContrastRatio(Color foreground, Color background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static bool HasAccessibleTextContrast(Color foreground, Color background) =>
        ContrastRatio(foreground, background) >= MinimumTextContrastRatio;

    public static bool IsAcceptedSurfaceOpacity(double opacity) =>
        opacity >= MinimumOverlayOpacity && opacity <= MaximumSurfaceOpacity;

    private static double RelativeLuminance(Color color) =>
        0.2126 * Linearize(color.R)
        + 0.7152 * Linearize(color.G)
        + 0.0722 * Linearize(color.B);

    private static double Linearize(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
