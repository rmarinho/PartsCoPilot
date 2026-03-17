using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly IUserDataRepository _userData;

    public FavoritesViewModel(IUserDataRepository userData)
    {
        _userData = userData;
    }

    [ObservableProperty]
    private bool _isLoadingFavorites;

    [ObservableProperty]
    private bool _isLoadingHistory;

    [ObservableProperty]
    private bool _hasFavorites;

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFavoritesTab))]
    [NotifyPropertyChangedFor(nameof(IsHistoryTab))]
    [NotifyPropertyChangedFor(nameof(HasNoFavorites))]
    [NotifyPropertyChangedFor(nameof(HasNoHistory))]
    private int _selectedTabIndex;

    public bool IsFavoritesTab => SelectedTabIndex == 0;
    public bool IsHistoryTab => SelectedTabIndex == 1;
    public bool HasNoFavorites => IsFavoritesTab && !HasFavorites && !IsLoadingFavorites;
    public bool HasNoHistory => IsHistoryTab && !HasHistory && !IsLoadingHistory;

    public ObservableCollection<FavoriteItemViewModel> Favorites { get; } = [];
    public ObservableCollection<SearchHistoryItemViewModel> RecentSearches { get; } = [];

    partial void OnHasFavoritesChanged(bool value) => OnPropertyChanged(nameof(HasNoFavorites));
    partial void OnHasHistoryChanged(bool value) => OnPropertyChanged(nameof(HasNoHistory));
    partial void OnIsLoadingFavoritesChanged(bool value) => OnPropertyChanged(nameof(HasNoFavorites));
    partial void OnIsLoadingHistoryChanged(bool value) => OnPropertyChanged(nameof(HasNoHistory));

    [RelayCommand]
    private void SwitchToFavorites() => SelectedTabIndex = 0;

    [RelayCommand]
    private void SwitchToHistory() => SelectedTabIndex = 1;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await Task.WhenAll(LoadFavoritesAsync(), LoadHistoryAsync());
    }

    private async Task LoadFavoritesAsync()
    {
        try
        {
            IsLoadingFavorites = true;
            Favorites.Clear();

            var entries = await _userData.GetFavoritesAsync();
            foreach (var entry in entries)
                Favorites.Add(new FavoriteItemViewModel(entry));

            HasFavorites = Favorites.Count > 0;
        }
        finally
        {
            IsLoadingFavorites = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            IsLoadingHistory = true;
            RecentSearches.Clear();

            var entries = await _userData.GetRecentSearchesAsync();
            foreach (var entry in entries)
                RecentSearches.Add(new SearchHistoryItemViewModel(entry));

            HasHistory = RecentSearches.Count > 0;
        }
        finally
        {
            IsLoadingHistory = false;
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(FavoriteItemViewModel? item)
    {
        if (item is null) return;

        await _userData.RemoveFavoriteAsync(item.Entry.PartRecordId);
        Favorites.Remove(item);
        HasFavorites = Favorites.Count > 0;
    }

    [RelayCommand]
    private async Task SearchAgainAsync(SearchHistoryItemViewModel? item)
    {
        if (item is null) return;

        await Shell.Current.GoToAsync($"search?query={Uri.EscapeDataString(item.QueryText)}");
    }
}

public partial class FavoriteItemViewModel : ObservableObject
{
    public FavoriteItemViewModel(FavoriteEntry entry)
    {
        Entry = entry;
    }

    public FavoriteEntry Entry { get; }

    public string PartNumber => Entry.PartNumber;
    public string Description => Entry.Description;
    public string? Model => Entry.Model;
    public int PageNumber => Entry.PageNumber;
    public string? Illustration => Entry.Illustration;
    public string SavedAt => Entry.SavedAt.ToLocalTime().ToString("MMM d, yyyy");
}

public partial class SearchHistoryItemViewModel : ObservableObject
{
    public SearchHistoryItemViewModel(SearchHistoryEntry entry)
    {
        Entry = entry;
    }

    public SearchHistoryEntry Entry { get; }

    public string QueryText => Entry.QueryText;
    public int ResultCount => Entry.ResultCount;
    public string SearchedAt => Entry.SearchedAt.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}
