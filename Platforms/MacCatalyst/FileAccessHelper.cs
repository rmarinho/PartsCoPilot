using Foundation;

namespace PartsCopilot.Platforms.MacCatalyst;

/// <summary>
/// Helper for handling security-scoped file access on Mac Catalyst.
/// Files picked via FilePicker need explicit StartAccessingSecurityScopedResource.
/// </summary>
public static class FileAccessHelper
{
    /// <summary>
    /// Wraps file operations with proper security-scoped resource handling.
    /// Returns the result of the action, automatically releasing the resource when done.
    /// </summary>
    public static T WithSecurityScope<T>(string filePath, Func<T> action)
    {
        NSUrl? url = null;
        bool accessStarted = false;

        try
        {
            // Convert file path to NSUrl
            url = NSUrl.FromFilename(filePath);
            
            // Start accessing the security-scoped resource
            accessStarted = url.StartAccessingSecurityScopedResource();
            
            // Perform the file operation
            return action();
        }
        finally
        {
            // Always release the security-scoped resource
            if (accessStarted && url != null)
            {
                url.StopAccessingSecurityScopedResource();
            }
        }
    }

    /// <summary>
    /// Async version for async file operations.
    /// </summary>
    public static async Task<T> WithSecurityScopeAsync<T>(string filePath, Func<Task<T>> action)
    {
        NSUrl? url = null;
        bool accessStarted = false;

        try
        {
            url = NSUrl.FromFilename(filePath);
            accessStarted = url.StartAccessingSecurityScopedResource();
            return await action();
        }
        finally
        {
            if (accessStarted && url != null)
            {
                url.StopAccessingSecurityScopedResource();
            }
        }
    }
}
