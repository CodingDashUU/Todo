namespace Washu.Todo;

public static class QueryableExtensions
{
    extension(IQueryable<TodoList> query)
    {
        public IQueryable<TodoList> ByTodoList(Guid listId) => query.Where(l => l.Id == listId);
    }
    extension(IQueryable<TaskEntry> query)
    {
        public IQueryable<TaskEntry> ForTodoList(Guid listId) => query.Where(l => l.TodoListId == listId);
        public IQueryable<TaskEntry> ByTask(Guid taskId) => query.Where(l => l.Id == taskId);
    }
}