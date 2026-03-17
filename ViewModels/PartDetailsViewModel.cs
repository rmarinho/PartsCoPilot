using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

public partial class PartDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IUserDataRepository _userData;

    public PartDetailsViewModel(IUserDataRepository userData)
    {
        _userData = userData;
    }

    [ObservableProperty]
    private PartRecord? _part;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string? _errorMessage;

    // Derived display properties
    public string PartNumber => Part?.PartNumber ?? "";
    public string Description => Part?.Description ?? "";
    public string? Position => Part?.Position;
    public string? Illustration => Part?.Illustration;
    public int PageNumber => Part?.PageNumber ?? 0;
    public string? Quantity => Part?.Quantity;
    public string? Model => Part?.Model;
    public string? Remark => Part?.Remark;
    public string? Section => Part?.Section;
    public string PageDisplay => Part is not null ? $"Page {Part.PageNumber}" : "";
    public bool HasRemark => !string.IsNullOrWhiteSpace(Part?.Remark);
    public bool HasPosition => !string.IsNullOrWhiteSpace(Part?.Position);
    public bool HasIllustration => !string.IsNullOrWhiteSpace(Part?.Illustration);
    public bool HasQuantity => !string.IsNullOrWhiteSpace(Part?.Quantity);
    public bool HasModel => !string.IsNullOrWhiteSpace(Part?.Model);
    public bool HasSection => !string.IsNullOrWhiteSpace(Part?.Section);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Part", out var partObj) && partObj is PartRecord part)
        {
            Part = part;
            OnPropertyChanged(nameof(PartNumber));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Illustration));
            OnPropertyChanged(nameof(PageNumber));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(Model));
            OnPropertyChanged(nameof(Remark));
            OnPropertyChanged(nameof(Section));
            OnPropertyChanged(nameof(PageDisplay));
            OnPropertyChanged(nameof(HasRemark));
            OnPropertyChanged(nameof(HasPosition));
            OnPropertyChanged(nameof(HasIllustration));
            OnPropertyChanged(nameof(HasQuantity));
            OnPropertyChanged(nameof(HasModel));
            OnPropertyChanged(nameof(HasSection));
        }

        if (query.TryGetValue("IsFavorite", out var favObj) && favObj is bool fav)
        {
            IsFavorite = fav;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Part is null) return;

        try
        {
            if (IsFavorite)
            {
                await _userData.RemoveFavoriteAsync(Part.Id);
                IsFavorite = false;
            }
            else
            {
                await _userData.SaveFavoriteAsync(new FavoriteEntry
                {
                    PartRecordId = Part.Id,
                    PartNumber = Part.PartNumber,
                    Description = Part.Description,
                    Model = Part.Model,
                    PageNumber = Part.PageNumber,
                    Illustration = Part.Illustration,
                    ManualId = Part.ManualId,
                });
                IsFavorite = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenPageAsync()
    {
        if (Part is null) return;

        var parameters = new Dictionary<string, object>
        {
            ["ManualId"] = Part.ManualId,
            ["PageNumber"] = Part.PageNumber,
        };
        if (Part.Illustration is not null)
            parameters["Illustration"] = Part.Illustration;

        await Shell.Current.GoToAsync("manual-viewer", parameters);
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (Part is null) return;

        await Shell.Current.GoToAsync("compare-parts", new Dictionary<string, object>
        {
            ["Part"] = Part,
        });
    }
}
