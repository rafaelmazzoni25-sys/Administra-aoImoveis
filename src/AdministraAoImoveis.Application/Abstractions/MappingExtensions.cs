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
}
