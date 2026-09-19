namespace Washu.Framework.Identity;

public sealed class UserTimeZoneProvider
{
    public string TimeZoneId { get; init; } = "UTC";
    public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}