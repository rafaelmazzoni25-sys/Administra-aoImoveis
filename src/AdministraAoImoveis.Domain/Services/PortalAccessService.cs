using System.Security.Cryptography;
using System.Text;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class PortalAccessService
{
    private readonly IPortalAccountRepository _portalAccountRepository;

    public PortalAccessService(IPortalAccountRepository portalAccountRepository)
    {
        _portalAccountRepository = portalAccountRepository;
    }

    public async Task<PortalAccount> RegisterAsync(string email, string name, PortalRole role, string password, CancellationToken cancellationToken = default)
    {
        var account = new PortalAccount(Guid.NewGuid(), email, name, role, Hash(password));
        await _portalAccountRepository.AddAsync(account, cancellationToken);
        return account;
    }

    public async Task<PortalAccount> ResetPasswordAsync(Guid accountId, string password, CancellationToken cancellationToken = default)
    {
        var account = await EnsureAccountAsync(accountId, cancellationToken);
        account.ChangePassword(Hash(password));
        await _portalAccountRepository.UpdateAsync(account, cancellationToken);
        return account;
    }

    public async Task<PortalAccount> UpdateRoleAsync(Guid accountId, PortalRole role, CancellationToken cancellationToken = default)
    {
        var account = await EnsureAccountAsync(accountId, cancellationToken);
        account.UpdateRole(role);
        await _portalAccountRepository.UpdateAsync(account, cancellationToken);
        return account;
    }

    public async Task DeactivateAsync(Guid accountId, string reason, CancellationToken cancellationToken = default)
    {
        var account = await EnsureAccountAsync(accountId, cancellationToken);
        account.Deactivate(reason);
        await _portalAccountRepository.UpdateAsync(account, cancellationToken);
    }

    public async Task<PortalAccount> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccountRepository.GetByEmailAsync(email, cancellationToken) ?? throw new InvalidOperationException("Conta não localizada");
        if (!account.Active || account.HashedPassword != Hash(password))
        {
            throw new InvalidOperationException("Credenciais inválidas");
        }

        account.RecordAccess("Autenticação realizada");
        await _portalAccountRepository.UpdateAsync(account, cancellationToken);
        return account;
    }

    private static string Hash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private async Task<PortalAccount> EnsureAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _portalAccountRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Conta não encontrada");
    }
}
