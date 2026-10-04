namespace Washu.Framework.Extensions;

public static class CollectionExtensions
{
    extension<T>(IEnumerable<T> collection)
    {
        public bool IsEmpty => !collection.Any();
        public bool IsNotEmpty => collection.Any();
    }
}