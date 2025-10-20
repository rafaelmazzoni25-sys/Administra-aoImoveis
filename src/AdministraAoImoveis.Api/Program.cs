using System.Linq;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Application.Services;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAdministraAoImoveis();
builder.Services.AddScoped<PropertyAppService>();
builder.Services.AddScoped<NegotiationAppService>();
builder.Services.AddScoped<InspectionAppService>();
builder.Services.AddScoped<TaskAppService>();
builder.Services.AddScoped<FinancialAppService>();
builder.Services.AddScoped<DocumentWorkflowAppService>();
builder.Services.AddScoped<CommunicationAppService>();
builder.Services.AddScoped<LeadAppService>();
builder.Services.AddScoped<PortalAppService>();
builder.Services.AddScoped<NotificationAppService>();
builder.Services.AddScoped<AuditAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/properties", async (PropertyRequest request, PropertyAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterAsync(request.Code, request.Address, request.Type, request.SizeSquareMeters, request.Bedrooms, request.OwnerName, request.Description, token);
    return Results.Created($"/api/properties/{dto.Id}", dto);
}).WithName("RegisterProperty").WithOpenApi();

app.MapGet("/api/properties/{code}", async (string code, PropertyAppService service, CancellationToken token) =>
{
    var dto = await service.GetByCodeAsync(code, token);
    return Results.Ok(dto);
}).WithName("GetPropertyByCode").WithOpenApi();

app.MapGet("/api/properties", async (DateTime? referenceDate, PropertyAppService service, CancellationToken token) =>
{
    var reference = referenceDate ?? DateTime.UtcNow;
    var items = await service.SearchAvailableAsync(reference, token);
    return Results.Ok(items);
}).WithName("SearchAvailableProperties").WithOpenApi();

app.MapPost("/api/negotiations", async (NegotiationRequest request, NegotiationAppService service, CancellationToken token) =>
{
    var dto = await service.CreateAsync(request.PropertyId, request.InterestedName, request.InterestedEmail, request.BrokerName, token);
    return Results.Created($"/api/negotiations/{dto.Id}", dto);
}).WithName("CreateNegotiation").WithOpenApi();

app.MapPost("/api/negotiations/{id}/advance", async (Guid id, AdvanceNegotiationRequest request, NegotiationAppService service, CancellationToken token) =>
{
    var dto = await service.AdvanceAsync(id, request.Stage, request.Notes, token);
    return Results.Ok(dto);
}).WithName("AdvanceNegotiation").WithOpenApi();

app.MapGet("/api/negotiations/property/{propertyId}", async (Guid propertyId, NegotiationAppService service, CancellationToken token) =>
{
    var items = await service.GetActiveByPropertyAsync(propertyId, token);
    return Results.Ok(items);
}).WithName("GetPropertyNegotiations").WithOpenApi();

app.MapPost("/api/inspections", async (InspectionRequest request, InspectionAppService service, CancellationToken token) =>
{
    var dto = await service.ScheduleAsync(request.PropertyId, request.Type, request.ScheduledFor, request.Responsible, token);
    return Results.Created($"/api/inspections/{dto.Id}", dto);
}).WithName("ScheduleInspection").WithOpenApi();

app.MapPost("/api/inspections/{id}/complete", async (Guid id, CompleteInspectionRequest request, InspectionAppService service, CancellationToken token) =>
{
    var dto = await service.CompleteAsync(id, request.HasPending, request.PendingDescriptions ?? Array.Empty<string>(), token);
    return Results.Ok(dto);
}).WithName("CompleteInspection").WithOpenApi();

app.MapGet("/api/inspections/property/{propertyId}", async (Guid propertyId, InspectionAppService service, CancellationToken token) =>
{
    var items = await service.GetScheduledForPropertyAsync(propertyId, token);
    return Results.Ok(items);
}).WithName("GetPropertyInspections").WithOpenApi();

app.MapPost("/api/tasks", async (TaskRequest request, TaskAppService service, CancellationToken token) =>
{
    var dto = await service.CreateAsync(request.Type, request.Title, request.Description, request.Sector, request.Owner, request.Priority, request.DueDate, token);
    return Results.Created($"/api/tasks/{dto.Id}", dto);
}).WithName("CreateTask").WithOpenApi();

app.MapPost("/api/tasks/{id}/status", async (Guid id, UpdateTaskStatusRequest request, TaskAppService service, CancellationToken token) =>
{
    var dto = await service.UpdateStatusAsync(id, request.Status, request.User, request.Notes, token);
    return Results.Ok(dto);
}).WithName("UpdateTaskStatus").WithOpenApi();

