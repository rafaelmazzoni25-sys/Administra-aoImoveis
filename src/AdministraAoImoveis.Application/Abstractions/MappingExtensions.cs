using System.Linq;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Application.Abstractions;

public static class MappingExtensions
{
    public static PropertyDto ToDto(this Property property) => new(
        property.Id,
        property.Code,
        property.Address,
        property.Type,
        property.SizeSquareMeters,
        property.Bedrooms,
        property.OwnerName,
        property.Status,
        property.AvailableFrom,
        property.ActiveNegotiationId,
        property.HasOpenMaintenance,
        property.HasOpenPending);

    public static NegotiationDto ToDto(this Negotiation negotiation) => new(
        negotiation.Id,
        negotiation.PropertyId,
        negotiation.InterestedName,
        negotiation.InterestedEmail,
        negotiation.BrokerName,
        negotiation.Stage,
        negotiation.CreatedAt,
        negotiation.ClosedAt,
        negotiation.ProposalExpiresAt,
        negotiation.SignalAmount);

    public static InspectionDto ToDto(this Inspection inspection) => new(
        inspection.Id,
        inspection.PropertyId,
        inspection.Type,
        inspection.ScheduledFor,
        inspection.Responsible,
        inspection.Status,
        inspection.StartedAt,
        inspection.FinishedAt,
        inspection.ChecklistItems.ToList(),
        inspection.Photos.ToList());

    public static TaskDto ToDto(this TaskItem task) => new(
        task.Id,
        task.Type,
        task.Title,
        task.Description,
        task.Sector,
        task.Owner,
        task.Priority,
        task.CreatedAt,
        task.DueDate,
        task.Status,
        task.Updates.Select(u => $"[{u.OccurredAt:dd/MM HH:mm}] {u.User}: {u.Message}").ToList());

    public static CommunicationDto ToDto(this CommunicationThread thread) => new(
        thread.Id,
        thread.ContextType,
        thread.ContextId,
        thread.Title,
        thread.CreatedAt,
        thread.Archived,
        thread.Participants.ToList(),
        thread.Messages.Select(m => new CommunicationMessageDto(m.Id, m.Author, m.Content, m.SentAt, m.Mentions.ToList())).ToList());

    public static DocumentWorkflowDto ToDto(this DocumentWorkflow workflow) => new(
        workflow.Id,
        workflow.ReferenceId,
        workflow.ReferenceType,
        workflow.DocumentType,
        workflow.Title,
        workflow.ContentTemplate,
        workflow.Status,
        workflow.CreatedAt,
        workflow.CompletedAt,
        workflow.Signers.Select(s => new DocumentSignerDto(s.Id, s.Name, s.Email, s.Mandatory, s.SignedAt, s.SignedFilePath)).ToList(),
        workflow.History.Select(h => new DocumentWorkflowHistoryDto(h.OccurredAt, h.Status, h.Notes)).ToList(),
        workflow.GeneratedDocument?.ToDto());

    public static DocumentRecordDto ToDto(this DocumentRecord record) => new(
        record.Id,
        record.OwnerId,
        record.OwnerType,
        record.Type,
        record.FileName,
        record.StoragePath,
        record.UploadedAt,
        record.ExpiresAt);

    public static FinancialEntryDto ToDto(this FinancialEntry entry) => new(
        entry.Id,
        entry.ReferenceId,
        entry.ReferenceType,
        entry.Type,
        entry.Amount,
        entry.DueDate,
        entry.BlocksAvailability,
        entry.Status,
        entry.PaidAt,
        entry.History.Select(h => new FinancialHistoryDto(h.OccurredAt, h.Status, h.Notes)).ToList());

    public static LeadDto ToDto(this Lead lead) => new(
        lead.Id,
        lead.Name,
        lead.Contact,
        lead.Source,
        lead.DesiredPropertyType,
        lead.Budget,
        lead.Status,
        lead.AssignedTo,
        lead.CreatedAt,
        lead.Interactions.Select(i => new LeadInteractionDto(i.OccurredAt, i.Description, i.Actor)).ToList());

    public static PortalAccountDto ToDto(this PortalAccount account) => new(
        account.Id,
        account.Email,
        account.Name,
        account.Role,
        account.Active,
        account.CreatedAt,
        account.Logs.Select(l => new PortalAccessLogDto(l.OccurredAt, l.Description)).ToList());

    public static NotificationDto ToDto(this NotificationMessage notification) => new(
        notification.Id,
        notification.Recipient,
        notification.Title,
        notification.Message,
        notification.Severity,
        notification.RelatedModule,
        notification.CreatedAt,
        notification.ReadAt);

    public static AuditLogDto ToDto(this AuditLogEntry entry) => new(
        entry.Id,
        entry.Actor,
        entry.Action,
        entry.Target,
        entry.Details,
        entry.OccurredAt);
}
