using EmbyBadges.Enhancer;
using EmbyBadges.ImageProcessing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using Xunit;

namespace EmbyBadges.Tests;

/// <summary>SyncDetector.CountProgress (contracts/models.md §8) : dédoublonnage par InternalId.</summary>
public class CountProgressTests
{
    [Fact]
    public void Vide_ZeroSurZero()
    {
        var (played, total) = SyncDetector.CountProgress(Array.Empty<(long id, bool played)>());
        Assert.Equal(0, played);
        Assert.Equal(0, total);
    }

    [Fact]
    public void TousLus()
    {
        var (played, total) = SyncDetector.CountProgress(new[] { (1L, true), (2L, true), (3L, true) });
        Assert.Equal(3, played);
        Assert.Equal(3, total);
    }

    [Fact]
    public void AucunLu()
    {
        var (played, total) = SyncDetector.CountProgress(new[] { (1L, false), (2L, false) });
        Assert.Equal(0, played);
        Assert.Equal(2, total);
    }

    [Fact]
    public void Mélange()
    {
        var (played, total) = SyncDetector.CountProgress(new[] { (1L, true), (2L, false), (3L, true), (4L, false) });
        Assert.Equal(2, played);
        Assert.Equal(4, total);
    }

    [Fact]
    public void Doublons_CompteUneFois()
    {
        var (played, total) = SyncDetector.CountProgress(new[] { (1L, true), (1L, true), (2L, false), (2L, false) });
        Assert.Equal(1, played);
        Assert.Equal(2, total);
    }

    [Fact]
    public void DoublonLu_CompteUnSeulLu()
    {
        var (played, total) = SyncDetector.CountProgress(new[] { (5L, true), (5L, true), (5L, true) });
        Assert.Equal(1, played);
        Assert.Equal(1, total);
    }
}

/// <summary>BadgeRenderer.ProgressLabel (§11) : « lus/total », culture invariante, lus bornés.</summary>
public class ProgressLabelTests
{
    [Fact] public void TroisSurDouze() => Assert.Equal("3/12", BadgeRenderer.ProgressLabel(3, 12));
    [Fact] public void ZeroSurCinq() => Assert.Equal("0/5", BadgeRenderer.ProgressLabel(0, 5));
    [Fact] public void ToutLu() => Assert.Equal("7/7", BadgeRenderer.ProgressLabel(7, 7));
    [Fact] public void LusSupérieursAuTotal_Bornés() => Assert.Equal("5/5", BadgeRenderer.ProgressLabel(9, 5));
    [Fact] public void LusNégatifs_BornésAZero() => Assert.Equal("0/5", BadgeRenderer.ProgressLabel(-2, 5));
    [Fact] public void TotalZero_Null() => Assert.Null(BadgeRenderer.ProgressLabel(0, 0));

    [Fact]
    public void CultureInvariante_PasDeSéparateurDeMilliers()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");
            Assert.Equal("1234/5678", BadgeRenderer.ProgressLabel(1234, 5678));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }
}

/// <summary>SyncDetector.IsStale (§13) : 5 min, ou 30 s si un événement a marqué l'index « dirty ».</summary>
public class IsStaleTests
{
    private static readonly DateTime T0 = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact] public void Frais_NonPérimé() => Assert.False(SyncDetector.IsStale(T0, T0.AddSeconds(10), false));
    [Fact] public void QuatreMinutes59_NonPérimé() => Assert.False(SyncDetector.IsStale(T0, T0.AddSeconds(299), false));
    [Fact] public void CinqMinutes_Périmé() => Assert.True(SyncDetector.IsStale(T0, T0.AddMinutes(5), false));
    [Fact] public void PlusDeCinqMinutes_Périmé() => Assert.True(SyncDetector.IsStale(T0, T0.AddMinutes(6), false));
    [Fact] public void Dirty29s_NonPérimé() => Assert.False(SyncDetector.IsStale(T0, T0.AddSeconds(29), true));
    [Fact] public void Dirty30s_Périmé() => Assert.True(SyncDetector.IsStale(T0, T0.AddSeconds(30), true));
    [Fact] public void Dirty31s_Périmé() => Assert.True(SyncDetector.IsStale(T0, T0.AddSeconds(31), true));
    [Fact] public void NonDirty30s_NonPérimé() => Assert.False(SyncDetector.IsStale(T0, T0.AddSeconds(30), false));
}

/// <summary>PlaylistEventsEntryPoint.ShouldInvalidateOnUserData (§13) : filtre des UserDataSaved.</summary>
public class ShouldInvalidateOnUserDataTests
{
    private static readonly HashSet<long> Members = new() { 10, 20, 30 };

    [Theory]
    [InlineData(UserDataSaveReason.PlaybackProgress)]
    [InlineData(UserDataSaveReason.PlaybackStart)]
    public void LectureEnCours_Ignorée(UserDataSaveReason reason)
        => Assert.False(PlaylistEventsEntryPoint.ShouldInvalidateOnUserData(reason, 10, Members));

    [Theory]
    [InlineData(UserDataSaveReason.PlaybackFinished)]
    [InlineData(UserDataSaveReason.TogglePlayed)]
    public void FinOuBascule_MembreDeLIndex_Invalide(UserDataSaveReason reason)
        => Assert.True(PlaylistEventsEntryPoint.ShouldInvalidateOnUserData(reason, 20, Members));

    [Theory]
    [InlineData(UserDataSaveReason.PlaybackFinished)]
    [InlineData(UserDataSaveReason.TogglePlayed)]
    public void ItemHorsMembers_Ignoré(UserDataSaveReason reason)
        => Assert.False(PlaylistEventsEntryPoint.ShouldInvalidateOnUserData(reason, 99, Members));

    [Fact]
    public void MembersVide_Ignoré()
        => Assert.False(PlaylistEventsEntryPoint.ShouldInvalidateOnUserData(
            UserDataSaveReason.TogglePlayed, 10, new HashSet<long>()));
}
