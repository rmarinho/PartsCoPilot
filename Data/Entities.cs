using SQLite;
using PartsCopilot.Models;

namespace PartsCopilot.Data;

/// <summary>
/// SQLite table DTOs. We use separate table classes to decouple from domain records.
/// </summary>
[Table("Manuals")]
public class ManualEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }
    public string? YearRange { get; set; }
    public string? ManualType { get; set; }
    public int PageCount { get; set; }
    public int PartCount { get; set; }
    public DateTime ImportedAt { get; set; }

    public ManualMetadata ToDomain() => new()
    {
        Id = Id, Title = Title, FilePath = FilePath,
        VehicleMake = VehicleMake, VehicleModel = VehicleModel,
        YearRange = YearRange, ManualType = ManualType,
        PageCount = PageCount, PartCount = PartCount, ImportedAt = ImportedAt
    };

    public static ManualEntity FromDomain(ManualMetadata m) => new()
    {
        Id = m.Id, Title = m.Title, FilePath = m.FilePath,
        VehicleMake = m.VehicleMake, VehicleModel = m.VehicleModel,
        YearRange = m.YearRange, ManualType = m.ManualType,
        PageCount = m.PageCount, PartCount = m.PartCount, ImportedAt = m.ImportedAt
    };
}

[Table("Parts")]
public class PartEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    [Indexed] public string ManualId { get; set; } = "";
    public string? Position { get; set; }
    [Indexed] public string PartNumber { get; set; } = "";
    [Indexed] public string PartNumberNormalized { get; set; } = "";
    public string Description { get; set; } = "";
    [Indexed] public string SearchText { get; set; } = "";
    public string? Remark { get; set; }
    public string? Quantity { get; set; }
    public string? Model { get; set; }
    public string? Section { get; set; }
    public string? Illustration { get; set; }
    public int PageNumber { get; set; }

    public PartRecord ToDomain() => new()
    {
        Id = Id, ManualId = ManualId, Position = Position,
        PartNumber = PartNumber, PartNumberNormalized = PartNumberNormalized,
        Description = Description, SearchText = SearchText,
        Remark = Remark, Quantity = Quantity, Model = Model,
        Section = Section, Illustration = Illustration, PageNumber = PageNumber
    };

    public static PartEntity FromDomain(PartRecord p) => new()
    {
        Id = p.Id, ManualId = p.ManualId, Position = p.Position,
        PartNumber = p.PartNumber, PartNumberNormalized = p.PartNumberNormalized,
        Description = p.Description, SearchText = p.SearchText,
        Remark = p.Remark, Quantity = p.Quantity, Model = p.Model,
        Section = p.Section, Illustration = p.Illustration, PageNumber = p.PageNumber
    };
}

[Table("Pages")]
public class PageEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    [Indexed] public string ManualId { get; set; } = "";
    public int PageNumber { get; set; }
    public string RawText { get; set; } = "";
    public string? Illustration { get; set; }
    public string? Section { get; set; }
    public string PageType { get; set; } = "";

    public ManualPage ToDomain() => new()
    {
        Id = Id, ManualId = ManualId, PageNumber = PageNumber,
        RawText = RawText, Illustration = Illustration,
        Section = Section, PageType = PageType
    };

    public static PageEntity FromDomain(ManualPage p) => new()
    {
        Id = p.Id, ManualId = p.ManualId, PageNumber = p.PageNumber,
        RawText = p.RawText, Illustration = p.Illustration,
        Section = p.Section, PageType = p.PageType
    };
}

[Table("IllustrationGroups")]
public class IllustrationGroupEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    [Indexed] public string ManualId { get; set; } = "";
    public string IllustrationNumber { get; set; } = "";
    public string? Title { get; set; }
    public int StartPage { get; set; }
    public int? EndPage { get; set; }

    public IllustrationGroup ToDomain() => new()
    {
        Id = Id, ManualId = ManualId, IllustrationNumber = IllustrationNumber,
        Title = Title, StartPage = StartPage, EndPage = EndPage
    };

    public static IllustrationGroupEntity FromDomain(IllustrationGroup g) => new()
    {
        Id = g.Id, ManualId = g.ManualId, IllustrationNumber = g.IllustrationNumber,
        Title = g.Title, StartPage = g.StartPage, EndPage = g.EndPage
    };
}

[Table("SearchHistory")]
public class SearchHistoryEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string QueryText { get; set; } = "";
    public string? ManualId { get; set; }
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }

    public SearchHistoryEntry ToDomain() => new()
    {
        Id = Id, QueryText = QueryText, ManualId = ManualId,
        ResultCount = ResultCount, SearchedAt = SearchedAt
    };

    public static SearchHistoryEntity FromDomain(SearchHistoryEntry e) => new()
    {
        Id = e.Id, QueryText = e.QueryText, ManualId = e.ManualId,
        ResultCount = e.ResultCount, SearchedAt = e.SearchedAt
    };
}

[Table("Favorites")]
public class FavoriteEntity
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string PartRecordId { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ManualId { get; set; }
    public DateTime SavedAt { get; set; }

    public FavoriteEntry ToDomain() => new()
    {
        Id = Id, PartRecordId = PartRecordId, PartNumber = PartNumber,
        Description = Description, ManualId = ManualId, SavedAt = SavedAt
    };

    public static FavoriteEntity FromDomain(FavoriteEntry e) => new()
    {
        Id = e.Id, PartRecordId = e.PartRecordId, PartNumber = e.PartNumber,
        Description = e.Description, ManualId = e.ManualId, SavedAt = e.SavedAt
    };
}
