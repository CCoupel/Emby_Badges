using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using SkiaSharp;

namespace EmbyBadges.ImageProcessing;

/// <summary>
/// Charge les badges PNG embarqués dans le DLL et les met en cache.
/// Nommage des ressources : EmbyBadges.Icons.{nom}.png
/// </summary>
public static class IconLoader
{
    private static readonly ConcurrentDictionary<string, SKImage?> _cache = new();
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static SKTypeface? _fontCache;
    private static readonly object _fontLock = new();

    /// <summary>Retourne le badge PNG embarqué (ex: "res_1080p", "lang_french").</summary>
    public static SKImage? Get(string name)
        => _cache.GetOrAdd(name, LoadImage);

    /// <summary>Retourne la police Trebuchet Bold embarquée.</summary>
    public static SKTypeface GetFont()
    {
        if (_fontCache != null) return _fontCache;
        lock (_fontLock)
        {
            if (_fontCache != null) return _fontCache;
            using var stream = _assembly.GetManifestResourceStream("EmbyBadges.Icons.badge_font.ttf");
            _fontCache = stream != null
                ? SKTypeface.FromStream(stream)
                : SKTypeface.Default;
            return _fontCache;
        }
    }

    private static SKImage? LoadImage(string name)
    {
        var resourceName = $"EmbyBadges.Icons.{name}.png";
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        var bitmap = SKBitmap.Decode(ms);
        return bitmap is null ? null : SKImage.FromBitmap(bitmap);
    }
}
