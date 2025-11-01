using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenHtmlToPdf;

namespace AdministraAoImoveis.Web.Services.Contracts;

public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ILogger<ContractService> _logger;

    public ContractService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IAuditTrailService auditTrailService,
        ILogger<ContractService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditTrailService = auditTrailService;
        _logger = logger;
    }

    public async Task<ContractOperationResult> GenerateAsync(ContractGenerationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var property = await _context.Imoveis
            .Include(p => p.Proprietario)
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Interessado)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (property is null)
        {
            return ContractOperationResult.Failure("Imóvel não encontrado.");
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Interessado)
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId && n.ImovelId == request.PropertyId, cancellationToken);

        if (negotiation is null)
        {
            return ContractOperationResult.Failure("Negociação não encontrada para o imóvel informado.");
        }

        var templateData = new ContractTemplateData(
            request.ValorAluguel,
            negotiation.ValorSinal,
            request.Encargos,
            request.DataInicio,
            request.DataFim);

        var html = ContractTemplateRenderer.Render(property, negotiation, templateData);
        var baseFileName = $"contrato-{property.CodigoInterno}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        StoredFile? storedFile = null;

        try
        {
            var pdfBytes = OpenHtmlToPdf.Pdf
                .From(html)
                .Content();

            if (pdfBytes.Length > 0)
            {
                await using var pdfStream = new MemoryStream(pdfBytes);
                storedFile = await _fileStorageService.SaveAsync(
                    $"{baseFileName}.pdf",
                    "application/pdf",
                    pdfStream,
                    ContractConstants.StorageCategory,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao gerar ou salvar PDF do contrato para o imóvel {PropertyId}. Arquivo HTML será utilizado.",
                property.Id);
        }

        if (storedFile is null)
        {
            try
            {
                await using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(html));
                storedFile = await _fileStorageService.SaveAsync(
                    $"{baseFileName}.html",
                    "text/html",
                    htmlStream,
                    ContractConstants.StorageCategory,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao salvar o arquivo temporário do contrato para o imóvel {PropertyId}",
                    property.Id);
                return ContractOperationResult.Failure("Não foi possível gerar o arquivo do contrato.");
            }
        }

        storedFile.CreatedBy = request.Usuario;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.Arquivos.Add(storedFile);

            var documentosExistentes = await _context.PropertyDocuments
                .Where(d => d.ImovelId == property.Id && d.Descricao == ContractConstants.DocumentDescription)
                .OrderByDescending(d => d.Versao)
                .ToListAsync(cancellationToken);

            var versao = documentosExistentes.Count == 0
                ? 1
                : documentosExistentes.Max(d => d.Versao) + 1;

            var documento = new PropertyDocument
            {
                ImovelId = property.Id,
                ArquivoId = storedFile.Id,
                Descricao = ContractConstants.DocumentDescription,
                Versao = versao,
                Status = DocumentStatus.Pendente,
                ValidoAte = request.DataFim,
                RequerAceiteProprietario = true,
                CreatedBy = request.Usuario
            };

            _context.PropertyDocuments.Add(documento);

            var contrato = new Contract
            {
                ImovelId = property.Id,
                NegociacaoId = negotiation.Id,
                DataInicio = request.DataInicio,
                DataFim = request.DataFim,
                ValorAluguel = request.ValorAluguel,
                Encargos = request.Encargos,
                Ativo = false,
                DocumentoContratoId = storedFile.Id,
                CreatedBy = request.Usuario
            };

            _context.Contratos.Add(contrato);

            property.UpdatedAt = DateTime.UtcNow;
            property.UpdatedBy = request.Usuario;

            negotiation.UpdatedAt = DateTime.UtcNow;
            if (negotiation.Etapa < NegotiationStage.ContratoEmitido)
            {
                negotiation.Etapa = NegotiationStage.ContratoEmitido;
            }

            var historico = new PropertyHistoryEvent
            {
                ImovelId = property.Id,
                Titulo = "Geração de contrato",
                Descricao = $"Contrato criado a partir da negociação {negotiation.Id}",
                Usuario = request.Usuario,
                CreatedBy = request.Usuario,
                OcorreuEm = DateTime.UtcNow
            };

            _context.PropertyHistory.Add(historico);

            await _context.SaveChangesAsync(cancellationToken);

            await _auditTrailService.RegisterAsync(
                "Contract",
                contrato.Id,
                "GENERATE",
                string.Empty,
                JsonSerializer.Serialize(contrato),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await _auditTrailService.RegisterAsync(
                "PropertyDocument",
                documento.Id,
                "CONTRACT_TEMPLATE",
                string.Empty,
                JsonSerializer.Serialize(documento),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Contrato {ContractId} gerado para o imóvel {PropertyId}", contrato.Id, property.Id);

            return ContractOperationResult.SuccessResult(contrato, documento);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao registrar contrato para o imóvel {PropertyId}", property.Id);

            if (storedFile is not null)
            {
                await _fileStorageService.DeleteAsync(storedFile, CancellationToken.None);
            }

            return ContractOperationResult.Failure("Erro ao salvar o contrato. Consulte os logs para mais detalhes.");
        }
    }

    public async Task<ContractOperationResult> ActivateAsync(ContractActivationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contrato = await _context.Contratos
            .Include(c => c.Imovel)
            .Include(c => c.Negociacao)
            .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

        if (contrato is null)
        {
            return ContractOperationResult.Failure("Contrato não encontrado.");
        }

        if (contrato.Ativo)
        {
            return ContractOperationResult.Failure("O contrato já está ativo.");
        }

        if (contrato.Imovel is null)
        {
            return ContractOperationResult.Failure("Imóvel associado ao contrato não foi encontrado.");
        }

        var documento = await _context.PropertyDocuments
            .Where(d => d.ImovelId == contrato.ImovelId && d.Descricao == ContractConstants.DocumentDescription)
            .OrderByDescending(d => d.Versao)
            .FirstOrDefaultAsync(cancellationToken);

        var antes = JsonSerializer.Serialize(contrato);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            contrato.Ativo = true;
            contrato.UpdatedAt = DateTime.UtcNow;
            contrato.UpdatedBy = request.Usuario;

            var imovel = contrato.Imovel;
            imovel.ContratoAtivoId = contrato.Id;
            imovel.StatusDisponibilidade = AvailabilityStatus.Ocupado;
            imovel.DataPrevistaDisponibilidade = contrato.DataFim;
            imovel.UpdatedAt = DateTime.UtcNow;
            imovel.UpdatedBy = request.Usuario;

            if (documento is not null)
            {
                documento.Status = DocumentStatus.Aprovado;
                documento.ValidoAte = contrato.DataFim;
                documento.UpdatedAt = DateTime.UtcNow;
                documento.UpdatedBy = request.Usuario;
            }

            if (contrato.Negociacao is not null)
            {
                contrato.Negociacao.Ativa = false;
                contrato.Negociacao.Etapa = NegotiationStage.Assinatura;
                contrato.Negociacao.UpdatedAt = DateTime.UtcNow;
            }

            var historico = new PropertyHistoryEvent
            {
                ImovelId = imovel.Id,
                Titulo = "Contrato ativado",
                Descricao = $"Contrato iniciado em {contrato.DataInicio:dd/MM/yyyy}.",
                Usuario = request.Usuario,
                CreatedBy = request.Usuario,
                OcorreuEm = DateTime.UtcNow
            };

            _context.PropertyHistory.Add(historico);

            await _context.SaveChangesAsync(cancellationToken);

            await _auditTrailService.RegisterAsync(
                "Contract",
                contrato.Id,
                "ACTIVATE",
                antes,
                JsonSerializer.Serialize(contrato),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Contrato {ContractId} ativado", contrato.Id);

            return ContractOperationResult.SuccessResult(contrato, documento);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao ativar contrato {ContractId}", contrato.Id);
            return ContractOperationResult.Failure("Erro ao ativar o contrato. Consulte os logs para mais detalhes.");
        }
    }

    public async Task<ContractOperationResult> AttachDocumentAsync(ContractAttachmentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contrato = await _context.Contratos
            .Include(c => c.Imovel)
            .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

        if (contrato is null)
        {
            return ContractOperationResult.Failure("Contrato não encontrado.");
        }

        if (contrato.Imovel is null)
        {
            return ContractOperationResult.Failure("Imóvel associado ao contrato não foi encontrado.");
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        StoredFile? storedFile = null;
        try
        {
            storedFile = await _fileStorageService.SaveAsync(
                request.FileName,
                request.ContentType,
                request.Content,
                ContractConstants.StorageCategory,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao salvar o anexo do contrato {ContractId}", contrato.Id);
            return ContractOperationResult.Failure("Não foi possível armazenar o arquivo anexado.");
        }

        storedFile.CreatedBy = request.Usuario;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.Arquivos.Add(storedFile);

            var documentos = await _context.PropertyDocuments
                .Where(d => d.ImovelId == contrato.ImovelId && d.Descricao == ContractConstants.DocumentDescription)
                .OrderByDescending(d => d.Versao)
                .ToListAsync(cancellationToken);

            foreach (var doc in documentos.Where(d => d.Status == DocumentStatus.Aprovado))
            {
                doc.Status = DocumentStatus.Obsoleto;
                doc.UpdatedAt = DateTime.UtcNow;
                doc.UpdatedBy = request.Usuario;
            }

            var versao = documentos.Count == 0
                ? 1
                : documentos.Max(d => d.Versao) + 1;

            var documento = new PropertyDocument
            {
                ImovelId = contrato.ImovelId,
                ArquivoId = storedFile.Id,
                Descricao = ContractConstants.DocumentDescription,
                Versao = versao,
                Status = DocumentStatus.Aprovado,
                ValidoAte = contrato.DataFim,
                RequerAceiteProprietario = false,
                CreatedBy = request.Usuario
            };

            _context.PropertyDocuments.Add(documento);

            var antes = JsonSerializer.Serialize(contrato);
            contrato.DocumentoContratoId = storedFile.Id;
            contrato.UpdatedAt = DateTime.UtcNow;
            contrato.UpdatedBy = request.Usuario;

            var historico = new PropertyHistoryEvent
            {
                ImovelId = contrato.ImovelId,
                Titulo = "Contrato anexado",
                Descricao = "Nova versão do contrato anexada ao cadastro do imóvel.",
                Usuario = request.Usuario,
                CreatedBy = request.Usuario,
                OcorreuEm = DateTime.UtcNow
            };

            _context.PropertyHistory.Add(historico);

            await _context.SaveChangesAsync(cancellationToken);

            await _auditTrailService.RegisterAsync(
                "Contract",
                contrato.Id,
                "ATTACH_DOCUMENT",
                antes,
                JsonSerializer.Serialize(contrato),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await _auditTrailService.RegisterAsync(
                "PropertyDocument",
                documento.Id,
                "UPLOAD",
                string.Empty,
                JsonSerializer.Serialize(documento),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Anexo salvo para o contrato {ContractId}", contrato.Id);

            return ContractOperationResult.SuccessResult(contrato, documento);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao anexar arquivo ao contrato {ContractId}", contrato.Id);

            if (storedFile is not null)
            {
                await _fileStorageService.DeleteAsync(storedFile, CancellationToken.None);
            }

            return ContractOperationResult.Failure("Erro ao anexar arquivo ao contrato.");
        }
    }

    public async Task<ContractOperationResult> CloseAsync(ContractClosureRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contrato = await _context.Contratos
            .Include(c => c.Imovel)
            .Include(c => c.Negociacao)
            .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

        if (contrato is null)
        {
            return ContractOperationResult.Failure("Contrato não encontrado.");
        }

        if (!contrato.Ativo && contrato.DataFim.HasValue && contrato.DataFim.Value <= request.DataEncerramento)
        {
            return ContractOperationResult.Failure("O contrato já está encerrado para a data informada.");
        }

        if (contrato.Imovel is null)
        {
            return ContractOperationResult.Failure("Imóvel associado ao contrato não foi encontrado.");
        }

        var documento = await _context.PropertyDocuments
            .Where(d => d.ImovelId == contrato.ImovelId && d.Descricao == ContractConstants.DocumentDescription)
            .OrderByDescending(d => d.Versao)
            .FirstOrDefaultAsync(cancellationToken);

        var antes = JsonSerializer.Serialize(contrato);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            contrato.Ativo = false;
            contrato.DataFim = request.DataEncerramento;
            contrato.UpdatedAt = DateTime.UtcNow;
            contrato.UpdatedBy = request.Usuario;

            var imovel = contrato.Imovel;
            if (imovel.ContratoAtivoId == contrato.Id)
            {
                imovel.ContratoAtivoId = null;
                imovel.StatusDisponibilidade = AvailabilityStatus.Disponivel;
                imovel.DataPrevistaDisponibilidade = null;
                imovel.UpdatedAt = DateTime.UtcNow;
                imovel.UpdatedBy = request.Usuario;
            }

            if (documento is not null)
            {
                documento.Status = DocumentStatus.Expirado;
                documento.ValidoAte = request.DataEncerramento;
                documento.UpdatedAt = DateTime.UtcNow;
                documento.UpdatedBy = request.Usuario;
            }

            if (contrato.Negociacao is not null)
            {
                contrato.Negociacao.Ativa = false;
                contrato.Negociacao.Etapa = NegotiationStage.EntregaDeChaves;
                contrato.Negociacao.UpdatedAt = DateTime.UtcNow;
            }

            var descricaoHistorico = string.IsNullOrWhiteSpace(request.Observacoes)
                ? "Contrato encerrado."
                : $"Contrato encerrado. Observações: {request.Observacoes}";

            var historico = new PropertyHistoryEvent
            {
                ImovelId = contrato.ImovelId,
                Titulo = "Contrato encerrado",
                Descricao = descricaoHistorico,
                Usuario = request.Usuario,
                CreatedBy = request.Usuario,
                OcorreuEm = DateTime.UtcNow
            };

            _context.PropertyHistory.Add(historico);

            await _context.SaveChangesAsync(cancellationToken);

            await _auditTrailService.RegisterAsync(
                "Contract",
                contrato.Id,
                "CLOSE",
                antes,
                JsonSerializer.Serialize(contrato),
                request.Usuario,
                request.Ip,
                request.Host,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Contrato {ContractId} encerrado", contrato.Id);

            return ContractOperationResult.SuccessResult(contrato, documento);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao encerrar contrato {ContractId}", contrato.Id);
            return ContractOperationResult.Failure("Erro ao encerrar o contrato. Consulte os logs para mais detalhes.");
        }
    }
}
