using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetOverdueAsync(DateTime referenceDate, CancellationToken cancellationToken = default);
}
