namespace AdministraAoImoveis.Web.Infrastructure.FileStorage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string BasePath { get; set; } = "D:/Imobiliaria/Arquivos";
    public long MaxFileSizeInBytes { get; set; } = 1024L * 1024 * 100; // 100 MB
}
