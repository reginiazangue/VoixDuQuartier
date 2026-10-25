using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class NotificationsViewModel : BaseViewModel
{
    private readonly IDataService _data;

    public NotificationsViewModel(IDataService data) => _data = data;

    public ObservableCollection<AppNotification> Items { get; } = new();

    /// <summary>État « vide » : aucune notification (ou notifications désactivées dans le profil).</summary>
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string emptyTitle = "Aucune notification pour le moment";
    [ObservableProperty] private string emptyMessage = "Vous serez prévenu dès qu'un signalement évolue.";

    [RelayCommand]
    private async Task LoadAsync()
    {
        Items.Clear();

        if (Preferences.Default.Get("notifications_enabled", true))
        {
            foreach (var n in await _data.GetNotificationsAsync()) Items.Add(n);
            EmptyTitle = "Aucune notification pour le moment";
            EmptyMessage = "Vous serez prévenu dès qu'un signalement évolue.";
        }
        else
        {
            EmptyTitle = "Notifications désactivées";
            EmptyMessage = "Activez-les dans votre profil pour suivre vos signalements.";
        }

        IsEmpty = Items.Count == 0;
    }

    /// <summary>Marque tout comme lu à l'écran, puis l'enregistre sur le serveur
    /// (sinon les notifications redeviendraient « non lues » au prochain chargement).</summary>
    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        foreach (var n in Items) n.IsRead = true;
        await _data.MarkAllNotificationsReadAsync();
    }
}