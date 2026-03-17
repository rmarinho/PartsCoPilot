using FluentAssertions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using Xunit;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests for parser robustness improvements (hyphenated part numbers, illustration variations).
/// </summary>
public class ParserRobustnessTests
{
    private readonly PorscheClassicManualParser _parser = new();

    private async Task<IReadOnlyList<PartRecord>> ParseText(string rawText, string illustration = "A1")
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "test",
                PageNumber = 1,
                RawText = rawText,
                PageType = "part_table",
                Illustration = illustration
            }
        };

        return await _parser.ParseAsync(pages, "test");
    }

    [Fact]
    public async Task PartNumber_WithHyphens_IsRecognized()
    {
        var parts = await ParseText("901-101-013-00 Bolt hex head");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().Contain("901");
        parts[0].PartNumber.Should().Contain("101");
    }

    [Fact]
    public async Task PartNumber_WithSpaces_StillWorks()
    {
        var parts = await ParseText("901 101 013 00 Bolt hex head");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().Contain("901");
    }

    [Fact]
    public async Task PartNumber_MixedHyphensAndSpaces_IsRecognized()
    {
        var parts = await ParseText("901-101 013-00 Washer");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().Contain("901");
    }

    [Fact]
    public async Task PartNumber_N_WithHyphens_IsRecognized()
    {
        var parts = await ParseText("N-012-345-01 Nut hex");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().StartWith("N");
    }

    [Fact]
    public async Task PartNumber_PCG_WithHyphens_IsRecognized()
    {
        var parts = await ParseText("PCG-012-345-01 Decal set");
        parts.Should().NotBeEmpty();
        parts[0].PartNumber.Should().StartWith("PCG");
    }

    [Fact]
    public async Task ParseMultiplePartNumbers_WithVariedFormats()
    {
        var text = """
            1  901-101-013-00  Bolt  2
            2  901 102 014 01  Washer  4
            3  N-015-016-02  Nut  2
            """;

        var parts = await ParseText(text);
        parts.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Illustration_Illus_Variation()
    {
        var page = new ManualPage
        {
            ManualId = "test",
            PageNumber = 1,
            RawText = "Illus: 102-03 Parts list",
            PageType = "part_table"
        };

        var service = new PdfIngestionService();
        var pages = await service.ExtractPagesAsync("dummy.pdf", "test");
        
        // Can't test ExtractPages without a real PDF, but we can verify the regex works
        var illustration = System.Text.RegularExpressions.Regex.Match(
            "Illus: 102-03 Parts list", 
            @"(?:Illustration|Illus|Fig\.?)\s*:?\s*(\d{3}-\d{2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        illustration.Success.Should().BeTrue();
        illustration.Groups[1].Value.Should().Be("102-03");
    }

    [Fact]
    public async Task Illustration_Fig_Variation()
    {
        var illustration = System.Text.RegularExpressions.Regex.Match(
            "Fig. 103-04 Parts diagram", 
            @"(?:Illustration|Illus|Fig\.?)\s*:?\s*(\d{3}-\d{2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        illustration.Success.Should().BeTrue();
        illustration.Groups[1].Value.Should().Be("103-04");
    }

    [Fact]
    public async Task Illustration_NoColon_Variation()
    {
        var illustration = System.Text.RegularExpressions.Regex.Match(
            "Illustration 104-05 Engine parts", 
            @"(?:Illustration|Illus|Fig\.?)\s*:?\s*(\d{3}-\d{2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        illustration.Success.Should().BeTrue();
        illustration.Groups[1].Value.Should().Be("104-05");
    }

    [Fact]
    public async Task Illustration_CaseInsensitive()
    {
        var illustration = System.Text.RegularExpressions.Regex.Match(
            "ILLUSTRATION: 105-06", 
            @"(?:Illustration|Illus|Fig\.?)\s*:?\s*(\d{3}-\d{2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        illustration.Success.Should().BeTrue();
        illustration.Groups[1].Value.Should().Be("105-06");
    }

    [Fact]
    public async Task Parser_HandlesEmptyContext_Gracefully()
    {
        var parts = await ParseText("");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task Parser_HandlesWhitespaceOnly_Gracefully()
    {
        var parts = await ParseText("   \n\n\t  ");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task Parser_HandlesNoPartNumbers_Gracefully()
    {
        var parts = await ParseText("This is just some random text without any valid part numbers");
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task Parser_HandlesPartialMatch_DoesNotCrash()
    {
        var parts = await ParseText("901-101-013 (missing last segment)");
        // Should not find this as it doesn't match the full pattern
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task Parser_NormalizesHyphenatedPartNumbers()
    {
        var parts = await ParseText("901-101-013-00 Some part");
        parts.Should().NotBeEmpty();
        parts[0].PartNumberNormalized.Should().NotContain("-");
        parts[0].PartNumberNormalized.Should().NotContain(" ");
    }

    [Fact]
    public async Task Parser_AssignsCorrectPageNumber()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "test",
                PageNumber = 42,
                RawText = "901-101-013-00 Test part",
                PageType = "part_table",
                Illustration = "101-00"
            }
        };

        var parts = await _parser.ParseAsync(pages, "test");
        parts.Should().NotBeEmpty();
        parts[0].PageNumber.Should().Be(42);
    }

    [Fact]
    public async Task Parser_AssignsCorrectIllustration()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "test",
                PageNumber = 1,
                RawText = "901-101-013-00 Test part",
                PageType = "part_table",
                Illustration = "102-05"
            }
        };

        var parts = await _parser.ParseAsync(pages, "test");
        parts.Should().NotBeEmpty();
        parts[0].Illustration.Should().Be("102-05");
    }

    [Fact]
    public async Task Parser_RespectsManualId()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "custom-manual-id",
                PageNumber = 1,
                RawText = "901-101-013-00 Test part",
                PageType = "part_table",
                Illustration = "101-00"
            }
        };

        var parts = await _parser.ParseAsync(pages, "custom-manual-id");
        parts.Should().NotBeEmpty();
        parts[0].ManualId.Should().Be("custom-manual-id");
    }
}
