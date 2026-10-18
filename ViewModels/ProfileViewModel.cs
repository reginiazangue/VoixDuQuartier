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

    partial void OnDarkModeChanged(bool value)
    {
        Preferences.Default.Set("dark_mode", value);
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    partial void OnNotificationsEnabledChanged(bool value)
        => Preferences.Default.Set("notifications_enabled", value);

    // La traduction complète de l'interface viendra avec les fichiers de ressources (.resx).
    partial void OnLanguageChanged(string value)
        => Preferences.Default.Set("language", value);

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = _session.CurrentUser;
        FullName = user?.FullName ?? string.Empty;
        Neighborhood = user is null || string.IsNullOrEmpty(user.Neighborhood) ? string.Empty : $"Quartier : {user.Neighborhood}";
        Initial = user?.Initial ?? "?";

        _mine = (await _data.GetReportsAsync()).Where(r => r.IsMine).ToList();
        ReportCount = _mine.Count;
        SupportCount = _mine.Sum(r => r.SupportCount);
        ResolvedCount = _mine.Count(r => r.Status == ReportStatus.Resolved);
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

    private void RefreshList()
    {
        MyReports.Clear();
        var wanted = _mine.Where(r => (r.Status == ReportStatus.Resolved) == ShowResolved);
        foreach (var report in wanted) MyReports.Add(report);
        IsMyReportsEmpty = MyReports.Count == 0;
    }
}
