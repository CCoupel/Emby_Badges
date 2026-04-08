using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace EmbyBadges.Enhancer;

/// <summary>
/// Extrait les informations de résolution, langue et versions multiples
/// depuis les métadonnées d'un item Emby.
/// </summary>
public static class BadgeDataExtractor
{
    public static MediaInfo GetMediaInfo(
        BaseItem item,
        ILibraryManager libraryManager,
        IApplicationPaths appPaths,
        IUserDataManager userDataManager,
        IUserManager userManager,
        ILogger logger)
    {
        var streams = item.GetMediaStreams() ?? new List<MediaStream>();

        var videoStream = streams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
        var audioStreams = streams.Where(s => s.Type == MediaStreamType.Audio).ToList();

        var connectors   = new List<string>();
        var resIcons     = new List<string>();
        bool hasMultiple = false;
        bool isFromVl    = false;

        // Résolution de la version principale
        var mainRes = DetectResolutionIcon(videoStream);
        if (mainRes != null) resIcons.Add(mainRes);

        // Lire la racine VirtualLib une seule fois (null si VL non installé)
        var vlRoot = ReadVirtualLibRoot(appPaths.PluginConfigurationsPath);

        if (item is Video video)
        {
            var altIds = video.GetAlternateVersionIds().ToList();
            hasMultiple = altIds.Any();

            // Résolutions des versions alternatives (dédupliquées)
            foreach (var id in altIds)
            {
                var alt = libraryManager.GetItemById(id);
                if (alt == null) continue;
                var altStreams = alt.GetMediaStreams() ?? new List<MediaStream>();
                var altVid    = altStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
                var altRes    = DetectResolutionIcon(altVid);
                if (altRes != null && !resIcons.Contains(altRes)) resIcons.Add(altRes);
            }

            if (vlRoot != null)
            {
                // Detecter si le média provient de VirtualLib
                isFromVl = ConnectorFromPath(video.Path, vlRoot) != null;

                // Extraire les connecteurs
                if (hasMultiple)
                    connectors = ExtractConnectors(video, altIds, libraryManager, vlRoot);
                else if (isFromVl)
                {
                    // Version unique VL : extraire quand même le connecteur principal
                    var c = ConnectorFromPath(video.Path, vlRoot);
                    if (c != null) connectors.Add(c);
                }
            }
        }

        return new MediaInfo
        {
            ResolutionIcons     = resIcons,
            AudioLanguages      = DetectLanguages(audioStreams),
            HasAudioStreams      = audioStreams.Count > 0,
            HasMultipleVersions = hasMultiple,
            IsFromVirtualLib    = isFromVl,
            VersionConnectors   = connectors,
            IsFavorite          = DetectFavorite(item, userDataManager, userManager, logger)
        };
    }

    // ── Favorites detection ──────────────────────────────────────────────────

    private static bool DetectFavorite(BaseItem item, IUserDataManager userDataManager, IUserManager userManager, ILogger logger)
    {
        try
        {
#pragma warning disable CS0618
            var user = userManager.Users.FirstOrDefault();
#pragma warning restore CS0618
            if (user == null) return false;
            return userDataManager.GetUserData(user, item).IsFavorite;
        }
        catch (Exception ex)
        {
            logger.ErrorException("EmbyBadges: erreur détection favori pour {0}", ex, item.Name);
            return false;
        }
    }

    // ── VirtualLib connector detection ──────────────────────────────────────

    private static string? ReadVirtualLibRoot(string configDir)
    {
        try
        {
            var xmlPath = Path.Combine(configDir, "VirtualLib.xml");
            if (!File.Exists(xmlPath)) return null;
            return XDocument.Load(xmlPath).Root?.Element("VirtualLibraryRootPath")?.Value;
        }
        catch { return null; }
    }

    private static List<string> ExtractConnectors(Video main, List<long> altIds, ILibraryManager libraryManager, string vlRoot)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        void TryAdd(string? path)
        {
            var name = ConnectorFromPath(path, vlRoot);
            if (name != null && seen.Add(name)) result.Add(name);
        }

        TryAdd(main.Path);
        foreach (var id in altIds)
            TryAdd(libraryManager.GetItemById(id)?.Path);

        return result;
    }

    private static string? ConnectorFromPath(string? path, string vlRoot)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var norm = path.Replace('\\', '/');
        var root = vlRoot.TrimEnd('/', '\\').Replace('\\', '/') + "/";
        if (!norm.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
        var segment = norm[root.Length..].Split('/')[0];
        return string.IsNullOrEmpty(segment) ? null : segment;
    }

    /// <summary>
    /// Détecte la résolution via DisplayTitle du stream vidéo (même approche qu'EmbyIcons).
    /// Retourne le nom de l'icône embarquée (ex: "res_1080p") ou null.
    /// </summary>
    private static string? DetectResolutionIcon(MediaStream? videoStream)
    {
        if (videoStream is null) return null;

        // Priorité 1 : DisplayTitle (ex: "1080p", "4K", "720p")
        var title = (videoStream.DisplayTitle ?? "").ToLowerInvariant();

        if (title.Contains("4k") || title.Contains("2160"))  return "res_4k";
        if (title.Contains("1080"))                           return "res_1080p";
        if (title.Contains("720"))                            return "res_720p";
        if (title.Contains("480") || title.Contains("576"))   return "res_480p";

        // Fallback : pixels bruts
        var height = videoStream.Height ?? 0;
        return height switch
        {
            >= 2160 => "res_4k",
            >= 1080 => "res_1080p",
            >= 720  => "res_720p",
            > 0     => "res_480p",
            _       => null
        };
    }

    /// <summary>
    /// Détecte les langues audio via DisplayLanguage (ex: "French", "English").
    /// Retourne la liste des noms d'icônes embarquées (ex: "lang_french").
    /// </summary>
    private static List<string> DetectLanguages(List<MediaStream> audioStreams)
    {
        var seen = new HashSet<string>();
        var icons = new List<string>();

        foreach (var stream in audioStreams)
        {
            var lang = (stream.DisplayLanguage ?? "").ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(lang)) continue;

            var iconName = lang switch
            {
                "french"   => "lang_french",
                "english"  => "lang_english",
                "japanese" => "lang_japanese",
                _          => null
            };

            if (iconName is not null && seen.Add(iconName))
                icons.Add(iconName);
        }

        return icons;
    }
}

public class MediaInfo
{
    /// <summary>Icônes résolution de toutes les versions (ex: ["res_4k","res_1080p"]), dédupliquées.</summary>
    public List<string> ResolutionIcons { get; set; } = new();

    /// <summary>True si le média possède au moins un flux audio.</summary>
    public bool HasAudioStreams { get; set; }

    /// <summary>Liste des icônes langue audio (ex: ["lang_french", "lang_english"]).</summary>
    public List<string> AudioLanguages { get; set; } = new();

    /// <summary>True si plusieurs versions du média existent.</summary>
    public bool HasMultipleVersions { get; set; }

    /// <summary>Noms des connecteurs VirtualLib des versions (vide si VirtualLib non installé).</summary>
    public List<string> VersionConnectors { get; set; } = new();

    /// <summary>True si le média provient de VirtualLib.</summary>
    public bool IsFromVirtualLib { get; set; }

    /// <summary>True si au moins un utilisateur a marqué l'item comme favori.</summary>
    public bool IsFavorite { get; set; }
}
