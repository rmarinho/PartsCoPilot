using FluentAssertions;
using PartsCopilot.Services;
using Xunit;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests for PDF ingestion error handling.
/// </summary>
public class PdfIngestionErrorTests
{
    private readonly PdfIngestionService _service = new();

    [Fact]
    public async Task ExtractPages_NonExistentFile_ThrowsInformativeError()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid()}.pdf");

        var act = async () => await _service.ExtractPagesAsync(nonExistentPath, "test");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExtractPages_EmptyFilePath_ThrowsError()
    {
        var act = async () => await _service.ExtractPagesAsync("", "test");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ExtractPages_DirectoryInsteadOfFile_ThrowsError()
    {
        var tempDir = Path.GetTempPath();

        var act = async () => await _service.ExtractPagesAsync(tempDir, "test");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExtractPages_CancellationToken_Respected()
    {
        // Create a fake PDF path (won't be accessed due to immediate cancellation)
        var fakePath = Path.Combine(Path.GetTempPath(), "fake.pdf");
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var act = async () => await _service.ExtractPagesAsync(fakePath, "test", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExtractPages_InvalidManualId_StillProcesses()
    {
        // Even with invalid manual ID, the service should attempt to process
        // (though it will fail on file not found)
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");

        var act = async () => await _service.ExtractPagesAsync(nonExistentPath, "");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
