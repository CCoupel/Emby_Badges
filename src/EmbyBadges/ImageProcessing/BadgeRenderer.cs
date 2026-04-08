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

    private static readonly SKColor ColorMulti    = new SKColor(180, 90,  0);
    private static readonly SKColor ColorVo       = new SKColor(30,  100, 180);
    private static readonly SKColor ColorFavorite = new SKColor(200, 30,  60);

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

        // Résolution (toutes les versions, dédupliquées)
        TryAddPng(list, mediaInfo.ResolutionIcons, config.ShowSd,     config.Resolution, "res_480p");
        TryAddPng(list, mediaInfo.ResolutionIcons, config.ShowHd,     config.Resolution, "res_720p");
        TryAddPng(list, mediaInfo.ResolutionIcons, config.ShowFullHd, config.Resolution, "res_1080p");
        TryAddPng(list, mediaInfo.ResolutionIcons, config.Show4K,     config.Resolution, "res_4k");

        // Langues audio — mise en surbrillance si langue originale du média
        bool hl = config.HighlightOriginalLanguage;
        string? orig = mediaInfo.OriginalLanguageIcon;
        TryAddPng(list, mediaInfo.AudioLanguages, config.ShowFrench,    config.Language, "lang_french",   hl && orig == "lang_french");
        TryAddPng(list, mediaInfo.AudioLanguages, config.ShowEnglish,   config.Language, "lang_english",  hl && orig == "lang_english");
        TryAddPng(list, mediaInfo.AudioLanguages, config.ShowJapanese,  config.Language, "lang_japanese", hl && orig == "lang_japanese");

        // VO : flux audio non géré présent ET la langue originale du film est non gérée.
        // Un film américain avec doublages ES/PT n'affiche pas VO (original = EN, géré).
        // Un film coréen avec flux KO affiche VO (original = KO, non géré), highlighté.
        bool showVo = config.ShowVo
            && mediaInfo.HasUnmanagedAudioLanguage
            && mediaInfo.HasKnownOriginCountry
            && mediaInfo.OriginalLanguageIcon == null;
        if (showVo)
            list.Add(new TextBadge("VO", ColorVo, config.Language) { IsHighlighted = config.HighlightOriginalLanguage });

        // Badge multi-version / VirtualLib
        bool showMultiBadge = config.ShowMulti && (
            mediaInfo.HasMultipleVersions ||
            (config.MultiVersionTrigger == MultiVersionTrigger.AlwaysForVirtualLib && mediaInfo.IsFromVirtualLib)
        );

        if (showMultiBadge)
        {
            if (mediaInfo.VersionConnectors.Count > 0)
                foreach (var connector in mediaInfo.VersionConnectors)
                    list.Add(new TextBadge(
                        connector.Length > 0 ? connector[0].ToString().ToUpperInvariant() : "?",
                        ColorMulti, config.MultiVersion));
            else
                list.Add(new TextBadge("MULTI", ColorMulti, config.MultiVersion));
        }

        // Favoris
        if (config.ShowFavorites && mediaInfo.IsFavorite)
            list.Add(new HeartBadge(ColorFavorite, config.Favorites));

        return list;
    }

    private static void TryAddPng(List<BadgeItem> list, List<string> actuals, bool enabled, GroupConfig cfg, string expected, bool highlighted = false)
    {
        if (!enabled || !actuals.Contains(expected)) return;
        var icon = IconLoader.Get(expected);
        if (icon is not null) list.Add(new PngBadge(icon, cfg) { IsHighlighted = highlighted });
    }

    // ── Rendu d'un groupe de badges dans un coin ─────────────────────────────

    private static void DrawGroup(SKCanvas canvas, List<BadgeItem> badges,
        BadgePosition position, int imgW, int imgH)
    {
        if (badges.Count == 0) return;

        var dims = badges.Select(b =>
        {
            int h = CalcSize(b.BadgeConfig, imgW, imgH);
            return (badge: b, h, w: b.Width(h), margin: CalcMargin(b.BadgeConfig, imgW));
        }).ToList();

        int spacing = Math.Max(2, dims.Max(d => d.h) / 12);
        float totalW = dims.Sum(d => d.w) + spacing * (dims.Count - 1);

        int anchorMargin = dims[0].margin;

        float startX = position switch
        {
            BadgePosition.TopRight or BadgePosition.BottomRight or BadgePosition.CenterRight
                => imgW - anchorMargin - totalW,
            BadgePosition.TopCenter or BadgePosition.BottomCenter
                => (imgW - totalW) / 2f,
            _ => anchorMargin   // Left variants
        };

        float x = startX;
        foreach (var (badge, h, w, margin) in dims)
        {
            float y = position switch
            {
                BadgePosition.TopLeft or BadgePosition.TopRight or BadgePosition.TopCenter
                    => margin,
                BadgePosition.CenterLeft or BadgePosition.CenterRight
                    => (imgH - h) / 2f,
                _ => imgH - margin - h  // Bottom variants
            };

            badge.Draw(canvas, x, y, h, badge.BadgeConfig.Opacity);
            x += w + spacing;
        }
    }

    private static int CalcSize(GroupConfig cfg, int imgW, int imgH)
        => (int)Math.Clamp(Math.Min(imgW, imgH) * cfg.SizePercent / 100.0, 16, 300);

    private static int CalcMargin(GroupConfig cfg, int imgW)
        => (int)Math.Clamp(imgW * cfg.MarginPercent / 100.0, 2, 80);

    // ── Types de badges ──────────────────────────────────────────────────────

    private static readonly SKColor ColorHighlight = new SKColor(255, 215, 0); // or

    private abstract class BadgeItem
    {
        public GroupConfig BadgeConfig { get; }
        public bool IsHighlighted { get; set; }
        protected BadgeItem(GroupConfig cfg) => BadgeConfig = cfg;
        public abstract float Width(int iconSize);
        public abstract void Draw(SKCanvas canvas, float x, float y, int iconSize, float opacity);
    }

    private class PngBadge : BadgeItem
    {
        private readonly SKImage _image;
        public PngBadge(SKImage image, GroupConfig cfg) : base(cfg) => _image = image;

        public override float Width(int h) => h * ((float)_image.Width / _image.Height);

        public override void Draw(SKCanvas canvas, float x, float y, int h, float opacity)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color       = SKColors.White.WithAlpha((byte)(opacity * 255))
            };
            var rect = new SKRect(x, y, x + Width(h), y + h);
            canvas.DrawImage(_image, rect, paint);

            if (IsHighlighted)
            {
                float stroke = Math.Max(2f, h / 10f);
                using var border = new SKPaint
                {
                    Color       = ColorHighlight.WithAlpha((byte)(opacity * 255)),
                    IsAntialias = true,
                    Style       = SKPaintStyle.Stroke,
                    StrokeWidth = stroke
                };
                canvas.DrawRect(rect, border);
            }
        }
    }

    private class TextBadge : BadgeItem
    {
        private readonly string  _label;
        private readonly SKColor _bg;

        public TextBadge(string label, SKColor bg, GroupConfig cfg) : base(cfg)
        { _label = label; _bg = bg; }

        public override float Width(int h)
        {
            using var p = MakePaint(h);
            return p.MeasureText(_label) + h * 0.5f;
        }

        public override void Draw(SKCanvas canvas, float x, float y, int h, float opacity)
        {
            float w = Width(h);
            var rrect = new SKRoundRect(new SKRect(x, y, x + w, y + h), h * 0.2f);

            using var bgPaint = new SKPaint
            {
                Color       = _bg.WithAlpha((byte)(opacity * 210)),
                IsAntialias = true
            };
            canvas.DrawRoundRect(rrect, bgPaint);

            if (IsHighlighted)
            {
                float stroke = Math.Max(2f, h / 10f);
                using var border = new SKPaint
                {
                    Color       = ColorHighlight.WithAlpha((byte)(opacity * 255)),
                    IsAntialias = true,
                    Style       = SKPaintStyle.Stroke,
                    StrokeWidth = stroke
                };
                canvas.DrawRoundRect(rrect, border);
            }

            using var tp = MakePaint(h);
            tp.Color = SKColors.White.WithAlpha((byte)(opacity * 255));

            // Centrage vertical : ty = baseline tel que le texte est centré dans [y, y+h]
            // Ascent est négatif, Descent positif dans SkiaSharp
            var m = tp.FontMetrics;
            float ty = y + h / 2f - (m.Ascent + m.Descent) / 2f;
            canvas.DrawText(_label, x + w / 2f, ty, tp);
        }

        private static SKPaint MakePaint(int h) => new()
        {
            TextSize    = h * 0.55f,
            IsAntialias = true,
            Typeface    = IconLoader.GetFont(),
            TextAlign   = SKTextAlign.Center
        };
    }

    private class HeartBadge : BadgeItem
    {
        private readonly SKColor _color;
        public HeartBadge(SKColor color, GroupConfig cfg) : base(cfg) => _color = color;

        public override float Width(int h) => h; // carré

        public override void Draw(SKCanvas canvas, float x, float y, int h, float opacity)
        {
            using var paint = new SKPaint
            {
                Color       = _color.WithAlpha((byte)(opacity * 255)),
                IsAntialias = true
            };
            using var path = HeartPath(x, y, h, h);
            canvas.DrawPath(path, paint);
        }

        private static SKPath HeartPath(float x, float y, float w, float h)
        {
            // Coeur symétrique avec courbes de Bézier
            float cx = x + w / 2f;
            float top = y + h * 0.22f;

            var p = new SKPath();
            p.MoveTo(cx, y + h * 0.95f);

            // Côté gauche (bas → haut-gauche)
            p.CubicTo(
                x + w * 0.05f, y + h * 0.65f,
                x,             y + h * 0.38f,
                x + w * 0.22f, top);
            // Lobe gauche
            p.CubicTo(
                x + w * 0.38f, y,
                cx,            y + h * 0.15f,
                cx,            top);

            // Lobe droit (symétrique)
            p.CubicTo(
                cx,            y + h * 0.15f,
                x + w * 0.62f, y,
                x + w * 0.78f, top);
            // Côté droit (haut-droit → bas)
            p.CubicTo(
                x + w,         y + h * 0.38f,
                x + w * 0.95f, y + h * 0.65f,
                cx,            y + h * 0.95f);

            p.Close();
            return p;
        }
    }
}
