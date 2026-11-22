namespace Washu.Todo;

using Components;
using Tasks;

public enum TaskCompletionState : byte { NotCompleted, Overdue, NotCompletedInTime, Completed }

public static class TaskEntryExtensions
{
    extension(TaskEntry entry)
    {
        public TaskCompletionState DetermineCompletionState(DateTimeOffset now)
        {
            var completed = entry is { IsCompleted: true, DateCompleted: not null };
            var notCompleteInTime = completed && entry.DateCompleted > entry.GoalDate;
            var overdue = !completed && now > entry.GoalDate;
    
            if (overdue) return TaskCompletionState.Overdue;
            if (!completed) return TaskCompletionState.NotCompleted;
            return notCompleteInTime 
                ? TaskCompletionState.NotCompletedInTime 
                : TaskCompletionState.Completed;
        }
    }
}