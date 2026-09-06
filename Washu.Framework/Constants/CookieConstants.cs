namespace Washu.Framework.Constants;

public static class CookieConstants
{
    public static class Names
    {
        public const string Theme = ".Washu.Theme";
        // User Application Cookie
        public const string Application = ".Washu.Application";
        public const string ClientTimeZone = ".Washu.TimeZone";
    }

    public static class DurationInDays
    {
        public const ushort Theme = 365;
        public const ushort Application = 14;
        public const ushort ClientTimeZone = 365;
    }
}