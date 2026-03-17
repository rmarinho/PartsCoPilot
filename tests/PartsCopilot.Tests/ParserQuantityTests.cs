using FluentAssertions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using Xunit;

namespace PartsCopilot.Tests;

public class ParserQuantityTests
{
    private readonly PorscheClassicManualParser _parser = new();

    /// <summary>
    /// Helper: builds a fake page with the given text and runs the parser.
    /// Part numbers must match the regex: XXX YYY ZZZ NN (3-3-3-2 alphanumeric groups).
    /// </summary>
    private async Task<IReadOnlyList<PartRecord>> ParseText(string rawText)
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "test",
                PageNumber = 1,
                RawText = rawText,
                PageType = "part_table",
                Illustration = "A1"
            }
        };

        return await _parser.ParseAsync(pages, "test");
    }

    [Fact]
    public async Task ExtractsQuantity_TrailingDigit()
    {
        var parts = await ParseText("901 012 345 01 Gasket set 2");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("2");
    }

    [Fact]
    public async Task ExtractsQuantity_QtyLabel()
    {
        var parts = await ParseText("901 012 345 01 Oil seal Qty: 3");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("3");
    }

    [Fact]
    public async Task ExtractsQuantity_QtyLabelNoColon()
    {
        var parts = await ParseText("901 012 345 01 Bearing Qty 5");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("5");
    }

    [Fact]
    public async Task ExtractsQuantity_WithUnit_Pcs()
    {
        var parts = await ParseText("901 012 345 01 Bolt 4 pcs");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("4");
    }

    [Fact]
    public async Task ExtractsQuantity_WithUnit_Set()
    {
        var parts = await ParseText("901 012 345 01 Washer 1 set");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("1");
    }

    [Fact]
    public async Task ExtractsQuantity_WithUnit_Each()
    {
        var parts = await ParseText("901 012 345 01 Spring 6 each");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("6");
    }

    [Fact]
    public async Task ExtractsQuantity_FollowedByModel()
    {
        var parts = await ParseText("901 012 345 01 Piston ring 4 911T");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("4");
    }

    [Fact]
    public async Task ReturnsNull_WhenNoQuantity()
    {
        var parts = await ParseText("901 012 345 01 Description only here");
        parts.Should().NotBeEmpty();
        // No trailing digit or qty marker — quantity should be null
        parts[0].Quantity.Should().BeNull();
    }

    [Fact]
    public async Task DoesNotCrash_MalformedRow()
    {
        var parts = await ParseText("This is just random garbage text with no part numbers");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotCrash_EmptyText()
    {
        var parts = await ParseText("");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotCrash_NullishContent()
    {
        var parts = await ParseText("   \n\n\t  ");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task Parser_CanParse_RecognizesPorscheMarkers()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "test", PageNumber = 1, RawText = "Parts for 911 model", PageType = "cover" }
        };

        _parser.CanParse(pages).Should().BeTrue();
    }

    [Fact]
    public async Task Parser_CanParse_Rejects_UnrelatedManual()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "test", PageNumber = 1, RawText = "Ford Mustang parts list", PageType = "cover" }
        };

        _parser.CanParse(pages).Should().BeFalse();
    }

    [Fact]
    public async Task Parser_ExtractsMultipleParts()
    {
        var text = """
            1  901 012 345 01  Gasket set  2  911
            2  901 012 345 02  Oil seal  1  912
            """;

        var parts = await ParseText(text);
        parts.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Parser_NormalizesPartNumber()
    {
        var parts = await ParseText("901 012 345 01 Some part");
        parts.Should().NotBeEmpty();
        parts[0].PartNumberNormalized.Should().NotContain(" ");
        parts[0].PartNumberNormalized.Should().Be(parts[0].PartNumber.Replace(" ", "").ToUpperInvariant());
    }

    [Fact]
    public async Task Parser_AssignsIllustration()
    {
        var parts = await ParseText("901 012 345 01 Some part 2");
        parts.Should().NotBeEmpty();
        parts[0].Illustration.Should().Be("A1");
    }

    [Fact]
    public async Task Parser_AssignsManualId()
    {
        var parts = await ParseText("901 012 345 01 Some part 3");
        parts.Should().NotBeEmpty();
        parts[0].ManualId.Should().Be("test");
    }

    [Fact]
    public async Task Parser_FallsBackDescription_WhenMissing()
    {
        var parts = await ParseText("901 012 345 01  123 456");
        parts.Should().NotBeEmpty();
        parts[0].Description.Should().Be("Unknown part");
    }

    [Fact]
    public async Task Parser_HandlesNPartNumber()
    {
        var parts = await ParseText("N 012 345 01 Nut hex");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().StartWith("N");
    }

    [Fact]
    public async Task Parser_HandlesPCGPartNumber()
    {
        var parts = await ParseText("PCG 012 345 01 Decal set 2 pcs");
        parts.Should().NotBeEmpty();
        parts[0].Quantity.Should().Be("2");
    }
}
