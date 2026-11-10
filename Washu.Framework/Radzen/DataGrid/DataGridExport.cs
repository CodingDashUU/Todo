using Radzen;
using Radzen.Blazor;

namespace Washu.Framework.Radzen.DataGrid;

public sealed record DataGridExportCommand<TEntity>(
    RadzenDataGrid<TEntity> DataGrid, 
    DataGridExportMode ExportMode, 
    DataGridExportScope Scope, 
    string FileName, 
    string ExcelTitle, 
    bool UseTheme) where TEntity : class;
public enum DataGridExportMode : byte { Excel, Csv }