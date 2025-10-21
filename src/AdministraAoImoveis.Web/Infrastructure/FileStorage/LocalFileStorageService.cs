using System.Security.Cryptography;
using AdministraAoImoveis.Web.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AdministraAoImoveis.Web.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorageService> logger,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _logger = logger;
        _basePath = ResolveBasePath(_options.BasePath, hostEnvironment.ContentRootPath);
        _options.BasePath = _basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<StoredFile> SaveAsync(string fileName, string contentType, Stream content, string category, CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var safeFileName = Path.GetFileName(fileName);
        var sanitizedCategory = string.IsNullOrWhiteSpace(category)
            ? "geral"
            : category.Trim().Replace("..", string.Empty).Replace('/', '-').Replace('\\', '-');
        var year = DateTime.UtcNow.ToString("yyyy");
        var month = DateTime.UtcNow.ToString("MM");
        var relativeDirectory = Path.Combine(year, month, sanitizedCategory);
        var absoluteDirectory = Path.Combine(_basePath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(safeFileName)}";
        var absolutePath = Path.Combine(absoluteDirectory, storedFileName);
        var relativePath = Path.GetRelativePath(_basePath, absolutePath).Replace('\\', '/');

        var fileCreated = false;

        try
        {
            await using (var fileStream = new FileStream(
                           absolutePath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None,
                           81920,
                           useAsync: true))
            {
                fileCreated = true;
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            var fileInfo = new FileInfo(absolutePath);
            if (fileInfo.Length > _options.MaxFileSizeInBytes)
            {
                throw new InvalidOperationException($"Arquivo excede o tamanho máximo permitido de {_options.MaxFileSizeInBytes} bytes.");
            }

            var storedFile = new StoredFile
            {
                NomeOriginal = safeFileName,
                CaminhoRelativo = relativePath,
                ConteudoTipo = contentType,
                TamanhoEmBytes = fileInfo.Length,
                Categoria = sanitizedCategory,
                Hash = await ComputeHashAsync(absolutePath, cancellationToken)
            };

            _logger.LogInformation("Arquivo {FileName} salvo em {Path}", safeFileName, absolutePath);
            return storedFile;
        }
        catch
        {
            if (fileCreated && File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                _logger.LogWarning("Arquivo {Path} removido após falha durante o salvamento", absolutePath);
            }

            throw;
        }
    }

    public Task<Stream> OpenAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = Path.Combine(_basePath, file.CaminhoRelativo.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning("Arquivo {Path} não encontrado para o registro {FileId}", absolutePath, file.Id);
            throw new FileNotFoundException("Arquivo não encontrado no armazenamento. Ele pode ter sido removido ou está inconsistente com o cadastro.", file.CaminhoRelativo);
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = Path.Combine(_basePath, file.CaminhoRelativo.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogInformation("Arquivo {Path} removido", absolutePath);
        }
        return Task.CompletedTask;
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string ResolveBasePath(string? configuredBasePath, string contentRootPath)
    {
        var basePath = string.IsNullOrWhiteSpace(configuredBasePath)
            ? Path.Combine(contentRootPath, "storage")
            : configuredBasePath;

        if (!Path.IsPathRooted(basePath))
        {
            basePath = Path.Combine(contentRootPath, basePath);
        }

        return Path.GetFullPath(basePath);
    }
}
