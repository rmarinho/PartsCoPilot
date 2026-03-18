using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class ManualViewerViewModel : ObservableObject, IQueryAttributable
{
    private readonly IManualNavigationService _navigation;
    private readonly IPartsRepository _repo;

    public ManualViewerViewModel(IManualNavigationService navigation, IPartsRepository repo)
    {
        _navigation = navigation;
        _repo = repo;
    }

    [ObservableProperty]
    private ManualPage? _page;

    [ObservableProperty]
    private string _pageTitle = "Manual Page";

    [ObservableProperty]
    private string _pageContent = "";

    [ObservableProperty]
    private string? _illustration;

    [ObservableProperty]
    private string? _section;

    [ObservableProperty]
    private string? _pageType;

    [ObservableProperty]
    private int _pageNumber;

    [ObservableProperty]
    private string _manualId = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasContent;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _manualTitle;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private string _pageIndicator = "";

    [ObservableProperty]
    private ImageSource? _pageImageSource;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private bool _isImageMode = true;

    public bool IsTextMode => !IsImageMode || !HasImage;
    public bool ShowImageView => IsImageMode && HasImage;
    public string ViewModeLabel => IsImageMode && HasImage ? "Text" : "Image";
    public bool CanToggleViewMode => HasImage;

    partial void OnIsImageModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTextMode));
        OnPropertyChanged(nameof(ShowImageView));
        OnPropertyChanged(nameof(ViewModeLabel));
    }

    partial void OnHasImageChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTextMode));
        OnPropertyChanged(nameof(ShowImageView));
        OnPropertyChanged(nameof(ViewModeLabel));
        OnPropertyChanged(nameof(CanToggleViewMode));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ManualId", out var midObj) && midObj is string mid)
            ManualId = mid;

        if (query.TryGetValue("PageNumber", out var pnObj) && pnObj is int pn)
            PageNumber = pn;
        else if (query.TryGetValue("PageNumber", out var pnStrObj) && pnStrObj is string pnStr && int.TryParse(pnStr, out var parsed))
            PageNumber = parsed;

        if (query.TryGetValue("Illustration", out var illObj) && illObj is string ill)
            Illustration = ill;

        LoadPageCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadPageAsync()
    {
        if (string.IsNullOrEmpty(ManualId) || PageNumber <= 0)
        {
            HasError = true;
            HasContent = false;
            ErrorMessage = "No page information provided.";
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            HasContent = false;
            HasImage = false;
            PageImageSource = null;

            // Load manual metadata for title and page count
            var manual = await _repo.GetManualAsync(ManualId);
            if (manual is not null)
            {
                ManualTitle = manual.Title;
                TotalPages = manual.PageCount;
            }

            // Load the page
            var page = await _repo.GetPageAsync(ManualId, PageNumber);
            if (page is null)
            {
                HasError = true;
                ErrorMessage = $"Page {PageNumber} not found in this manual.";
                return;
            }

            Page = page;
            PageContent = page.RawText;
            Illustration = page.Illustration ?? Illustration;
            Section = page.Section;
            PageType = page.PageType;
            PageTitle = BuildPageTitle(page);
            HasContent = true;

            // Load rendered page image if available
            if (page.ImageData is { Length: > 0 })
            {
                PageImageSource = ImageSource.FromStream(() => new MemoryStream(page.ImageData));
                HasImage = true;
                IsImageMode = true;
            }
            else
            {
                HasImage = false;
                IsImageMode = false;
            }

            UpdateNavigationState();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load page: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        if (HasImage)
            IsImageMode = !IsImageMode;
    }

    [RelayCommand]
    private async Task GoToPreviousPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
            await LoadPageAsync();
        }
    }

    [RelayCommand]
    private async Task GoToNextPageAsync()
    {
        if (TotalPages <= 0 || PageNumber < TotalPages)
        {
            PageNumber++;
            await LoadPageAsync();
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void UpdateNavigationState()
    {
        CanGoBack = PageNumber > 1;
        CanGoForward = TotalPages <= 0 || PageNumber < TotalPages;
        PageIndicator = TotalPages > 0
            ? $"Page {PageNumber} of {TotalPages}"
            : $"Page {PageNumber}";
    }

    private static string BuildPageTitle(ManualPage page)
    {
        var parts = new List<string> { $"Page {page.PageNumber}" };
        if (!string.IsNullOrWhiteSpace(page.Illustration))
            parts.Add($"Ill. {page.Illustration}");
        if (!string.IsNullOrWhiteSpace(page.Section))
            parts.Add(page.Section);
        return string.Join(" — ", parts);
    }
}
