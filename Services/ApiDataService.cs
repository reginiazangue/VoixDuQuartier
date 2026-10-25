using System.Diagnostics;
using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

/// <summary>
/// Remplace InMemoryDataService : même « prise » (IDataService), mais les données viennent du backend.
/// </summary>
public class ApiDataService : IDataService
{
    // Les écrans Connexion / Inscription sont branchés sur le backend (ApiSessionService),
    // donc la connexion automatique au compte de démonstration est désactivée.
    private const bool UseDemoAutoLogin = false;
    private const string DemoEmail = "demo@voixduquartier.cm";
    private const string DemoPassword = "Demo1234!";

    // Zone couverte par la carte du quartier (Douala).
    // Sert à convertir latitude/longitude <-> MapX/MapY (valeurs de 0 à 1).
    private const double LatNorth = 4.095;
    private const double LatSouth = 4.080;
    private const double LngWest = 9.725;
    private const double LngEast = 9.745;

    private readonly ApiService _api;
    private readonly Dictionary<string, Report> _cache = new();
    private bool _connectionAlertShown;
    private ApiProfile? _profile; // dernier profil reçu du serveur

    public ApiDataService(ApiService api)
    {
        _api = api;
    }

    // ---------- IDataService ----------

    public async Task<IReadOnlyList<Report>> GetReportsAsync()
    {
        try
        {
            var reports = await LoadReportsAsync(mine: false);

            _cache.Clear();
            foreach (var report in reports)
            {
                _cache[report.Id] = report;
            }

            return reports;
        }
        catch (ApiException ex)
        {
            await HandleReadErrorAsync(ex);
            return _cache.Values.ToList();
        }
    }

    /// <summary>Les signalements de l'utilisateur connecté (écran Profil).</summary>
    public async Task<IReadOnlyList<Report>> GetMyReportsAsync()
    {
        try
        {
            var reports = await LoadReportsAsync(mine: true);

            // On les garde en mémoire pour que l'écran Détail les retrouve.
            foreach (var report in reports)
            {
                _cache[report.Id] = report;
            }

            return reports;
        }
        catch (ApiException ex)
        {
            await HandleReadErrorAsync(ex);
            return _cache.Values.Where(r => r.IsMine).ToList();
        }
    }

    private async Task<List<Report>> LoadReportsAsync(bool mine)
    {
        var page = await CallAsync(() => _api.GetReportsAsync(mine: mine));

        // Le détail contient tout ce dont l'application a besoin (description, adresse, position, soutien).
        var details = await Task.WhenAll(page.Items.Select(i => _api.GetReportAsync(i.Id)));
        return details.Select(ToReport).ToList();
    }

    public Report? GetReport(string id) =>
        _cache.TryGetValue(id, out var report) ? report : null;

    public async Task<Report> AddReportAsync(Report report)
    {
        try
        {
            var (latitude, longitude) = ToLatLng(report.MapX, report.MapY);

            var created = await CallAsync(() => _api.CreateReportAsync(
                string.IsNullOrWhiteSpace(report.Title) ? null : report.Title,
                report.Description,
                ToApiCategory(report.Category),
                latitude,
                longitude,
                string.IsNullOrWhiteSpace(report.Location) ? null : report.Location));

            // Envoi de la photo (si l'utilisateur en a choisi une)
            if (report.HasPhoto && File.Exists(report.PhotoPath))
            {
                try
                {
                    await using var stream = File.OpenRead(report.PhotoPath!);
                    created = await _api.UploadPhotoAsync(created.Id, stream, Path.GetFileName(report.PhotoPath!));
                }
                catch (Exception ex) when (ex is ApiException or IOException)
                {
                    Debug.WriteLine($"[API] Photo non envoyée : {ex.Message}");
                }
            }

            var saved = ToReport(created);
            _cache[saved.Id] = saved;
            return saved;
        }
        catch (ApiException ex)
        {
            await ShowErrorAsync("Le signalement n'a pas été enregistré sur le serveur : " + ex.Message);
            return report;
        }
    }

    public async Task<IReadOnlyList<AppNotification>> GetNotificationsAsync()
    {
        try
        {
            var items = await CallAsync(() => _api.GetNotificationsAsync());

            return items.Select(n => new AppNotification
            {
                Title = n.Message,
                Subtitle = n.Type switch
                {
                    "StatusChanged" => "Changement de statut",
                    "ReportSupported" => "Nouveau soutien",
                    _ => "Information"
                },
                TimeText = RelativeTime(n.CreatedAt),
                IsRead = n.IsRead
            }).ToList();
        }
        catch (ApiException ex)
        {
            await HandleReadErrorAsync(ex);
            return Array.Empty<AppNotification>();
        }
    }

    /// <summary>Bouton « Soutenir » : le serveur décide, puis l'écran suit sa réponse.</summary>
    public async Task ToggleSupportAsync(Report report)
    {
        if (!int.TryParse(report.Id, out var id)) return;

        try
        {
            var result = await CallAsync(() => _api.ToggleSupportAsync(id));
            report.IsSupported = result.Supported;
            report.SupportCount = result.SupportCount;
        }
        catch (ApiException ex)
        {
            // 401 : la session a expiré, l'application renvoie déjà vers la connexion.
            if (ex.StatusCode != 401)
            {
                await ShowErrorAsync("Le soutien n'a pas été enregistré : " + ex.Message);
            }
        }
    }

