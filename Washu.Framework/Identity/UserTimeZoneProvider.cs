namespace Washu.Framework.Identity;

public class UserTimeZoneProvider
{
    public string TimeZoneId { get; set; } = "UTC";
    public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}