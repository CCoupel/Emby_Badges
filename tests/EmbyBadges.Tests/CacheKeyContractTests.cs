using Xunit;

namespace EmbyBadges.Tests;

/// <summary>
/// Contrôle statique de la clé de cache (contracts/models.md §5). GetConfigurationCacheKey dépend du SDK Emby
/// (ILogManager, ILibraryManager...) et n'est pas instanciable en test unitaire : on vérifie donc que le source
/// de BadgeEnhancer.cs inclut tous les champs Synchro dans le string.Join de la clé. Un champ oublié =
/// images en cache périmées (voir CLAUDE.md « Cache key completeness »).
/// </summary>
public class CacheKeyContractTests
{
    private static string KeySource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "EmbyBadges.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        var text = File.ReadAllText(Path.Combine(dir!.FullName, "src", "EmbyBadges", "Enhancer", "BadgeEnhancer.cs"));
        var start = text.IndexOf("GetConfigurationCacheKey", StringComparison.Ordinal);
        Assert.True(start >= 0, "GetConfigurationCacheKey introuvable");
        var end = text.IndexOf("GetEnhancedImageSize", start, StringComparison.Ordinal);
        Assert.True(end > start, "fin de GetConfigurationCacheKey introuvable");
        return text.Substring(start, end - start);
    }

    [Theory]
    [InlineData("GC(config.Sync)")]
    [InlineData("config.ShowSync")]
    [InlineData("config.SyncRenderMode")]
    [InlineData("config.ShowSyncPosition")]
    [InlineData("config.ShowSyncRead")]
    [InlineData("config.ShowSyncRemoved")]
    [InlineData("(int)mediaInfo.SyncTypes")]
    public void CleDeCache_ContientLeChampSynchro(string fragment)
        => Assert.Contains(fragment, KeySource());

    // LOT-1 (contracts/models.md §12) : partage de playlist. Favoris d'une playlist = segments existants.
    [Theory]
    [InlineData("GC(config.PlaylistShare)")]
    [InlineData("config.ShowPlaylistShare")]
    [InlineData("mediaInfo.IsPlaylist")]
    [InlineData("mediaInfo.PlaylistShared")]
    [InlineData("(int)mediaInfo.PlaylistSyncTypes")]
    public void CleDeCache_ContientLeChampPartagePlaylist(string fragment)
        => Assert.Contains(fragment, KeySource());

    [Theory]
    [InlineData("GC(config.Favorites)")]
    [InlineData("config.ShowFavorites")]
    [InlineData("mediaInfo.IsFavorite")]
    public void CleDeCache_ContientLesChampsFavorisPourPlaylist(string fragment)
        => Assert.Contains(fragment, KeySource());
}
