using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Helpers;
using VoixDuQuartier.Models;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly ISessionService _session;

    public RegisterViewModel(ISessionService session) => _session = session;

    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string neighborhood = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;

    [ObservableProperty] private string fullNameError = string.Empty;
    [ObservableProperty] private string emailError = string.Empty;
    [ObservableProperty] private string neighborhoodError = string.Empty;
    [ObservableProperty] private string passwordError = string.Empty;
    [ObservableProperty] private string confirmError = string.Empty;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        FullNameError = Validators.ValidateRequired(FullName, "Saisissez votre nom complet") ?? string.Empty;
        EmailError = Validators.ValidateEmail(Email) ?? string.Empty;
        NeighborhoodError = Validators.ValidateRequired(Neighborhood, "Indiquez votre quartier") ?? string.Empty;
        PasswordError = Validators.ValidateNewPassword(Password) ?? string.Empty;
        ConfirmError = Validators.ValidateConfirmation(Password, ConfirmPassword) ?? string.Empty;

        if (new[] { FullNameError, EmailError, NeighborhoodError, PasswordError, ConfirmError }.Any(e => e.Length > 0))
            return;

        IsBusy = true;
        try
        {
            await _session.RegisterAsync(new UserProfile
            {
                FullName = FullName.Trim(),
                Email = Email.Trim(),
                Neighborhood = Neighborhood.Trim()
            }, Password);

            Password = ConfirmPassword = string.Empty;
            await Shell.Current.GoToAsync("//main/feed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToLoginAsync() => Shell.Current.GoToAsync("//login");
}
