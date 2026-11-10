using Radzen;
using Radzen.Blazor;

namespace Washu.Framework.Radzen.DataGrid;

public class DataGridFilterHelpers<TEntity>(RadzenDataGrid<TEntity> grid)
where TEntity : class
{
    private static bool IsNullOrEmpty(FilterOperator? value) => value is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty or FilterOperator.IsNull or FilterOperator.IsNotNull;
    public void OnFilter(DataGridColumnFilterEventArgs<TEntity> args)
    {
        if (!IsNullOrEmpty(args.Column?.GetFilterOperator())) return;
        OnFilterCleared(args);
        grid.Reload();
    }
    public void OnFilterCleared(DataGridColumnFilterEventArgs<TEntity> args)
    {
        var column = grid.ColumnsCollection.FirstOrDefault(c => c.Property == (args.Column?.Property ?? string.Empty));
        column?.FilterValue = null;
        grid.Reload();
    }
}