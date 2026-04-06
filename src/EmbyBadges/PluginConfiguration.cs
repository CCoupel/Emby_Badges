using MediaBrowser.Model.Plugins;

namespace EmbyBadges;

public class PluginConfiguration : BasePluginConfiguration
{
    // --- Activation globale ---
    public bool EnableBadges { get; set; } = true;

    // --- Badges résolution ---
    public bool ShowResolutionBadge { get; set; } = true;
    public bool ShowSd { get; set; } = true;
    public bool ShowHd { get; set; } = true;
    public bool ShowFullHd { get; set; } = true;
    public bool Show4K { get; set; } = true;

    // --- Badges langue ---
    public bool ShowLanguageBadge { get; set; } = true;
    public bool ShowFrench { get; set; } = true;
    public bool ShowEnglish { get; set; } = true;
    public bool ShowOriginalVersion { get; set; } = true;

    // --- Badge versions multiples ---
    public bool ShowMultiVersionBadge { get; set; } = true;

    // --- Apparence ---
    public BadgePosition Position { get; set; } = BadgePosition.BottomLeft;
    public int BadgeSize { get; set; } = 40;          // hauteur en pixels
    public int BadgeMargin { get; set; } = 8;          // marge bord image en pixels
    public int BadgeSpacing { get; set; } = 4;         // espace entre badges en pixels
    public float BadgeOpacity { get; set; } = 0.90f;   // 0.0 à 1.0
}

public enum BadgePosition
{
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}
