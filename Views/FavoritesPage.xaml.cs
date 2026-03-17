using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class FavoritesPage : ContentPage
{
    public FavoritesPage(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FavoritesViewModel vm)
            await vm.LoadDataCommand.ExecuteAsync(null);
    }
}
