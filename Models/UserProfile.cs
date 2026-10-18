namespace VoixDuQuartier.Models;

public class UserProfile
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;

    public string Initial => string.IsNullOrWhiteSpace(FullName)
        ? "?"
        : FullName.Trim()[0].ToString().ToUpperInvariant();
}
