using System.Globalization;
namespace Microsoft.Maui.Controls
{
    public interface IValueConverter
    {
        object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
        object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
    }
    public interface IQueryAttributable { void ApplyQueryAttributes(IDictionary<string, object> query); }
    public class Shell
    {
        private static Shell? _current;
        public static Shell Current { get => _current ??= new Shell(); set => _current = value; }
        public virtual Task GoToAsync(string state) => Task.CompletedTask;
        public virtual Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters) => Task.CompletedTask;
        public virtual Task GoToAsync(string state, IDictionary<string, object> parameters) => Task.CompletedTask;
    }
    public class ImageSource
    {
        public static ImageSource FromStream(Func<Stream> stream) => new ImageSource();
        public static ImageSource FromFile(string file) => new ImageSource();
    }
    public class ShellNavigationState
    {
        public string Location { get; }
        public ShellNavigationState(string location) => Location = location;
        public static implicit operator ShellNavigationState(string value) => new(value);
    }
}
namespace Microsoft.Maui.Graphics
{
    public class Color
    {
        public string Argb { get; }
        private Color(string argb) => Argb = argb;
        public static Color FromArgb(string argb) => new(argb);
        public override bool Equals(object? obj) => obj is Color c && c.Argb == Argb;
        public override int GetHashCode() => Argb.GetHashCode();
    }
}
namespace Microsoft.Maui.Storage
{
    public class FileResult { public string FullPath { get; set; } = ""; public string FileName { get; set; } = ""; }
    public class PickOptions { public string? PickerTitle { get; set; } public FilePickerFileType? FileTypes { get; set; } }
    public class FilePickerFileType { public FilePickerFileType(IDictionary<Microsoft.Maui.Devices.DevicePlatform, IEnumerable<string>> ft) { } }
    public class FilePicker
    {
        private static FilePicker? _default;
        public static FilePicker Default { get => _default ??= new FilePicker(); set => _default = value; }
        public virtual Task<FileResult?> PickAsync(PickOptions? options = null) => Task.FromResult<FileResult?>(null);
    }
}
namespace Microsoft.Maui.Devices
{
    public readonly struct DevicePlatform : IEquatable<DevicePlatform>
    {
        private readonly string _platform;
        private DevicePlatform(string platform) => _platform = platform;
        public static DevicePlatform iOS => new("iOS");
        public static DevicePlatform Android => new("Android");
        public static DevicePlatform MacCatalyst => new("MacCatalyst");
        public static DevicePlatform WinUI => new("WinUI");
        public bool Equals(DevicePlatform other) => _platform == other._platform;
        public override bool Equals(object? obj) => obj is DevicePlatform dp && Equals(dp);
        public override int GetHashCode() => _platform?.GetHashCode() ?? 0;
    }
}
