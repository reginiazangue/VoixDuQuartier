using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class FeedViewModel : BaseViewModel
{
    private readonly IDataService _data;
    private List<Report> _all = new();

    public FeedViewModel(IDataService data)
    {
        _data = data;

        CategoryChips = new ObservableCollection<FilterChip>
        {
            new("Tous", null, true),
            new(Categories.Road, Categories.Road),
            new(Categories.Lighting, Categories.Lighting),
            new(Categories.Waste, Categories.Waste),
            new(Categories.Other, Categories.Other)
        };

        StatusChips = new ObservableCollection<FilterChip>
        {
            new("Tous les statuts", null, true),
            new("Envoyé", nameof(ReportStatus.Sent)),
            new("Pris en charge", nameof(ReportStatus.Acknowledged)),
            new("En cours", nameof(ReportStatus.InProgress)),
            new("Résolu", nameof(ReportStatus.Resolved))
        };
    }

    public ObservableCollection<FilterChip> CategoryChips { get; }
    public ObservableCollection<FilterChip> StatusChips { get; }
    public ObservableCollection<Report> Reports { get; } = new();

    /// <summary>État « chargement » (squelettes gris) affiché au premier chargement.</summary>
    [ObservableProperty] private bool isLoading;

    /// <summary>État « vide » : aucun signalement ne correspond aux filtres.</summary>
    [ObservableProperty] private bool isEmpty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = _all.Count == 0;
        try
        {
            _all = (await _data.GetReportsAsync()).ToList();
        }
        finally
        {
            IsLoading = false;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private void SelectCategory(FilterChip chip)
    {
        Select(CategoryChips, chip);
        ApplyFilters();
    }

    [RelayCommand]
    private void SelectStatus(FilterChip chip)
    {
        Select(StatusChips, chip);
        ApplyFilters();
    }

    [RelayCommand]
    private Task OpenDetailAsync(Report report)
        => Shell.Current.GoToAsync($"detail?id={report.Id}");

    private static void Select(IEnumerable<FilterChip> chips, FilterChip selected)
    {
        foreach (var chip in chips) chip.IsSelected = chip == selected;
    }

    private void ApplyFilters()
    {
        var category = CategoryChips.FirstOrDefault(c => c.IsSelected)?.Value;
        var status = StatusChips.FirstOrDefault(c => c.IsSelected)?.Value;

        var query = _all.Where(r =>
            (category is null || r.Category == category) &&
            (status is null || r.Status.ToString() == status));

        Reports.Clear();
        foreach (var report in query) Reports.Add(report);

        IsEmpty = Reports.Count == 0 && !IsLoading;
    }
}
