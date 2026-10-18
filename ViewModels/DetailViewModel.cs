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

    [RelayCommand]
    private void ToggleSupport()
    {
        if (Report is null) return;

        if (Report.IsSupported)
        {
            Report.IsSupported = false;
            Report.SupportCount--;
        }
        else
        {
            Report.IsSupported = true;
            Report.SupportCount++;
        }
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");
}
