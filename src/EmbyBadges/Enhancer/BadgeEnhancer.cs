using System;
using System.Threading.Tasks;
using EmbyBadges.ImageProcessing;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace EmbyBadges.Enhancer;

/// <summary>
/// Intercepte les images servies par Emby et y superpose les badges.
/// Auto-découvert par Emby — pas besoin d'enregistrement manuel.
/// </summary>
public class BadgeEnhancer : IImageEnhancer
{
    private readonly ILogger _logger;
    private readonly BadgeRenderer _renderer;
    private readonly ILibraryManager _libraryManager;
    private readonly IApplicationPaths _appPaths;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;

    public BadgeEnhancer(
        ILogManager logManager,
        ILibraryManager libraryManager,
        IApplicationPaths appPaths,
        IUserDataManager userDataManager,
        IUserManager userManager)
    {
        _logger = logManager.GetLogger(nameof(BadgeEnhancer));
        _libraryManager = libraryManager;
        _appPaths = appPaths;
        _userDataManager = userDataManager;
        _userManager = userManager;
        _renderer = new BadgeRenderer(_logger);
    }

    /// <summary>
    /// Priorité d'exécution dans la chaîne d'enhancers.
    /// Last = s'exécute après tous les autres enhancers.
    /// </summary>
    public MetadataProviderPriority Priority => MetadataProviderPriority.Last;

    /// <summary>
    /// Indique à Emby sur quels items et types d'images ce plugin doit s'appliquer.
    /// </summary>
    public bool Supports(BaseItem item, ImageType imageType)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.EnableBadges)
            return false;

        // Uniquement les posters et vignettes
        if (imageType != ImageType.Primary && imageType != ImageType.Thumb)
            return false;

        // Films et épisodes uniquement
        return item is Movie || item is Episode;
    }

    /// <summary>
    /// Clé de cache : doit changer quand la config ou les infos du média changent.
    /// Emby invalide le cache et rappelle EnhanceImageAsync si la clé change.
    /// </summary>
    public string GetConfigurationCacheKey(BaseItem item, ImageType imageType)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null)
            return string.Empty;

        var mediaInfo = BadgeDataExtractor.GetMediaInfo(item, _libraryManager, _appPaths, _userDataManager, _userManager, _logger);

        static string GC(GroupConfig g) =>
            $"{g.Position}{g.SizePercent}{g.MarginPercent}{g.Opacity}";

        return string.Join("_",
            nameof(EmbyBadges),
            GC(config.Resolution),
            GC(config.Language),
            GC(config.MultiVersion),
            GC(config.Favorites),
            config.ShowSd, config.ShowHd, config.ShowFullHd, config.Show4K,
            config.ShowFrench, config.ShowEnglish, config.ShowJapanese, config.ShowVo, config.ShowMulti,
            config.MultiVersionTrigger, config.ShowFavorites, config.HighlightOriginalLanguage,
            string.Join(",", mediaInfo.ResolutionIcons),
            string.Join(",", mediaInfo.AudioLanguages),
            mediaInfo.OriginalLanguageIcon,
            mediaInfo.HasKnownOriginCountry,
            mediaInfo.HasUnmanagedAudioLanguage,
            string.Join(",", mediaInfo.VersionConnectors),
            mediaInfo.IsFromVirtualLib,
            mediaInfo.IsFavorite
        );
    }

    /// <summary>
    /// Retourne la taille de l'image en sortie.
    /// Les badges sont dessinés dans le canvas existant — pas de redimensionnement.
    /// </summary>
    public ImageSize GetEnhancedImageSize(BaseItem item, ImageType imageType, int imageIndex, ImageSize originalImageSize)
        => originalImageSize;

    /// <summary>
    /// Métadonnées sur l'image produite.
    /// RequiresTransparency = false car on produit du JPEG opaque.
    /// </summary>
    public EnhancedImageInfo GetEnhancedImageInfo(BaseItem item, string inputFile, ImageType imageType, int imageIndex)
        => new EnhancedImageInfo { RequiresTransparency = false };

    /// <summary>
    /// Coeur du plugin : lit inputFile, superpose les badges, écrit outputFile.
    /// </summary>
    public async Task EnhanceImageAsync(BaseItem item, string inputFile, string outputFile, ImageType imageType, int imageIndex)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var mediaInfo = BadgeDataExtractor.GetMediaInfo(item, _libraryManager, _appPaths, _userDataManager, _userManager, _logger);

            await _renderer.RenderBadgesAsync(inputFile, outputFile, mediaInfo, config);
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Erreur lors du rendu des badges pour {0}", ex, item.Name);

            // Fallback : copier l'image originale sans badge
            System.IO.File.Copy(inputFile, outputFile, overwrite: true);
        }
    }
}
