using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace VoixDuQuartier.Services;

public class ApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Catégories : "Voirie", "Eclairage", "Dechets", "Autre"
// Statuts    : "Received" (Reçu), "InProgress" (En cours), "Resolved" (Résolu)
public record ApiUser(int Id, string FullName, string Email, string Quartier, string Role);
public record ApiAuthResponse(string Token, DateTime ExpiresAtUtc, ApiUser User);
public record ApiReportSummary(int Id, string Title, string Category, string Status, string Quartier, string? PhotoUrl, int SupportCount, DateTime CreatedAt);
public record ApiStatusEntry(string Status, DateTime ChangedAt, string? Note);
public record ApiReportDetail(int Id, string Title, string Description, string Category, string Status, string Quartier, string? Address, double Latitude, double Longitude, string? PhotoUrl, int SupportCount, bool SupportedByMe, bool IsMine, string AuthorName, DateTime CreatedAt, List<ApiStatusEntry> History);
public record ApiPagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);
public record ApiMapPin(int Id, string Title, string Category, string Status, double Latitude, double Longitude);
public record ApiSupportResult(bool Supported, int SupportCount);
public record ApiNotification(int Id, string Type, string Message, int? ReportId, bool IsRead, DateTime CreatedAt);
public record ApiUnreadCount(int Count);
public record ApiProfile(int Id, string FullName, string Email, string Quartier, int ReportCount, int SupportsReceived, int ResolvedCount, bool NotificationsEnabled, string Language);

/// <summary>Le « serveur » entre l'application et le backend.</summary>
public class ApiService
{
    private const string TokenKey = "vdq_token";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private string? _token;
    private bool _tokenLoaded;

    /// <summary>Déclenché quand le serveur refuse le token (session expirée) : l'application renvoie vers la connexion.</summary>
    public event Action? SessionExpired;

