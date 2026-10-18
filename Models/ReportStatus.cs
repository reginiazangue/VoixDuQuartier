namespace VoixDuQuartier.Models;

public enum ReportStatus
{
    Sent = 0,
    Acknowledged = 1,
    InProgress = 2,
    Resolved = 3
}

public static class StatusHelper
{
    public static string Label(ReportStatus status) => status switch
    {
        ReportStatus.Sent => "Envoyé",
        ReportStatus.Acknowledged => "Pris en charge",
        ReportStatus.InProgress => "En cours",
        ReportStatus.Resolved => "Résolu",
        _ => string.Empty
    };

    /// <summary>Gris = envoyé, orange = en traitement, vert = résolu (palette des maquettes).</summary>
    public static Color Color(ReportStatus status) => status switch
    {
        ReportStatus.Sent => Microsoft.Maui.Graphics.Color.FromArgb("#8A8A8A"),
        ReportStatus.Acknowledged => Microsoft.Maui.Graphics.Color.FromArgb("#F2A93B"),
        ReportStatus.InProgress => Microsoft.Maui.Graphics.Color.FromArgb("#F2A93B"),
        ReportStatus.Resolved => Microsoft.Maui.Graphics.Color.FromArgb("#2E9E6B"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#8A8A8A")
    };
}

public static class Categories
{
    public const string Road = "Voirie";
    public const string Lighting = "Éclairage";
    public const string Waste = "Déchets";
    public const string Other = "Autre";

    public static readonly string[] All = { Road, Lighting, Waste, Other };

    public static Color Color(string category) => category switch
    {
        Road => Microsoft.Maui.Graphics.Color.FromArgb("#2E9E6B"),
        Lighting => Microsoft.Maui.Graphics.Color.FromArgb("#F2A93B"),
        Waste => Microsoft.Maui.Graphics.Color.FromArgb("#8A8A8A"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#E5484D")
    };
}
