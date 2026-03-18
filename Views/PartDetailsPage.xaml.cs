using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class PartDetailsPage : ContentPage
{
    public PartDetailsPage(PartDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