app.MapGet("/api/tasks/overdue", async (TaskAppService service, CancellationToken token) =>
{
    var items = await service.GetOverdueAsync(token);
    return Results.Ok(items);
}).WithName("GetOverdueTasks").WithOpenApi();

app.MapGet("/api/tasks/matrix", async (TaskAppService service, CancellationToken token) =>
{
    var matrix = await service.GetPriorityMatrixAsync(token);
    return Results.Ok(matrix);
}).WithName("GetTaskPriorityMatrix").WithOpenApi();

app.MapPost("/api/financial", async (FinancialEntryRequest request, FinancialAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterAsync(request.ReferenceId, request.ReferenceType, request.Type, request.Amount, request.Currency, request.DueDate, request.BlocksAvailability, token);
    return Results.Created($"/api/financial/{dto.Id}", dto);
}).WithName("RegisterFinancialEntry").WithOpenApi();

app.MapPut("/api/financial/{id}/amount", async (Guid id, UpdateFinancialAmountRequest request, FinancialAppService service, CancellationToken token) =>
{
    var dto = await service.UpdateAmountAsync(id, request.Amount, request.Currency, token);
    return Results.Ok(dto);
}).WithName("UpdateFinancialAmount").WithOpenApi();

app.MapPost("/api/financial/{id}/payment", async (Guid id, RegisterPaymentRequest request, FinancialAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterPaymentAsync(id, request.PaidAt, token);
    return Results.Ok(dto);
}).WithName("RegisterFinancialPayment").WithOpenApi();

app.MapPost("/api/financial/{id}/cancel", async (Guid id, CancelRequest request, FinancialAppService service, CancellationToken token) =>
{
    var dto = await service.CancelAsync(id, request.Reason, token);
    return Results.Ok(dto);
}).WithName("CancelFinancialEntry").WithOpenApi();

app.MapGet("/api/financial/blocking", async (FinancialAppService service, CancellationToken token) =>
{
    var items = await service.GetBlockingAsync(token);
    return Results.Ok(items);
}).WithName("GetBlockingFinancialEntries").WithOpenApi();

app.MapPost("/api/documents/workflows", async (DocumentWorkflowRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var signers = request.Signers?.Select(s => new DocumentSignerDto(s.Id ?? Guid.Empty, s.Name, s.Email, s.Mandatory, null, null)).ToArray() ?? Array.Empty<DocumentSignerDto>();
    var dto = await service.CreateAsync(request.ReferenceId, request.ReferenceType, request.DocumentType, request.Title, request.ContentTemplate, signers, token);
    return Results.Created($"/api/documents/workflows/{dto.Id}", dto);
}).WithName("CreateDocumentWorkflow").WithOpenApi();

app.MapPut("/api/documents/workflows/{id}/template", async (Guid id, DocumentTemplateRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var dto = await service.UpdateTemplateAsync(id, request.Title, request.Template, token);
    return Results.Ok(dto);
}).WithName("UpdateDocumentTemplate").WithOpenApi();

app.MapPost("/api/documents/workflows/{id}/activate", async (Guid id, DocumentActivationRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var dto = await service.ActivateAsync(id, request.FileName, request.StoragePath, request.ExpiresAt, token);
    return Results.Ok(dto);
}).WithName("ActivateDocumentWorkflow").WithOpenApi();

app.MapPost("/api/documents/workflows/{id}/signatures", async (Guid id, RegisterSignatureRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterSignatureAsync(id, request.SignerId, request.FilePath, token);
    return Results.Ok(dto);
}).WithName("RegisterDocumentSignature").WithOpenApi();

app.MapPost("/api/documents/workflows/{id}/archive", async (Guid id, CancelRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var dto = await service.ArchiveAsync(id, request.Reason, token);
    return Results.Ok(dto);
}).WithName("ArchiveDocumentWorkflow").WithOpenApi();

app.MapPost("/api/documents/workflows/{id}/cancel", async (Guid id, CancelRequest request, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var dto = await service.CancelAsync(id, request.Reason, token);
    return Results.Ok(dto);
}).WithName("CancelDocumentWorkflow").WithOpenApi();

app.MapGet("/api/documents/reference/{referenceType}/{referenceId}", async (string referenceType, Guid referenceId, DocumentWorkflowAppService service, CancellationToken token) =>
{
    var items = await service.GetByReferenceAsync(referenceId, referenceType, token);
    return Results.Ok(items);
}).WithName("GetDocumentWorkflowsByReference").WithOpenApi();

