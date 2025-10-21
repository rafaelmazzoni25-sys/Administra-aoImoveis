using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
[Route("Imoveis/{propertyId:guid}/Documentos")]
public class DocumentosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DocumentosController> _logger;

    private const string StorageCategory = "property-documents";

    public DocumentosController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<DocumentosController> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
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
            .Include(p => p.Documentos)
                .ThenInclude(d => d.Arquivo)
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
                }
        };

        return model;
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
            Observacoes = documento.Observacoes
        };
    }
}
