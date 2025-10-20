using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryTaskRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _store.Tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Tasks.TryGetValue(id, out var task);
        return Task.FromResult<TaskItem?>(task);
    }

    public Task<IReadOnlyList<TaskItem>> GetOverdueAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var result = _store.Tasks.Values
            .Where(t => t.IsOverdue(referenceDate))
            .ToList();
        return Task.FromResult<IReadOnlyList<TaskItem>>(result);
    }

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _store.Tasks[task.Id] = task;
        return Task.CompletedTask;
    }
}
