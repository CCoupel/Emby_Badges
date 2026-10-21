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