    // Émulateur Android : 10.0.2.2 = le PC. Windows : localhost.
    public static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    }

    /// <summary>Transforme "/uploads/abc.jpg" en adresse complète pour afficher une image.</summary>
    public static string? PhotoAbsoluteUrl(string? photoUrl) =>
        string.IsNullOrEmpty(photoUrl) ? null : BaseUrl + photoUrl;

    // ---------- Connexion ----------

    public async Task<bool> IsLoggedInAsync()
    {
        await LoadTokenAsync();
        return !string.IsNullOrEmpty(_token);
    }

    public async Task<ApiAuthResponse> LoginAsync(string email, string password)
    {
        var auth = await SendAsync<ApiAuthResponse>(HttpMethod.Post, "api/auth/login", Body(new { email, password }));
        await SaveTokenAsync(auth.Token);
        return auth;
    }

    public async Task<ApiAuthResponse> RegisterAsync(string fullName, string email, string quartier, string password, string confirmPassword)
    {
        var auth = await SendAsync<ApiAuthResponse>(HttpMethod.Post, "api/auth/register",
            Body(new { fullName, email, quartier, password, confirmPassword }));
        await SaveTokenAsync(auth.Token);
        return auth;
    }

    public void Logout()
    {
        _token = null;
        _tokenLoaded = true;
        try { SecureStorage.Default.Remove(TokenKey); } catch { }
        Preferences.Default.Remove(TokenKey);
    }

    // ---------- Signalements ----------

    public Task<ApiPagedResult<ApiReportSummary>> GetReportsAsync(
        string? category = null, string? status = null, bool mine = false, string? search = null, int page = 1)
    {
        var query = new List<string> { $"page={page}", $"mine={mine.ToString().ToLowerInvariant()}" };
        if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrEmpty(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return SendAsync<ApiPagedResult<ApiReportSummary>>(HttpMethod.Get, "api/reports?" + string.Join("&", query));
    }

    public Task<List<ApiMapPin>> GetMapPinsAsync() =>
        SendAsync<List<ApiMapPin>>(HttpMethod.Get, "api/reports/map");

    public Task<ApiReportDetail> GetReportAsync(int id) =>
        SendAsync<ApiReportDetail>(HttpMethod.Get, $"api/reports/{id}");

    public Task<ApiReportDetail> CreateReportAsync(
        string? title, string description, string category, double latitude, double longitude, string? address) =>
        SendAsync<ApiReportDetail>(HttpMethod.Post, "api/reports",
            Body(new { title, description, category, latitude, longitude, address }));

    public Task<ApiReportDetail> UploadPhotoAsync(int reportId, Stream photo, string fileName)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(photo), "file", fileName);
        return SendAsync<ApiReportDetail>(HttpMethod.Post, $"api/reports/{reportId}/photo", content);
    }

    public Task<ApiSupportResult> ToggleSupportAsync(int reportId) =>
        SendAsync<ApiSupportResult>(HttpMethod.Post, $"api/reports/{reportId}/support");

    public Task DeleteReportAsync(int reportId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/reports/{reportId}");

    // ---------- Notifications ----------

    public Task<List<ApiNotification>> GetNotificationsAsync() =>
        SendAsync<List<ApiNotification>>(HttpMethod.Get, "api/notifications");

    public Task<ApiUnreadCount> GetUnreadCountAsync() =>
        SendAsync<ApiUnreadCount>(HttpMethod.Get, "api/notifications/unread-count");

    public Task MarkNotificationReadAsync(int id) =>
        SendAsync<object>(HttpMethod.Post, $"api/notifications/{id}/read");

    public Task MarkAllNotificationsReadAsync() =>
        SendAsync<object>(HttpMethod.Post, "api/notifications/read-all");

    // ---------- Profil ----------

    public Task<ApiProfile> GetProfileAsync() =>
        SendAsync<ApiProfile>(HttpMethod.Get, "api/profile/me");

    public Task<ApiProfile> UpdateProfileAsync(string fullName, string quartier, bool notificationsEnabled, string language) =>
        SendAsync<ApiProfile>(HttpMethod.Put, "api/profile/me",
            Body(new { fullName, quartier, notificationsEnabled, language }));

    public async Task DeleteAccountAsync()
    {
        await SendAsync<object>(HttpMethod.Delete, "api/profile/me");
        Logout();
    }

    // ---------- Mécanique interne ----------

    private static StringContent Body(object value) =>
        new(JsonSerializer.Serialize(value, Json), Encoding.UTF8, "application/json");

    private async Task<T> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        await LoadTokenAsync();

        // Connexion et inscription ne portent pas de token.
        var isAuthCall = url.StartsWith("api/auth/", StringComparison.OrdinalIgnoreCase);

        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (!isAuthCall && !string.IsNullOrEmpty(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException("Impossible de joindre le serveur. Vérifiez que le backend est lancé.", 0);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthCall)
                {
                    SessionExpired?.Invoke();
                }

                throw new ApiException(await ReadErrorAsync(response), (int)response.StatusCode);
            }

            if (typeof(T) == typeof(object) || response.StatusCode == HttpStatusCode.NoContent)
            {
                return default!;
            }

            return (await response.Content.ReadFromJsonAsync<T>(Json))!;
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // Erreurs de validation : on affiche le premier message
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in errors.EnumerateObject())
                {
                    foreach (var message in field.Value.EnumerateArray())
                    {
                        return message.GetString() ?? "Données invalides.";
                    }
                }
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString()!;
            }
        }
        catch
        {
            // Corps vide ou illisible : on utilise le message par défaut ci-dessous.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Session expirée, reconnectez-vous.",
            HttpStatusCode.Forbidden => "Action non autorisée.",
            HttpStatusCode.NotFound => "Élément introuvable.",
            _ => "Une erreur est survenue."
        };
    }

    private async Task LoadTokenAsync()
    {
        if (_tokenLoaded) return;
        _tokenLoaded = true;
        try { _token = await SecureStorage.Default.GetAsync(TokenKey); }
        catch { _token = Preferences.Default.Get(TokenKey, string.Empty); }
    }

    private async Task SaveTokenAsync(string token)
    {
        _token = token;
        _tokenLoaded = true;
        try { await SecureStorage.Default.SetAsync(TokenKey, token); }
        catch { Preferences.Default.Set(TokenKey, token); }
    }
}