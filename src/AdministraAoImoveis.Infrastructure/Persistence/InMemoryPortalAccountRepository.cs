using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryPortalAccountRepository : IPortalAccountRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryPortalAccountRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(PortalAccount account, CancellationToken cancellationToken = default)
    {
        _store.PortalAccounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<PortalAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var account = _store.PortalAccounts.Values.FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<PortalAccount?>(account);
    }

    public Task<PortalAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.PortalAccounts.TryGetValue(id, out var account);
        return Task.FromResult<PortalAccount?>(account);
    }

    public Task UpdateAsync(PortalAccount account, CancellationToken cancellationToken = default)
    {
        _store.PortalAccounts[account.Id] = account;
        return Task.CompletedTask;
    }
}
