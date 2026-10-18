using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

/// <summary>Accès aux données. L'implémentation actuelle est en mémoire ; elle sera remplacée par l'API.</summary>
public interface IDataService
{
    Task<IReadOnlyList<Report>> GetReportsAsync();
    Report? GetReport(string id);
    Task<Report> AddReportAsync(Report report);
    Task<IReadOnlyList<AppNotification>> GetNotificationsAsync();
}
