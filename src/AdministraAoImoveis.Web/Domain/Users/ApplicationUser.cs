using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Identity;

namespace AdministraAoImoveis.Web.Domain.Users;

public class ApplicationUser : IdentityUser
{
    public UserRoleProfile Perfil { get; set; } = UserRoleProfile.Comercial;
    public string NomeCompleto { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
