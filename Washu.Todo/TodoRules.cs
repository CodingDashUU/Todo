namespace Washu.Todo;

using Framework.Constants;

public static class TodoRules
{
    public static class TaskName
    {
        public const byte MinimumLength = 3;
        public const byte MaximumLength = 50;
        public const string AllowedSymbols = "!@$%&(){}[]/.,><";
        public static readonly string AllowedCharacters = $"{Characters.Alphanumeric}{Characters.Whitespace}{AllowedSymbols}";
    }
    public static class ListName
    {
        public const byte MinimumLength = 3;
        public const byte MaximumLength = 30;
        public const string AllowedSymbols = "!#$&()/?";
        public static readonly string AllowedCharacters = $"{Characters.Alphanumeric}{Characters.Whitespace}{AllowedSymbols}";
    }
}