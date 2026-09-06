namespace Washu.Framework.Constants;

public static class Characters
{
    public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Numbers = "0123456789";
    public const string Alphanumeric = $"{Lowercase}{Uppercase}{Numbers}";
    public const char Whitespace = ' ';
}