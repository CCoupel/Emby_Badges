using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;

namespace EmbyBadges.Enhancer;

/// <summary>
/// Extrait les informations de résolution, langue et versions multiples
/// depuis les métadonnées d'un item Emby.
/// </summary>
public static class BadgeDataExtractor
{
    public static MediaInfo GetMediaInfo(BaseItem item)
    {
        var streams = item.GetMediaStreams() ?? new List<MediaStream>();

        var videoStream = streams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
        var audioStreams = streams.Where(s => s.Type == MediaStreamType.Audio).ToList();

        return new MediaInfo
        {
            ResolutionIcon = DetectResolutionIcon(videoStream),
            AudioLanguages = DetectLanguages(audioStreams),
            HasMultipleVersions = item is Video video && video.GetAlternateVersionIds().Any()
        };
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
                "french"  => "lang_french",
                "english" => "lang_english",
                _         => null
            };

            if (iconName is not null && seen.Add(iconName))
                icons.Add(iconName);
        }

        return icons;
    }
}

public class MediaInfo
{
    /// <summary>Nom de l'icône résolution (ex: "res_1080p"), ou null.</summary>
    public string? ResolutionIcon { get; set; }

    /// <summary>Liste des icônes langue audio (ex: ["lang_french", "lang_english"]).</summary>
    public List<string> AudioLanguages { get; set; } = new();

    /// <summary>True si plusieurs versions du média existent.</summary>
    public bool HasMultipleVersions { get; set; }
}
