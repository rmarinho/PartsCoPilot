using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.Data;

/// <summary>
/// Populates the database with realistic sample data for development.
/// Uses plausible Porsche 911/912 part numbers and descriptions.
/// </summary>
public class SeedDataService
{
    private readonly IPartsRepository _repo;
    private readonly AppDatabase _db;
    private const string SeedManualId = "seed-911-912-1965-1969";

    public SeedDataService(AppDatabase db, IPartsRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    /// <summary>
    /// Seeds sample data if the database is empty. Idempotent.
    /// </summary>
    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        var manuals = await _repo.GetAllManualsAsync(ct);
        if (manuals.Count > 0)
            return;

        await SeedAsync(ct);
    }

    /// <summary>
    /// Forces a full seed (clears existing seed data first).
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Clean any prior seed data
        await _repo.DeleteManualAsync(SeedManualId, ct);

        await SeedManualAsync(ct);
        await SeedIllustrationGroupsAsync(ct);
        await SeedPagesAsync(ct);
        await SeedPartsAsync(ct);
        await SeedVehicleTypesAsync(ct);
        await SeedEngineTypesAsync(ct);
        await SeedTransmissionTypesAsync(ct);
        await SeedLegendEntriesAsync(ct);
    }

    private async Task SeedManualAsync(CancellationToken ct)
    {
        await _repo.SaveManualAsync(new ManualMetadata
        {
            Id = SeedManualId,
            Title = "Porsche 911/912 Parts List 1965-1969",
            FilePath = "seed://sample-manual.pdf",
            VehicleMake = "Porsche",
            VehicleModel = "911/912",
            YearRange = "1965-1969",
            ManualType = "porsche-classic",
            PageCount = 180,
            PartCount = 25,
            ImportedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task SeedIllustrationGroupsAsync(CancellationToken ct)
    {
        var groups = new List<IllustrationGroup>
        {
            new() { ManualId = SeedManualId, IllustrationNumber = "101-00", Title = "Crankcase", StartPage = 10, EndPage = 15 },
            new() { ManualId = SeedManualId, IllustrationNumber = "101-05", Title = "Crankshaft, Connecting Rods", StartPage = 16, EndPage = 20 },
            new() { ManualId = SeedManualId, IllustrationNumber = "102-00", Title = "Cylinder Head", StartPage = 21, EndPage = 28 },
            new() { ManualId = SeedManualId, IllustrationNumber = "107-00", Title = "Lubrication", StartPage = 40, EndPage = 46 },
            new() { ManualId = SeedManualId, IllustrationNumber = "202-00", Title = "Fuel System", StartPage = 60, EndPage = 68 },
        };

        await _repo.SaveIllustrationGroupsAsync(groups, ct);
    }

    private async Task SeedPagesAsync(CancellationToken ct)
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = SeedManualId, PageNumber = 10, RawText = "Illustration: 101-00\nCrankcase\nPos Part Number Description Remark Qty Model", Illustration = "101-00", PageType = "part_table" },
            new() { ManualId = SeedManualId, PageNumber = 16, RawText = "Illustration: 101-05\nCrankshaft, Connecting Rods\nPos Part Number Description Remark Qty Model", Illustration = "101-05", PageType = "part_table" },
            new() { ManualId = SeedManualId, PageNumber = 21, RawText = "Illustration: 102-00\nCylinder Head\nPos Part Number Description Remark Qty Model", Illustration = "102-00", PageType = "part_table" },
            new() { ManualId = SeedManualId, PageNumber = 42, RawText = "Illustration: 107-00\nLubrication\nPos Part Number Description Remark Qty Model", Illustration = "107-00", PageType = "part_table" },
            new() { ManualId = SeedManualId, PageNumber = 60, RawText = "Illustration: 202-00\nFuel System\nPos Part Number Description Remark Qty Model", Illustration = "202-00", PageType = "part_table" },
            new() { ManualId = SeedManualId, PageNumber = 1, RawText = "SUMMARY TYPES\n911 Coupe 1965-1969\n912 Coupe 1965-1969", PageType = "summary" },
            new() { ManualId = SeedManualId, PageNumber = 2, RawText = "SUMMARY ENGINES\n901/01 flat-6 2.0L\n616/36 flat-4 1.6L", PageType = "summary" },
            new() { ManualId = SeedManualId, PageNumber = 3, RawText = "LEGENDS\nA = 911 all years\nB = 912 all years\nC = 911S only", PageType = "legend" },
        };

        await _repo.SavePagesAsync(pages, ct);
    }

    private async Task SeedPartsAsync(CancellationToken ct)
    {
        var parts = new List<PartRecord>
        {
            // 101-00 Crankcase
            MakePart("1", "901 101 013 00", "Crankcase", "101-00", 10, "1", "911", null),
            MakePart("2", "901 101 014 00", "Crankcase bolt", "101-00", 10, "8", "911", null),
            MakePart("3", "912 101 013 00", "Crankcase", "101-00", 10, "1", "912", null),
            MakePart("4", "999 084 009 02", "Hex bolt M8x30", "101-00", 10, "12", "911/912", null),
            MakePart("5", "999 110 253 02", "Gasket ring", "101-00", 10, "2", "911/912", null),

            // 101-05 Crankshaft
            MakePart("1", "901 102 011 03", "Crankshaft", "101-05", 16, "1", "911", "-68"),
            MakePart("2", "901 102 011 04", "Crankshaft", "101-05", 16, "1", "911", "69"),
            MakePart("3", "912 102 011 00", "Crankshaft", "101-05", 16, "1", "912", null),
            MakePart("4", "901 102 103 00", "Connecting rod", "101-05", 16, "6", "911", null),
            MakePart("5", "912 102 103 00", "Connecting rod", "101-05", 16, "4", "912", null),
            MakePart("6", "901 102 143 00", "Rod bearing shell", "101-05", 16, "12", "911", null),

            // 102-00 Cylinder Head
            MakePart("1", "901 104 301 04", "Cylinder head", "102-00", 21, "2", "911", "-68"),
            MakePart("2", "901 104 301 05", "Cylinder head", "102-00", 21, "2", "911S", "69"),
            MakePart("3", "912 104 301 01", "Cylinder head", "102-00", 21, "2", "912", null),
            MakePart("4", "901 105 101 04", "Intake valve", "102-00", 21, "12", "911", null),
            MakePart("5", "901 105 102 04", "Exhaust valve", "102-00", 21, "12", "911", null),

            // 107-00 Lubrication
            MakePart("1", "901 107 005 03", "Oil pump", "107-00", 42, "1", "911", null),
            MakePart("2", "912 107 005 01", "Oil pump", "107-00", 42, "1", "912", null),
            MakePart("58", "901 107 751 00", "Oil thermostat", "107-00", 42, "1", "911", null),
            MakePart("59", "999 110 259 00", "O-ring seal", "107-00", 42, "2", "911/912", null),
            MakePart("60", "901 107 764 00", "Oil cooler", "107-00", 42, "1", "911", null),

            // 202-00 Fuel System
            MakePart("1", "901 108 101 00", "Carburetor Solex 40 PI", "202-00", 60, "6", "911", "-67"),
            MakePart("2", "901 108 102 00", "Carburetor Weber 40 IDA", "202-00", 60, "6", "911", "68"),
            MakePart("3", "912 108 101 00", "Carburetor Solex 40 PII-4", "202-00", 60, "2", "912", null),
            MakePart("4", "901 108 901 00", "Fuel pump", "202-00", 60, "1", "911", null),
            MakePart("5", "912 108 901 00", "Fuel pump", "202-00", 60, "1", "912", null),
        };

        await _repo.SavePartsAsync(parts, ct);
    }

    private async Task SeedVehicleTypesAsync(CancellationToken ct)
    {
        var types = new List<VehicleType>
        {
            new() { ManualId = SeedManualId, Code = "901", ModelName = "911", Variant = "Coupe", YearFrom = 1965, YearTo = 1969, ChassisRange = "300001-305100" },
            new() { ManualId = SeedManualId, Code = "901", ModelName = "911S", Variant = "Coupe", YearFrom = 1967, YearTo = 1969, ChassisRange = "305101-311999" },
            new() { ManualId = SeedManualId, Code = "901", ModelName = "911T", Variant = "Coupe", YearFrom = 1968, YearTo = 1969, ChassisRange = "11800001-11899999" },
            new() { ManualId = SeedManualId, Code = "901", ModelName = "911E", Variant = "Coupe", YearFrom = 1969, YearTo = 1969, ChassisRange = "11900001-11999999" },
            new() { ManualId = SeedManualId, Code = "901", ModelName = "911L", Variant = "Coupe", YearFrom = 1968, YearTo = 1968, ChassisRange = "11800001-11899999" },
            new() { ManualId = SeedManualId, Code = "902", ModelName = "911 Targa", Variant = "Targa", YearFrom = 1967, YearTo = 1969, ChassisRange = "500001-509999" },
            new() { ManualId = SeedManualId, Code = "920", ModelName = "912", Variant = "Coupe", YearFrom = 1965, YearTo = 1969, ChassisRange = "350001-359999" },
            new() { ManualId = SeedManualId, Code = "920", ModelName = "912 Targa", Variant = "Targa", YearFrom = 1967, YearTo = 1969, ChassisRange = "550001-559999" },
        };

        await _repo.SaveVehicleTypesAsync(types, ct);
    }

    private async Task SeedEngineTypesAsync(CancellationToken ct)
    {
        var types = new List<EngineType>
        {
            new() { ManualId = SeedManualId, Code = "901/01", EngineName = "Flat-6 2.0L", Displacement = "1991cc", Power = "130 HP", ApplicableModels = "911", YearFrom = 1965, YearTo = 1967 },
            new() { ManualId = SeedManualId, Code = "901/02", EngineName = "Flat-6 2.0L S", Displacement = "1991cc", Power = "160 HP", ApplicableModels = "911S", YearFrom = 1967, YearTo = 1968 },
            new() { ManualId = SeedManualId, Code = "901/03", EngineName = "Flat-6 2.0L T", Displacement = "1991cc", Power = "110 HP", ApplicableModels = "911T", YearFrom = 1968, YearTo = 1969 },
            new() { ManualId = SeedManualId, Code = "901/06", EngineName = "Flat-6 2.0L E", Displacement = "1991cc", Power = "140 HP", ApplicableModels = "911E", YearFrom = 1969, YearTo = 1969 },
            new() { ManualId = SeedManualId, Code = "616/36", EngineName = "Flat-4 1.6L", Displacement = "1582cc", Power = "90 HP", ApplicableModels = "912", YearFrom = 1965, YearTo = 1969 },
        };

        await _repo.SaveEngineTypesAsync(types, ct);
    }

    private async Task SeedTransmissionTypesAsync(CancellationToken ct)
    {
        var types = new List<TransmissionType>
        {
            new() { ManualId = SeedManualId, Code = "901/0", TransmissionName = "4-speed manual", Type = "Manual", ApplicableModels = "911,912", YearFrom = 1965, YearTo = 1967 },
            new() { ManualId = SeedManualId, Code = "901/1", TransmissionName = "5-speed manual (dog-leg)", Type = "Manual", ApplicableModels = "911,912", YearFrom = 1965, YearTo = 1967 },
            new() { ManualId = SeedManualId, Code = "905/0", TransmissionName = "Sportomatic", Type = "Semi-automatic", ApplicableModels = "911", YearFrom = 1968, YearTo = 1969 },
            new() { ManualId = SeedManualId, Code = "901/2", TransmissionName = "5-speed manual", Type = "Manual", ApplicableModels = "911T,911E,911S", YearFrom = 1968, YearTo = 1969 },
        };

        await _repo.SaveTransmissionTypesAsync(types, ct);
    }

    private async Task SeedLegendEntriesAsync(CancellationToken ct)
    {
        var entries = new List<LegendEntry>
        {
            new() { ManualId = SeedManualId, Code = "A", Description = "All 911 models", Illustration = "101-00", ApplicableModels = "911,911S,911T,911E,911L", YearRange = "1965-1969" },
            new() { ManualId = SeedManualId, Code = "B", Description = "All 912 models", Illustration = "101-00", ApplicableModels = "912", YearRange = "1965-1969" },
            new() { ManualId = SeedManualId, Code = "C", Description = "911S only", Illustration = "102-00", ApplicableModels = "911S", YearRange = "1967-1969" },
            new() { ManualId = SeedManualId, Code = "D", Description = "From chassis no. 305101", Illustration = "101-05", ApplicableModels = "911", YearRange = "1967-1969", Notes = "Revised crankshaft design" },
            new() { ManualId = SeedManualId, Code = "E", Description = "Up to chassis no. 305100", Illustration = "101-05", ApplicableModels = "911", YearRange = "1965-1967", Notes = "Early production crankshaft" },
        };

        await _repo.SaveLegendEntriesAsync(entries, ct);
    }

    private PartRecord MakePart(string pos, string partNumber, string desc, string illustration, int page, string qty, string model, string? remark)
    {
        var normalized = partNumber.Replace(" ", "").ToUpperInvariant();
        var searchText = $"{partNumber} {normalized} {desc} {model} {remark}".ToLowerInvariant();
        return new PartRecord
        {
            ManualId = SeedManualId,
            Position = pos,
            PartNumber = partNumber,
            PartNumberNormalized = normalized,
            Description = desc,
            SearchText = searchText,
            Remark = remark,
            Quantity = qty,
            Model = model,
            Illustration = illustration,
            PageNumber = page,
        };
    }
}
