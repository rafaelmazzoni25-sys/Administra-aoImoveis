using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services;
using AdministraAoImoveis.Web.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.GestaoImoveis)]
[Route("Imoveis/{propertyId:guid}/Documentos")]
public class DocumentosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DocumentosController> _logger;
    private readonly IAuditTrailService _auditTrailService;

    private const string StorageCategory = "property-documents";

    public DocumentosController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<DocumentosController> logger,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _auditTrailService = auditTrailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await LoadPropertyAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        await NormalizeDocumentStatusesAsync(property, cancellationToken);

        var model = BuildLibraryViewModel(property);
        return View(model);
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        Guid propertyId,
        PropertyDocumentUploadInputModel input,
        CancellationToken cancellationToken)
    {
        var property = await LoadPropertyAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        if (input.Arquivo is null)
        {
            ModelState.AddModelError(nameof(PropertyDocumentUploadInputModel.Arquivo), "Selecione um arquivo válido.");
        }

        if (input.ValidoAte.HasValue && input.ValidoAte.Value.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(PropertyDocumentUploadInputModel.ValidoAte), "Validade não pode estar no passado.");
        }

        if (!ModelState.IsValid)
        {
            input.Arquivo = null;
            var invalidModel = BuildLibraryViewModel(property, input);
            return View("Index", invalidModel);
        }

        var user = User?.Identity?.Name ?? "Sistema";
        var agora = DateTime.UtcNow;
        var descricaoNormalizada = input.Descricao.Trim();
        var observacoesNormalizadas = string.IsNullOrWhiteSpace(input.Observacoes)
            ? null
            : input.Observacoes.Trim();

        var existingVersions = property.Documentos
            .Where(d => string.Equals(d.Descricao, descricaoNormalizada, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var version in existingVersions.Where(v => v.Status == DocumentStatus.Aprovado))
        {
            version.Status = DocumentStatus.Obsoleto;
            version.UpdatedAt = agora;
            version.UpdatedBy = user;
        }

        var proximaVersao = existingVersions.Any()
            ? existingVersions.Max(d => d.Versao) + 1
            : 1;

        await using var content = input.Arquivo!.OpenReadStream();
        var storedFile = await _fileStorageService.SaveAsync(
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            content,
            StorageCategory,
            cancellationToken);

        storedFile.CreatedBy = user;
        _context.Arquivos.Add(storedFile);

        var novoDocumento = new PropertyDocument
        {
            ImovelId = property.Id,
            ArquivoId = storedFile.Id,
            Arquivo = storedFile,
            Descricao = descricaoNormalizada,
            Versao = proximaVersao,
            Status = input.AprovarAutomaticamente ? DocumentStatus.Aprovado : DocumentStatus.Pendente,
            ValidoAte = input.ValidoAte,
            RequerAceiteProprietario = input.RequerAceiteProprietario,
            Observacoes = observacoesNormalizadas,
            CreatedBy = user
        };

        if (input.AprovarAutomaticamente)
        {
            novoDocumento.RevisadoEm = agora;
            novoDocumento.RevisadoPor = user;
            novoDocumento.UpdatedAt = agora;
            novoDocumento.UpdatedBy = user;

            if (novoDocumento.ValidoAte.HasValue && novoDocumento.ValidoAte.Value < agora)
            {
                novoDocumento.Status = DocumentStatus.Expirado;
            }
        }

        _context.PropertyDocuments.Add(novoDocumento);
        await _context.SaveChangesAsync(cancellationToken);

        var depois = JsonSerializer.Serialize(novoDocumento);
        await RegistrarAuditoriaDocumentoAsync(novoDocumento, "CREATE", string.Empty, depois, cancellationToken);

        _logger.LogInformation(
            "Documento {Descricao} v{Versao} cadastrado para o imóvel {ImovelId} por {Usuario}",
            novoDocumento.Descricao,
            novoDocumento.Versao,
            property.Id,
            user);

        TempData["Success"] = existingVersions.Any()
            ? "Nova versão do documento cadastrada com sucesso."
            : "Documento registrado com sucesso.";

        return RedirectToAction(nameof(Index), new { propertyId });
    }

    [HttpPost("{documentId:guid}/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        Guid propertyId,
        Guid documentId,
        PropertyDocumentReviewInputModel input,
        CancellationToken cancellationToken)
    {
        var documento = await _context.PropertyDocuments
            .Include(d => d.Imovel)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ImovelId == propertyId, cancellationToken);

        if (documento is null)
        {
            return NotFound();
        }

        if (documento.Status is DocumentStatus.Aprovado or DocumentStatus.Rejeitado or DocumentStatus.Obsoleto or DocumentStatus.Expirado)
        {
            TempData["Error"] = "Somente documentos pendentes podem ser revisados.";
            return RedirectToAction(nameof(Index), new { propertyId });
        }

        if (!input.Aprovar && string.IsNullOrWhiteSpace(input.Comentario))
        {
            TempData["Error"] = "Informe o motivo da reprovação.";
            return RedirectToAction(nameof(Index), new { propertyId });
        }

        var user = User?.Identity?.Name ?? "Sistema";
        var agora = DateTime.UtcNow;
        var antes = JsonSerializer.Serialize(documento);

        documento.Status = input.Aprovar ? DocumentStatus.Aprovado : DocumentStatus.Rejeitado;
        documento.RevisadoEm = agora;
        documento.RevisadoPor = user;
        documento.UpdatedAt = agora;
        documento.UpdatedBy = user;

        if (!string.IsNullOrWhiteSpace(input.Comentario))
        {
            documento.Observacoes = input.Comentario.Trim();
        }

        if (documento.Status == DocumentStatus.Aprovado && documento.ValidoAte.HasValue && documento.ValidoAte.Value < agora)
        {
            documento.Status = DocumentStatus.Expirado;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var depois = JsonSerializer.Serialize(documento);
        var operacao = input.Aprovar ? "APPROVE" : "REJECT";
        await RegistrarAuditoriaDocumentoAsync(documento, operacao, antes, depois, cancellationToken);

        _logger.LogInformation(
            "Documento {DocumentoId} revisado por {Usuario}. Status: {Status}",
            documento.Id,
            user,
            documento.Status);

        TempData["Success"] = input.Aprovar
            ? "Documento aprovado com sucesso."
            : "Documento rejeitado e solicitante notificado.";

        return RedirectToAction(nameof(Index), new { propertyId });
    }

    [HttpPost("{documentId:guid}/aceite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAceite(
        Guid propertyId,
        Guid documentId,
        PropertyDocumentAcceptanceInputModel input,
        CancellationToken cancellationToken)
    {
        var documento = await _context.PropertyDocuments
            .Include(d => d.Aceites)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ImovelId == propertyId, cancellationToken);

        if (documento is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var mensagens = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToArray();

            if (mensagens.Length > 0)
            {
                TempData["Error"] = string.Join(" ", mensagens);
            }
            return RedirectToAction(nameof(Index), new { propertyId });
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        var agora = DateTime.UtcNow;
        var antesDocumento = JsonSerializer.Serialize(documento);
        var aceite = new PropertyDocumentAcceptance
        {
            DocumentoId = documento.Id,
            Tipo = input.Tipo,
            Nome = input.Nome.Trim(),
            Cargo = input.Cargo?.Trim() ?? string.Empty,
            UsuarioSistema = usuario,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty,
            CreatedBy = usuario,
            RegistradoEm = agora
        };

        documento.Aceites.Add(aceite);
        documento.UpdatedAt = agora;
        documento.UpdatedBy = usuario;

        await _context.SaveChangesAsync(cancellationToken);

        var depoisDocumento = JsonSerializer.Serialize(documento);
        await RegistrarAuditoriaDocumentoAsync(documento, "ACCEPTANCE_REGISTER", antesDocumento, depoisDocumento, cancellationToken);
        await RegistrarAuditoriaAceiteAsync(aceite, "CREATE", cancellationToken);

        TempData["Success"] = "Aceite registrado com sucesso.";
        return RedirectToAction(nameof(Index), new { propertyId });
    }

    [HttpPost("modelos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarModelo(
        Guid propertyId,
        DocumentTemplateType tipo,
        CancellationToken cancellationToken)
    {
        var property = await LoadPropertyAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        var html = RenderTemplate(tipo, property);
        var nomeArquivo = tipo switch
        {
            DocumentTemplateType.ContratoLocacao => $"contrato-locacao-{property.CodigoInterno}-{DateTime.UtcNow:yyyyMMddHHmmss}.html",
            DocumentTemplateType.LaudoVistoria => $"laudo-vistoria-{property.CodigoInterno}-{DateTime.UtcNow:yyyyMMddHHmmss}.html",
            _ => $"documento-{property.CodigoInterno}-{DateTime.UtcNow:yyyyMMddHHmmss}.html"
        };

        return File(Encoding.UTF8.GetBytes(html), "text/html", nomeArquivo);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid propertyId, Guid documentId, CancellationToken cancellationToken)
    {
        var documento = await _context.PropertyDocuments
            .Include(d => d.Arquivo)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ImovelId == propertyId, cancellationToken);

        if (documento is null || documento.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(documento.Arquivo, cancellationToken);
        await RegistrarAuditoriaDocumentoAsync(documento, "DOWNLOAD", string.Empty, string.Empty, cancellationToken);
        _logger.LogInformation(
            "Arquivo {DocumentoId} do imóvel {ImovelId} enviado para download por {Usuario}",
            documento.Id,
            propertyId,
            User?.Identity?.Name ?? "Sistema");
        return File(stream, documento.Arquivo.ConteudoTipo, documento.Arquivo.NomeOriginal);
    }

    private async Task<Property?> LoadPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _context.Imoveis
            .Include(p => p.Proprietario)
            .Include(p => p.Documentos)
                .ThenInclude(d => d.Arquivo)
            .Include(p => p.Documentos)
                .ThenInclude(d => d.Aceites)
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Interessado)
            .Include(p => p.Vistorias)
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
    }

    private async Task NormalizeDocumentStatusesAsync(Property property, CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var alterado = false;

        foreach (var documento in property.Documentos)
        {
            if (documento.Status == DocumentStatus.Aprovado && documento.ValidoAte.HasValue && documento.ValidoAte.Value < agora)
            {
                documento.Status = DocumentStatus.Expirado;
                documento.UpdatedAt = agora;
                documento.UpdatedBy = User?.Identity?.Name ?? "Sistema";
                alterado = true;
            }
        }

        if (alterado)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private PropertyDocumentLibraryViewModel BuildLibraryViewModel(Property property, PropertyDocumentUploadInputModel? upload = null)
    {
        var agora = DateTime.UtcNow;

        var grupos = property.Documentos
            .OrderBy(d => d.Descricao)
            .ThenByDescending(d => d.Versao)
            .GroupBy(d => d.Descricao)
            .Select(grupo =>
            {
                var versoes = grupo
                    .OrderByDescending(d => d.Versao)
                    .Select(d => MapVersion(d, agora))
                    .ToList();

                return new PropertyDocumentGroupViewModel
                {
                    Descricao = grupo.Key,
                    VersaoAtual = versoes.FirstOrDefault(),
                    Historico = versoes.Skip(1).ToList()
                };
            })
            .OrderBy(g => g.Descricao)
            .ToList();

        var endereco = string.Join(" ", new[]
        {
            property.Endereco,
            string.IsNullOrWhiteSpace(property.Bairro) ? null : $"- {property.Bairro}",
            $"- {property.Cidade}/{property.Estado}"
        }.Where(p => !string.IsNullOrWhiteSpace(p)));

        var model = new PropertyDocumentLibraryViewModel
        {
            ImovelId = property.Id,
            CodigoInterno = property.CodigoInterno,
            Titulo = property.Titulo,
            Endereco = endereco,
            Grupos = grupos,
            Upload = upload is null
                ? new PropertyDocumentUploadInputModel()
                : new PropertyDocumentUploadInputModel
                {
                    Descricao = upload.Descricao,
                    Observacoes = upload.Observacoes,
                    ValidoAte = upload.ValidoAte,
                    RequerAceiteProprietario = upload.RequerAceiteProprietario,
                    AprovarAutomaticamente = upload.AprovarAutomaticamente
                },
            ModelosDisponiveis = GetTemplateOptions()
        };

        return model;
    }

    private static IReadOnlyCollection<DocumentTemplateOptionViewModel> GetTemplateOptions()
    {
        return new[]
        {
            new DocumentTemplateOptionViewModel
            {
                Valor = DocumentTemplateType.ContratoLocacao.ToString(),
                Descricao = "Contrato interno de locação"
            },
            new DocumentTemplateOptionViewModel
            {
                Valor = DocumentTemplateType.LaudoVistoria.ToString(),
                Descricao = "Laudo de vistoria"
            }
        };
    }

    private static PropertyDocumentVersionViewModel MapVersion(PropertyDocument documento, DateTime agora)
    {
        var expirado = documento.ValidoAte.HasValue && documento.ValidoAte.Value < agora;

        return new PropertyDocumentVersionViewModel
        {
            Id = documento.Id,
            Versao = documento.Versao,
            Status = expirado && documento.Status == DocumentStatus.Aprovado
                ? DocumentStatus.Expirado
                : documento.Status,
            CreatedAt = documento.CreatedAt,
            CreatedBy = documento.CreatedBy,
            ValidoAte = documento.ValidoAte,
            Expirado = expirado,
            RequerAceiteProprietario = documento.RequerAceiteProprietario,
            RevisadoEm = documento.RevisadoEm,
            RevisadoPor = documento.RevisadoPor,
            Observacoes = documento.Observacoes,
            Aceites = documento.Aceites
                .OrderBy(a => a.RegistradoEm)
                .Select(MapAcceptance)
                .ToList()
        };
    }

    private static PropertyDocumentAcceptanceViewModel MapAcceptance(PropertyDocumentAcceptance aceite)
    {
        return new PropertyDocumentAcceptanceViewModel
        {
            Id = aceite.Id,
            Tipo = aceite.Tipo,
            Nome = aceite.Nome,
            Cargo = aceite.Cargo,
            UsuarioSistema = aceite.UsuarioSistema,
            Ip = aceite.Ip,
            Host = aceite.Host,
            RegistradoEm = aceite.RegistradoEm
        };
    }

    private async Task RegistrarAuditoriaDocumentoAsync(PropertyDocument documento, string operacao, string antes, string depois, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            "PropertyDocument",
            documento.Id,
            operacao,
            antes ?? string.Empty,
            depois ?? string.Empty,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private async Task RegistrarAuditoriaAceiteAsync(PropertyDocumentAcceptance aceite, string operacao, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;
        var payload = JsonSerializer.Serialize(aceite);

        await _auditTrailService.RegisterAsync(
            "PropertyDocumentAcceptance",
            aceite.Id,
            operacao,
            string.Empty,
            payload,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private string RenderTemplate(DocumentTemplateType tipo, Property property)
    {
        return tipo switch
        {
            DocumentTemplateType.ContratoLocacao => ContractTemplateRenderer.Render(property),
            DocumentTemplateType.LaudoVistoria => RenderInspectionTemplate(property),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null)
        };
    }

    private static string RenderInspectionTemplate(Property property)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var vistoria = property.Vistorias
            .OrderByDescending(v => v.Inicio ?? v.AgendadaPara)
            .FirstOrDefault();

        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" /><title>Laudo de Vistoria</title></head><body>");
        sb.AppendLine($"<h1>Laudo de Vistoria - {WebUtility.HtmlEncode(property.CodigoInterno)}</h1>");
        sb.AppendLine($"<p>Gerado em {DateTime.UtcNow.ToLocalTime():dd/MM/yyyy HH:mm}</p>");
        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Dados do Imóvel</h2>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(property.Endereco)} - {WebUtility.HtmlEncode(property.Bairro)} - {WebUtility.HtmlEncode(property.Cidade)}/{WebUtility.HtmlEncode(property.Estado)}</p>");
        sb.AppendLine("</section>");

        if (vistoria is not null)
        {
            sb.AppendLine("<section>");
            sb.AppendLine("<h2>Informações da vistoria</h2>");
            sb.AppendLine($"<p><strong>Tipo:</strong> {WebUtility.HtmlEncode(vistoria.Tipo.ToString())}</p>");
            sb.AppendLine($"<p><strong>Status:</strong> {WebUtility.HtmlEncode(vistoria.Status.ToString())}</p>");
            sb.AppendLine($"<p><strong>Responsável:</strong> {WebUtility.HtmlEncode(vistoria.Responsavel)}</p>");
            sb.AppendLine($"<p><strong>Início:</strong> {(vistoria.Inicio?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", cultura) ?? "--")}</p>");
            sb.AppendLine($"<p><strong>Fim:</strong> {(vistoria.Fim?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", cultura) ?? "--")}</p>");
            sb.AppendLine("</section>");

            sb.AppendLine("<section>");
            sb.AppendLine("<h2>Checklist registrado</h2>");
            sb.AppendLine(RenderChecklist(vistoria.ChecklistJson));
            sb.AppendLine("</section>");

            if (!string.IsNullOrWhiteSpace(vistoria.Observacoes))
            {
                sb.AppendLine("<section>");
                sb.AppendLine("<h2>Observações</h2>");
                sb.AppendLine($"<p>{WebUtility.HtmlEncode(vistoria.Observacoes)}</p>");
                sb.AppendLine("</section>");
            }
        }
        else
        {
            sb.AppendLine("<section>");
            sb.AppendLine("<h2>Informações da vistoria</h2>");
            sb.AppendLine("<p>Nenhuma vistoria cadastrada. Preencha os dados manualmente.</p>");
            sb.AppendLine("</section>");
        }

        sb.AppendLine("<section>");
        sb.AppendLine("<h2>Aceite presencial</h2>");
        sb.AppendLine("<p>__________________________________________</p>");
        sb.AppendLine("<p>__________________________________________</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string RenderChecklist(string? checklistJson)
    {
        if (string.IsNullOrWhiteSpace(checklistJson))
        {
            return "<p>Checklist não informado.</p>";
        }

        try
        {
            using var document = JsonDocument.Parse(checklistJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var builder = new StringBuilder("<ul>");
                foreach (var item in document.RootElement.EnumerateObject())
                {
                    var value = item.Value.ValueKind switch
                    {
                        JsonValueKind.String => item.Value.GetString(),
                        JsonValueKind.Number => item.Value.ToString(),
                        JsonValueKind.True => "Sim",
                        JsonValueKind.False => "Não",
                        _ => item.Value.ToString()
                    };
                    builder.AppendLine($"<li><strong>{WebUtility.HtmlEncode(item.Name)}:</strong> {WebUtility.HtmlEncode(value ?? string.Empty)}</li>");
                }
                builder.AppendLine("</ul>");
                return builder.ToString();
            }
        }
        catch (JsonException)
        {
            // Ignora parsing e retorna conteúdo bruto abaixo.
        }

        return $"<pre>{WebUtility.HtmlEncode(checklistJson)}</pre>";
    }
}
