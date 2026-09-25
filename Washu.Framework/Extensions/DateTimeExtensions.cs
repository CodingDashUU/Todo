namespace Washu.Framework.Extensions;

public static class DateTimeExtensions
{
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateTimeOffset ToTimeZone(TimeZoneInfo timeZoneInfo) => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }
}