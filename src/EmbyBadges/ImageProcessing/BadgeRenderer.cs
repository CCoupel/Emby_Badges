using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmbyBadges.Enhancer;
using MediaBrowser.Model.Logging;
using SkiaSharp;

namespace EmbyBadges.ImageProcessing;

public class BadgeRenderer
{
    private readonly ILogger _logger;

    private static readonly SKColor ColorMulti = new SKColor(180, 90, 0);
    private static readonly SKColor ColorVo    = new SKColor(30, 100, 180);

    public BadgeRenderer(ILogger logger) => _logger = logger;

    public Task RenderBadgesAsync(string inputFile, string outputFile, MediaInfo mediaInfo, PluginConfiguration config)
    {
        using var inputStream = File.OpenRead(inputFile);
        using var original    = SKBitmap.Decode(inputStream)
            ?? throw new InvalidOperationException($"Impossible de décoder : {inputFile}");

        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(original, 0, 0);

        var badges = BuildBadgeList(mediaInfo, config);

        // Grouper par position et dessiner chaque groupe
        foreach (var group in badges.GroupBy(b => b.BadgeConfig.Position))
            DrawGroup(canvas, group.ToList(), group.Key, original.Width, original.Height);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 92);
        var tmp = outputFile + ".tmp";
        using (var os = File.OpenWrite(tmp)) data.SaveTo(os);
        File.Move(tmp, outputFile, overwrite: true);

        return Task.CompletedTask;
    }

    // ── Construction de la liste de badges ──────────────────────────────────

    private static List<BadgeItem> BuildBadgeList(MediaInfo mediaInfo, PluginConfiguration config)
    {
        var list = new List<BadgeItem>();

        // Résolution
        TryAddPng(list, mediaInfo.ResolutionIcon, config.Sd,     "res_480p");
        TryAddPng(list, mediaInfo.ResolutionIcon, config.Hd,     "res_720p");
        TryAddPng(list, mediaInfo.ResolutionIcon, config.FullHd, "res_1080p");
        TryAddPng(list, mediaInfo.ResolutionIcon, config.Uhd4K,  "res_4k");

        // Langues audio
        foreach (var langIcon in mediaInfo.AudioLanguages)
        {
            if (langIcon == "lang_french")  TryAddPng(list, langIcon, config.French,  "lang_french");
            if (langIcon == "lang_english") TryAddPng(list, langIcon, config.English, "lang_english");
        }

        // VO : si langue activée mais aucun badge langue n'a été ajouté
        if (config.Vo.Enabled && mediaInfo.AudioLanguages.Count == 0)
            list.Add(new TextBadge("VO", ColorVo, config.Vo));

        // Versions multiples
        if (config.MultiVersion.Enabled && mediaInfo.HasMultipleVersions)
            list.Add(new TextBadge("MULTI", ColorMulti, config.MultiVersion));

        return list;
    }

    private static void TryAddPng(List<BadgeItem> list, string? actual, BadgeConfig cfg, string expected)
    {
        if (!cfg.Enabled || actual != expected) return;
        var icon = IconLoader.Get(expected);
        if (icon is not null) list.Add(new PngBadge(icon, cfg));
    }

    // ── Rendu d'un groupe de badges dans un coin ─────────────────────────────

    private static void DrawGroup(SKCanvas canvas, List<BadgeItem> badges,
        BadgePosition position, int imgW, int imgH)
    {
        if (badges.Count == 0) return;

        // Calculer dimensions de chaque badge
        var dims = badges.Select(b =>
        {
            int h = CalcSize(b.BadgeConfig, imgW, imgH);
            return (badge: b, h, w: b.Width(h), margin: CalcMargin(b.BadgeConfig, imgW));
        }).ToList();

        int spacing = Math.Max(2, dims.Max(d => d.h) / 12);
        float totalW = dims.Sum(d => d.w) + spacing * (dims.Count - 1);

        // Ancre horizontale (utilise la marge du premier badge du groupe)
        int anchorMargin = dims[0].margin;
        float startX = position is BadgePosition.BottomRight or BadgePosition.TopRight
            ? imgW - anchorMargin - totalW
            : anchorMargin;

        float x = startX;
        foreach (var (badge, h, w, margin) in dims)
        {
            float y = position is BadgePosition.TopLeft or BadgePosition.TopRight
                ? margin
                : imgH - margin - h;

            badge.Draw(canvas, x, y, h, badge.BadgeConfig.Opacity);
            x += w + spacing;
        }
    }

    private static int CalcSize(BadgeConfig cfg, int imgW, int imgH)
        => (int)Math.Clamp(Math.Min(imgW, imgH) * cfg.SizePercent / 100.0, 16, 300);

    private static int CalcMargin(BadgeConfig cfg, int imgW)
        => (int)Math.Clamp(imgW * cfg.MarginPercent / 100.0, 2, 80);

    // ── Types de badges ──────────────────────────────────────────────────────

    private abstract class BadgeItem
    {
        public BadgeConfig BadgeConfig { get; }
        protected BadgeItem(BadgeConfig cfg) => BadgeConfig = cfg;
        public abstract float Width(int iconSize);
        public abstract void Draw(SKCanvas canvas, float x, float y, int iconSize, float opacity);
    }

    private class PngBadge : BadgeItem
    {
        private readonly SKImage _image;
        public PngBadge(SKImage image, BadgeConfig cfg) : base(cfg) => _image = image;

        public override float Width(int h) => h * ((float)_image.Width / _image.Height);

        public override void Draw(SKCanvas canvas, float x, float y, int h, float opacity)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color       = SKColors.White.WithAlpha((byte)(opacity * 255))
            };
            canvas.DrawImage(_image, new SKRect(x, y, x + Width(h), y + h), paint);
        }
    }

    private class TextBadge : BadgeItem
    {
        private readonly string   _label;
        private readonly SKColor  _bg;
        public TextBadge(string label, SKColor bg, BadgeConfig cfg) : base(cfg)
        { _label = label; _bg = bg; }

        public override float Width(int h)
        {
            using var p = TextPaint(h);
            return p.MeasureText(_label) + h * 0.45f;
        }

        public override void Draw(SKCanvas canvas, float x, float y, int h, float opacity)
        {
            float w = Width(h);
            using var bgPaint = new SKPaint
            {
                Color       = _bg.WithAlpha((byte)(opacity * 210)),
                IsAntialias = true
            };
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + w, y + h), h * 0.18f), bgPaint);

            using var tp = TextPaint(h);
            tp.Color = SKColors.White.WithAlpha((byte)(opacity * 255));
            float ty = y + (h + tp.TextSize) / 2f - tp.FontMetrics.Descent;
            canvas.DrawText(_label, x + w / 2f, ty, tp);
        }

        private static SKPaint TextPaint(int h) => new()
        {
            TextSize    = h * 0.52f,
            IsAntialias = true,
            Typeface    = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign   = SKTextAlign.Center
        };
    }
}
