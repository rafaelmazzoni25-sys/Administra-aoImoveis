using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Domain.Services;

public sealed class FinancialControlService
{
    private readonly IFinancialRepository _financialRepository;
    private readonly IPropertyRepository _propertyRepository;

    public FinancialControlService(IFinancialRepository financialRepository, IPropertyRepository propertyRepository)
    {
        _financialRepository = financialRepository;
        _propertyRepository = propertyRepository;
    }

    public async Task<FinancialEntry> RegisterAsync(FinancialEntry entry, CancellationToken cancellationToken = default)
    {
        await _financialRepository.AddAsync(entry, cancellationToken);
        if (entry.BlocksAvailability)
        {
            await BlockPropertyAsync(entry.ReferenceId, cancellationToken);
        }

        return entry;
    }

    public async Task<FinancialEntry> UpdateAmountAsync(Guid entryId, Money amount, CancellationToken cancellationToken = default)
    {
        var entry = await EnsureEntryAsync(entryId, cancellationToken);
        entry.UpdateAmount(amount);
        await _financialRepository.UpdateAsync(entry, cancellationToken);
        return entry;
    }

    public async Task<FinancialEntry> RegisterPaymentAsync(Guid entryId, DateTime paidAt, CancellationToken cancellationToken = default)
    {
        var entry = await EnsureEntryAsync(entryId, cancellationToken);
        entry.MarkReceived(paidAt);
        await _financialRepository.UpdateAsync(entry, cancellationToken);
        await ReleasePropertyIfClearedAsync(entry.ReferenceId, cancellationToken);
        return entry;
    }

    public async Task<FinancialEntry> CancelAsync(Guid entryId, string reason, CancellationToken cancellationToken = default)
    {
        var entry = await EnsureEntryAsync(entryId, cancellationToken);
        entry.Cancel(reason);
        await _financialRepository.UpdateAsync(entry, cancellationToken);
        await ReleasePropertyIfClearedAsync(entry.ReferenceId, cancellationToken);
        return entry;
    }

    public async Task EscalateOverdueAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var entries = await _financialRepository.GetBlockingAsync(cancellationToken);
        foreach (var entry in entries)
        {
            entry.MarkOverdue();
            await _financialRepository.UpdateAsync(entry, cancellationToken);
        }
    }

    private async Task<FinancialEntry> EnsureEntryAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _financialRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Lançamento não encontrado");
    }

    private async Task BlockPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return;
        }

        property.MarkPending(true);
        await _propertyRepository.UpdateAsync(property, cancellationToken);
    }

    private async Task ReleasePropertyIfClearedAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return;
        }

        var openBlocks = await _financialRepository.GetBlockingByPropertyAsync(propertyId, cancellationToken);
        if (!openBlocks.Any(e => e.Status != FinancialEntryStatus.Received && e.Status != FinancialEntryStatus.Cancelled))
        {
            property.MarkPending(false);
            await _propertyRepository.UpdateAsync(property, cancellationToken);
        }
    }
}
