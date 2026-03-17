using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PartsCopilot.Data;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
using PartsCopilot.Views;

namespace PartsCopilot;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Database
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "partscopilot.db3");
		builder.Services.AddSingleton(new AppDatabase(dbPath));

		// Services
		builder.Services.AddSingleton<IPdfIngestionService, PdfIngestionService>();
		builder.Services.AddSingleton<IManualParser, PorscheClassicManualParser>();
		builder.Services.AddSingleton<IPartsRepository, PartsRepository>();
		builder.Services.AddSingleton<ISearchService, HybridSearchService>();
		builder.Services.AddSingleton<IUserDataRepository, UserDataRepository>();

		// ViewModels
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<SearchViewModel>();

		// Pages
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SearchPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
