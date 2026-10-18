namespace VoixDuQuartier;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Mode sombre par défaut (écoconception), modifiable dans le profil.
        var dark = Preferences.Default.Get("dark_mode", true);
        UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

#if WINDOWS
        // Sur PC, la fenêtre prend la forme d'un téléphone pour tester l'interface mobile.
        window.Width = 400;
        window.Height = 860;
#endif

        return window;
    }
}
