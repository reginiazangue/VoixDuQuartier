using CommunityToolkit.Mvvm.ComponentModel;

namespace VoixDuQuartier.Models;

public partial class AppNotification : ObservableObject
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnread))]
    private bool isRead;

    public bool IsUnread => !IsRead;
}
