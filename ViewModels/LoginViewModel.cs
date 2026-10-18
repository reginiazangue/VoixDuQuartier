using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoixDuQuartier.Helpers;
using VoixDuQuartier.Services;

namespace VoixDuQuartier.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ISessionService _session;

    public LoginViewModel(ISessionService session) => _session = session;

    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string emailError = string.Empty;
    [ObservableProperty] private string passwordError = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        EmailError = Validators.ValidateEmail(Email) ?? string.Empty;
        PasswordError = Validators.ValidateRequired(Password, "Saisissez votre mot de passe") ?? string.Empty;
        if (EmailError.Length > 0 || PasswordError.Length > 0) return;

        IsBusy = true;
        try
        {
            var ok = await _session.LoginAsync(Email.Trim(), Password);
            if (!ok)
            {
                PasswordError = "E-mail ou mot de passe incorrect";
                return;
            }

            Password = string.Empty;
            await Shell.Current.GoToAsync("//main/feed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => Shell.Current.GoToAsync("//register");

    [RelayCommand]
    private Task ForgotPasswordAsync() =>
        Shell.Current.DisplayAlert(
            "Mot de passe oublié",
            "La récupération par e-mail sera disponible avec le backend.",
            "OK");
}
