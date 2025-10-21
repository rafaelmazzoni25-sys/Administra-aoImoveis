using System.Text.Json;
using System.Text.RegularExpressions;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.Operacional)]
public class ComunicacoesController : Controller
{
    private static readonly Regex MentionRegex = new("@([\\w\\.\\-]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditTrailService _auditTrailService;

    public ComunicacoesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _userManager = userManager;
        _auditTrailService = auditTrailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ActivityLinkType? contextoTipo,
        [FromQuery] Guid? contextoId,
        CancellationToken cancellationToken)
    {
        var recentes = await CarregarContextosRecentesAsync(cancellationToken);

        if (!contextoTipo.HasValue || !contextoId.HasValue)
        {
            return View(new ContextConversationViewModel
            {
                Recentes = recentes
            });
        }

        var contexto = await BuildContextSummaryAsync(contextoTipo.Value, contextoId.Value, cancellationToken);
        if (contexto is null)
        {
            return NotFound();
        }

        var mensagens = await _context.Mensagens
            .AsNoTracking()
            .Where(m => m.ContextoTipo == contextoTipo.Value && m.ContextoId == contextoId.Value)
            .Include(m => m.Mentions)
            .OrderBy(m => m.EnviadaEm)
            .ToListAsync(cancellationToken);

        var userIds = mensagens
            .Select(m => m.UsuarioId)
            .Concat(mensagens.SelectMany(m => m.Mentions.Select(mm => mm.UsuarioMencionadoId)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var usuarios = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Nome = string.IsNullOrWhiteSpace(u.NomeCompleto)
                    ? u.UserName ?? u.Email ?? "Usuário"
                    : u.NomeCompleto
            })
            .ToListAsync(cancellationToken);

        var lookup = usuarios.ToDictionary(u => u.Id, u => u.Nome, StringComparer.OrdinalIgnoreCase);
        var usuarioAtualId = _userManager.GetUserId(User) ?? string.Empty;

        var itens = mensagens
            .Select(m => new ContextMessageItemViewModel
            {
                Id = m.Id,
                Autor = lookup.TryGetValue(m.UsuarioId, out var autor) ? autor : "Usuário",
                Mensagem = m.Mensagem,
                EnviadaEm = m.EnviadaEm,
                Mentions = m.Mentions
                    .Select(mm => lookup.TryGetValue(mm.UsuarioMencionadoId, out var mention) ? mention : "Usuário")
                    .ToArray(),
                DoUsuarioAtual = string.Equals(m.UsuarioId, usuarioAtualId, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        var model = new ContextConversationViewModel
        {
            Contexto = contexto,
            Mensagens = itens,
            NovaMensagem = new ContextMessageInputModel
            {
                ContextoId = contextoId.Value,
                ContextoTipo = contextoTipo.Value
            },
            Recentes = recentes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostMessage(ContextMessageInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await Index(input.ContextoTipo, input.ContextoId, cancellationToken);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var contexto = await BuildContextSummaryAsync(input.ContextoTipo, input.ContextoId, cancellationToken);
        if (contexto is null)
        {
            return NotFound();
        }

        var usuario = await _userManager.GetUserAsync(User);
        var autor = string.IsNullOrWhiteSpace(usuario?.NomeCompleto)
            ? usuario?.UserName ?? usuario?.Email ?? "Usuário"
            : usuario!.NomeCompleto;

        var mensagem = new ContextMessage
        {
            ContextoTipo = input.ContextoTipo,
            ContextoId = input.ContextoId,
            UsuarioId = userId,
            Mensagem = input.Mensagem.Trim(),
            EnviadaEm = DateTime.UtcNow,
            CreatedBy = autor,
            UpdatedBy = autor,
            UpdatedAt = DateTime.UtcNow
        };

        var mencionados = await ResolverMencoesAsync(input.Mensagem, userId, cancellationToken);
        var mentions = mencionados
            .Select(u => new ContextMessageMention
            {
                MensagemId = mensagem.Id,
                UsuarioMencionadoId = u.Id,
                Notificado = true,
                CreatedBy = autor
            })
            .ToList();

        _context.Mensagens.Add(mensagem);
        if (mentions.Count > 0)
        {
            _context.MensagensMencoes.AddRange(mentions);
            foreach (var usuarioMencionado in mencionados)
            {
                var notificacao = new InAppNotification
                {
                    UsuarioId = usuarioMencionado.Id,
                    Titulo = "Você foi mencionado",
                    Mensagem = $"{autor} mencionou você em {contexto.Titulo}",
                    LinkDestino = Url.Action(nameof(Index), new
                    {
                        contextoTipo = input.ContextoTipo,
                        contextoId = input.ContextoId
                    }),
                    Lida = false,
                    CreatedBy = autor
                };

                _context.Notificacoes.Add(notificacao);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(mensagem, cancellationToken);

        TempData["Success"] = "Mensagem registrada.";
        return RedirectToAction(nameof(Index), new
        {
            contextoTipo = input.ContextoTipo,
            contextoId = input.ContextoId
        });
    }

    private async Task RegistrarAuditoriaAsync(ContextMessage mensagem, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            "ContextMessage",
            mensagem.Id,
            "CREATE",
            string.Empty,
            JsonSerializer.Serialize(mensagem),
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<ApplicationUser>> ResolverMencoesAsync(
        string mensagem,
        string autorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            return Array.Empty<ApplicationUser>();
        }

        var usernames = MentionRegex
            .Matches(mensagem)
            .Select(m => m.Groups[1].Value)
            .Where(nome => !string.IsNullOrWhiteSpace(nome))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (usernames.Length == 0)
        {
            return Array.Empty<ApplicationUser>();
        }

        var mencionados = new List<ApplicationUser>();
        foreach (var username in usernames)
        {
            var usuario = await _userManager.FindByNameAsync(username);
            if (usuario is null || string.Equals(usuario.Id, autorId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            mencionados.Add(usuario);
        }

        return mencionados;
    }

    private async Task<IReadOnlyCollection<ContextSummaryViewModel>> CarregarContextosRecentesAsync(CancellationToken cancellationToken)
    {
        var recentes = await _context.Mensagens
            .AsNoTracking()
            .OrderByDescending(m => m.EnviadaEm)
            .Take(50)
            .Select(m => new { m.ContextoTipo, m.ContextoId })
            .ToListAsync(cancellationToken);

        var distintos = recentes
            .DistinctBy(m => new { m.ContextoTipo, m.ContextoId })
            .Take(8)
            .ToList();

        var resultados = new List<ContextSummaryViewModel>();
        foreach (var item in distintos)
        {
            var resumo = await BuildContextSummaryAsync(item.ContextoTipo, item.ContextoId, cancellationToken);
            if (resumo is not null)
            {
                resultados.Add(resumo);
            }
        }

        return resultados;
    }

    private async Task<ContextSummaryViewModel?> BuildContextSummaryAsync(
        ActivityLinkType contextoTipo,
        Guid contextoId,
        CancellationToken cancellationToken)
    {
        switch (contextoTipo)
        {
            case ActivityLinkType.Imovel:
                {
                    var property = await _context.Imoveis
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == contextoId, cancellationToken);
                    if (property is null)
                    {
                        return null;
                    }

                    var descricao = string.Join(" ", new[]
                    {
                        property.Endereco,
                        string.IsNullOrWhiteSpace(property.Bairro) ? null : property.Bairro,
                        string.IsNullOrWhiteSpace(property.Cidade) ? null : property.Cidade
                    }.Where(p => !string.IsNullOrWhiteSpace(p)));

                    return new ContextSummaryViewModel
                    {
                        ContextoTipo = contextoTipo,
                        ContextoId = contextoId,
                        Titulo = $"Imóvel {property.CodigoInterno}",
                        Descricao = descricao,
                        LinkDestino = Url.Action("Details", "Imoveis", new { id = contextoId })
                    };
                }
            case ActivityLinkType.Negociacao:
                {
                    var negotiation = await _context.Negociacoes
                        .AsNoTracking()
                        .Include(n => n.Imovel)
                        .FirstOrDefaultAsync(n => n.Id == contextoId, cancellationToken);
                    if (negotiation is null)
                    {
                        return null;
                    }

                    var descricao = negotiation.Imovel is null
                        ? string.Empty
                        : $"{negotiation.Imovel.CodigoInterno} - {negotiation.Imovel.Titulo}";

                    return new ContextSummaryViewModel
                    {
                        ContextoTipo = contextoTipo,
                        ContextoId = contextoId,
                        Titulo = $"Negociação ({negotiation.Etapa})",
                        Descricao = descricao,
                        LinkDestino = Url.Action("Index", "Negociacoes")
                    };
                }
            case ActivityLinkType.Vistoria:
                {
                    var vistoria = await _context.Vistorias
                        .AsNoTracking()
                        .Include(v => v.Imovel)
                        .FirstOrDefaultAsync(v => v.Id == contextoId, cancellationToken);
                    if (vistoria is null)
                    {
                        return null;
                    }

                    var descricao = vistoria.Imovel is null
                        ? string.Empty
                        : $"{vistoria.Imovel.CodigoInterno} - {vistoria.Imovel.Titulo}";

                    return new ContextSummaryViewModel
                    {
                        ContextoTipo = contextoTipo,
                        ContextoId = contextoId,
                        Titulo = $"Vistoria {vistoria.Tipo}",
                        Descricao = descricao,
                        LinkDestino = Url.Action("Details", "Vistorias", new { id = contextoId })
                    };
                }
            case ActivityLinkType.Contrato:
                {
                    var contrato = await _context.Contratos
                        .AsNoTracking()
                        .Include(c => c.Imovel)
                        .FirstOrDefaultAsync(c => c.Id == contextoId, cancellationToken);
                    if (contrato is null)
                    {
                        return null;
                    }

                    var descricao = contrato.Imovel is null
                        ? string.Empty
                        : $"{contrato.Imovel.CodigoInterno} - {contrato.Imovel.Titulo}";

                    return new ContextSummaryViewModel
                    {
                        ContextoTipo = contextoTipo,
                        ContextoId = contextoId,
                        Titulo = contrato.Ativo ? "Contrato ativo" : "Contrato encerrado",
                        Descricao = descricao,
                        LinkDestino = Url.Action("Details", "Contratos", new { id = contrato.Id })
                    };
                }
            case ActivityLinkType.Atividade:
                {
                    var atividade = await _context.Atividades
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == contextoId, cancellationToken);
                    if (atividade is null)
                    {
                        return null;
                    }

                    return new ContextSummaryViewModel
                    {
                        ContextoTipo = contextoTipo,
                        ContextoId = contextoId,
                        Titulo = $"Atividade: {atividade.Titulo}",
                        Descricao = $"Status {atividade.Status} | Prioridade {atividade.Prioridade}",
                        LinkDestino = Url.Action("Details", "Atividades", new { id = contextoId })
                    };
                }
            default:
                return null;
        }
    }
}
