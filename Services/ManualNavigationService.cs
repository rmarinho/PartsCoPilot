using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Maps part records back to their source PDF pages and illustration groups.
/// </summary>
public sealed class ManualNavigationService : IManualNavigationService
{
    private readonly IPartsRepository _repo;

    public ManualNavigationService(IPartsRepository repo) => _repo = repo;

    public int GetPageNumber(PartRecord part)
    {
        ArgumentNullException.ThrowIfNull(part);
        return part.PageNumber;
    }

    public string? GetIllustrationGroup(PartRecord part)
    {
        ArgumentNullException.ThrowIfNull(part);
        return part.Illustration;
    }

    public async Task<ManualPage?> GetPageAsync(PartRecord part, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(part);
        return await _repo.GetPageAsync(part.ManualId, part.PageNumber, ct);
    }

    public async Task<IllustrationGroup?> GetIllustrationAsync(PartRecord part, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(part);
        if (part.Illustration is null)
            return null;

        var groups = await GetIllustrationGroupsForManualAsync(part.ManualId, ct);
        return groups.FirstOrDefault(g =>
            string.Equals(g.IllustrationNumber, part.Illustration, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns all illustration groups for a manual. Useful for table-of-contents views.
    /// </summary>
    public async Task<IReadOnlyList<IllustrationGroup>> GetIllustrationGroupsForManualAsync(
        string manualId, CancellationToken ct = default)
    {
        // Delegate to the repository's page/group data.
        // For now we query parts to extract distinct illustrations.
        var parts = await _repo.GetPartsByManualAsync(manualId, ct);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<IllustrationGroup>();

        foreach (var p in parts.Where(p => p.Illustration is not null).OrderBy(p => p.PageNumber))
        {
            if (seen.Add(p.Illustration!))
            {
                groups.Add(new IllustrationGroup
                {
                    ManualId = manualId,
                    IllustrationNumber = p.Illustration!,
                    StartPage = p.PageNumber,
                });
            }
        }

        return groups;
    }
}
