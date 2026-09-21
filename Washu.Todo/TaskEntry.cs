namespace Washu.Todo;

/// <summary>
/// The core model of the To-do app, contains all the properties and methods that will be stored in the database, or any type of data structure relating to a set of Tasks.
/// All properties relating to time are stored in <b>universal time (UTC)</b>
/// </summary>
/// <param name="name">The name of the task entry you want to set it to</param>
/// <param name="goalDate">The date and time you want to complete the task by that you set the task entry to</param>
public sealed class TaskEntry(string name, DateTimeOffset goalDate, Guid id, Guid todoListId)
{
    public Guid Id { get; set; } = id;
    public Guid TodoListId { get; set; } = todoListId;
    /// <summary>
    /// The name of the task
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Whether the task is completed or not
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// The date and time the task was created in <b>universal time (UTC)</b>
    /// </summary>
    public DateTimeOffset DateCreated { get; private init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time you want to complete the task by in <b>universal time (UTC)</b>
    /// </summary>
    public DateTimeOffset GoalDate
    {
        get;
        private set => field = value.ToUniversalTime();
    } = goalDate.ToUniversalTime();

    /// <summary>
    /// The date and time at which the task was completed, is null by default
    /// </summary>
    public DateTimeOffset? DateCompleted
    {
        get;
        private set => field = value?.ToUniversalTime();
    } = null;

    /// <summary>
    /// <para>Toggles the task. (e.g. if <see cref="IsCompleted"/> was true beforehand, it will become false, and vice versa).</para>
    /// If <see cref="IsCompleted"/> is true, then the <see cref="DateCompleted"/> will be set
    /// else, they will reset.
    /// </summary>
    public void Toggle()
    {
        IsCompleted = !IsCompleted;
        if (IsCompleted)
        {
            DateCompleted = DateTimeOffset.UtcNow;
            return;
        }
        DateCompleted = null;
    }

    /// <summary>
    /// <para>Will reset the task back as if it was never completed.</para>
    /// Properties affected: <see cref="IsCompleted"/> and <see cref="DateCompleted"/>
    /// </summary>
    private void ResetTask()
    {
        IsCompleted = false;
        DateCompleted = null;
    }

    /// <summary>
    /// <para>Will change the goal date of the task. </para>
    /// Doing this will reset the task!
    /// </summary>
    /// <param name="dateTime">The date and time you want to complete the task by that you change the task entry to</param>
    public void ChangeGoalDate(DateTimeOffset dateTime)
    {
        if (dateTime == GoalDate) return;
        GoalDate = dateTime;
        ResetTask();
    }
}