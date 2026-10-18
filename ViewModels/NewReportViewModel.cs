using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Helpers;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class NewReportViewModel : BaseViewModel
{
    private readonly IDataService _data;
    private readonly ISessionService _session;

    public NewReportViewModel(IDataService data, ISessionService session)
    {
        _data = data;
        _session = session;

        CategoryChips = new ObservableCollection<FilterChip>(
            Categories.All.Select((c, i) => new FilterChip(c, c, i == 0)));

        // IsBusy est déclaré dans BaseViewModel : on écoute son changement pour réévaluer le bouton « Envoyer ».
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy)) SendCommand.NotifyCanExecuteChanged();
        };

        ResetForm();
    }

    public ObservableCollection<FilterChip> CategoryChips { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto), nameof(HasNoPhoto))]
    private string? photoPath;

    [ObservableProperty] private string locationText = "Ma position actuelle";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionCounter), nameof(DescriptionError))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string description = string.Empty;

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public bool HasNoPhoto => !HasPhoto;
    public string DescriptionCounter => $"{Description.Length}/{Validators.MaxDescriptionLength}";

    /// <summary>Message d'erreur affiché dès que l'utilisateur a commencé à écrire (état « erreur »).</summary>
    public string DescriptionError =>
        Description.Length > 0 ? Validators.ValidateDescription(Description) ?? string.Empty : string.Empty;

    private bool CanSend() => Validators.ValidateDescription(Description) is null && !IsBusy;

    [RelayCommand]
    private void SelectCategory(FilterChip chip)
    {
        foreach (var c in CategoryChips) c.IsSelected = c == chip;
    }

    [RelayCommand]
    private async Task ChoosePhotoAsync()
    {
        try
        {
            var action = await Shell.Current.DisplayActionSheet(
                "Ajouter une photo", "Annuler", null,
                "Prendre une photo", "Choisir dans la galerie");

            FileResult? result = action switch
            {
                "Prendre une photo" when MediaPicker.Default.IsCaptureSupported
                    => await MediaPicker.Default.CapturePhotoAsync(),
                "Choisir dans la galerie" => await MediaPicker.Default.PickPhotoAsync(),
                _ => null
            };
            if (result is null) return;

            // Copie dans le cache de l'application pour garder une photo accessible.
            var target = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(result.FileName)}");
            await using var source = await result.OpenReadAsync();
            await using var destination = File.OpenWrite(target);
            await source.CopyToAsync(destination);

            PhotoPath = target;
        }
        catch (Exception ex) when (ex is PermissionException or FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Photo", "Impossible d'accéder à la caméra ou à la galerie.", "OK");
        }
    }

    [RelayCommand]
    private void RemovePhoto() => PhotoPath = null;

    [RelayCommand]
    private async Task UseMyLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Position", "Autorisez la localisation pour utiliser votre position.", "OK");
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            LocationText = location is null
                ? "Position introuvable"
                : $"{location.Latitude:F4}, {location.Longitude:F4}";
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException)
        {
            await Shell.Current.DisplayAlert("Position", "La géolocalisation n'est pas disponible sur cet appareil.", "OK");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        IsBusy = true;
        try
        {
            var category = CategoryChips.First(c => c.IsSelected).Label;
            var neighborhood = _session.CurrentUser?.Neighborhood;
            var location = string.IsNullOrWhiteSpace(neighborhood) ? LocationText : neighborhood!;

            var report = await _data.AddReportAsync(new Report
            {
                Title = TitleBuilder.Build(category, Description),
                Description = Description.Trim(),
                Category = category,
                Location = location,
                PhotoPath = PhotoPath,
                IsMine = true,
                // Position fictive sur la carte tant que la géolocalisation n'est pas stockée.
                MapX = 0.15 + Random.Shared.NextDouble() * 0.7,
                MapY = 0.15 + Random.Shared.NextDouble() * 0.7
            });

            ResetForm();
            await Shell.Current.GoToAsync($"confirmation?id={report.Id}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        PhotoPath = null;
        Description = string.Empty;
        LocationText = "Ma position actuelle";
        foreach (var chip in CategoryChips) chip.IsSelected = chip == CategoryChips[0];
    }
}