using VoixDuQuartier.Models;

namespace VoixDuQuartier.Controls;

/// <summary>Pastille de statut : gris = envoyé, orange = en traitement, vert = résolu.</summary>
public partial class StatusPill : ContentView
{
    public static readonly BindableProperty StatusProperty =
        BindableProperty.Create(
            nameof(Status), typeof(ReportStatus), typeof(StatusPill), ReportStatus.Sent,
            propertyChanged: (bindable, _, _) => ((StatusPill)bindable).Refresh());

    public StatusPill()
    {
        InitializeComponent();
        Refresh();
    }

    public ReportStatus Status
    {
        get => (ReportStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private void Refresh()
    {
        Pill.BackgroundColor = StatusHelper.Color(Status);
        PillLabel.Text = StatusHelper.Label(Status);
    }
}
