using System.Globalization;
using Xunit;
using FluentAssertions;
using PartsCopilot.Converters;
namespace PartsCopilot.Tests;
public class ValueConvertersTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    public class InvertedBoolConverterTests
    {
        private readonly InvertedBoolConverter _conv = new();
        [Theory] [InlineData(true, false)] [InlineData(false, true)] public void Convert_InvertsBool(bool input, bool expected) => _conv.Convert(input, typeof(bool), null, Culture).Should().Be(expected);
        [Fact] public void Convert_NonBool_ReturnsTrue() => _conv.Convert("x", typeof(bool), null, Culture).Should().Be(true);
        [Fact] public void Convert_Null_ReturnsTrue() => _conv.Convert(null, typeof(bool), null, Culture).Should().Be(true);
        [Theory] [InlineData(true, false)] [InlineData(false, true)] public void ConvertBack_InvertsBool(bool input, bool expected) => _conv.ConvertBack(input, typeof(bool), null, Culture).Should().Be(expected);
        [Fact] public void ConvertBack_Null_ReturnsFalse() => _conv.ConvertBack(null, typeof(bool), null, Culture).Should().Be(false);
    }
    public class IsNotNullConverterTests
    {
        private readonly IsNotNullConverter _conv = new();
        [Fact] public void Convert_Null_ReturnsFalse() => _conv.Convert(null, typeof(bool), null, Culture).Should().Be(false);
        [Fact] public void Convert_EmptyString_ReturnsFalse() => _conv.Convert("", typeof(bool), null, Culture).Should().Be(false);
        [Fact] public void Convert_NonEmptyString_ReturnsTrue() => _conv.Convert("hello", typeof(bool), null, Culture).Should().Be(true);
        [Fact] public void Convert_NonNullObject_ReturnsTrue() => _conv.Convert(42, typeof(bool), null, Culture).Should().Be(true);
        [Fact] public void ConvertBack_ThrowsNotSupported() { ((Action)(() => _conv.ConvertBack(true, typeof(object), null, Culture))).Should().Throw<NotSupportedException>(); }
    }
    public class ProgressConverterTests
    {
        private readonly ProgressConverter _conv = new();
        [Theory] [InlineData(0, 0.0)] [InlineData(50, 0.5)] [InlineData(100, 1.0)] public void Convert_IntToFraction(int input, double expected) => _conv.Convert(input, typeof(double), null, Culture).Should().Be(expected);
        [Fact] public void Convert_NonInt_ReturnsZero() => _conv.Convert("50", typeof(double), null, Culture).Should().Be(0.0);
        [Fact] public void Convert_Null_ReturnsZero() => _conv.Convert(null, typeof(double), null, Culture).Should().Be(0.0);
        [Fact] public void ConvertBack_ThrowsNotSupported() { ((Action)(() => _conv.ConvertBack(0.5, typeof(int), null, Culture))).Should().Throw<NotSupportedException>(); }
    }
    public class BoolToFavoriteIconConverterTests
    {
        private readonly BoolToFavoriteIconConverter _conv = new();
        [Fact] public void Convert_True_ReturnsFilled() => _conv.Convert(true, typeof(string), null, Culture).Should().Be("\u2665");
        [Fact] public void Convert_False_ReturnsEmpty() => _conv.Convert(false, typeof(string), null, Culture).Should().Be("\u2661");
        [Fact] public void Convert_Null_ReturnsEmpty() => _conv.Convert(null, typeof(string), null, Culture).Should().Be("\u2661");
        [Fact] public void ConvertBack_ThrowsNotSupported() { ((Action)(() => _conv.ConvertBack("\u2665", typeof(bool), null, Culture))).Should().Throw<NotSupportedException>(); }
    }
    public class CompareFieldBackgroundConverterTests
    {
        private readonly CompareFieldBackgroundConverter _conv = new();
        [Fact] public void Convert_True_ReturnsGreenTint() { var r = _conv.Convert(true, typeof(object), null, Culture); r.Should().BeOfType<Microsoft.Maui.Graphics.Color>(); ((Microsoft.Maui.Graphics.Color)r).Argb.Should().Be("#1A4CAF50"); }
        [Fact] public void Convert_False_ReturnsOrangeTint() { var r = _conv.Convert(false, typeof(object), null, Culture); ((Microsoft.Maui.Graphics.Color)r).Argb.Should().Be("#1AFF9800"); }
        [Fact] public void Convert_Null_ReturnsOrangeTint() { var r = _conv.Convert(null, typeof(object), null, Culture); ((Microsoft.Maui.Graphics.Color)r).Argb.Should().Be("#1AFF9800"); }
        [Fact] public void ConvertBack_ThrowsNotSupported() { ((Action)(() => _conv.ConvertBack(null, typeof(bool), null, Culture))).Should().Throw<NotSupportedException>(); }
    }
}
