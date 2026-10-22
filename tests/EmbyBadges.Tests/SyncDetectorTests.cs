using EmbyBadges.Enhancer;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Xunit;

namespace EmbyBadges.Tests;

/// <summary>Spécification de SyncDetector.EvaluateTags (contracts/models.md §1-2, GATE 2 H3).</summary>
public class EvaluateTagsTests
{
    [Fact]
    public void PropagerLuOui_DonneRead()
        => Assert.Equal(SyncTypes.Read, SyncDetector.EvaluateTags(new[] { "propager-lu=OUI" }));

    [Fact]
    public void ProgagerAvancementOui_DonnePosition()
        => Assert.Equal(SyncTypes.Position, SyncDetector.EvaluateTags(new[] { "propager-avancement=OUI" }));

    [Theory]
    [InlineData("Propager-Lu = oui")]
    [InlineData("  propager-lu=OUI  ")]
    [InlineData("PROPAGER-LU =Oui")]
    public void CasseEtEspacesTolerés(string tag)
        => Assert.Equal(SyncTypes.Read, SyncDetector.EvaluateTags(new[] { tag }));

    [Fact]
    public void OuiEtNon_NonLEmporte()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(new[] { "propager-lu=OUI", "propager-lu=NON" }));

    [Fact]
    public void OuiEtNon_OrdreInverse_NonLEmporte()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(new[] { "propager-lu=NON", "propager-lu=OUI" }));

    [Fact]
    public void NonSeul_Inactif()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(new[] { "propager-lu=NON" }));

    [Theory]
    [InlineData("remove-si-lu=OUIX")]
    [InlineData("remove-si-lu-oui")]
    [InlineData("remove-si-lu=")]
    [InlineData("propager-lu=OUIX")]
    [InlineData("propager-avancement-oui")]
    [InlineData("propager-avancement=")]
    [InlineData("xpropager-lu=OUI")]
    public void VariantesInvalides_SontIgnorées(string tag)
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(new[] { tag }));

    [Fact]
    public void VariantesInvalides_NeNeutralisentPasUnTagValide()
        => Assert.Equal(SyncTypes.Read, SyncDetector.EvaluateTags(new[] { "propager-lu=OUIX", "propager-lu=OUI" }));

    [Fact]
    public void RemoveSiLuSeul_DonneNone_H3()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(new[] { "remove-si-lu=OUI" }));

    [Fact]
    public void RemoveSiLuAvecPropagerLu_DonneRemovedEtRead_H3()
        => Assert.Equal(SyncTypes.Removed | SyncTypes.Read,
            SyncDetector.EvaluateTags(new[] { "remove-si-lu=OUI", "propager-lu=OUI" }));

    [Fact]
    public void RemoveSiLuOui_PropagerLuNon_DonneNone()
        => Assert.Equal(SyncTypes.None,
            SyncDetector.EvaluateTags(new[] { "remove-si-lu=OUI", "propager-lu=NON" }));

    [Fact]
    public void TroisFamillesActives_DonnentLesTroisTypes()
        => Assert.Equal(SyncTypes.Position | SyncTypes.Read | SyncTypes.Removed,
            SyncDetector.EvaluateTags(new[] { "propager-avancement=OUI", "propager-lu=OUI", "remove-si-lu=OUI" }));

    [Fact]
    public void TagsEtrangers_SontIgnorés()
        => Assert.Equal(SyncTypes.Position,
            SyncDetector.EvaluateTags(new[] { "favori", "propager-avancement=OUI", "autre=OUI" }));

    [Fact]
    public void TagsNull_DonneNone()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(null));

    [Fact]
    public void TagsVides_DonneNone()
        => Assert.Equal(SyncTypes.None, SyncDetector.EvaluateTags(Array.Empty<string>()));

    [Fact]
    public void SyncTypes_ValeursDuContrat()
    {
        Assert.Equal(0, (int)SyncTypes.None);
        Assert.Equal(1, (int)SyncTypes.Position);
        Assert.Equal(2, (int)SyncTypes.Read);
        Assert.Equal(4, (int)SyncTypes.Removed);
    }
}

/// <summary>Spécification de SyncDetector.IsShared (contracts/models.md §1 : propriétaire ManageDelete + membre ≠ propriétaire).</summary>
public class IsSharedTests
{
    private static UserItemShare Row(long user, UserItemShareLevel? level)
        => new UserItemShare { ItemId = 1, UserId = user, ShareLevel = level };

    [Fact]
    public void ManageDeleteSeul_NonPartagée()
        => Assert.False(SyncDetector.IsShared(new[] { Row(1, UserItemShareLevel.ManageDelete) }));

    [Fact]
    public void ManageDeletePlusReadAutreUtilisateur_Partagée()
        => Assert.True(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(2, UserItemShareLevel.Read),
        }));

    [Theory]
    [InlineData(UserItemShareLevel.Write)]
    [InlineData(UserItemShareLevel.Manage)]
    public void MembreNiveauSupérieurARead_Partagée(UserItemShareLevel level)
        => Assert.True(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(2, level),
        }));

    [Fact]
    public void ReadSansManageDelete_NonPartagée()
        => Assert.False(SyncDetector.IsShared(new[] { Row(1, UserItemShareLevel.Read), Row(2, UserItemShareLevel.Read) }));

    [Fact]
    public void ManageDeletePlusNone_NonPartagée()
        => Assert.False(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(2, UserItemShareLevel.None),
        }));

    [Fact]
    public void ManageDeletePlusNiveauNull_NonPartagée()
        => Assert.False(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(2, null),
        }));

    [Fact]
    public void LigneReadDuProprietaireLuiMeme_NeComptePas()
        => Assert.False(SyncDetector.IsShared(new[]
        {
            Row(1, UserItemShareLevel.ManageDelete), Row(1, UserItemShareLevel.Read),
        }));

    [Fact]
    public void AucuneLigne_NonPartagée()
        => Assert.False(SyncDetector.IsShared(Array.Empty<UserItemShare>()));
}
