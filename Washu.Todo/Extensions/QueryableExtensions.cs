using System.Linq.Dynamic.Core;
using Radzen;
using Washu.Framework.Entities.NamedEntity;
using Washu.Todo.Lists.Features;
using Washu.Todo.Tasks;

namespace Washu.Todo.Extensions;

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

    extension<T>(IQueryable<T> query)
    where T : class
    {
        public async Task<IQueryable<T>> ApplyDataGridFiltersAsync(LoadDataArgs args, Func<IQueryable<T>, ValueTask> onFilteredAndSorted)
        {
            if (!string.IsNullOrEmpty(args.Filter)) query = query.Where(args.Filter);
            if (!string.IsNullOrEmpty(args.OrderBy)) query = query.OrderBy(args.OrderBy);
            await onFilteredAndSorted(query);
            if (args.Skip is not null) query = query.Skip(args.Skip.Value);
            if (args.Top is not null) query = query.Take(args.Top.Value);
            return query;
        }
    }

    extension<T>(IQueryable<T> query) 
        where T : class, INamedEntity
    {
        public async Task<IQueryable<T>> ApplyDataGridDropDownFiltersAsync(LoadDataArgs args, Func<IQueryable<T>, ValueTask> onFilteredAndSorted)
        {
            if (!string.IsNullOrEmpty(args.Filter)) query = query.Where(t => t.Name.ToLower().Contains(args.Filter.ToLower()));
            if (!string.IsNullOrEmpty(args.OrderBy)) query = query.OrderBy(args.OrderBy);
            await onFilteredAndSorted(query);
            if (args.Skip is not null) query = query.Skip(args.Skip.Value);
            if (args.Top is not null) query = query.Take(args.Top.Value);
            return query;
        }
    }
}