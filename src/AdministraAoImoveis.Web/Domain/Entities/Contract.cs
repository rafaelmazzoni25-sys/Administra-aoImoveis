namespace AdministraAoImoveis.Web.Domain.Entities;

public class Contract : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public Guid NegociacaoId { get; set; }
    public Negotiation? Negociacao { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal ValorAluguel { get; set; }
    public decimal Encargos { get; set; }
    public bool Ativo { get; set; }
    public Guid? DocumentoContratoId { get; set; }
    public StoredFile? DocumentoContrato { get; set; }
}
