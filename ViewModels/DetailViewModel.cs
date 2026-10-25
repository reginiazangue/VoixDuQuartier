using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

[QueryProperty(nameof(ReportId), "id")]
public partial class DetailViewModel : BaseViewModel
{
    private static readonly ReportStatus[] Steps_ =
    {
        ReportStatus.Sent, ReportStatus.Acknowledged, ReportStatus.InProgress, ReportStatus.Resolved
    };

    private readonly IDataService _data;

    public DetailViewModel(IDataService data) => _data = data;

    [ObservableProperty] private string reportId = string.Empty;
    [ObservableProperty] private Report? report;

    /// <summary>Frise du statut : Envoyé → Pris en charge → En cours → Résolu.</summary>
    public ObservableCollection<TimelineStep> Steps { get; } = new();

    partial void OnReportIdChanged(string value)
    {
        Report = _data.GetReport(value);
        BuildSteps();
    }

    private void BuildSteps()
    {
        Steps.Clear();
        if (Report is null) return;

        foreach (var step in Steps_)
        {
            Steps.Add(new TimelineStep
            {
                Label = StatusHelper.Label(step),
                Reached = (int)Report.Status >= (int)step
            });
        }
    }

    /// <summary>
    /// Le soutien est d'abord enregistré par le serveur. Le bouton et le compteur ne changent
    /// qu'une fois la réponse reçue : si le serveur refuse, l'écran reste exact.
    /// </summary>
    [RelayCommand]
    private async Task ToggleSupportAsync()
    {
        if (Report is null || IsBusy) return; // IsBusy évite les doubles appuis

        IsBusy = true;
        try
        {
            await _data.ToggleSupportAsync(Report);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");
}