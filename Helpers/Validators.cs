using System.Text.RegularExpressions;

namespace VoixDuQuartier.Helpers;

/// <summary>Règles de validation des formulaires (sans dépendance MAUI : testables).</summary>
public static class Validators
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const int MinPasswordLength = 8;
    public const int MinDescriptionLength = 10;
    public const int MaxDescriptionLength = 300;

    public static bool IsValidEmail(string? value)
        => !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value.Trim());

    /// <summary>Retourne un message d'erreur, ou null si la valeur est valide.</summary>
    public static string? ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Saisissez votre adresse e-mail";
        return IsValidEmail(value) ? null : "Adresse e-mail incorrecte";
    }

    public static string? ValidateRequired(string? value, string message)
        => string.IsNullOrWhiteSpace(value) ? message : null;

    public static string? ValidateNewPassword(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "Saisissez un mot de passe";
        return value.Length >= MinPasswordLength
            ? null
            : $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères";
    }

    public static string? ValidateConfirmation(string? password, string? confirmation)
        => password == confirmation ? null : "Les mots de passe ne correspondent pas";

    public static string? ValidateDescription(string? value)
    {
        var length = value?.Trim().Length ?? 0;
        return length >= MinDescriptionLength
            ? null
            : $"Décrivez le problème ({MinDescriptionLength} caractères minimum)";
    }
}
