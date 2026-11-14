namespace Washu.Todo.Extensions;

using Components;
using Radzen;

public static class LoadDataConfigExtensions
{
    extension(LoadDataConfig)
    {
        public static LoadDataConfig FromRadzenArgs(LoadDataArgs args) =>
            new() { Filter = args.Filter, OrderBy = args.OrderBy, Skip = args.Skip, Top = args.Top };
    }
}