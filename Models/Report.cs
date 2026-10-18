using CommunityToolkit.Mvvm.ComponentModel;

namespace VoixDuQuartier.Models;

public partial class Report : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = Categories.Road;
    public string Location { get; init; } = string.Empty;
    public string? PhotoPath { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public bool IsMine { get; init; }

    /// <summary>Position relative sur la carte (0 à 1), en attendant la vraie géolocalisation.</summary>
    public double MapX { get; init; }
    public double MapY { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    private ReportStatus status = ReportStatus.Sent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportText), nameof(SupportButtonText))]
    private int supportCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportButtonText))]
    private bool isSupported;

    public string StatusLabel => StatusHelper.Label(Status);
    public string Subtitle => $"{Category}, {Location}";
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public bool HasNoPhoto => !HasPhoto;
    public Rect PinBounds => new(MapX, MapY, -1, -1);

    public string SupportText => SupportCount switch
    {
        0 => "Aucun soutien",
        1 => "1 soutien",
        _ => $"{SupportCount} soutiens"
    };

    public string SupportButtonText => IsSupported
        ? $"Soutenu ({SupportCount})"
        : $"Soutenir ({SupportCount})";
}
