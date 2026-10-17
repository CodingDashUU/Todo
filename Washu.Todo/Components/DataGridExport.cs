namespace Washu.Todo.Components;

using Radzen;
using Radzen.Blazor;

public sealed record DataGridExportCommand(
    RadzenDataGrid<TaskEntry> DataGrid, 
    DataGridExportMode ExportMode, 
    DataGridExportScope Scope, 
    string FileName, 
    string ExcelTitle, 
    bool UseTheme);
public enum DataGridExportMode : byte { Excel, Csv }