app.MapPost("/api/communications", async (CommunicationThreadRequest request, CommunicationAppService service, CancellationToken token) =>
{
    var dto = await service.CreateThreadAsync(request.ContextType, request.ContextId, request.Title, request.Participants, token);
    return Results.Created($"/api/communications/{dto.Id}", dto);
}).WithName("CreateCommunicationThread").WithOpenApi();

app.MapPost("/api/communications/{id}/messages", async (Guid id, PostMessageRequest request, CommunicationAppService service, CancellationToken token) =>
{
    var message = await service.PostMessageAsync(id, request.Author, request.Message, request.Mentions, token);
    return Results.Ok(message);
}).WithName("PostCommunicationMessage").WithOpenApi();

app.MapPost("/api/communications/{id}/archive", async (Guid id, CommunicationAppService service, CancellationToken token) =>
{
    await service.ArchiveThreadAsync(id, token);
    return Results.NoContent();
}).WithName("ArchiveCommunicationThread").WithOpenApi();

app.MapGet("/api/communications/{id}", async (Guid id, CommunicationAppService service, CancellationToken token) =>
{
    var dto = await service.GetAsync(id, token);
    return Results.Ok(dto);
}).WithName("GetCommunicationThread").WithOpenApi();

app.MapPost("/api/leads", async (LeadRequest request, LeadAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterAsync(request.Name, request.Contact, request.Source, request.DesiredPropertyType, request.Budget, token);
    return Results.Created($"/api/leads/{dto.Id}", dto);
}).WithName("RegisterLead").WithOpenApi();

app.MapPost("/api/leads/{id}/assign", async (Guid id, AssignLeadRequest request, LeadAppService service, CancellationToken token) =>
{
    var dto = await service.AssignAsync(id, request.Owner, token);
    return Results.Ok(dto);
}).WithName("AssignLead").WithOpenApi();

app.MapPost("/api/leads/{id}/status", async (Guid id, LeadStatusRequest request, LeadAppService service, CancellationToken token) =>
{
    var dto = await service.ChangeStatusAsync(id, request.Status, request.Actor, token);
    return Results.Ok(dto);
}).WithName("ChangeLeadStatus").WithOpenApi();

app.MapPost("/api/leads/{id}/interactions", async (Guid id, LeadInteractionRequest request, LeadAppService service, CancellationToken token) =>
{
    var dto = await service.RecordInteractionAsync(id, request.Description, request.Actor, token);
    return Results.Ok(dto);
}).WithName("RecordLeadInteraction").WithOpenApi();

app.MapGet("/api/leads", async (string? assignedTo, LeadAppService service, CancellationToken token) =>
{
    var items = await service.SearchAsync(assignedTo, token);
    return Results.Ok(items);
}).WithName("SearchLeads").WithOpenApi();

app.MapPost("/api/portal/accounts", async (PortalAccountRequest request, PortalAppService service, CancellationToken token) =>
{
    var dto = await service.RegisterAsync(request.Email, request.Name, request.Role, request.Password, token);
    return Results.Created($"/api/portal/accounts/{dto.Id}", dto);
}).WithName("RegisterPortalAccount").WithOpenApi();

app.MapPost("/api/portal/accounts/{id}/password", async (Guid id, ResetPasswordRequest request, PortalAppService service, CancellationToken token) =>
{
    var dto = await service.ResetPasswordAsync(id, request.Password, token);
    return Results.Ok(dto);
}).WithName("ResetPortalPassword").WithOpenApi();

app.MapPost("/api/portal/accounts/{id}/role", async (Guid id, PortalRoleRequest request, PortalAppService service, CancellationToken token) =>
{
    var dto = await service.UpdateRoleAsync(id, request.Role, token);
    return Results.Ok(dto);
}).WithName("UpdatePortalRole").WithOpenApi();

app.MapPost("/api/portal/accounts/{id}/deactivate", async (Guid id, CancelRequest request, PortalAppService service, CancellationToken token) =>
{
    await service.DeactivateAsync(id, request.Reason, token);
    return Results.NoContent();
}).WithName("DeactivatePortalAccount").WithOpenApi();

app.MapPost("/api/portal/authenticate", async (PortalAuthenticationRequest request, PortalAppService service, CancellationToken token) =>
{
    var dto = await service.AuthenticateAsync(request.Email, request.Password, token);
    return Results.Ok(dto);
}).WithName("AuthenticatePortalAccount").WithOpenApi();

