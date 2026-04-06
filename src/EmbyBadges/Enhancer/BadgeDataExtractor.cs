using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
        var subtitleStreams = streams.Where(s => s.Type == MediaStreamType.Subtitle).ToList();

        return new MediaInfo
        {
            Resolution = DetectResolution(videoStream),
            Languages = DetectLanguages(audioStreams, subtitleStreams),
            HasMultipleVersions = item is Video video && video.GetAlternateVersionIds().Any()
        };
    }

    private static Resolution DetectResolution(MediaStream? videoStream)
    {
        if (videoStream is null) return Resolution.Unknown;

        var height = videoStream.Height ?? 0;

        return height switch
        {
            >= 2160 => Resolution.UHD4K,
            >= 1080 => Resolution.FullHD,
            >= 720  => Resolution.HD,
            > 0     => Resolution.SD,
            _       => Resolution.Unknown
        };
    }

    private static List<string> DetectLanguages(List<MediaStream> audioStreams, List<MediaStream> subtitleStreams)
    {
        var languages = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var stream in audioStreams.Concat(subtitleStreams))
        {
            if (!string.IsNullOrWhiteSpace(stream.Language))
                languages.Add(stream.Language.ToLowerInvariant());
        }

        return languages.ToList();
    }
}

public class MediaInfo
{
    public Resolution Resolution { get; set; } = Resolution.Unknown;
    public List<string> Languages { get; set; } = new();
    public bool HasMultipleVersions { get; set; }
}

public enum Resolution
{
    Unknown,
    SD,
    HD,
    FullHD,
    UHD4K
}
