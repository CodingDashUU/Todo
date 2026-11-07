namespace Washu.Todo.Components;

using Radzen;
using Radzen.Blazor;

public class DataGridFilterHelpers<TEntity>(RadzenDataGrid<TEntity> grid)
where TEntity : class
{
    private static bool IsNullOrEmpty(FilterOperator? value) => value is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty or FilterOperator.IsNull or FilterOperator.IsNotNull;
    public void OnFilter(DataGridColumnFilterEventArgs<TaskEntry> args)
    {
        if (!IsNullOrEmpty(args.Column?.GetFilterOperator())) return;
        OnFilterCleared(args);
        grid.Reload();
    }
    public void OnFilterCleared(DataGridColumnFilterEventArgs<TaskEntry> args)
    {
        var column = grid.ColumnsCollection.FirstOrDefault(c => c.Property == (args.Column?.Property ?? string.Empty));
        column?.FilterValue = null;
        grid.Reload();
    }
}