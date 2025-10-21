using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Imoveis => Set<Property>();
    public DbSet<PropertyHistoryEvent> PropertyHistory => Set<PropertyHistoryEvent>();
    public DbSet<Owner> Proprietarios => Set<Owner>();
    public DbSet<InterestedParty> Interessados => Set<InterestedParty>();
    public DbSet<Negotiation> Negociacoes => Set<Negotiation>();
    public DbSet<NegotiationEvent> NegociacaoEventos => Set<NegotiationEvent>();
    public DbSet<Inspection> Vistorias => Set<Inspection>();
    public DbSet<Activity> Atividades => Set<Activity>();
    public DbSet<ActivityComment> AtividadeComentarios => Set<ActivityComment>();
    public DbSet<ActivityAttachment> AtividadeAnexos => Set<ActivityAttachment>();
    public DbSet<Contract> Contratos => Set<Contract>();
    public DbSet<StoredFile> Arquivos => Set<StoredFile>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<PropertyDocumentAcceptance> DocumentAcceptances => Set<PropertyDocumentAcceptance>();
    public DbSet<NegotiationDocument> NegotiationDocuments => Set<NegotiationDocument>();
    public DbSet<InspectionDocument> InspectionDocuments => Set<InspectionDocument>();
    public DbSet<FinancialTransaction> LancamentosFinanceiros => Set<FinancialTransaction>();
    public DbSet<MaintenanceOrder> Manutencoes => Set<MaintenanceOrder>();
    public DbSet<MaintenanceOrderDocument> MaintenanceDocuments => Set<MaintenanceOrderDocument>();
    public DbSet<InAppNotification> Notificacoes => Set<InAppNotification>();
    public DbSet<AuditLogEntry> AuditTrail => Set<AuditLogEntry>();
    public DbSet<ScheduleEntry> Agenda => Set<ScheduleEntry>();
    public DbSet<ContextMessage> Mensagens => Set<ContextMessage>();
    public DbSet<ContextMessageMention> MensagensMencoes => Set<ContextMessageMention>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>()
            .HasIndex(p => p.CodigoInterno)
            .IsUnique();

        builder.Entity<Property>()
            .HasMany(p => p.Negociacoes)
            .WithOne(n => n.Imovel)
            .HasForeignKey(n => n.ImovelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Property>()
            .HasMany(p => p.Vistorias)
            .WithOne(v => v.Imovel)
            .HasForeignKey(v => v.ImovelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Property>()
            .HasMany(p => p.Documentos)
            .WithOne(d => d.Imovel)
            .HasForeignKey(d => d.ImovelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PropertyDocument>()
            .HasMany(d => d.Aceites)
            .WithOne(a => a.Documento)
            .HasForeignKey(a => a.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Owner>()
            .HasMany(o => o.Imoveis)
            .WithOne(p => p.Proprietario)
            .HasForeignKey(p => p.ProprietarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InterestedParty>()
            .HasMany(i => i.Negociacoes)
            .WithOne(n => n.Interessado)
            .HasForeignKey(n => n.InteressadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Activity>()
            .HasMany(a => a.Comentarios)
            .WithOne(c => c.Atividade)
            .HasForeignKey(c => c.AtividadeId);

        builder.Entity<Activity>()
            .HasMany(a => a.Anexos)
            .WithOne(a => a.Atividade)
            .HasForeignKey(a => a.AtividadeId);

        builder.Entity<StoredFile>()
            .Property(f => f.CaminhoRelativo)
            .HasMaxLength(512);

        builder.Entity<InAppNotification>()
            .HasIndex(n => new { n.UsuarioId, n.Lida });

        builder.Entity<AuditLogEntry>()
            .HasIndex(a => new { a.Entidade, a.EntidadeId, a.RegistradoEm });

        builder.Entity<ScheduleEntry>()
            .HasIndex(a => new { a.Inicio, a.Fim });

        builder.Entity<MaintenanceOrder>()
            .HasOne(m => m.Vistoria)
            .WithMany(v => v.OrdensManutencao)
            .HasForeignKey(m => m.VistoriaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
