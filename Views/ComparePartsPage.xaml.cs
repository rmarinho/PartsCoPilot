using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class ComparePartsPage : ContentPage
{
    public ComparePartsPage(ComparePartsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
