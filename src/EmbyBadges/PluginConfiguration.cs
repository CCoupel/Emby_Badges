using MediaBrowser.Model.Plugins;

namespace EmbyBadges;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableBadges { get; set; } = true;

    // Chaque badge a sa propre configuration d'apparence
    public BadgeConfig Sd          { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig Hd          { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig FullHd      { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig Uhd4K       { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig French      { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig English     { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig Vo          { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public BadgeConfig MultiVersion { get; set; } = new() { Position = BadgePosition.TopRight };
}

public class BadgeConfig
{
    public bool Enabled          { get; set; } = true;
    public BadgePosition Position { get; set; } = BadgePosition.BottomLeft;
    /// <summary>Hauteur en % de la plus petite dimension de l'image (1–20).</summary>
    public double SizePercent    { get; set; } = 8.0;
    /// <summary>Marge par rapport au bord en % de la largeur (0–10).</summary>
    public double MarginPercent  { get; set; } = 2.0;
    /// <summary>Opacité 0.0–1.0.</summary>
    public float Opacity         { get; set; } = 0.92f;
}

public enum BadgePosition
{
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}
