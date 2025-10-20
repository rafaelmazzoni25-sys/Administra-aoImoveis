using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class PortalAppService
{
    private readonly PortalAccessService _portalAccessService;
    private readonly IPortalAccountRepository _portalAccountRepository;

    public PortalAppService(PortalAccessService portalAccessService, IPortalAccountRepository portalAccountRepository)
    {
        _portalAccessService = portalAccessService;
        _portalAccountRepository = portalAccountRepository;
    }

    public async Task<PortalAccountDto> RegisterAsync(string email, string name, PortalRole role, string password, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccessService.RegisterAsync(email, name, role, password, cancellationToken);
        return account.ToDto();
    }

    public async Task<PortalAccountDto> ResetPasswordAsync(Guid accountId, string password, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccessService.ResetPasswordAsync(accountId, password, cancellationToken);
        return account.ToDto();
    }

    public async Task<PortalAccountDto> UpdateRoleAsync(Guid accountId, PortalRole role, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccessService.UpdateRoleAsync(accountId, role, cancellationToken);
        return account.ToDto();
    }

    public async Task DeactivateAsync(Guid accountId, string reason, CancellationToken cancellationToken = default)
    {
        await _portalAccessService.DeactivateAsync(accountId, reason, cancellationToken);
    }

    public async Task<PortalAccountDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccessService.AuthenticateAsync(email, password, cancellationToken);
        return account.ToDto();
    }

    public async Task<PortalAccountDto> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _portalAccountRepository.GetByIdAsync(accountId, cancellationToken) ?? throw new InvalidOperationException("Conta não encontrada");
        return account.ToDto();
    }
}
