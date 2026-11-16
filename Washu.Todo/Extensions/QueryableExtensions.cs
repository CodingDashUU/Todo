using System.Linq.Dynamic.Core;
using Radzen;

namespace Washu.Todo.Extensions;

using Components;
using Domain;
using Framework.Entities;

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
        public async Task<IQueryable<T>> ApplyDataGridFiltersAsync(LoadDataConfig config, Func<IQueryable<T>, ValueTask> onFilteredAndSorted)
        {
            if (!string.IsNullOrEmpty(config.Filter)) query = query.Where(config.Filter);
            if (!string.IsNullOrEmpty(config.OrderBy)) query = query.OrderBy(config.OrderBy);
            await onFilteredAndSorted(query);
            if (config.Skip is not null) query = query.Skip(config.Skip.Value);
            if (config.Top is not null) query = query.Take(config.Top.Value);
            return query;
        }
    }

    extension<T>(IQueryable<T> query) 
        where T : class, INamedEntity
    {
        public async Task<IQueryable<T>> ApplyDataGridDropDownFiltersAsync(LoadDataConfig config, Func<IQueryable<T>, ValueTask> onFilteredAndSorted)
        {
            if (!string.IsNullOrEmpty(config.Filter)) query = query.Where(t => t.Name.ToLower().Contains(config.Filter.ToLower()));
            if (!string.IsNullOrEmpty(config.OrderBy)) query = query.OrderBy(config.OrderBy);
            await onFilteredAndSorted(query);
            if (config.Skip is not null) query = query.Skip(config.Skip.Value);
            if (config.Top is not null) query = query.Take(config.Top.Value);
            return query;
        }
    }
}