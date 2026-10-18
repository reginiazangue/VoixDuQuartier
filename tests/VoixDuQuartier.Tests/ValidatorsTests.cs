using VoixDuQuartier.Helpers;
using Xunit;

namespace VoixDuQuartier.Tests;

public class ValidatorsTests
{
    [Theory]
    [InlineData("reginia@gmail.com")]
    [InlineData("prenom.nom@mail.univ.cm")]
    [InlineData("  user@mail.com  ")]
    public void IsValidEmail_accepte_les_adresses_correctes(string email)
        => Assert.True(Validators.IsValidEmail(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sans-arobase.com")]
    [InlineData("user@@mail.com")]
    [InlineData("user mail@x.com")]
    [InlineData("user@mail")]
    [InlineData("user@mail.c")]
    public void IsValidEmail_refuse_les_adresses_incorrectes(string? email)
        => Assert.False(Validators.IsValidEmail(email));

    [Fact]
    public void ValidateEmail_retourne_les_messages_des_maquettes()
    {
        Assert.Equal("Saisissez votre adresse e-mail", Validators.ValidateEmail(""));
        Assert.Equal("Adresse e-mail incorrecte", Validators.ValidateEmail("abc"));
        Assert.Null(Validators.ValidateEmail("abc@mail.com"));
    }

    [Fact]
    public void ValidateRequired_refuse_les_valeurs_vides()
    {
        Assert.Equal("Obligatoire", Validators.ValidateRequired("  ", "Obligatoire"));
        Assert.Equal("Obligatoire", Validators.ValidateRequired(null, "Obligatoire"));
        Assert.Null(Validators.ValidateRequired("PK17", "Obligatoire"));
    }

    [Fact]
    public void ValidateNewPassword_exige_8_caracteres()
    {
        Assert.Equal("Saisissez un mot de passe", Validators.ValidateNewPassword(""));
        Assert.NotNull(Validators.ValidateNewPassword("1234567"));
        Assert.Null(Validators.ValidateNewPassword("12345678"));
    }

    [Fact]
    public void ValidateConfirmation_compare_les_deux_mots_de_passe()
    {
        Assert.Null(Validators.ValidateConfirmation("motdepasse1", "motdepasse1"));
        Assert.Equal("Les mots de passe ne correspondent pas", Validators.ValidateConfirmation("motdepasse1", "motdepasse2"));
    }

    [Theory]
    [InlineData("court")]
    [InlineData("123456789")]          // 9 caractères
    [InlineData("   123456789   ")]    // les espaces ne comptent pas
    public void ValidateDescription_refuse_moins_de_10_caracteres(string description)
        => Assert.Equal("Décrivez le problème (10 caractères minimum)", Validators.ValidateDescription(description));

    [Fact]
    public void ValidateDescription_accepte_10_caracteres_ou_plus()
    {
        Assert.Null(Validators.ValidateDescription("1234567890"));
        Assert.Null(Validators.ValidateDescription("Un grand trou devant l'école."));
    }
}
