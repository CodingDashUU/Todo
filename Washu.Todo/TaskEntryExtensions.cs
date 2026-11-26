namespace Washu.Todo;

using Components;
using Tasks;

public readonly record struct NotCompleted;
public readonly record struct Overdue;
public readonly record struct NotCompletedInTime;
public readonly record struct Completed;

public union TaskCompletionState(NotCompleted, Overdue, NotCompletedInTime, Completed);
public static class TaskEntryExtensions
{
    extension(TaskEntry entry)
    {
        public TaskCompletionState DetermineCompletionState(DateTimeOffset now)
        {
            var completed = entry is { IsCompleted: true, DateCompleted: not null };
            var notCompleteInTime = completed && entry.DateCompleted > entry.GoalDate;
            var overdue = !completed && now > entry.GoalDate;
    
            if (overdue) return new Overdue();
            if (!completed) return new NotCompleted();
            return notCompleteInTime 
                ? new NotCompletedInTime()
                : new Completed();
        }
    }
}