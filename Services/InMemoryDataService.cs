using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

public class InMemoryDataService : IDataService
{
    private readonly List<Report> _reports = new()
    {
        new Report
        {
            Id = "r1", Title = "Nid-de-poule rue des Palmiers", Category = Categories.Road, Location = "PK17",
            Description = "Un grand trou dans la chaussée devant l'école, dangereux pour les motos et les piétons.",
            PhotoPath = "nid_de_poule.jpeg",
            Status = ReportStatus.InProgress, SupportCount = 24, IsMine = true, MapX = 0.28, MapY = 0.34,
            CreatedAt = DateTime.Now.AddDays(-2)
        },
        new Report
        {
            Id = "r2", Title = "Dépôt d'ordures", Category = Categories.Waste, Location = "Marché, PK14",
            Description = "Des déchets s'accumulent à l'entrée du marché depuis plusieurs semaines.",
            PhotoPath = "ordures.jpeg",
            Status = ReportStatus.Resolved, SupportCount = 12, IsMine = true, MapX = 0.52, MapY = 0.56,
            CreatedAt = DateTime.Now.AddDays(-9)
        },
        new Report
        {
            Id = "r3", Title = "Lampadaire éteint", Category = Categories.Lighting, Location = "Rue de l'école",
            Description = "Le lampadaire est éteint depuis une semaine, la rue est très sombre le soir.",
            PhotoPath = "lampadaire.jpeg",
            Status = ReportStatus.Sent, SupportCount = 31, IsMine = true, MapX = 0.85, MapY = 0.38,
            CreatedAt = DateTime.Now.AddDays(-1)
        },
        new Report
        {
            Id = "r4", Title = "Câble électrique à terre", Category = Categories.Other, Location = "Carrefour PK15",
            Description = "Un câble pend à hauteur d'homme près du carrefour, danger pour les passants.",
            PhotoPath = "cable.jpeg",
            Status = ReportStatus.Acknowledged, SupportCount = 8, MapX = 0.46, MapY = 0.78,
            CreatedAt = DateTime.Now.AddDays(-4)
        }
    };

    private readonly List<AppNotification> _notifications = new()
    {
        new AppNotification { Title = "Votre signalement est pris en charge", Subtitle = "Nid-de-poule, PK17", TimeText = "Il y a 4 h" },
        new AppNotification { Title = "6 voisins ont soutenu votre signalement", Subtitle = "Lampadaire éteint", TimeText = "Hier" },
        new AppNotification { Title = "Signalement résolu", Subtitle = "Dépôt d'ordures, Marché", TimeText = "Il y a 2 j", IsRead = true },
        new AppNotification { Title = "Nouveau signalement dans votre quartier", Subtitle = "Voirie, PK14", TimeText = "Il y a 3 j", IsRead = true }
    };

    public async Task<IReadOnlyList<Report>> GetReportsAsync()
    {
        await Task.Delay(700); // simule le temps de chargement réseau (état « Chargement… »)
        return _reports.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Report? GetReport(string id) => _reports.FirstOrDefault(r => r.Id == id);

    public async Task<Report> AddReportAsync(Report report)
    {
        await Task.Delay(600);
        _reports.Add(report);
        return report;
    }

    public async Task<IReadOnlyList<AppNotification>> GetNotificationsAsync()
    {
        await Task.Delay(300);
        return _notifications;
    }
}