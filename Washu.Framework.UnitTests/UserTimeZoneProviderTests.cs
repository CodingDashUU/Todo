namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Identity;

public class UserTimeZoneProviderTests
{
    [Fact]
    public void Default_TimeZoneId_IsUtc()
    {
        var provider = new UserTimeZoneProvider();

        provider.TimeZoneId.Should().Be("UTC");
        provider.TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
    }

    [Fact]
    public void CustomTimeZoneId_ResolvesMatchingTimeZoneInfo()
    {
        var provider = new UserTimeZoneProvider
        {
            TimeZoneId = "UTC"
        };

        provider.TimeZoneInfo.Id.Should().Be("UTC");
    }

    [Fact]
    public void InvalidTimeZoneId_ThrowsTimeZoneNotFoundException_WhenAccessingTimeZoneInfo()
    {
        var provider = new UserTimeZoneProvider
        {
            TimeZoneId = "NonExistent_Invalid_Zone_123"
        };

        var act = () => _ = provider.TimeZoneInfo;

        act.Should().Throw<TimeZoneNotFoundException>();
    }
}
