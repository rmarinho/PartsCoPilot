using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class SearchPage : ContentPage
{
    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SearchViewModel vm)
            await vm.LoadManualInfoCommand.ExecuteAsync(null);
    }
}
