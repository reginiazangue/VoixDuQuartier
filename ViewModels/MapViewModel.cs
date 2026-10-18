using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class MapViewModel : BaseViewModel
{
    private readonly IDataService _data;
    private List<Report> _all = new();

    public MapViewModel(IDataService data)
    {
        _data = data;
        CategoryChips = new ObservableCollection<FilterChip>
        {
            new("Toutes", null, true),
            new(Categories.Road, Categories.Road),
            new(Categories.Lighting, Categories.Lighting),
            new(Categories.Waste, Categories.Waste),
            new(Categories.Other, Categories.Other)
        };
    }

    public ObservableCollection<FilterChip> CategoryChips { get; }
    public ObservableCollection<Report> Pins { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _all = (await _data.GetReportsAsync()).ToList();
        Apply();
    }

    [RelayCommand]
    private void SelectCategory(FilterChip chip)
    {
        foreach (var c in CategoryChips) c.IsSelected = c == chip;
        Apply();
    }

    [RelayCommand]
    private Task OpenDetailAsync(Report report)
        => Shell.Current.GoToAsync($"detail?id={report.Id}");

    private void Apply()
    {
        var category = CategoryChips.FirstOrDefault(c => c.IsSelected)?.Value;
        Pins.Clear();
        foreach (var report in _all.Where(r => category is null || r.Category == category))
            Pins.Add(report);
    }
}
