using VoixDuQuartier.Helpers;
using Xunit;

namespace VoixDuQuartier.Tests;

public class TitleBuilderTests
{
    [Fact]
    public void Build_prend_la_premiere_phrase_de_la_description()
    {
        var title = TitleBuilder.Build("Voirie", "Un grand trou devant l'école. Très dangereux pour les motos.");

        Assert.Equal("Voirie : Un grand trou devant l'école", title);
    }

    [Fact]
    public void Build_accepte_exactement_45_caracteres_sans_les_couper()
    {
        var text = new string('a', TitleBuilder.MaxExcerptLength);

        Assert.Equal($"Voirie : {text}", TitleBuilder.Build("Voirie", text));
    }

    [Fact]
    public void Build_coupe_les_descriptions_trop_longues_avec_des_points_de_suspension()
    {
        var text = new string('a', 100);

        var title = TitleBuilder.Build("Déchets", text);

        Assert.Equal($"Déchets : {new string('a', TitleBuilder.MaxExcerptLength)}…", title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_sans_description_retourne_la_categorie(string description)
        => Assert.Equal("Éclairage", TitleBuilder.Build("Éclairage", description));

    [Fact]
    public void Build_coupe_au_premier_retour_a_la_ligne()
    {
        Assert.Equal("Autre : Câble à terre", TitleBuilder.Build("Autre", "Câble à terre\nprès du carrefour"));
    }
}
