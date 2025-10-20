using System.Security.Cryptography;
using AdministraAoImoveis.Web.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AdministraAoImoveis.Web.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        Directory.CreateDirectory(_options.BasePath);
    }

    public async Task<StoredFile> SaveAsync(string fileName, string contentType, Stream content, string category, CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var safeFileName = Path.GetFileName(fileName);
        var relativeDirectory = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), category);
        var absoluteDirectory = Path.Combine(_options.BasePath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(safeFileName)}";
        var relativePath = Path.Combine(relativeDirectory, storedFileName).Replace('\\', '/');
        var absolutePath = Path.Combine(_options.BasePath, relativePath);

        await using (var fileStream = File.Create(absolutePath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var fileInfo = new FileInfo(absolutePath);
        if (fileInfo.Length > _options.MaxFileSizeInBytes)
        {
            File.Delete(absolutePath);
            throw new InvalidOperationException($"Arquivo excede o tamanho máximo permitido de {_options.MaxFileSizeInBytes} bytes.");
        }

        var storedFile = new StoredFile
        {
            NomeOriginal = safeFileName,
            CaminhoRelativo = relativePath,
            ConteudoTipo = contentType,
            TamanhoEmBytes = fileInfo.Length,
            Categoria = category,
            Hash = await ComputeHashAsync(absolutePath, cancellationToken)
        };

        _logger.LogInformation("Arquivo {FileName} salvo em {Path}", safeFileName, absolutePath);
        return storedFile;
    }

    public Task<Stream> OpenAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_options.BasePath, file.CaminhoRelativo);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_options.BasePath, file.CaminhoRelativo);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogInformation("Arquivo {Path} removido", absolutePath);
        }
        return Task.CompletedTask;
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
