using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AdministraAoImoveis.Web.Data;

public static class ApplicationDbInitializer
{
    private static readonly UserRoleProfile[] DefaultProfiles =
    {
        UserRoleProfile.Admin,
        UserRoleProfile.Comercial,
        UserRoleProfile.Vistoria,
        UserRoleProfile.Manutencao,
        UserRoleProfile.Financeiro,
        UserRoleProfile.Juridico,
        UserRoleProfile.Proprietario,
        UserRoleProfile.Interessado
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var profile in DefaultProfiles)
        {
            var roleName = profile.ToString().ToUpperInvariant();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminEmail = "admin@local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Perfil = UserRoleProfile.Admin,
                NomeCompleto = "Administrador do Sistema"
            };

            var result = await userManager.CreateAsync(adminUser, "Adm1n!234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoleProfile.Admin.ToString().ToUpperInvariant());
            }
        }
    }
}