app.MapGet("/api/portal/accounts/{id}", async (Guid id, PortalAppService service, CancellationToken token) =>
{
    var dto = await service.GetAsync(id, token);
    return Results.Ok(dto);
}).WithName("GetPortalAccount").WithOpenApi();

app.MapPost("/api/notifications", async (NotificationRequest request, NotificationAppService service, CancellationToken token) =>
{
    var dto = await service.NotifyAsync(request.Recipient, request.Title, request.Message, request.Severity, request.Module, token);
    return Results.Created($"/api/notifications/{dto.Id}", dto);
}).WithName("SendNotification").WithOpenApi();

app.MapPost("/api/notifications/{id}/read", async (Guid id, NotificationAppService service, CancellationToken token) =>
{
    await service.MarkReadAsync(id, token);
    return Results.NoContent();
}).WithName("MarkNotificationRead").WithOpenApi();

app.MapGet("/api/notifications/pending/{recipient}", async (string recipient, NotificationAppService service, CancellationToken token) =>
{
    var items = await service.GetPendingAsync(recipient, token);
    return Results.Ok(items);
}).WithName("GetPendingNotifications").WithOpenApi();

app.MapPost("/api/audit", async (AuditRequest request, AuditAppService service, CancellationToken token) =>
{
    var dto = await service.RecordAsync(request.Actor, request.Action, request.Target, request.Details, token);
    return Results.Created($"/api/audit/{dto.Id}", dto);
}).WithName("RecordAuditEntry").WithOpenApi();

app.MapGet("/api/audit", async (int? take, AuditAppService service, CancellationToken token) =>
{
    var limit = take is > 0 ? take.Value : 20;
    var items = await service.GetRecentAsync(limit, token);
    return Results.Ok(items);
}).WithName("GetAuditTrail").WithOpenApi();

app.Run();

internal sealed record PropertyRequest(string Code, string Address, string Type, decimal SizeSquareMeters, int Bedrooms, string OwnerName, string? Description);
internal sealed record NegotiationRequest(Guid PropertyId, string InterestedName, string InterestedEmail, string BrokerName);
internal sealed record AdvanceNegotiationRequest(NegotiationStage Stage, string Notes);
internal sealed record InspectionRequest(Guid PropertyId, InspectionType Type, DateTime ScheduledFor, string Responsible);
internal sealed record CompleteInspectionRequest(bool HasPending, string[]? PendingDescriptions);
internal sealed record TaskRequest(TaskType Type, string Title, string Description, string Sector, string Owner, TaskPriority Priority, DateTime? DueDate);
internal sealed record UpdateTaskStatusRequest(TaskStatus Status, string User, string Notes);
internal sealed record FinancialEntryRequest(Guid ReferenceId, string ReferenceType, FinancialEntryType Type, decimal Amount, string Currency, DateTime DueDate, bool BlocksAvailability);
internal sealed record UpdateFinancialAmountRequest(decimal Amount, string Currency);
internal sealed record RegisterPaymentRequest(DateTime PaidAt);
internal sealed record CancelRequest(string Reason);
internal sealed record DocumentWorkflowRequest(Guid ReferenceId, string ReferenceType, DocumentType DocumentType, string Title, string ContentTemplate, DocumentSignerRequest[]? Signers);
internal sealed record DocumentSignerRequest(Guid? Id, string Name, string Email, bool Mandatory);
internal sealed record DocumentTemplateRequest(string Title, string Template);
internal sealed record DocumentActivationRequest(string FileName, string StoragePath, DateTime? ExpiresAt);
internal sealed record RegisterSignatureRequest(Guid SignerId, string FilePath);
internal sealed record CommunicationThreadRequest(CommunicationContextType ContextType, Guid ContextId, string Title, string[] Participants);
internal sealed record PostMessageRequest(string Author, string Message, string[]? Mentions);
internal sealed record LeadRequest(string Name, string Contact, LeadSource Source, string DesiredPropertyType, decimal? Budget);
internal sealed record AssignLeadRequest(string Owner);
internal sealed record LeadStatusRequest(LeadStatus Status, string Actor);
internal sealed record LeadInteractionRequest(string Description, string Actor);
internal sealed record PortalAccountRequest(string Email, string Name, PortalRole Role, string Password);
internal sealed record ResetPasswordRequest(string Password);
internal sealed record PortalRoleRequest(PortalRole Role);
internal sealed record PortalAuthenticationRequest(string Email, string Password);
internal sealed record NotificationRequest(string Recipient, string Title, string Message, NotificationSeverity Severity, string Module);
internal sealed record AuditRequest(string Actor, string Action, string Target, string Details);
