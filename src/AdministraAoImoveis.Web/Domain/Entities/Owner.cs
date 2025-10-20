namespace AdministraAoImoveis.Web.Domain.Entities;

public class Owner : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public string? UsuarioId { get; set; }
    public ICollection<Property> Imoveis { get; set; } = new List<Property>();
}
