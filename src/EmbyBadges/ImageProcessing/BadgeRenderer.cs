using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmbyBadges.Enhancer;
using MediaBrowser.Model.Logging;
using SkiaSharp;

namespace EmbyBadges.ImageProcessing;

/// <summary>
/// Superpose les badges PNG (résolution, langue) et les badges texte (VO, MULTI)
/// sur une image avec SkiaSharp.
/// </summary>
public class BadgeRenderer
{
    private readonly ILogger _logger;

    private static readonly SKColor ColorMulti = new SKColor(180, 90, 0);
    private static readonly SKColor ColorVo    = new SKColor(30, 100, 180);

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
        canvas.DrawBitmap(original, 0, 0);

        int iconSize = (int)Math.Clamp(Math.Min(original.Width, original.Height) * config.BadgeSizePercent / 100.0, 20, 200);
        int margin   = (int)Math.Clamp(original.Width * config.BadgeMarginPercent / 100.0, 4, 80);
        int spacing  = Math.Max(2, iconSize / 10);

        var badges = BuildBadgeList(mediaInfo, config);
        DrawBadges(canvas, badges, original.Width, original.Height, iconSize, margin, spacing, config);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 92);

        var tmp = outputFile + ".tmp";
        using (var outputStream = File.OpenWrite(tmp))
            data.SaveTo(outputStream);
        File.Move(tmp, outputFile, overwrite: true);

        return Task.CompletedTask;
    }

    private List<BadgeItem> BuildBadgeList(MediaInfo mediaInfo, PluginConfiguration config)
    {
        var badges = new List<BadgeItem>();

        // Résolution
        if (config.ShowResolutionBadge && mediaInfo.ResolutionIcon is not null)
        {
            bool show = mediaInfo.ResolutionIcon switch
            {
                "res_4k"    => config.Show4K,
                "res_1080p" => config.ShowFullHd,
                "res_720p"  => config.ShowHd,
                "res_480p"  => config.ShowSd,
                _           => false
            };
            if (show)
            {
                var icon = IconLoader.Get(mediaInfo.ResolutionIcon);
                if (icon is not null)
                    badges.Add(new PngBadge(icon));
            }
        }

        // Langues audio
        if (config.ShowLanguageBadge)
        {
            foreach (var langIcon in mediaInfo.AudioLanguages)
            {
                bool show = langIcon switch
                {
                    "lang_french"  => config.ShowFrench,
                    "lang_english" => config.ShowEnglish,
                    _              => false
                };
                if (!show) continue;

                var icon = IconLoader.Get(langIcon);
                if (icon is not null)
                    badges.Add(new PngBadge(icon));
            }
        }

        // Badge VO si aucune langue reconnue
        if (config.ShowLanguageBadge && config.ShowOriginalVersion && badges.Count == 0)
            badges.Add(new TextBadge("VO", ColorVo));

        // Versions multiples
        if (config.ShowMultiVersionBadge && mediaInfo.HasMultipleVersions)
            badges.Add(new TextBadge("MULTI", ColorMulti));

        return badges;
    }

    private static void DrawBadges(SKCanvas canvas, List<BadgeItem> badges,
        int imgW, int imgH, int iconSize, int margin, int spacing,
        PluginConfiguration config)
    {
        if (badges.Count == 0) return;

        float x = config.Position switch
        {
            BadgePosition.BottomRight or BadgePosition.TopRight
                => imgW - margin - TotalWidth(badges, iconSize, spacing),
            _ => margin
        };

        float y = config.Position switch
        {
            BadgePosition.TopLeft or BadgePosition.TopRight => margin,
            _ => imgH - margin - iconSize
        };

        foreach (var badge in badges)
        {
            float w = badge.Width(iconSize);
            badge.Draw(canvas, x, y, iconSize, config.BadgeOpacity);
            x += w + spacing;
        }
    }

    private static float TotalWidth(List<BadgeItem> badges, int iconSize, int spacing)
    {
        float total = 0;
        foreach (var b in badges) total += b.Width(iconSize) + spacing;
        return total - spacing;
    }

    private abstract class BadgeItem
    {
        public abstract float Width(int iconSize);
        public abstract void Draw(SKCanvas canvas, float x, float y, int iconSize, float opacity);
    }

    private class PngBadge : BadgeItem
    {
        private readonly SKImage _image;
        public PngBadge(SKImage image) => _image = image;

        public override float Width(int iconSize)
            => iconSize * ((float)_image.Width / _image.Height);

        public override void Draw(SKCanvas canvas, float x, float y, int iconSize, float opacity)
        {
            float w = Width(iconSize);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color       = SKColors.White.WithAlpha((byte)(opacity * 255))
            };
            canvas.DrawImage(_image, new SKRect(x, y, x + w, y + iconSize), paint);
        }
    }

    private class TextBadge : BadgeItem
    {
        private readonly string _label;
        private readonly SKColor _bgColor;

        public TextBadge(string label, SKColor bgColor)
        {
            _label   = label;
            _bgColor = bgColor;
        }

        public override float Width(int iconSize)
        {
            using var paint = MakeTextPaint(iconSize);
            return paint.MeasureText(_label) + iconSize * 0.4f;
        }

        public override void Draw(SKCanvas canvas, float x, float y, int iconSize, float opacity)
        {
            float w = Width(iconSize);
            var rect = new SKRoundRect(new SKRect(x, y, x + w, y + iconSize), iconSize * 0.18f);

            using var bgPaint = new SKPaint
            {
                Color       = _bgColor.WithAlpha((byte)(opacity * 220)),
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, bgPaint);

            using var textPaint = MakeTextPaint(iconSize);
            textPaint.Color = SKColors.White.WithAlpha((byte)(opacity * 255));
            float textY = y + (iconSize + textPaint.TextSize) / 2f - textPaint.FontMetrics.Descent;
            canvas.DrawText(_label, x + w / 2f, textY, textPaint);
        }

        private static SKPaint MakeTextPaint(int iconSize) => new SKPaint
        {
            TextSize    = iconSize * 0.52f,
            IsAntialias = true,
            Typeface    = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign   = SKTextAlign.Center
        };
    }
}
