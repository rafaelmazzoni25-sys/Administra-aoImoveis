namespace AdministraAoImoveis.Web.Domain.Entities;

public class ScheduleEntry : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Setor { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Responsavel { get; set; } = string.Empty;
    public Guid? ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public Guid? VistoriaId { get; set; }
    public Inspection? Vistoria { get; set; }
    public Guid? NegociacaoId { get; set; }
    public Negotiation? Negociacao { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}
