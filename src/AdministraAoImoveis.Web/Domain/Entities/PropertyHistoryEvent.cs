namespace AdministraAoImoveis.Web.Domain.Entities;

public class PropertyHistoryEvent : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTime OcorreuEm { get; set; } = DateTime.UtcNow;
}
