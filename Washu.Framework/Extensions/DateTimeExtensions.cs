namespace Washu.Framework.Extensions;

public static class DateTimeExtensions
{
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateTimeOffset ToTimeZone(TimeZoneInfo timeZoneInfo) => TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);

        public DateTimeOffset WithoutSeconds => dateTimeOffset.AddSeconds(-1 * dateTimeOffset.Second);
    }
}