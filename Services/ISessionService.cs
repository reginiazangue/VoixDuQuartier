using VoixDuQuartier.Models;

namespace VoixDuQuartier.Services;

public interface ISessionService
{
    bool IsAuthenticated { get; }
    UserProfile? CurrentUser { get; }

    Task<bool> LoginAsync(string email, string password);
    Task RegisterAsync(UserProfile profile, string password);
    void Logout();
}
