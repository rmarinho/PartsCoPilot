using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class ComparePartsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPartsRepository _repository;

    public ComparePartsViewModel(IPartsRepository repository)
    {
        _repository = repository;
    }

    // --- Left part (passed via navigation) ---

    [ObservableProperty]
    private PartRecord? _leftPart;

    // --- Right part (user picks) ---

    [ObservableProperty]
    private PartRecord? _rightPart;

    // --- Search state for picking second part ---

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _showSearchPanel = true;

    public ObservableCollection<PartRecord> SearchResults { get; } = [];

    // --- Comparison field results ---

    public bool HasBothParts => LeftPart is not null && RightPart is not null;

    public bool PartNumbersMatch => HasBothParts && LeftPart!.PartNumber == RightPart!.PartNumber;
    public bool DescriptionsMatch => HasBothParts && LeftPart!.Description == RightPart!.Description;
    public bool ModelsMatch => HasBothParts && NormalizedEqual(LeftPart!.Model, RightPart!.Model);
    public bool IllustrationsMatch => HasBothParts && NormalizedEqual(LeftPart!.Illustration, RightPart!.Illustration);
    public bool SectionsMatch => HasBothParts && NormalizedEqual(LeftPart!.Section, RightPart!.Section);
    public bool PagesMatch => HasBothParts && LeftPart!.PageNumber == RightPart!.PageNumber;
    public bool QuantitiesMatch => HasBothParts && NormalizedEqual(LeftPart!.Quantity, RightPart!.Quantity);
    public bool RemarksMatch => HasBothParts && NormalizedEqual(LeftPart!.Remark, RightPart!.Remark);
    public bool PositionsMatch => HasBothParts && NormalizedEqual(LeftPart!.Position, RightPart!.Position);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Part", out var partObj) && partObj is PartRecord part)
        {
            LeftPart = part;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var query = SearchQuery?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        IsSearching = true;
        try
        {
            var results = await _repository.SearchPartsAsync(query, LeftPart?.ManualId);
            SearchResults.Clear();
            foreach (var r in results)
            {
                // Don't show the left part in search results
                if (r.Id != LeftPart?.Id)
                    SearchResults.Add(r);
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void SelectPart(PartRecord part)
    {
        RightPart = part;
        ShowSearchPanel = false;
        NotifyComparisonChanged();
    }

    [RelayCommand]
    private void ClearRightPart()
    {
        RightPart = null;
        ShowSearchPanel = true;
        NotifyComparisonChanged();
    }

    [RelayCommand]
    private void SwapParts()
    {
        if (!HasBothParts) return;
        (LeftPart, RightPart) = (RightPart, LeftPart);
        NotifyComparisonChanged();
    }

    private void NotifyComparisonChanged()
    {
        OnPropertyChanged(nameof(HasBothParts));
        OnPropertyChanged(nameof(PartNumbersMatch));
        OnPropertyChanged(nameof(DescriptionsMatch));
        OnPropertyChanged(nameof(ModelsMatch));
        OnPropertyChanged(nameof(IllustrationsMatch));
        OnPropertyChanged(nameof(SectionsMatch));
        OnPropertyChanged(nameof(PagesMatch));
        OnPropertyChanged(nameof(QuantitiesMatch));
        OnPropertyChanged(nameof(RemarksMatch));
        OnPropertyChanged(nameof(PositionsMatch));
    }

    private static bool NormalizedEqual(string? a, string? b)
    {
        var aEmpty = string.IsNullOrWhiteSpace(a);
        var bEmpty = string.IsNullOrWhiteSpace(b);
        if (aEmpty && bEmpty) return true;
        if (aEmpty || bEmpty) return false;
        return string.Equals(a!.Trim(), b!.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
