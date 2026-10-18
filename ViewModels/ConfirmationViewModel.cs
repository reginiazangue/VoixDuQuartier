using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

[QueryProperty(nameof(ReportId), "id")]
public partial class ConfirmationViewModel : BaseViewModel
{
    private readonly IDataService _data;

    public ConfirmationViewModel(IDataService data) => _data = data;

    [ObservableProperty] private string reportId = string.Empty;
    [ObservableProperty] private Report? report;

    partial void OnReportIdChanged(string value) => Report = _data.GetReport(value);

    [RelayCommand]
    private Task ViewReportAsync() => Shell.Current.GoToAsync($"detail?id={ReportId}");

    [RelayCommand]
    private Task BackToFeedAsync() => Shell.Current.GoToAsync("//main/feed");
}
