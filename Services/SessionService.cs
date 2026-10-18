using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

/// <summary>
/// Session de DÉMONSTRATION (frontend seul) : aucun mot de passe n'est stocké ni vérifié.
/// Avec le backend, ce service enverra les identifiants en HTTPS, recevra un jeton (JWT)
/// et le conservera dans SecureStorage.
/// </summary>
public class SessionService : ISessionService
{
    private const string KeyEmail = "user_email";
    private const string KeyName = "user_name";
    private const string KeyNeighborhood = "user_neighborhood";

    public bool IsAuthenticated => CurrentUser is not null;

    public UserProfile? CurrentUser { get; private set; }

    public SessionService()
    {
        var email = Preferences.Default.Get(KeyEmail, string.Empty);
        if (!string.IsNullOrEmpty(email))
        {
            CurrentUser = new UserProfile
            {
                Email = email,
                FullName = Preferences.Default.Get(KeyName, "Utilisateur"),
                Neighborhood = Preferences.Default.Get(KeyNeighborhood, string.Empty)
            };
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        await Task.Delay(500); // simule l'appel réseau
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return false;

        var knownEmail = Preferences.Default.Get(KeyEmail, string.Empty);
        var sameAccount = string.Equals(knownEmail, email, StringComparison.OrdinalIgnoreCase);

        CurrentUser = new UserProfile
        {
            Email = email,
            FullName = sameAccount ? Preferences.Default.Get(KeyName, DefaultName(email)) : DefaultName(email),
            Neighborhood = sameAccount ? Preferences.Default.Get(KeyNeighborhood, string.Empty) : string.Empty
        };
        Save(CurrentUser);
        return true;
    }

    public async Task RegisterAsync(UserProfile profile, string password)
    {
        await Task.Delay(500);
        CurrentUser = profile;
        Save(profile);
    }

    public void Logout()
    {
        CurrentUser = null;
        Preferences.Default.Remove(KeyEmail);
        Preferences.Default.Remove(KeyName);
        Preferences.Default.Remove(KeyNeighborhood);
    }

    private static string DefaultName(string email) => email.Split('@')[0];

    private static void Save(UserProfile user)
    {
        Preferences.Default.Set(KeyEmail, user.Email);
        Preferences.Default.Set(KeyName, user.FullName);
        Preferences.Default.Set(KeyNeighborhood, user.Neighborhood);
    }
}
