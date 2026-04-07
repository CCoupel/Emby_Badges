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

    /// <summary>
    /// Retourne le badge correspondant au nom (ex: "res_1080p", "lang_french").
    /// Retourne null si la ressource n'existe pas.
    /// </summary>
    public static SKImage? Get(string name)
    {
        return _cache.GetOrAdd(name, LoadFromAssembly);
    }

    private static SKImage? LoadFromAssembly(string name)
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
