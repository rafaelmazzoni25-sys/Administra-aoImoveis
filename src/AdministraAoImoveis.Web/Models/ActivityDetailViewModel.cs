using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Http;

namespace AdministraAoImoveis.Web.Models;

public class ActivityDetailViewModel
{
    public Activity Activity { get; init; } = null!;
    public IReadOnlyCollection<ActivityComment> Comentarios { get; init; } = Array.Empty<ActivityComment>();
    public IReadOnlyCollection<ActivityAttachment> Anexos { get; init; } = Array.Empty<ActivityAttachment>();
    public bool EstaAtrasada { get; init; }
    public bool EmRisco { get; init; }
    public TimeSpan? TempoRestante { get; init; }
    public double PercentualSlaConsumido { get; init; }
    public ActivityUpdateInputModel Atualizacao { get; init; } = new();
}

public class ActivityUpdateInputModel
{
    [Required]
    public ActivityStatus Status { get; set; }

    [Required]
    public PriorityLevel Prioridade { get; set; }

    [Required(ErrorMessage = "Informe o responsável pela atividade.")]
    [StringLength(128)]
    public string? Responsavel { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? DataLimite { get; set; }
}

public class ActivityCommentInputModel
{
    [Required(ErrorMessage = "Descreva o comentário.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "O comentário deve ter entre 3 e 2000 caracteres.")]
    public string Texto { get; set; } = string.Empty;
}

public class ActivityAttachmentInputModel
{
    [Required(ErrorMessage = "Selecione um arquivo para enviar.")]
    public IFormFile? Arquivo { get; set; }
}
