using MediaBrowser.Model.Plugins;

namespace EmbyBadges;

public class PluginConfiguration : BasePluginConfiguration
{
    // --- Activation globale ---
    public bool EnableBadges { get; set; } = true;

    // --- Badges résolution ---
    public bool ShowResolutionBadge { get; set; } = true;
    public bool ShowSd     { get; set; } = true;
    public bool ShowHd     { get; set; } = true;
    public bool ShowFullHd { get; set; } = true;
    public bool Show4K     { get; set; } = true;

    // --- Badges langue ---
    public bool ShowLanguageBadge   { get; set; } = true;
    public bool ShowFrench          { get; set; } = true;
    public bool ShowEnglish         { get; set; } = true;
    public bool ShowOriginalVersion { get; set; } = true;  // badge texte "VO"

    // --- Badge versions multiples ---
    public bool ShowMultiVersionBadge { get; set; } = true;  // badge texte "MULTI"

    // --- Apparence ---
    public BadgePosition Position    { get; set; } = BadgePosition.BottomLeft;

    /// <summary>Hauteur des badges en % de la plus petite dimension de l'image (1–20).</summary>
    public double BadgeSizePercent   { get; set; } = 8.0;

    /// <summary>Marge par rapport au bord en % de la largeur de l'image (0–10).</summary>
    public double BadgeMarginPercent { get; set; } = 2.0;

    /// <summary>Opacité des badges (0.0 à 1.0).</summary>
    public float BadgeOpacity        { get; set; } = 0.92f;
}

public enum BadgePosition
{
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}
