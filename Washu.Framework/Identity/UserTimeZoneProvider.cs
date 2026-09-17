namespace Washu.Framework.Identity;

public sealed class UserTimeZoneProvider
{
    public string TimeZoneId { get; set; } = "UTC";
    public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}