using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class ManualViewerPage : ContentPage
{
    public ManualViewerPage(ManualViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
