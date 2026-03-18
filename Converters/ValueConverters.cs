using System.Globalization;

namespace PartsCopilot.Converters;

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && (value is not string s || !string.IsNullOrEmpty(s));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i ? i / 100.0 : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "♥" : "♡";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns a green-tinted background when fields match, orange-tinted when they differ.
/// </summary>
public class CompareFieldBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return Color.FromArgb("#1A4CAF50"); // green tint — match
        return Color.FromArgb("#1AFF9800"); // orange tint — difference
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
