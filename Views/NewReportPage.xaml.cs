using VoixDuQuartier.ViewModels;

namespace VoixDuQuartier.Views;

public partial class NewReportPage : ContentPage
{
    public NewReportPage(NewReportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
