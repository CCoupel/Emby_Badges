using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmbyBadges.Enhancer;
using MediaBrowser.Model.Logging;
using SkiaSharp;

namespace EmbyBadges.ImageProcessing;

/// <summary>
/// Dessine les badges sur une image avec SkiaSharp.
/// </summary>
public class BadgeRenderer
{
    private readonly ILogger _logger;

    // Couleurs des badges par type
    private static readonly SKColor ColorResolution = new SKColor(0, 0, 0, 200);
    private static readonly SKColor ColorLanguage   = new SKColor(30, 30, 120, 200);
    private static readonly SKColor ColorMulti      = new SKColor(120, 60, 0, 200);
    private static readonly SKColor ColorText       = SKColors.White;

    public BadgeRenderer(ILogger logger)
    {
        _logger = logger;
    }

    public Task RenderBadgesAsync(string inputFile, string outputFile, MediaInfo mediaInfo, PluginConfiguration config)
    {
        using var inputStream = File.OpenRead(inputFile);
        using var original = SKBitmap.Decode(inputStream);

        if (original is null)
            throw new InvalidOperationException($"Impossible de décoder l'image : {inputFile}");

        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        var canvas = surface.Canvas;

        // Dessiner l'image originale
        canvas.DrawBitmap(original, 0, 0);

        // Construire la liste des badges à afficher
        var badges = BuildBadgeList(mediaInfo, config);

        // Dessiner les badges positionnés
        DrawBadges(canvas, badges, original.Width, original.Height, config);

        canvas.Flush();

        // Écrire le résultat
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        using var outputStream = File.OpenWrite(outputFile);
        data.SaveTo(outputStream);

        return Task.CompletedTask;
    }

    private List<BadgeInfo> BuildBadgeList(MediaInfo mediaInfo, PluginConfiguration config)
    {
        var badges = new List<BadgeInfo>();

        // Badge résolution
        if (config.ShowResolutionBadge)
        {
            var (label, show) = mediaInfo.Resolution switch
            {
                Resolution.UHD4K  => ("4K",   config.Show4K),
                Resolution.FullHD => ("FHD",  config.ShowFullHd),
                Resolution.HD     => ("HD",   config.ShowHd),
                Resolution.SD     => ("SD",   config.ShowSd),
                _                 => (null,   false)
            };
            if (show && label is not null)
                badges.Add(new BadgeInfo(label, ColorResolution));
        }

        // Badges langue
        if (config.ShowLanguageBadge)
        {
            foreach (var lang in mediaInfo.Languages)
            {
                var (label, show) = lang switch
                {
                    "fre" or "fra" or "fr" => ("FR",  config.ShowFrench),
                    "eng" or "en"          => ("EN",  config.ShowEnglish),
                    _                      => (null,  false)
                };
                if (show && label is not null)
                    badges.Add(new BadgeInfo(label, ColorLanguage));
            }

            // VO : si la langue originale n'est pas FR ni EN
            if (config.ShowOriginalVersion)
                badges.Add(new BadgeInfo("VO", ColorLanguage));
        }

        // Badge versions multiples
        if (config.ShowMultiVersionBadge && mediaInfo.HasMultipleVersions)
            badges.Add(new BadgeInfo("MULTI", ColorMulti));

        return badges;
    }

    private void DrawBadges(SKCanvas canvas, List<BadgeInfo> badges, int imageWidth, int imageHeight, PluginConfiguration config)
    {
        if (badges.Count == 0) return;

        var badgeH = config.BadgeSize;
        var margin = config.BadgeMargin;
        var spacing = config.BadgeSpacing;
        var opacity = (byte)(config.BadgeOpacity * 255);

        using var textPaint = new SKPaint
        {
            Color = ColorText,
            TextSize = badgeH * 0.55f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Center
        };

        // Calcul de la largeur de chaque badge selon son texte
        float totalWidth = 0;
        var widths = new List<float>();
        foreach (var badge in badges)
        {
            var w = textPaint.MeasureText(badge.Label) + badgeH * 0.5f;
            widths.Add(w);
            totalWidth += w + spacing;
        }
        totalWidth -= spacing;

        // Position de départ selon le coin choisi
        float startX = config.Position switch
        {
            BadgePosition.BottomRight or BadgePosition.TopRight => imageWidth - margin - totalWidth,
            _ => margin
        };
        float startY = config.Position switch
        {
            BadgePosition.TopLeft or BadgePosition.TopRight => margin,
            _ => imageHeight - margin - badgeH
        };

        float x = startX;
        for (int i = 0; i < badges.Count; i++)
        {
            var badge = badges[i];
            var w = widths[i];
            var rect = new SKRoundRect(new SKRect(x, startY, x + w, startY + badgeH), badgeH * 0.2f);

            // Fond du badge
            using var bgPaint = new SKPaint
            {
                Color = badge.Color.WithAlpha(opacity),
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, bgPaint);

            // Texte centré verticalement
            var textY = startY + (badgeH + textPaint.TextSize) / 2f - textPaint.FontMetrics.Descent;
            canvas.DrawText(badge.Label, x + w / 2f, textY, textPaint);

            x += w + spacing;
        }
    }

    private record BadgeInfo(string Label, SKColor Color);
}
