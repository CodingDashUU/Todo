namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void WithoutSeconds_ShouldZeroOutSeconds_WhilePreservingOtherComponents()
    {
        var original = new DateTimeOffset(2026, 10, 24, 15, 30, 45, TimeSpan.FromHours(2));

        var result = original.WithoutSeconds;

        result.Second.Should().Be(0, "WithoutSeconds should clear the seconds component");
        result.Minute.Should().Be(30);
        result.Hour.Should().Be(15);
        result.Day.Should().Be(24);
        result.Month.Should().Be(10);
        result.Year.Should().Be(2026);
        result.Offset.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void WithoutSeconds_WhenSecondsAlreadyZero_ShouldRemainUnchanged()
    {
        var original = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);

        var result = original.WithoutSeconds;

        result.Should().Be(original);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void ToTimeZone_ShouldConvertDateTimeOffsetToTargetTimeZone()
    {
        var utcTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");

        var converted = utcTime.ToTimeZone(targetTimeZone);

        converted.Should().Be(utcTime);
        converted.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToTimeZone_ShouldAdjustOffsetAndClockTime_ForNonUtcTimeZone()
    {
        var utcTime = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        // Custom fixed timezone: +05:30
        var customZone = TimeZoneInfo.CreateCustomTimeZone("CustomPlus530", TimeSpan.FromMinutes(330), "Custom Zone", "Custom Standard");

        var converted = utcTime.ToTimeZone(customZone);

        converted.Offset.Should().Be(TimeSpan.FromMinutes(330));
        converted.Hour.Should().Be(17);
        converted.Minute.Should().Be(30);
        converted.ToUniversalTime().Should().Be(utcTime.ToUniversalTime());
    }
}
