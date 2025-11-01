using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Infrastructure.Logging;
using AdministraAoImoveis.Web.Services;
using AdministraAoImoveis.Web.Services.Contracts;
using AdministraAoImoveis.Web.Services.DocumentExpiration;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<PropertyDocumentExpirationOptions>(
    builder.Configuration.GetSection(PropertyDocumentExpirationOptions.SectionName));

var configuredProvider = builder.Configuration["DatabaseProvider"];
var selectedProvider = string.IsNullOrWhiteSpace(configuredProvider)
    ? (OperatingSystem.IsWindows() ? "SqlServer" : "Sqlite")
    : configuredProvider;
var useSqlite = string.Equals(selectedProvider, "Sqlite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dataDirectory);
            sqliteConnectionString = $"Data Source={Path.Combine(dataDirectory, "app.db")}";
        }
        else
        {
            var connectionBuilder = new SqliteConnectionStringBuilder(sqliteConnectionString);
            var dataSourcePath = connectionBuilder.DataSource;
            var absoluteDataSourcePath = Path.IsPathRooted(dataSourcePath)
                ? dataSourcePath
                : Path.Combine(builder.Environment.ContentRootPath, dataSourcePath);
            var dataDirectory = Path.GetDirectoryName(absoluteDataSourcePath);

            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
        }

        options.UseSqlite(sqliteConnectionString);
    }
    else
    {
        var sqlServerConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                     ?? "Server=(localdb)\\MSSQLLocalDB;Database=AdministraAoImoveis;Trusted_Connection=True;MultipleActiveResultSets=true";

        options.UseSqlServer(sqlServerConnectionString);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PolicyNames.ContractsRead,
        policy => policy.RequireRole(RoleSets.ContractsReaders));
    options.AddPolicy(
        PolicyNames.ContractsManage,
        policy => policy.RequireRole(RoleSets.ContractsManagers));
    options.AddPolicy(
        PolicyNames.PropertyDocumentsRead,
        policy => policy.RequireRole(RoleSets.PropertyDocumentReaders));
    options.AddPolicy(
        PolicyNames.PropertyDocumentsManage,
        policy => policy.RequireRole(RoleSets.PropertyDocumentManagers));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddHostedService<PropertyDocumentExpirationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await ApplicationDbInitializer.SeedAsync(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
