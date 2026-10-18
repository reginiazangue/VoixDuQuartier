using VoixDuQuartier.Services;

namespace VoixDuQuartier.Views;

public partial class SplashPage : ContentPage
{
    private readonly ISessionService _session;

    public SplashPage(ISessionService session)
    {
        InitializeComponent();
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(1800);
        await Shell.Current.GoToAsync(_session.IsAuthenticated ? "//main/feed" : "//login");
    }
}
