using System;

namespace AdministraAoImoveis.Web.Services.DocumentExpiration;

public class PropertyDocumentExpirationOptions
{
    public const string SectionName = "DocumentExpiration";

    /// <summary>
    /// Intervalo entre execuções do job de expiração.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Indica se o job deve executar imediatamente na inicialização.
    /// </summary>
    public bool RunOnStartup { get; set; } = true;
}
