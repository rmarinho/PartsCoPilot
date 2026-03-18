using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using PartsCopilot.Data;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
using PartsCopilot.Views;
#if DEBUG
using MauiDevFlow.Agent;
#endif

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
		builder.Services.AddSingleton<SeedDataService>();

		// Settings (SecureStorage-backed)
		builder.Services.AddSingleton<ISettingsService, SettingsService>();

		// AI layer
		builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
		builder.Services.AddSingleton<IPartsAiService, PartsAiService>();
		builder.Services.AddSingleton<IManualNavigationService, ManualNavigationService>();

		// Semantic Kernel — configure your API key via environment or settings
		var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-placeholder";
		var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
		builder.Services.AddSingleton(sp =>
		{
			var kernelBuilder = Kernel.CreateBuilder();
			kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiKey);
			kernelBuilder.Services.AddLogging(l => l.AddDebug());
			return kernelBuilder.Build();
		});

		// ViewModels
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<SearchViewModel>();
		builder.Services.AddTransient<FavoritesViewModel>();
		builder.Services.AddTransient<PartDetailsViewModel>();
		builder.Services.AddTransient<ManualViewerViewModel>();
		builder.Services.AddTransient<ComparePartsViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Pages
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SearchPage>();
		builder.Services.AddTransient<FavoritesPage>();
		builder.Services.AddTransient<PartDetailsPage>();
		builder.Services.AddTransient<ManualViewerPage>();
		builder.Services.AddTransient<ComparePartsPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
		builder.AddMauiDevFlowAgent();
#endif

		return builder.Build();
	}
}
