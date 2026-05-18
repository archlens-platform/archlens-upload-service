using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace ArchLens.Upload.Infrastructure.Storage;

public sealed class MinioStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IMinioClient client, IConfiguration configuration, ILogger<MinioStorageService> logger)
    {
        _client = client;
        _bucketName = configuration["MinIO:BucketName"] ?? "archlens-diagrams";
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{SanitizeFileName(fileName)}";

        await EnsureBucketExistsAsync(cancellationToken);

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), cancellationToken);

        _logger.LogInformation("File uploaded to MinIO: {ObjectName}", objectName);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storagePath)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream)), cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storagePath), cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName), cancellationToken);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var sanitized = Path.GetFileName(fileName);
        return string.Join("_", sanitized.Split(Path.GetInvalidFileNameChars()));
    }
}
