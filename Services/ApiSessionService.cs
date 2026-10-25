using System.Diagnostics;
using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

/// <summary>
/// Remplace SessionService : les comptes sont maintenant créés et vérifiés par le backend.
/// Le token de connexion est conservé par ApiService (SecureStorage).
/// </summary>
public class ApiSessionService : ISessionService
{
    private const string KeySignedIn = "user_signed_in";
    private const string KeyEmail = "user_email";
    private const string KeyName = "user_name";
    private const string KeyNeighborhood = "user_neighborhood";

    private readonly ApiService _api;
    private bool _handlingExpiry;

    public bool IsAuthenticated => CurrentUser is not null;

    public UserProfile? CurrentUser { get; private set; }

    public ApiSessionService(ApiService api)
    {
        _api = api;
        _api.SessionExpired += OnSessionExpired;

        // Au démarrage, on retrouve l'utilisateur connecté la dernière fois.
        if (Preferences.Default.Get(KeySignedIn, false))
        {
            CurrentUser = new UserProfile
            {
                Email = Preferences.Default.Get(KeyEmail, string.Empty),
                FullName = Preferences.Default.Get(KeyName, "Utilisateur"),
                Neighborhood = Preferences.Default.Get(KeyNeighborhood, string.Empty)
            };
        }
    }

    /// <summary>Retourne false si l'e-mail ou le mot de passe est incorrect.
    /// Les autres problèmes (serveur injoignable...) lèvent une ApiException avec un message prêt à afficher.</summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var auth = await _api.LoginAsync(email, password);
            SetUser(auth.User);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            return false;
        }
    }

    public async Task RegisterAsync(UserProfile profile, string password)
    {
        // Le formulaire a déjà vérifié que la confirmation est identique : on envoie le mot de passe deux fois.
        var auth = await _api.RegisterAsync(
            profile.FullName, profile.Email, profile.Neighborhood, password, password);
        SetUser(auth.User);
    }

    public void Logout()
    {
        _api.Logout();
        ClearLocalSession();
    }

    private void SetUser(ApiUser user)
    {
        CurrentUser = new UserProfile
        {
            Email = user.Email,
            FullName = user.FullName,
            Neighborhood = user.Quartier
        };

        Preferences.Default.Set(KeySignedIn, true);
        Preferences.Default.Set(KeyEmail, user.Email);
        Preferences.Default.Set(KeyName, user.FullName);
        Preferences.Default.Set(KeyNeighborhood, user.Quartier);
    }

    private void ClearLocalSession()
    {
        CurrentUser = null;
        Preferences.Default.Remove(KeySignedIn);
        Preferences.Default.Remove(KeyEmail);
        Preferences.Default.Remove(KeyName);
        Preferences.Default.Remove(KeyNeighborhood);
    }

    /// <summary>Le serveur a refusé le token : on déconnecte et on renvoie vers l'écran de connexion.</summary>
    private void OnSessionExpired()
    {
        if (_handlingExpiry || CurrentUser is null) return;
        _handlingExpiry = true;

        _api.Logout();
        ClearLocalSession();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.GoToAsync("//login");
                    await Shell.Current.DisplayAlert("Session expirée", "Veuillez vous reconnecter.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Session] {ex.Message}");
            }
            finally
            {
                _handlingExpiry = false;
            }
        });
    }
}