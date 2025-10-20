namespace AdministraAoImoveis.Web.Domain.Entities;

public class StoredFile : BaseEntity
{
    public string NomeOriginal { get; set; } = string.Empty;
    public string CaminhoRelativo { get; set; } = string.Empty;
    public string ConteudoTipo { get; set; } = string.Empty;
    public long TamanhoEmBytes { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
}
