using System.Collections.Concurrent;
using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Infrastructure.Persistence;

internal sealed class InMemoryDataStore
{
    public ConcurrentDictionary<Guid, Property> Properties { get; } = new();
    public ConcurrentDictionary<Guid, Negotiation> Negotiations { get; } = new();
    public ConcurrentDictionary<Guid, Inspection> Inspections { get; } = new();
    public ConcurrentDictionary<Guid, TaskItem> Tasks { get; } = new();
    public ConcurrentDictionary<Guid, MaintenanceOrder> MaintenanceOrders { get; } = new();
    public ConcurrentDictionary<Guid, AgendaEvent> AgendaEvents { get; } = new();
    public ConcurrentDictionary<Guid, DocumentRecord> Documents { get; } = new();
    public ConcurrentDictionary<Guid, DocumentWorkflow> DocumentWorkflows { get; } = new();
    public ConcurrentDictionary<Guid, CommunicationThread> CommunicationThreads { get; } = new();
    public ConcurrentDictionary<Guid, FinancialEntry> FinancialEntries { get; } = new();
    public ConcurrentDictionary<Guid, Lead> Leads { get; } = new();
    public ConcurrentDictionary<Guid, PortalAccount> PortalAccounts { get; } = new();
    public ConcurrentDictionary<Guid, AuditLogEntry> AuditLogs { get; } = new();
    public ConcurrentDictionary<Guid, NotificationMessage> Notifications { get; } = new();
}
