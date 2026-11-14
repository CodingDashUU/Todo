namespace Washu.Todo.Components;

public class LoadDataConfig
{
    public string? Filter { get; init; }
    public string? OrderBy { get; init; }
    public int? Skip { get; init; }
    public int? Top { get; init; }
}