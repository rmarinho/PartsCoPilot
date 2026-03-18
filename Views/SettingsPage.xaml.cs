using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SettingsViewModel vm)
            await vm.LoadSettingsCommand.ExecuteAsync(null);
    }
}
