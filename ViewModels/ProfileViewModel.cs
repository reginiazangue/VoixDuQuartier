using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IDataService _data;
    private readonly ISessionService _session;
    private List<Report> _mine = new();

    // Vrai pendant qu'on applique les réglages reçus du serveur : on ne les renvoie pas aussitôt au serveur.
    private bool _applyingServerSettings;

    public ProfileViewModel(IDataService data, ISessionService session)
    {
        _data = data;
        _session = session;

        darkMode = Preferences.Default.Get("dark_mode", true);
        notificationsEnabled = Preferences.Default.Get("notifications_enabled", true);
        language = Preferences.Default.Get("language", "Français");
    }

    public string[] Languages { get; } = { "Français", "English" };
    public ObservableCollection<Report> MyReports { get; } = new();

    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string neighborhood = string.Empty;
    [ObservableProperty] private string initial = "?";
    [ObservableProperty] private int reportCount;
    [ObservableProperty] private int supportCount;
    [ObservableProperty] private int resolvedCount;
    [ObservableProperty] private bool isMyReportsEmpty;

    /// <summary>false = onglet « En cours », true = onglet « Résolus ».</summary>
    [ObservableProperty] private bool showResolved;

    [ObservableProperty] private bool darkMode;
    [ObservableProperty] private bool notificationsEnabled;
    [ObservableProperty] private string language;

    public bool ShowInProgress => !ShowResolved;

    partial void OnShowResolvedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowInProgress));
        RefreshList();
    }

    // Le mode sombre reste un réglage de l'appareil : il n'est pas envoyé au serveur.
    partial void OnDarkModeChanged(bool value)
    {
        Preferences.Default.Set("dark_mode", value);
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        Preferences.Default.Set("notifications_enabled", value);
        _ = SaveSettingsAsync();
    }

    // La traduction complète de l'interface viendra avec les fichiers de ressources (.resx).
    partial void OnLanguageChanged(string value)
    {
        Preferences.Default.Set("language", value);
        _ = SaveSettingsAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = _session.CurrentUser;
        FullName = user?.FullName ?? string.Empty;
        Neighborhood = user is null || string.IsNullOrEmpty(user.Neighborhood) ? string.Empty : $"Quartier : {user.Neighborhood}";
        Initial = user?.Initial ?? "?";

        // Liste « Mes signalements » : demandée directement au serveur (uniquement les miens)
        _mine = (await _data.GetMyReportsAsync()).ToList();

        // Chiffres calculés ici, au cas où le serveur ne répondrait pas à la requête suivante
        ReportCount = _mine.Count;
        SupportCount = _mine.Sum(r => r.SupportCount);
        ResolvedCount = _mine.Count(r => r.Status == ReportStatus.Resolved);

        // Chiffres officiels et réglages enregistrés sur le serveur
        var info = await _data.GetProfileInfoAsync();
        if (info is not null)
        {
            ReportCount = info.ReportCount;
            SupportCount = info.SupportCount;
            ResolvedCount = info.ResolvedCount;

            _applyingServerSettings = true;
            try
            {
                NotificationsEnabled = info.NotificationsEnabled;
                Language = FromApiLanguage(info.Language);
            }
            finally
            {
                _applyingServerSettings = false;
            }
        }

        RefreshList();
    }

    [RelayCommand]
    private void SelectTab(string tab) => ShowResolved = tab == "resolved";

    [RelayCommand]
    private Task OpenDetailAsync(Report report)
        => Shell.Current.GoToAsync($"detail?id={report.Id}");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("Déconnexion", "Voulez-vous vraiment vous déconnecter ?", "Se déconnecter", "Annuler");
        if (!confirm) return;

        _session.Logout();
        await Shell.Current.GoToAsync("//login");
    }

    private async Task SaveSettingsAsync()
    {
        if (_applyingServerSettings) return;

        try
        {
            await _data.SaveSettingsAsync(NotificationsEnabled, ToApiLanguage(Language));
        }
        catch (Exception ex)
        {
            // Le réglage reste enregistré sur l'appareil ; on réessaiera au prochain changement.
            System.Diagnostics.Debug.WriteLine($"[Profil] Réglages non envoyés : {ex.Message}");
        }
    }

    private static string ToApiLanguage(string language) => language == "English" ? "en" : "fr";

    private static string FromApiLanguage(string language) => language == "en" ? "English" : "Français";

    private void RefreshList()
    {
        MyReports.Clear();
        var wanted = _mine.Where(r => (r.Status == ReportStatus.Resolved) == ShowResolved);
        foreach (var report in wanted) MyReports.Add(report);
        IsMyReportsEmpty = MyReports.Count == 0;
    }
}