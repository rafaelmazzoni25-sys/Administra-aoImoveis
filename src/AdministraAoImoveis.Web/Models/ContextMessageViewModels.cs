using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class ContextConversationViewModel
{
    public ContextSummaryViewModel? Contexto { get; set; }
    public IReadOnlyCollection<ContextMessageItemViewModel> Mensagens { get; set; }
        = Array.Empty<ContextMessageItemViewModel>();
    public ContextMessageInputModel NovaMensagem { get; set; } = new();
    public IReadOnlyCollection<ContextSummaryViewModel> Recentes { get; set; }
        = Array.Empty<ContextSummaryViewModel>();
}

public class ContextSummaryViewModel
{
    public ActivityLinkType ContextoTipo { get; set; }
    public Guid ContextoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? LinkDestino { get; set; }
}

public class ContextMessageItemViewModel
{
    public Guid Id { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime EnviadaEm { get; set; }
    public IReadOnlyCollection<string> Mentions { get; set; } = Array.Empty<string>();
    public bool DoUsuarioAtual { get; set; }
}

public class ContextMessageInputModel
{
    public ActivityLinkType ContextoTipo { get; set; }
    public Guid ContextoId { get; set; }
    [Required]
    [StringLength(2000)]
    public string Mensagem { get; set; } = string.Empty;
}
