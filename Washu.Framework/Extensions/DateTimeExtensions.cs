namespace Washu.Framework.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        public DateTime ToTimeZone(TimeZoneInfo timeZoneInfo)
        {
            var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime;
            return TimeZoneInfo.ConvertTime(utcDateTime, timeZoneInfo);
        }
    }
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateTimeOffset ToTimeZone(TimeZoneInfo timeZoneInfo) => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }
}