using Xunit;
using PartsCopilot.Models;
using PartsCopilot.Services;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests the PorscheClassicManualParser against known input patterns.
/// </summary>
public class PorscheClassicManualParserTests
{
    private readonly PorscheClassicManualParser _parser = new();

    [Fact]
    public void CanParse_ReturnsTrueForPorscheManual()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "m1", PageNumber = 1, RawText = "Porsche 911 Parts List", PageType = "summary" },
        };

        _parser.CanParse(pages).Should().BeTrue();
    }

    [Fact]
    public void CanParse_ReturnsTrueFor912Manual()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "m1", PageNumber = 1, RawText = "Model 912 spare parts catalog", PageType = "summary" },
        };

        _parser.CanParse(pages).Should().BeTrue();
    }

    [Fact]
    public void CanParse_ReturnsFalseForUnrelatedManual()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "m1", PageNumber = 1, RawText = "Volkswagen Beetle Parts Manual", PageType = "summary" },
        };

        _parser.CanParse(pages).Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_ExtractsStandardPartNumbers()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "m1",
                PageNumber = 42,
                RawText = "Illustration: 107-00\nLubrication\nPos Part Number Description Remark Qty Model\n58 901 107 751 00 Oil thermostat 1 911\n59 999 110 259 00 O-ring seal 2 911",
                Illustration = "107-00",
                PageType = "part_table"
            },
        };

        var parts = await _parser.ParseAsync(pages, "m1");

        parts.Should().HaveCountGreaterThanOrEqualTo(2);
        parts.Should().Contain(p => p.PartNumber == "901 107 751 00");
        parts.Should().Contain(p => p.PartNumber == "999 110 259 00");
    }

    [Fact]
    public async Task ParseAsync_NormalizesPartNumbers()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "m1",
                PageNumber = 10,
                RawText = "Illustration: 101-00\n1 901 101 013 00 Crankcase 1 911",
                Illustration = "101-00",
                PageType = "part_table"
            },
        };

        var parts = await _parser.ParseAsync(pages, "m1");

        parts.Should().NotBeEmpty();
        var crankcase = parts.First(p => p.PartNumber == "901 101 013 00");
        crankcase.PartNumberNormalized.Should().Be("90110101300");
    }

    [Fact]
    public async Task ParseAsync_AssignsIllustrationFromPage()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "m1",
                PageNumber = 16,
                RawText = "Illustration: 101-05\n1 901 102 011 03 Crankshaft 1 911",
                Illustration = "101-05",
                PageType = "part_table"
            },
        };

        var parts = await _parser.ParseAsync(pages, "m1");

        parts.Should().NotBeEmpty();
        parts[0].Illustration.Should().Be("101-05");
        parts[0].PageNumber.Should().Be(16);
    }

    [Fact]
    public async Task ParseAsync_SkipsNonPartTablePages()
    {
        var pages = new List<ManualPage>
        {
            new() { ManualId = "m1", PageNumber = 1, RawText = "SUMMARY TYPES\n901 101 013 00 in summary", PageType = "summary" },
        };

        var parts = await _parser.ParseAsync(pages, "m1");

        parts.Should().BeEmpty("parser only processes part_table pages");
    }

    [Fact]
    public async Task ParseAsync_SetsManualIdOnAllParts()
    {
        var pages = new List<ManualPage>
        {
            new()
            {
                ManualId = "test-id",
                PageNumber = 42,
                RawText = "Illustration: 107-00\n58 901 107 751 00 Oil thermostat 1 911",
                Illustration = "107-00",
                PageType = "part_table"
            },
        };

        var parts = await _parser.ParseAsync(pages, "test-id");

        parts.Should().NotBeEmpty();
        parts.Should().AllSatisfy(p => p.ManualId.Should().Be("test-id"));
    }
}
