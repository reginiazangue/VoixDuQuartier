using VoixDuQuartier.Services;
using VoixDuQuartier.ViewModels;
using VoixDuQuartier.Views;

namespace VoixDuQuartier;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Une seule famille de polices (Inter), 2 graisses : écoconception + cohérence avec Figma.
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        // Services (uniques pour toute l'application)
        builder.Services.AddSingleton<ISessionService, ApiSessionService>();
        builder.Services.AddSingleton<IDataService, ApiDataService>();
        builder.Services.AddSingleton<ApiService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<FeedViewModel>();
        builder.Services.AddTransient<NewReportViewModel>();
        builder.Services.AddTransient<ConfirmationViewModel>();
        builder.Services.AddTransient<DetailViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Pages
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<FeedPage>();
        builder.Services.AddTransient<NewReportPage>();
        builder.Services.AddTransient<ConfirmationPage>();
        builder.Services.AddTransient<DetailPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }
}
