using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

/// <summary>Chiffres et réglages du profil, tels que le serveur les connaît.
/// Language vaut "fr" ou "en".</summary>
public record ProfileInfo(
    int ReportCount,
    int SupportCount,
    int ResolvedCount,
    bool NotificationsEnabled,
    string Language);

/// <summary>Accès aux données. Deux versions existent : en mémoire (démo) et via le backend (API).</summary>
public interface IDataService
{
    Task<IReadOnlyList<Report>> GetReportsAsync();
    Report? GetReport(string id);
    Task<Report> AddReportAsync(Report report);
    Task<IReadOnlyList<AppNotification>> GetNotificationsAsync();

    // ---- Nouveautés ----
    // Chacune a un comportement par défaut : InMemoryDataService n'a donc pas besoin d'être modifié.

    /// <summary>Les signalements de l'utilisateur connecté.</summary>
    async Task<IReadOnlyList<Report>> GetMyReportsAsync()
    {
        var all = await GetReportsAsync();
        return all.Where(r => r.IsMine).ToList();
    }

    /// <summary>Bouton « Soutenir » : soutient, ou retire le soutien. Met à jour le signalement reçu.</summary>
    Task ToggleSupportAsync(Report report)
    {
        if (report.IsSupported)
        {
            report.IsSupported = false;
            report.SupportCount--;
        }
        else
        {
            report.IsSupported = true;
            report.SupportCount++;
        }
        return Task.CompletedTask;
    }

    /// <summary>Lien « Tout marquer comme lu ».</summary>
    Task MarkAllNotificationsReadAsync() => Task.CompletedTask;

    /// <summary>Chiffres et réglages du profil. Retourne null si le serveur n'est pas disponible.</summary>
    Task<ProfileInfo?> GetProfileInfoAsync() => Task.FromResult<ProfileInfo?>(null);

    /// <summary>Enregistre les réglages (notifications, langue "fr" ou "en").</summary>
    Task SaveSettingsAsync(bool notificationsEnabled, string language) => Task.CompletedTask;
}