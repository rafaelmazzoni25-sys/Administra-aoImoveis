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

app.Run();

internal sealed record PropertyRequest(string Code, string Address, string Type, decimal SizeSquareMeters, int Bedrooms, string OwnerName, string? Description);
internal sealed record NegotiationRequest(Guid PropertyId, string InterestedName, string InterestedEmail, string BrokerName);
internal sealed record AdvanceNegotiationRequest(NegotiationStage Stage, string Notes);
internal sealed record InspectionRequest(Guid PropertyId, InspectionType Type, DateTime ScheduledFor, string Responsible);
internal sealed record CompleteInspectionRequest(bool HasPending, string[]? PendingDescriptions);
internal sealed record TaskRequest(TaskType Type, string Title, string Description, string Sector, string Owner, TaskPriority Priority, DateTime? DueDate);
internal sealed record UpdateTaskStatusRequest(TaskStatus Status, string User, string Notes);
