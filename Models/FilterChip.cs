using CommunityToolkit.Mvvm.ComponentModel;

namespace VoixDuQuartier.Models;

/// <summary>Pastille de filtre (catégorie ou statut). Value = null signifie « Tous ».</summary>
public partial class FilterChip : ObservableObject
{
    public FilterChip(string label, string? value, bool isSelected = false)
    {
        Label = label;
        Value = value;
        this.isSelected = isSelected;
    }

    public string Label { get; }
    public string? Value { get; }

    [ObservableProperty]
    private bool isSelected;
}

public class TimelineStep
{
    public string Label { get; init; } = string.Empty;
    public bool Reached { get; init; }
}
