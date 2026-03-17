using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _search;
    private readonly IPartsRepository _repo;
    private readonly IUserDataRepository _userData;
    private CancellationTokenSource? _searchCts;

    public SearchViewModel(ISearchService search, IPartsRepository repo, IUserDataRepository userData)
    {
        _search = search;
        _repo = repo;
        _userData = userData;
    }

    [ObservableProperty]
    private string _queryText = "";

    [ObservableProperty]
    private string? _selectedModel;

    [ObservableProperty]
    private int? _selectedYear;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private string? _searchDuration;

    [ObservableProperty]
    private string? _activeManualTitle;

    public ObservableCollection<SearchCandidateViewModel> Results { get; } = [];

    public ObservableCollection<string> AvailableModels { get; } = ["911", "911 T", "911 E", "911 S", "911 L", "912"];

    public ObservableCollection<int> AvailableYears { get; } = [1965, 1966, 1967, 1968, 1969];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(QueryText))
            return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            IsSearching = true;
            HasNoResults = false;
            HasResults = false;
            ErrorMessage = null;
            Results.Clear();

            var context = new VehicleContext(Model: SelectedModel, Year: SelectedYear);
            var query = new SearchQuery(QueryText, context);

            var result = await _search.SearchAsync(query, ct);

            foreach (var candidate in result.Candidates)
                Results.Add(new SearchCandidateViewModel(candidate));

            TotalMatches = result.TotalMatches;
            SearchDuration = $"{result.SearchDuration.TotalMilliseconds:F0}ms";
            HasResults = Results.Count > 0;
            HasNoResults = Results.Count == 0;

            // Save to history
            await _userData.SaveSearchAsync(new SearchHistoryEntry
            {
                QueryText = QueryText,
                ResultCount = result.TotalMatches
            }, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedModel = null;
        SelectedYear = null;
    }

    [RelayCommand]
    private async Task LoadManualInfoAsync()
    {
        var manuals = await _repo.GetAllManualsAsync();
        var active = manuals.FirstOrDefault();
        ActiveManualTitle = active?.Title ?? "No manual imported";
    }
}

public partial class SearchCandidateViewModel : ObservableObject
{
    public SearchCandidateViewModel(SearchCandidate candidate)
    {
        Candidate = candidate;
        Part = candidate.Part;
    }

    public SearchCandidate Candidate { get; }
    public PartRecord Part { get; }

    public string PartNumber => Part.PartNumber;
    public string Description => Part.Description;
    public string? Quantity => Part.Quantity;
    public string? Model => Part.Model;
    public string? Illustration => Part.Illustration;
    public int PageNumber => Part.PageNumber;
    public string Score => $"{Candidate.Score:P0}";
    public string MatchReason => Candidate.MatchReason;
    public string? Remark => Part.Remark;

    [ObservableProperty]
    private bool _isFavorite;
}
