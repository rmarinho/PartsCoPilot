using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IPartsRepository _repo;
    private readonly IPdfIngestionService _ingestion;
    private readonly IManualParser _parser;
    private readonly IUserDataRepository _userData;

    public HomeViewModel(IPartsRepository repo, IPdfIngestionService ingestion, IManualParser parser, IUserDataRepository userData)
    {
        _repo = repo;
        _ingestion = ingestion;
        _parser = parser;
        _userData = userData;
    }

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string? _importStatus;

    [ObservableProperty]
    private int _importProgress;

    [ObservableProperty]
    private string? _activeManualTitle;

    [ObservableProperty]
    private int _totalParts;

    [ObservableProperty]
    private bool _hasManual;

    [RelayCommand]
    private async Task ImportManualAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a parts manual PDF",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, ["pdf"] },
                    { DevicePlatform.Android, ["application/pdf"] },
                    { DevicePlatform.MacCatalyst, ["pdf"] },
                    { DevicePlatform.WinUI, [".pdf"] },
                })
            });

            if (result is null) return;

            IsImporting = true;
            ImportStatus = "Extracting pages...";
            ImportProgress = 0;

            var manualId = Guid.NewGuid().ToString();

            // Stage 1: Extract pages
            var pages = await _ingestion.ExtractPagesAsync(result.FullPath, manualId);
            ImportStatus = $"Extracted {pages.Count} pages. Parsing parts...";
            ImportProgress = 30;

            // Stage 2: Parse parts
            var progress = new Progress<int>(p => ImportProgress = 30 + (int)(p * 0.6));
            var parts = await _parser.ParseAsync(pages, manualId, progress);
            ImportProgress = 90;

            // Stage 3: Save to database
            ImportStatus = $"Saving {parts.Count} parts...";
            var manual = new ManualMetadata
            {
                Id = manualId,
                Title = Path.GetFileNameWithoutExtension(result.FileName),
                FilePath = result.FullPath,
                ManualType = _parser.ManualType,
                PageCount = pages.Count,
                PartCount = parts.Count
            };

            await _repo.SaveManualAsync(manual);
            await _repo.SavePagesAsync(pages);
            await _repo.SavePartsAsync(parts);
            ImportProgress = 100;

            ImportStatus = $"Imported {parts.Count} parts from {pages.Count} pages";
            ActiveManualTitle = manual.Title;
            TotalParts = parts.Count;
            HasManual = true;
        }
        catch (Exception ex)
        {
            ImportStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task LoadStateAsync()
    {
        var manuals = await _repo.GetAllManualsAsync();
        var active = manuals.FirstOrDefault();
        if (active is not null)
        {
            ActiveManualTitle = active.Title;
            TotalParts = active.PartCount;
            HasManual = true;
        }
    }

    [RelayCommand]
    private async Task GoToSearchAsync()
    {
        await Shell.Current.GoToAsync("//search");
    }
}
