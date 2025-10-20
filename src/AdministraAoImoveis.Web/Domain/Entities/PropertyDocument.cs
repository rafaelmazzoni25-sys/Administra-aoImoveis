namespace AdministraAoImoveis.Web.Domain.Entities;

public class PropertyDocument : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Versao { get; set; } = 1;
}