    public async Task MarkAllNotificationsReadAsync()
    {
        try
        {
            await CallAsync(async () =>
            {
                await _api.MarkAllNotificationsReadAsync();
                return true;
            });
        }
        catch (ApiException ex)
        {
            Debug.WriteLine($"[API] Notifications non marquées comme lues : {ex.Message}");
        }
    }

    public async Task<ProfileInfo?> GetProfileInfoAsync()
    {
        try
        {
            _profile = await CallAsync(() => _api.GetProfileAsync());
            return new ProfileInfo(
                _profile.ReportCount,
                _profile.SupportsReceived,
                _profile.ResolvedCount,
                _profile.NotificationsEnabled,
                _profile.Language);
        }
        catch (ApiException ex)
        {
            await HandleReadErrorAsync(ex);
            return null;
        }
    }

    public async Task SaveSettingsAsync(bool notificationsEnabled, string language)
    {
        try
        {
            // Le serveur veut aussi le nom et le quartier : on reprend ceux qu'il connaît déjà.
            var current = _profile ?? await CallAsync(() => _api.GetProfileAsync());
            _profile = await CallAsync(() => _api.UpdateProfileAsync(
                current.FullName, current.Quartier, notificationsEnabled, language));
        }
        catch (ApiException ex)
        {
            Debug.WriteLine($"[API] Réglages non enregistrés : {ex.Message}");
        }
    }

    // ---------- Connexion automatique (temporaire) ----------

    private async Task EnsureSignedInAsync()
    {
        if (!UseDemoAutoLogin || await _api.IsLoggedInAsync()) return;
        await _api.LoginAsync(DemoEmail, DemoPassword);
    }

    private async Task<T> CallAsync<T>(Func<Task<T>> action)
    {
        await EnsureSignedInAsync();
        try
        {
            return await action();
        }
        catch (ApiException ex) when (ex.StatusCode == 401 && UseDemoAutoLogin)
        {
            // Token périmé ou compte recréé : on se reconnecte et on réessaie une fois.
            _api.Logout();
            await EnsureSignedInAsync();
            return await action();
        }
    }

    // ---------- Conversions backend <-> application ----------

    private static Report ToReport(ApiReportDetail d)
    {
        var (mapX, mapY) = ToMapXY(d.Latitude, d.Longitude);

        return new Report
        {
            Id = d.Id.ToString(),
            Title = d.Title,
            Description = d.Description,
            Category = FromApiCategory(d.Category),
            Location = string.IsNullOrWhiteSpace(d.Address) ? d.Quartier : d.Address!,
            PhotoPath = ApiService.PhotoAbsoluteUrl(d.PhotoUrl),
            CreatedAt = d.CreatedAt.ToLocalTime(),
            IsMine = d.IsMine,
            MapX = mapX,
            MapY = mapY,
            Status = FromApiStatus(d.Status),
            SupportCount = d.SupportCount,
            IsSupported = d.SupportedByMe
        };
    }

    private static string ToApiCategory(string category) => category switch
    {
        Categories.Road => "Voirie",
        Categories.Lighting => "Eclairage",
        Categories.Waste => "Dechets",
        _ => "Autre"
    };

    private static string FromApiCategory(string category) => category switch
    {
        "Voirie" => Categories.Road,
        "Eclairage" => Categories.Lighting,
        "Dechets" => Categories.Waste,
        _ => Categories.Other
    };

    private static ReportStatus FromApiStatus(string status) => status switch
    {
        "Acknowledged" => ReportStatus.Acknowledged,
        "InProgress" => ReportStatus.InProgress,
        "Resolved" => ReportStatus.Resolved,
        _ => ReportStatus.Sent   // "Received" côté backend
    };

    private static (double MapX, double MapY) ToMapXY(double latitude, double longitude)
    {
        var x = (longitude - LngWest) / (LngEast - LngWest);
        var y = (LatNorth - latitude) / (LatNorth - LatSouth);
        return (Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1));
    }

    private static (double Latitude, double Longitude) ToLatLng(double mapX, double mapY)
    {
        // Position non renseignée : centre de la carte
        if (mapX == 0 && mapY == 0)
        {
            mapX = 0.5;
            mapY = 0.5;
        }

        var latitude = LatNorth - Math.Clamp(mapY, 0, 1) * (LatNorth - LatSouth);
        var longitude = LngWest + Math.Clamp(mapX, 0, 1) * (LngEast - LngWest);
        return (latitude, longitude);
    }

    private static string RelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "À l'instant";
        if (span.TotalMinutes < 60) return $"Il y a {(int)span.TotalMinutes} min";
        if (span.TotalHours < 24) return $"Il y a {(int)span.TotalHours} h";
        if (span.TotalDays < 7) return $"Il y a {(int)span.TotalDays} j";
        return utc.ToLocalTime().ToString("dd/MM/yyyy");
    }

    // ---------- Messages d'erreur ----------

    private async Task HandleReadErrorAsync(ApiException ex)
    {
        Debug.WriteLine($"[API] {ex.StatusCode} : {ex.Message}");

        // Serveur injoignable : on prévient une seule fois pour ne pas répéter le message.
        if (ex.StatusCode == 0 && !_connectionAlertShown)
        {
            _connectionAlertShown = true;
            await ShowErrorAsync(ex.Message);
        }
    }

    private static Task ShowErrorAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlert("VoixDuQuartier", message, "OK");
            }
        });
}