using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class AuditLogListViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string? Entidade { get; set; }
    public string? Usuario { get; set; }
    public IReadOnlyCollection<string> EntidadesDisponiveis { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<AuditLogEntry> Registros { get; set; } = Array.Empty<AuditLogEntry>();
}
