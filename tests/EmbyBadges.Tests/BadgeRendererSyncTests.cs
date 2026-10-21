using EmbyBadges.Enhancer;
using EmbyBadges.ImageProcessing;
using SkiaSharp;
using Xunit;

namespace EmbyBadges.Tests;

/// <summary>Spécification de BadgeRenderer.LettersOf / ColorOf (contracts/models.md §4, GATE 2 H1 et H6).</summary>
public class LettersOfTests
{
    [Fact] public void None_ChaineVide() => Assert.Equal("", BadgeRenderer.LettersOf(SyncTypes.None));
    [Fact] public void Position_P() => Assert.Equal("P", BadgeRenderer.LettersOf(SyncTypes.Position));
    [Fact] public void Read_L() => Assert.Equal("L", BadgeRenderer.LettersOf(SyncTypes.Read));
    [Fact] public void Removed_S() => Assert.Equal("S", BadgeRenderer.LettersOf(SyncTypes.Removed));
    [Fact] public void ReadEtPosition_PL() => Assert.Equal("PL", BadgeRenderer.LettersOf(SyncTypes.Read | SyncTypes.Position));
    [Fact] public void PositionEtRemoved_PS() => Assert.Equal("PS", BadgeRenderer.LettersOf(SyncTypes.Removed | SyncTypes.Position));
    [Fact] public void ReadEtRemoved_LS() => Assert.Equal("LS", BadgeRenderer.LettersOf(SyncTypes.Removed | SyncTypes.Read));
    [Fact]
    public void Tous_PLS_OrdreFixe()
        => Assert.Equal("PLS", BadgeRenderer.LettersOf(SyncTypes.Removed | SyncTypes.Read | SyncTypes.Position));
}

public class ColorOfTests
{
    private static readonly SKColor Rouge = new(200, 30, 30);
    private static readonly SKColor Orange = new(220, 120, 0);
    private static readonly SKColor Vert = new(40, 150, 60);

    [Fact] public void Tous_Rouge() => Assert.Equal(Rouge, BadgeRenderer.ColorOf(SyncTypes.Removed | SyncTypes.Read | SyncTypes.Position));
    [Fact] public void RemovedSeul_Rouge() => Assert.Equal(Rouge, BadgeRenderer.ColorOf(SyncTypes.Removed));
    [Fact] public void RemovedEtPosition_Rouge() => Assert.Equal(Rouge, BadgeRenderer.ColorOf(SyncTypes.Removed | SyncTypes.Position));
    [Fact] public void ReadEtPosition_Orange() => Assert.Equal(Orange, BadgeRenderer.ColorOf(SyncTypes.Read | SyncTypes.Position));
    [Fact] public void ReadSeul_Orange() => Assert.Equal(Orange, BadgeRenderer.ColorOf(SyncTypes.Read));
    [Fact] public void PositionSeul_Vert() => Assert.Equal(Vert, BadgeRenderer.ColorOf(SyncTypes.Position));
}

/// <summary>BadgeRenderer.SyncShown : types détectés ∩ types activés (contracts/models.md §3, CA7).</summary>
public class SyncShownTests
{
    private const SyncTypes All = SyncTypes.Position | SyncTypes.Read | SyncTypes.Removed;

    [Fact]
    public void TousActives_RetourneLesTypesDetectes()
        => Assert.Equal(All, BadgeRenderer.SyncShown(All, new PluginConfiguration()));

    [Fact]
    public void PositionDesactive_EstRetire()
        => Assert.Equal(SyncTypes.Read | SyncTypes.Removed,
            BadgeRenderer.SyncShown(All, new PluginConfiguration { ShowSyncPosition = false }));

    [Fact]
    public void ToutDesactive_DonneNone()
        => Assert.Equal(SyncTypes.None, BadgeRenderer.SyncShown(All,
            new PluginConfiguration { ShowSyncPosition = false, ShowSyncRead = false, ShowSyncRemoved = false }));

    [Fact]
    public void TypeNonDetecte_NEstPasAjoute()
        => Assert.Equal(SyncTypes.Read, BadgeRenderer.SyncShown(SyncTypes.Read, new PluginConfiguration()));

    [Fact]
    public void AucunDetecte_DonneNone()
        => Assert.Equal(SyncTypes.None, BadgeRenderer.SyncShown(SyncTypes.None, new PluginConfiguration()));
}

public class SyncConfigDefaultsTests
{
    [Fact]
    public void ValeursParDefaut_ConformesAuContrat()
    {
        var c = new PluginConfiguration();
        Assert.True(c.ShowSync);
        Assert.Equal(SyncRenderMode.Letters, c.SyncRenderMode);
        Assert.True(c.ShowSyncPosition);
        Assert.True(c.ShowSyncRead);
        Assert.True(c.ShowSyncRemoved);
        Assert.Equal(BadgePosition.TopLeft, c.Sync.Position);
    }

    [Fact]
    public void TtlIndex_5Minutes()
        => Assert.Equal(TimeSpan.FromMinutes(5), EmbyBadges.Enhancer.SyncDetector.SyncIndexTtl);
}
