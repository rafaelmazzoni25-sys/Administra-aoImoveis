using System.Linq;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.GestaoContratos)]
public class ContratosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IContractService _contractService;
    private readonly IFileStorageService _fileStorageService;

    public ContratosController(
        ApplicationDbContext context,
        IContractService contractService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _contractService = contractService;
        _fileStorageService = fileStorageService;
    }

    public async Task<IActionResult> Index(bool incluirEncerrados = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Contratos
            .AsNoTracking()
            .Include(c => c.Imovel)
            .Include(c => c.Negociacao)
                .ThenInclude(n => n.Interessado)
            .AsQueryable();

        if (!incluirEncerrados)
        {
            query = query.Where(c => c.Ativo);
        }

        var contratos = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContractSummaryViewModel
            {
                Id = c.Id,
                PropertyId = c.ImovelId,
                PropertyCode = c.Imovel != null ? c.Imovel.CodigoInterno : string.Empty,
                PropertyTitle = c.Imovel != null ? c.Imovel.Titulo : string.Empty,
                Interessado = c.Negociacao != null && c.Negociacao.Interessado != null
                    ? c.Negociacao.Interessado.Nome
                    : string.Empty,
                Ativo = c.Ativo,
                DataInicio = c.DataInicio,
                DataFim = c.DataFim,
                ValorAluguel = c.ValorAluguel,
                Encargos = c.Encargos
            })
            .ToListAsync(cancellationToken);

        var model = new ContractListViewModel
        {
            IncluirEncerrados = incluirEncerrados,
            Contratos = contratos
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var property = await _context.Imoveis
            .AsNoTracking()
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Interessado)
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

        if (property is null)
        {
            return NotFound();
        }

        var negotiations = property.Negociacoes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NegotiationOptionViewModel
            {
                Id = n.Id,
                Interessado = n.Interessado?.Nome ?? "—",
                Etapa = n.Etapa,
                ValorProposta = n.ValorProposta,
                CriadaEm = n.CreatedAt
            })
            .ToList();

        if (negotiations.Count == 0)
        {
            TempData["Error"] = "Não há negociações disponíveis para gerar um contrato.";
            return RedirectToAction("Details", "Imoveis", new { id = propertyId });
        }

        var defaultNegotiation = negotiations.First();

        var model = new ContractGenerationViewModel
        {
            PropertyId = property.Id,
            PropertyCode = property.CodigoInterno,
            PropertyTitle = property.Titulo,
            Negotiations = negotiations,
            Input = new ContractGenerationInputModel
            {
                PropertyId = property.Id,
                NegotiationId = defaultNegotiation.Id,
                DataInicio = DateTime.UtcNow.Date,
                DataFim = DateTime.UtcNow.Date.AddYears(1),
                ValorAluguel = defaultNegotiation.ValorProposta ?? 0m,
                Encargos = 0m
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractGenerationInputModel input, CancellationToken cancellationToken = default)
    {
        var property = await _context.Imoveis
            .AsNoTracking()
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Interessado)
            .FirstOrDefaultAsync(p => p.Id == input.PropertyId, cancellationToken);

        if (property is null)
        {
            return NotFound();
        }

        var negotiations = property.Negociacoes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NegotiationOptionViewModel
            {
                Id = n.Id,
                Interessado = n.Interessado?.Nome ?? "—",
                Etapa = n.Etapa,
                ValorProposta = n.ValorProposta,
                CriadaEm = n.CreatedAt
            })
            .ToList();

        if (!ModelState.IsValid)
        {
            var invalidModel = new ContractGenerationViewModel
            {
                PropertyId = property.Id,
                PropertyCode = property.CodigoInterno,
                PropertyTitle = property.Titulo,
                Negotiations = negotiations,
                Input = input
            };

            return View(invalidModel);
        }

        if (negotiations.All(n => n.Id != input.NegotiationId))
        {
            ModelState.AddModelError(nameof(input.NegotiationId), "Negociação informada é inválida para este imóvel.");
            var invalidModel = new ContractGenerationViewModel
            {
                PropertyId = property.Id,
                PropertyCode = property.CodigoInterno,
                PropertyTitle = property.Titulo,
                Negotiations = negotiations,
                Input = input
            };
            return View(invalidModel);
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        var request = new ContractGenerationRequest(
            input.PropertyId,
            input.NegotiationId,
            input.DataInicio,
            input.DataFim,
            input.ValorAluguel,
            input.Encargos,
            usuario,
            ip,
            host);

        var resultado = await _contractService.GenerateAsync(request, cancellationToken);
        if (!resultado.Success)
        {
            ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "Falha ao gerar contrato.");
            var invalidModel = new ContractGenerationViewModel
            {
                PropertyId = property.Id,
                PropertyCode = property.CodigoInterno,
                PropertyTitle = property.Titulo,
                Negotiations = negotiations,
                Input = input
            };
            return View(invalidModel);
        }

        TempData["Success"] = "Contrato gerado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = resultado.Contract!.Id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var contrato = await _context.Contratos
            .AsNoTracking()
            .Include(c => c.Imovel)
            .Include(c => c.Negociacao)
                .ThenInclude(n => n.Interessado)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contrato is null)
        {
            return NotFound();
        }

        var documentos = await _context.PropertyDocuments
            .AsNoTracking()
            .Include(d => d.Arquivo)
            .Where(d => d.ImovelId == contrato.ImovelId && d.Descricao == ContractConstants.DocumentDescription)
            .OrderByDescending(d => d.Versao)
            .Select((d, index) => new ContractDocumentVersionViewModel
            {
                DocumentoId = d.Id,
                ArquivoId = d.ArquivoId,
                Versao = d.Versao,
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                ValidoAte = d.ValidoAte,
                Atual = index == 0,
                NomeArquivo = d.Arquivo != null ? d.Arquivo.NomeOriginal : "Contrato"
            })
            .ToListAsync(cancellationToken);

        var model = new ContractDetailViewModel
        {
            Id = contrato.Id,
            PropertyId = contrato.ImovelId,
            PropertyCode = contrato.Imovel?.CodigoInterno ?? string.Empty,
            PropertyTitle = contrato.Imovel?.Titulo ?? string.Empty,
            Interessado = contrato.Negociacao?.Interessado?.Nome ?? string.Empty,
            Ativo = contrato.Ativo,
            DataInicio = contrato.DataInicio,
            DataFim = contrato.DataFim,
            ValorAluguel = contrato.ValorAluguel,
            Encargos = contrato.Encargos,
            Documentos = documentos,
            Anexo = new ContractAttachmentInputModel
            {
                ContractId = contrato.Id
            },
            Encerramento = new ContractClosureInputModel
            {
                ContractId = contrato.Id,
                DataEncerramento = contrato.DataFim ?? DateTime.UtcNow.Date
            }
        };

        return View(model);
    }

    [HttpGet("{id:guid}/documentos/{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid id, Guid documentId, CancellationToken cancellationToken = default)
    {
        var contrato = await _context.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contrato is null)
        {
            return NotFound();
        }

        var documento = await _context.PropertyDocuments
            .Include(d => d.Arquivo)
            .FirstOrDefaultAsync(
                d => d.Id == documentId
                     && d.ImovelId == contrato.ImovelId
                     && d.Descricao == ContractConstants.DocumentDescription,
                cancellationToken);

        if (documento is null || documento.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(documento.Arquivo, cancellationToken);
        return File(stream, documento.Arquivo.ConteudoTipo, documento.Arquivo.NomeOriginal);
    }

    [HttpPost("{id:guid}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        var resultado = await _contractService.ActivateAsync(
            new ContractActivationRequest(id, usuario, ip, host),
            cancellationToken);

        if (!resultado.Success)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "Não foi possível ativar o contrato.";
        }
        else
        {
            TempData["Success"] = "Contrato ativado com sucesso.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/anexos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anexar(Guid id, ContractAttachmentInputModel input, CancellationToken cancellationToken = default)
    {
        if (input.Arquivo is null || input.Arquivo.Length == 0)
        {
            TempData["Error"] = "Selecione um arquivo válido.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await using var stream = input.Arquivo.OpenReadStream();
        var request = new ContractAttachmentRequest(
            id,
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            stream,
            usuario,
            ip,
            host);

        var resultado = await _contractService.AttachDocumentAsync(request, cancellationToken);
        if (!resultado.Success)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "Não foi possível anexar o documento.";
        }
        else
        {
            TempData["Success"] = "Documento anexado ao contrato.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/encerrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Encerrar(Guid id, ContractClosureInputModel input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Informe a data de encerramento.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        var request = new ContractClosureRequest(
            id,
            input.DataEncerramento,
            input.Observacoes,
            usuario,
            ip,
            host);

        var resultado = await _contractService.CloseAsync(request, cancellationToken);
        if (!resultado.Success)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "Não foi possível encerrar o contrato.";
        }
        else
        {
            TempData["Success"] = "Contrato encerrado com sucesso.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
