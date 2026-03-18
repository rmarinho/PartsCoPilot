using PartsCopilot.Data;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;

namespace PartsCopilot;

public partial class App : Application
{
	public App(AppDatabase database, ISettingsService settings)
	{
		InitializeComponent();
		Task.Run(async () => await database.InitializeAsync());

		// Wire up theme changes from SettingsViewModel
		SettingsViewModel.OnThemeChanged = preference =>
		{
			UserAppTheme = preference switch
			{
				ThemePreference.Light => AppTheme.Light,
				ThemePreference.Dark => AppTheme.Dark,
				_ => AppTheme.Unspecified,
			};
		};

		// Apply persisted theme on startup
		var saved = settings.GetThemePreference();
		if (saved != ThemePreference.System)
		{
			UserAppTheme = saved == ThemePreference.Light ? AppTheme.Light : AppTheme.Dark;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}