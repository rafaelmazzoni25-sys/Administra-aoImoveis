namespace AdministraAoImoveis.Web.Domain.Users;

public static class RoleSets
{
    public static readonly string[] ContractsReaders =
    {
        RoleNames.Admin,
        RoleNames.Comercial,
        RoleNames.Juridico
    };

    public static readonly string[] ContractsManagers =
    {
        RoleNames.Admin,
        RoleNames.Juridico
    };

    public static readonly string[] PropertyDocumentReaders =
    {
        RoleNames.Admin,
        RoleNames.Comercial,
        RoleNames.Juridico
    };

    public static readonly string[] PropertyDocumentManagers =
    {
        RoleNames.Admin,
        RoleNames.Juridico
    };
}
