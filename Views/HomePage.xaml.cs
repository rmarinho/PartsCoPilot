using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
            await vm.LoadStateCommand.ExecuteAsync(null);
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("search");
    }
}
