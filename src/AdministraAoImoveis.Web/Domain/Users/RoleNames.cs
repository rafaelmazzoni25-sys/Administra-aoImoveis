namespace AdministraAoImoveis.Web.Domain.Users;

public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string Comercial = "COMERCIAL";
    public const string Vistoria = "VISTORIA";
    public const string Manutencao = "MANUTENCAO";
    public const string Financeiro = "FINANCEIRO";
    public const string Juridico = "JURIDICO";
    public const string Proprietario = "PROPRIETARIO";
    public const string Interessado = "INTERESSADO";

    public const string GestaoImoveis = Admin + "," + Comercial + "," + Juridico;
    public const string Operacional = Admin + "," + Comercial + "," + Vistoria + "," + Manutencao + "," + Financeiro + "," + Juridico;
    public const string AgendaSetores = Admin + "," + Comercial + "," + Vistoria + "," + Manutencao + "," + Financeiro;
    public const string FinanceiroEquipe = Admin + "," + Financeiro;
    public const string VistoriaEquipe = Admin + "," + Vistoria;
    public const string ManutencaoEquipe = Admin + "," + Manutencao + "," + Vistoria;
    public const string Auditoria = Admin + "," + Juridico;
}
