using VoixDuQuartier.Views;

namespace VoixDuQuartier;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages ouvertes « par-dessus » les onglets (avec bouton retour).
        Routing.RegisterRoute("detail", typeof(DetailPage));
        Routing.RegisterRoute("confirmation", typeof(ConfirmationPage));
    }
}
