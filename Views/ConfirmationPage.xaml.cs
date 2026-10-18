using VoixDuQuartier.ViewModels;

namespace VoixDuQuartier.Views;

public partial class ConfirmationPage : ContentPage
{
    public ConfirmationPage(ConfirmationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>Le bouton « retour » du téléphone ramène au fil (et non au formulaire déjà envoyé).</summary>
    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//main/feed");
        return true;
    }
}
