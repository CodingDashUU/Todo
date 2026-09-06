namespace Washu.Framework.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime dateTimeOffset)
    {
        public DateTime ToTimeZone(TimeZoneInfo timeZoneInfo) => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateTimeOffset ToTimeZone(TimeZoneInfo timeZoneInfo) => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }
}