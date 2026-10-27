using EmbyBadges.Enhancer;
using EmbyBadges.ImageProcessing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using SkiaSharp;
using Xunit;

namespace EmbyBadges.Tests;

/// <summary>Spécification de SyncDetector.OwnerOf (contracts/models.md §7 : première ligne ManageDelete).</summary>
public class OwnerOfTests
{
    private static UserItemShare Row(long user, UserItemShareLevel? level)
        => new UserItemShare { ItemId = 1, UserId = user, ShareLevel = level };

    [Fact]
    public void Null_DonneNull() => Assert.Null(SyncDetector.OwnerOf(null));

    [Fact]
    public void AucuneLigne_DonneNull() => Assert.Null(SyncDetector.OwnerOf(Array.Empty<UserItemShare>()));

    [Fact]
    public void PremiereLigneManageDelete_EstLePropriétaire()
        => Assert.Equal(7L, SyncDetector.OwnerOf(new[]
        {
            Row(2, UserItemShareLevel.Read), Row(7, UserItemShareLevel.ManageDelete),
        }));

    [Fact]
    public void LignesNone_Ignorées()
        => Assert.Equal(3L, SyncDetector.OwnerOf(new[]
        {
            Row(1, UserItemShareLevel.None), Row(2, null), Row(3, UserItemShareLevel.ManageDelete),
        }));

    [Fact]
    public void PlusieursManageDelete_LaPremièreGagne()
        => Assert.Equal(4L, SyncDetector.OwnerOf(new[]
        {
            Row(4, UserItemShareLevel.ManageDelete), Row(5, UserItemShareLevel.ManageDelete),
        }));

    [Fact]
    public void SansManageDelete_DonneNull()
        => Assert.Null(SyncDetector.OwnerOf(new[]
        {
            Row(1, UserItemShareLevel.Read), Row(2, UserItemShareLevel.Manage),
        }));

    [Fact]
    public void PropriétaireSeul_ConnuMaisNonPartagé()
    {
        var rows = new[] { Row(9, UserItemShareLevel.ManageDelete) };
        Assert.Equal(9L, SyncDetector.OwnerOf(rows));
        Assert.False(SyncDetector.IsShared(rows));
    }
}

/// <summary>IsShared appliqué à des playlists non étiquetées : le partage est indépendant des étiquettes (§7).</summary>
public class IsSharedPlaylistNonEtiquetéeTests
{
    private static UserItemShare Row(long user, UserItemShareLevel? level)
        => new UserItemShare { ItemId = 1, UserId = user, ShareLevel = level };

    [Fact]
    public void TagsVides_MaisPartagée_RestePartagée()
    {
        Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(Array.Empty<string>()));
        Assert.True(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(2, UserItemShareLevel.Read),
        }));
    }

    [Fact]
    public void AucuneLigne_NonPartagée() => Assert.False(SyncDetector.IsShared(Array.Empty<UserItemShare>()));

    [Fact]
    public void Null_NonPartagée() => Assert.False(SyncDetector.IsShared(null));
}

/// <summary>
/// SyncDetector.IndexNeeded en LOT-1 : ShowSync || ShowPlaylistShare ; ShowPlaylistProgress est ignoré
/// jusqu'au LOT-2 (revue LOT-1, CA6).
/// </summary>
public class IndexNeededTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true,  false, false, true)]
    [InlineData(false, true,  false, true)]
    [InlineData(false, false, true,  false)] // progression seule : pas d'index en LOT-1
    [InlineData(true,  true,  false, true)]
    [InlineData(true,  false, true,  true)]
    [InlineData(false, true,  true,  true)]
    [InlineData(true,  true,  true,  true)]
    public void IndexNeeded_ShowSyncOuShowPlaylistShare(bool sync, bool share, bool progress, bool attendu)
    {
        var cfg = new PluginConfiguration
        {
            ShowSync = sync,
            ShowPlaylistShare = share,
            ShowPlaylistProgress = progress,
        };
        Assert.Equal(attendu, SyncDetector.IndexNeeded(cfg));
    }

    [Fact]
    public void ProgressionSeule_NeDéclenchePasLIndex()
        => Assert.False(SyncDetector.IndexNeeded(new PluginConfiguration
        {
            ShowSync = false,
            ShowPlaylistShare = false,
            ShowPlaylistProgress = true,
        }));
}

/// <summary>BadgeRenderer.ShareColorOf (§11) : sarcelle sans étiquette active, sinon ColorOf.</summary>
public class ShareColorOfTests
{
    private static readonly SKColor Sarcelle = new(0, 120, 140);
    private static readonly SKColor Rouge = new(200, 30, 30);
    private static readonly SKColor Orange = new(220, 120, 0);
    private static readonly SKColor Vert = new(40, 150, 60);

    [Fact] public void None_Sarcelle() => Assert.Equal(Sarcelle, BadgeRenderer.ShareColorOf(SyncTypes.None));
    [Fact] public void Position_Vert() => Assert.Equal(Vert, BadgeRenderer.ShareColorOf(SyncTypes.Position));
    [Fact] public void Read_Orange() => Assert.Equal(Orange, BadgeRenderer.ShareColorOf(SyncTypes.Read));
    [Fact] public void ReadEtRemoved_Rouge() => Assert.Equal(Rouge, BadgeRenderer.ShareColorOf(SyncTypes.Read | SyncTypes.Removed));
    [Fact] public void ReadEtPosition_Orange() => Assert.Equal(Orange, BadgeRenderer.ShareColorOf(SyncTypes.Read | SyncTypes.Position));
    [Fact] public void PositionEtRemoved_Rouge() => Assert.Equal(Rouge, BadgeRenderer.ShareColorOf(SyncTypes.Position | SyncTypes.Removed));

    [Fact]
    public void Tous_Rouge()
        => Assert.Equal(Rouge, BadgeRenderer.ShareColorOf(SyncTypes.Position | SyncTypes.Read | SyncTypes.Removed));

    [Theory]
    [InlineData(SyncTypes.Position)]
    [InlineData(SyncTypes.Read)]
    [InlineData(SyncTypes.Read | SyncTypes.Removed)]
    public void NonNone_EgalAColorOf(SyncTypes t)
        => Assert.Equal(BadgeRenderer.ColorOf(t), BadgeRenderer.ShareColorOf(t));
}

/// <summary>MediaInfo : défauts neutres des champs playlist pour tout item non-Playlist (§9).</summary>
public class MediaInfoPlaylistDefaultsTests
{
    [Fact]
    public void DéfautsNeutres()
    {
        var m = new MediaInfo();
        Assert.False(m.IsPlaylist);
        Assert.False(m.PlaylistShared);
        Assert.Equal(SyncTypes.None, m.PlaylistSyncTypes);
        Assert.Equal(0, m.PlaylistPlayed);
        Assert.Equal(0, m.PlaylistTotal);
    }
}
