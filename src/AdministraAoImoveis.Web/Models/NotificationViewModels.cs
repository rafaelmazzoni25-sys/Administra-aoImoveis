namespace AdministraAoImoveis.Web.Models;

public class NotificationListViewModel
{
    public IReadOnlyCollection<NotificationItemViewModel> Notificacoes { get; set; }
        = Array.Empty<NotificationItemViewModel>();
    public int TotalNaoLidas { get; set; }
}

public class NotificationItemViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Lida { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LidaEm { get; set; }
    public string? LinkDestino { get; set; }
}

public class NotificationBellViewModel
{
    public int NaoLidas { get; set; }
    public IReadOnlyCollection<NotificationBellItemViewModel> Recentes { get; set; }
        = Array.Empty<NotificationBellItemViewModel>();
}

public class NotificationBellItemViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? LinkDestino { get; set; }
    public bool Lida { get; set; }
}
