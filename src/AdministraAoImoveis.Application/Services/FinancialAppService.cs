using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Application.Services;

public sealed class FinancialAppService
{
    private readonly FinancialControlService _financialControlService;
    private readonly IFinancialRepository _financialRepository;

    public FinancialAppService(FinancialControlService financialControlService, IFinancialRepository financialRepository)
    {
        _financialControlService = financialControlService;
        _financialRepository = financialRepository;
    }

    public async Task<FinancialEntryDto> RegisterAsync(Guid referenceId, string referenceType, FinancialEntryType type, decimal amount, string currency, DateTime dueDate, bool blocksAvailability, CancellationToken cancellationToken = default)
    {
        var entry = new FinancialEntry(Guid.NewGuid(), referenceId, referenceType, type, new Money(amount, currency), dueDate, blocksAvailability);
        await _financialControlService.RegisterAsync(entry, cancellationToken);
        return entry.ToDto();
    }

    public async Task<FinancialEntryDto> UpdateAmountAsync(Guid entryId, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        var entry = await _financialControlService.UpdateAmountAsync(entryId, new Money(amount, currency), cancellationToken);
        return entry.ToDto();
    }

    public async Task<FinancialEntryDto> RegisterPaymentAsync(Guid entryId, DateTime paidAt, CancellationToken cancellationToken = default)
    {
        var entry = await _financialControlService.RegisterPaymentAsync(entryId, paidAt, cancellationToken);
        return entry.ToDto();
    }

    public async Task<FinancialEntryDto> CancelAsync(Guid entryId, string reason, CancellationToken cancellationToken = default)
    {
        var entry = await _financialControlService.CancelAsync(entryId, reason, cancellationToken);
        return entry.ToDto();
    }

    public async Task<IReadOnlyCollection<FinancialEntryDto>> GetBlockingAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _financialRepository.GetBlockingAsync(cancellationToken);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
