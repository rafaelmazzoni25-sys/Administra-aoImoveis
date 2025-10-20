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

        services.AddScoped<PropertyAvailabilityService>();
        services.AddScoped<NegotiationPipelineService>();
        services.AddScoped<InspectionWorkflowService>();
        services.AddScoped<TaskManagementService>();
        services.AddScoped<AgendaSchedulerService>();

        return services;
    }
}
