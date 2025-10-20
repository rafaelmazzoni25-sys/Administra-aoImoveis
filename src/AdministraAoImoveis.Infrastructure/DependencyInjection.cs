using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;
using AdministraAoImoveis.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AdministraAoImoveis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministraAoImoveis(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDataStore>();

        services.AddSingleton<IPropertyRepository, InMemoryPropertyRepository>();
        services.AddSingleton<INegotiationRepository, InMemoryNegotiationRepository>();
        services.AddSingleton<IInspectionRepository, InMemoryInspectionRepository>();
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
        services.AddSingleton<IMaintenanceRepository, InMemoryMaintenanceRepository>();
        services.AddSingleton<IAgendaRepository, InMemoryAgendaRepository>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IDocumentWorkflowRepository, InMemoryDocumentWorkflowRepository>();
        services.AddSingleton<ICommunicationRepository, InMemoryCommunicationRepository>();
        services.AddSingleton<IFinancialRepository, InMemoryFinancialRepository>();
        services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
        services.AddSingleton<IPortalAccountRepository, InMemoryPortalAccountRepository>();
        services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();

        services.AddScoped<PropertyAvailabilityService>();
        services.AddScoped<NegotiationPipelineService>();
        services.AddScoped<InspectionWorkflowService>();
        services.AddScoped<TaskManagementService>();
        services.AddScoped<AgendaSchedulerService>();
        services.AddScoped<DocumentWorkflowService>();
        services.AddScoped<CommunicationService>();
        services.AddScoped<FinancialControlService>();
        services.AddScoped<LeadCrmService>();
        services.AddScoped<PortalAccessService>();
        services.AddScoped<AuditTrailService>();
        services.AddScoped<NotificationService>();

        return services;
    }
}
