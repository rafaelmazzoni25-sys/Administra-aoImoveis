using System;
using AdministraAoImoveis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace AdministraAoImoveis.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Activity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("DataLimite")
                    .HasColumnType("datetime2");

                b.Property<string>("Descricao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Prioridade")
                    .HasColumnType("int");

                b.Property<string>("Responsavel")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<string>("Setor")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("VinculoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("VinculoTipo")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.ToTable("Atividades");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ActivityAttachment", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ArquivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("AtividadeId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("ArquivoId");

                b.HasIndex("AtividadeId");

                b.ToTable("AtividadeAnexos");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ActivityComment", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("AtividadeId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Autor")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("ComentadoEm")
                    .HasColumnType("datetime2");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Texto")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("AtividadeId");

                b.ToTable("AtividadeComentarios");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.AuditLogEntry", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Antes")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Depois")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Entidade")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("EntidadeId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Host")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Ip")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("RegistradoEm")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Usuario")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("Entidade", "EntidadeId", "RegistradoEm");

                b.ToTable("AuditTrail");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ContextMessage", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("ContextoTipo")
                    .HasColumnType("int");

                b.Property<Guid>("ContextoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime>("EnviadaEm")
                    .HasColumnType("datetime2");

                b.Property<string>("Mensagem")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UsuarioId")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.ToTable("Mensagens");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ContextMessageMention", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("MensagemId")
                    .HasColumnType("uniqueidentifier");

                b.Property<bool>("Notificado")
                    .HasColumnType("bit");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UsuarioMencionadoId")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("MensagemId");

                b.ToTable("MensagensMencoes");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Contract", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<bool>("Ativo")
                    .HasColumnType("bit");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("DataFim")
                    .HasColumnType("datetime2");

                b.Property<DateTime>("DataInicio")
                    .HasColumnType("datetime2");

                b.Property<Guid?>("DocumentoContratoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<decimal>("Encargos")
                    .HasColumnType("decimal(18,2)");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("NegociacaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<decimal>("ValorAluguel")
                    .HasColumnType("decimal(18,2)");

                b.HasKey("Id");

                b.HasIndex("DocumentoContratoId");

                b.HasIndex("ImovelId");

                b.HasIndex("NegociacaoId");

                b.ToTable("Contratos");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.FinancialTransaction", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("DataEfetivacao")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("DataPrevista")
                    .HasColumnType("datetime2");

                b.Property<Guid>("NegociacaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Observacao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<string>("TipoLancamento")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<decimal>("Valor")
                    .HasColumnType("decimal(18,2)");

                b.HasKey("Id");

                b.HasIndex("NegociacaoId");

                b.ToTable("LancamentosFinanceiros");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InAppNotification", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<bool>("Lida")
                    .HasColumnType("bit");

                b.Property<DateTime?>("LidaEm")
                    .HasColumnType("datetime2");

                b.Property<string>("LinkDestino")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Mensagem")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UsuarioId")
                    .IsRequired()
                    .HasColumnType("nvarchar(450)");

                b.HasKey("Id");

                b.HasIndex("UsuarioId");

                b.HasIndex("UsuarioId", "Lida");

                b.ToTable("InAppNotification");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Inspection", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("Fim")
                    .HasColumnType("datetime2");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("ChecklistJson")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("AgendadaPara")
                    .HasColumnType("datetime2");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("Inicio")
                    .HasColumnType("datetime2");

                b.Property<string>("Observacoes")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Responsavel")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("ImovelId");

                b.ToTable("Vistorias");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InspectionDocument", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ArquivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Tipo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("VistoriaId")
                    .HasColumnType("uniqueidentifier");

                b.HasKey("Id");

                b.HasIndex("ArquivoId");

                b.HasIndex("VistoriaId");

                b.ToTable("InspectionDocuments");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InterestedParty", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Documento")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Nome")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Telefone")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UsuarioId")
                    .HasColumnType("nvarchar(450)");

                b.HasKey("Id");

                b.ToTable("Interessados");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrder", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Categoria")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Contato")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<decimal?>("CustoEstimado")
                    .HasColumnType("decimal(18,2)");

                b.Property<decimal?>("CustoReal")
                    .HasColumnType("decimal(18,2)");

                b.Property<DateTime?>("DataConclusao")
                    .HasColumnType("datetime2");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("IniciadaEm")
                    .HasColumnType("datetime2");

                b.Property<string>("Descricao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("PrevisaoConclusao")
                    .HasColumnType("datetime2");

                b.Property<string>("Responsavel")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<int?>("StatusDisponibilidadeAnterior")
                    .HasColumnType("int");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid?>("VistoriaId")
                    .HasColumnType("uniqueidentifier");

                b.HasKey("Id");

                b.HasIndex("ImovelId");

                b.HasIndex("VistoriaId");

                b.ToTable("Manutencoes");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrderDocument", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ArquivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Categoria")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("OrdemManutencaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("ArquivoId");

                b.HasIndex("OrdemManutencaoId");

                b.ToTable("MaintenanceDocuments");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Negotiation", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<bool>("Ativa")
                    .HasColumnType("bit");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<int>("Etapa")
                    .HasColumnType("int");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("InteressadoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("ObservacoesInternas")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("ReservadoAte")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<decimal?>("ValorProposta")
                    .HasColumnType("decimal(18,2)");

                b.Property<decimal?>("ValorSinal")
                    .HasColumnType("decimal(18,2)");

                b.HasKey("Id");

                b.HasIndex("ImovelId");

                b.HasIndex("InteressadoId");

                b.ToTable("Negociacoes");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.NegotiationDocument", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ArquivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Categoria")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("NegociacaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Versao")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("ArquivoId");

                b.HasIndex("NegociacaoId");

                b.ToTable("NegotiationDocuments");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.NegotiationEvent", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Descricao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("NegociacaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("OcorridoEm")
                    .HasColumnType("datetime2");

                b.Property<string>("Responsavel")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("NegociacaoId");

                b.ToTable("NegotiationEvents");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Owner", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Documento")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Nome")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Observacoes")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Telefone")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UsuarioId")
                    .HasColumnType("nvarchar(450)");

                b.HasKey("Id");

                b.ToTable("Proprietarios");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Property", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<decimal>("Area")
                    .HasColumnType("decimal(18,2)");

                b.Property<string>("Bairro")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Banheiros")
                    .HasColumnType("int");

                b.Property<string>("CaracteristicasJson")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Cidade")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CodigoInterno")
                    .IsRequired()
                    .HasColumnType("nvarchar(450)");

                b.Property<Guid?>("ContratoAtivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("DataPrevistaDisponibilidade")
                    .HasColumnType("datetime2");

                b.Property<string>("Endereco")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("ProprietarioId")
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("Quartos")
                    .HasColumnType("int");

                b.Property<int>("StatusDisponibilidade")
                    .HasColumnType("int");

                b.Property<string>("Estado")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Tipo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Vagas")
                    .HasColumnType("int");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("CodigoInterno")
                    .IsUnique();

                b.HasIndex("ContratoAtivoId")
                    .HasFilter("[ContratoAtivoId] IS NOT NULL");

                b.HasIndex("ProprietarioId");

                b.ToTable("Imoveis");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyDocument", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ArquivoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Descricao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Observacoes")
                    .HasColumnType("nvarchar(max)");

                b.Property<bool>("RequerAceiteProprietario")
                    .HasColumnType("bit");

                b.Property<DateTime?>("RevisadoEm")
                    .HasColumnType("datetime2");

                b.Property<string>("RevisadoPor")
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("ValidoAte")
                    .HasColumnType("datetime2");

                b.Property<int>("Versao")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("ArquivoId");

                b.HasIndex("ImovelId");

                b.ToTable("PropertyDocuments");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyDocumentAcceptance", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Cargo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("DocumentoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Host")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Ip")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Nome")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("RegistradoEm")
                    .HasColumnType("datetime2");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UsuarioSistema")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("DocumentoId");

                b.ToTable("DocumentAcceptances");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyHistoryEvent", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Descricao")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("OcorreuEm")
                    .HasColumnType("datetime2");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Usuario")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("ImovelId");

                b.ToTable("PropertyHistory");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ScheduleEntry", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime>("Fim")
                    .HasColumnType("datetime2");

                b.Property<Guid?>("ImovelId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("Inicio")
                    .HasColumnType("datetime2");

                b.Property<Guid?>("NegociacaoId")
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Observacoes")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Responsavel")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Setor")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Tipo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Titulo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<Guid?>("VistoriaId")
                    .HasColumnType("uniqueidentifier");

                b.HasKey("Id");

                b.HasIndex("ImovelId");

                b.HasIndex("NegociacaoId");

                b.HasIndex("VistoriaId");

                b.HasIndex("Inicio", "Fim");

                b.ToTable("Agenda");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.StoredFile", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Categoria")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("ConteudoTipo")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Hash")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("NomeOriginal")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<long>("TamanhoEmBytes")
                    .HasColumnType("bigint");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("CaminhoRelativo")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("nvarchar(512)");

                b.HasKey("Id");

                b.ToTable("Arquivos");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", b =>
            {
                b.Property<string>("Id")
                    .HasColumnType("nvarchar(450)");

                b.Property<int>("AccessFailedCount")
                    .HasColumnType("int");

                b.Property<bool>("Ativo")
                    .HasColumnType("bit");

                b.Property<string>("ConcurrencyStamp")
                    .IsConcurrencyToken()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Email")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<bool>("EmailConfirmed")
                    .HasColumnType("bit");

                b.Property<bool>("LockoutEnabled")
                    .HasColumnType("bit");

                b.Property<DateTimeOffset?>("LockoutEnd")
                    .HasColumnType("datetimeoffset");

                b.Property<string>("NomeCompleto")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("NormalizedEmail")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("NormalizedUserName")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("PasswordHash")
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("Perfil")
                    .HasColumnType("int");

                b.Property<string>("PhoneNumber")
                    .HasColumnType("nvarchar(max)");

                b.Property<bool>("PhoneNumberConfirmed")
                    .HasColumnType("bit");

                b.Property<string>("SecurityStamp")
                    .HasColumnType("nvarchar(max)");

                b.Property<bool>("TwoFactorEnabled")
                    .HasColumnType("bit");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("UpdatedBy")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UserName")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("CreatedBy")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.HasIndex("NormalizedEmail")
                    .HasDatabaseName("EmailIndex");

                b.HasIndex("NormalizedUserName")
                    .IsUnique()
                    .HasDatabaseName("UserNameIndex")
                    .HasFilter("[NormalizedUserName] IS NOT NULL");

                b.ToTable("AspNetUsers", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
            {
                b.Property<string>("Id")
                    .HasColumnType("nvarchar(450)");

                b.Property<string>("ConcurrencyStamp")
                    .IsConcurrencyToken()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("Name")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("NormalizedName")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.HasKey("Id");

                b.HasIndex("NormalizedName")
                    .IsUnique()
                    .HasDatabaseName("RoleNameIndex")
                    .HasFilter("[NormalizedName] IS NOT NULL");

                b.ToTable("AspNetRoles", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("ClaimType")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("ClaimValue")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("RoleId")
                    .IsRequired()
                    .HasColumnType("nvarchar(450)");

                b.HasKey("Id");

                b.HasIndex("RoleId");

                b.ToTable("AspNetRoleClaims", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("ClaimType")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("ClaimValue")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UserId")
                    .IsRequired()
                    .HasColumnType("nvarchar(450)");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("AspNetUserClaims", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
            {
                b.Property<string>("LoginProvider")
                    .HasMaxLength(128)
                    .HasColumnType("nvarchar(128)");

                b.Property<string>("ProviderKey")
                    .HasMaxLength(128)
                    .HasColumnType("nvarchar(128)");

                b.Property<string>("ProviderDisplayName")
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("UserId")
                    .IsRequired()
                    .HasColumnType("nvarchar(450)");

                b.HasKey("LoginProvider", "ProviderKey");

                b.HasIndex("UserId");

                b.ToTable("AspNetUserLogins", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
            {
                b.Property<string>("UserId")
                    .HasColumnType("nvarchar(450)");

                b.Property<string>("RoleId")
                    .HasColumnType("nvarchar(450)");

                b.HasKey("UserId", "RoleId");

                b.HasIndex("RoleId");

                b.ToTable("AspNetUserRoles", (string)null);
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
            {
                b.Property<string>("UserId")
                    .HasColumnType("nvarchar(450)");

                b.Property<string>("LoginProvider")
                    .HasMaxLength(128)
                    .HasColumnType("nvarchar(128)");

                b.Property<string>("Name")
                    .HasMaxLength(128)
                    .HasColumnType("nvarchar(128)");

                b.Property<string>("Value")
                    .HasColumnType("nvarchar(max)");

                b.HasKey("UserId", "LoginProvider", "Name");

                b.ToTable("AspNetUserTokens", (string)null);
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ActivityAttachment", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "Arquivo")
                    .WithMany()
                    .HasForeignKey("ArquivoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Activity", "Atividade")
                    .WithMany("Anexos")
                    .HasForeignKey("AtividadeId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Arquivo");

                b.Navigation("Atividade");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ActivityComment", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Activity", "Atividade")
                    .WithMany("Comentarios")
                    .HasForeignKey("AtividadeId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Atividade");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Contract", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "DocumentoContrato")
                    .WithMany()
                    .HasForeignKey("DocumentoContratoId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Contratos")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Negotiation", "Negociacao")
                    .WithMany("Contratos")
                    .HasForeignKey("NegociacaoId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("DocumentoContrato");

                b.Navigation("Imovel");

                b.Navigation("Negociacao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ContextMessageMention", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.ContextMessage", "Mensagem")
                    .WithMany("Mentions")
                    .HasForeignKey("MensagemId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Mensagem");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.FinancialTransaction", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Negotiation", "Negociacao")
                    .WithMany("LancamentosFinanceiros")
                    .HasForeignKey("NegociacaoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Negociacao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InAppNotification", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UsuarioId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Inspection", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Vistorias")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Imovel");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InspectionDocument", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "Arquivo")
                    .WithMany()
                    .HasForeignKey("ArquivoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Inspection", "Vistoria")
                    .WithMany("Documentos")
                    .HasForeignKey("VistoriaId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Arquivo");

                b.Navigation("Vistoria");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrder", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Manutencoes")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Inspection", "Vistoria")
                    .WithMany("OrdensManutencao")
                    .HasForeignKey("VistoriaId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.Navigation("Imovel");

                b.Navigation("Vistoria");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrderDocument", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "Arquivo")
                    .WithMany()
                    .HasForeignKey("ArquivoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrder", "OrdemManutencao")
                    .WithMany("Documentos")
                    .HasForeignKey("OrdemManutencaoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Arquivo");

                b.Navigation("OrdemManutencao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Negotiation", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Negociacoes")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.InterestedParty", "Interessado")
                    .WithMany("Negociacoes")
                    .HasForeignKey("InteressadoId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("Imovel");

                b.Navigation("Interessado");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.NegotiationDocument", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "Arquivo")
                    .WithMany()
                    .HasForeignKey("ArquivoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Negotiation", "Negociacao")
                    .WithMany("Documentos")
                    .HasForeignKey("NegociacaoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Arquivo");

                b.Navigation("Negociacao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.NegotiationEvent", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Negotiation", "Negociacao")
                    .WithMany("Eventos")
                    .HasForeignKey("NegociacaoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Negociacao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Property", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Contract", "ContratoAtivo")
                    .WithMany()
                    .HasForeignKey("ContratoAtivoId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Owner", "Proprietario")
                    .WithMany("Imoveis")
                    .HasForeignKey("ProprietarioId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("ContratoAtivo");

                b.Navigation("Proprietario");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyDocument", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.StoredFile", "Arquivo")
                    .WithMany()
                    .HasForeignKey("ArquivoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Documentos")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Arquivo");

                b.Navigation("Imovel");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyDocumentAcceptance", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.PropertyDocument", "Documento")
                    .WithMany("Aceites")
                    .HasForeignKey("DocumentoId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Documento");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyHistoryEvent", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany("Historico")
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Imovel");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ScheduleEntry", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Property", "Imovel")
                    .WithMany()
                    .HasForeignKey("ImovelId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Negotiation", "Negociacao")
                    .WithMany()
                    .HasForeignKey("NegociacaoId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne("AdministraAoImoveis.Web.Domain.Entities.Inspection", "Vistoria")
                    .WithMany()
                    .HasForeignKey("VistoriaId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("Imovel");

                b.Navigation("Negociacao");

                b.Navigation("Vistoria");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
            {
                b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
            {
                b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
            {
                b.HasOne("AdministraAoImoveis.Web.Domain.Users.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Activity", b =>
            {
                b.Navigation("Anexos");

                b.Navigation("Comentarios");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.ContextMessage", b =>
            {
                b.Navigation("Mentions");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Inspection", b =>
            {
                b.Navigation("Documentos");

                b.Navigation("OrdensManutencao");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.InterestedParty", b =>
            {
                b.Navigation("Negociacoes");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.MaintenanceOrder", b =>
            {
                b.Navigation("Documentos");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Negotiation", b =>
            {
                b.Navigation("Contratos");

                b.Navigation("Documentos");

                b.Navigation("Eventos");

                b.Navigation("LancamentosFinanceiros");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Owner", b =>
            {
                b.Navigation("Imoveis");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.Property", b =>
            {
                b.Navigation("Contratos");

                b.Navigation("Documentos");

                b.Navigation("Historico");

                b.Navigation("Manutencoes");

                b.Navigation("Negociacoes");

                b.Navigation("Vistorias");
            });

            modelBuilder.Entity("AdministraAoImoveis.Web.Domain.Entities.PropertyDocument", b =>
            {
                b.Navigation("Aceites");
            });
        }
    }
}
