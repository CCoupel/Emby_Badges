using MediaBrowser.Model.Plugins;

namespace EmbyBadges;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableBadges { get; set; } = true;

    /// <summary>Clé API TMDB (v3) pour détecter la langue originale précisément. Optionnelle.</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>Affiche un overlay de debug sur chaque image avec la langue originale détectée et les flux audio.</summary>
    public bool DebugMode { get; set; } = false;

    // Paramètres partagés par groupe
    public GroupConfig Resolution   { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public GroupConfig Language     { get; set; } = new() { Position = BadgePosition.BottomLeft };
    public GroupConfig MultiVersion { get; set; } = new() { Position = BadgePosition.TopRight };
    public GroupConfig Favorites    { get; set; } = new() { Position = BadgePosition.BottomRight };

    /// <summary>Déclencheur du badge multi-version.</summary>
    public MultiVersionTrigger MultiVersionTrigger { get; set; } = MultiVersionTrigger.MultiVersionOnly;

    // Activation individuelle par badge
    public bool ShowSd        { get; set; } = true;
    public bool ShowHd        { get; set; } = true;
    public bool ShowFullHd    { get; set; } = true;
    public bool Show4K        { get; set; } = true;
    public bool ShowFrench              { get; set; } = true;
    public bool ShowEnglish             { get; set; } = true;
    public bool ShowJapanese            { get; set; } = true;
    public bool ShowVo                  { get; set; } = true;
    /// <summary>Met en surbrillance le badge de la langue originale du média (premier flux audio).</summary>
    public bool HighlightOriginalLanguage { get; set; } = true;
    public bool ShowMulti     { get; set; } = true;
    public bool ShowFavorites { get; set; } = true;
}

public class GroupConfig
{
    public BadgePosition Position { get; set; } = BadgePosition.BottomLeft;
    /// <summary>Hauteur en % de la plus petite dimension de l'image (1–20).</summary>
    public double SizePercent   { get; set; } = 8.0;
    /// <summary>Marge par rapport au bord en % de la largeur (0–10).</summary>
    public double MarginPercent { get; set; } = 2.0;
    /// <summary>Opacité 0.0–1.0.</summary>
    public float Opacity        { get; set; } = 0.92f;
}

public enum MultiVersionTrigger
{
    /// <summary>Badge affiché uniquement si le média a plusieurs versions.</summary>
    MultiVersionOnly,
    /// <summary>Badge affiché dès que le média provient de VirtualLib (même version unique).</summary>
    AlwaysForVirtualLib
}

public enum BadgePosition
{
    BottomLeft,
    BottomCenter,
    BottomRight,
    CenterLeft,
    CenterRight,
    TopLeft,
    TopCenter,
    TopRight
}
