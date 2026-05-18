using ArchLens.Upload.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel.Args;
using NSubstitute;

namespace ArchLens.Upload.Tests.Infrastructure.Storage;

public class MinioStorageServiceTests
{
    private readonly IMinioClient _minioClient = Substitute.For<IMinioClient>();
    private readonly MinioStorageService _service;
    private const string BucketName = "test-bucket";

    public MinioStorageServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MinIO:BucketName"] = BucketName
            })
            .Build();

        _minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _service = new MinioStorageService(_minioClient, config, NullLogger<MinioStorageService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_ShouldCallPutObject_AndReturnObjectName()
    {
        using var stream = new MemoryStream("file-content"u8.ToArray());

        var result = await _service.UploadAsync(stream, "diagram.png", "image/png");

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().EndWith("diagram.png");
        await _minioClient.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ShouldEnsureBucketExists()
    {
        using var stream = new MemoryStream("content"u8.ToArray());

        await _service.UploadAsync(stream, "file.png", "image/png");

        await _minioClient.Received(1).BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateBucket_WhenItDoesNotExist()
    {
        _minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(false);

        using var stream = new MemoryStream("content"u8.ToArray());

        await _service.UploadAsync(stream, "file.png", "image/png");

        await _minioClient.Received(1).MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ShouldNotCreateBucket_WhenItAlreadyExists()
    {
        using var stream = new MemoryStream("content"u8.ToArray());

        await _service.UploadAsync(stream, "file.png", "image/png");

        await _minioClient.DidNotReceive().MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ShouldGeneratePathWithDateAndGuid()
    {
        using var stream = new MemoryStream("content"u8.ToArray());

        var result = await _service.UploadAsync(stream, "test.png", "image/png");

        var parts = result.Split('/');
        parts.Should().HaveCount(5);
        int.TryParse(parts[0], out _).Should().BeTrue("first segment should be year");
        int.TryParse(parts[1], out _).Should().BeTrue("second segment should be month");
        int.TryParse(parts[2], out _).Should().BeTrue("third segment should be day");
        Guid.TryParse(parts[3], out _).Should().BeTrue("fourth segment should be a GUID");
        parts[4].Should().Be("test.png");
    }

    [Fact]
    public async Task UploadAsync_ShouldSanitizeFileName()
    {
        using var stream = new MemoryStream("content"u8.ToArray());

        var result = await _service.UploadAsync(stream, "path/to/my file.png", "image/png");

        result.Should().EndWith("my file.png");
        result.Should().NotContain("path/to/");
    }

    [Fact]
    public async Task DownloadAsync_ShouldCallGetObject()
    {
        await _service.DownloadAsync("some/path/file.png");

        await _minioClient.Received(1).GetObjectAsync(Arg.Any<GetObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnStream()
    {
        var result = await _service.DownloadAsync("some/path/file.png");

        result.Should().NotBeNull();
        result.Position.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRemoveObject()
    {
        await _service.DeleteAsync("some/path/file.png");

        await _minioClient.Received(1).RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _service.DeleteAsync("path/file.png", cts.Token);

        await _minioClient.Received(1).RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), cts.Token);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultBucketName_WhenConfigMissing()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new MinioStorageService(_minioClient, emptyConfig, NullLogger<MinioStorageService>.Instance);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadAsync_ShouldPassCancellationToken()
    {
        using var stream = new MemoryStream("content"u8.ToArray());
        using var cts = new CancellationTokenSource();

        await _service.UploadAsync(stream, "test.png", "image/png", cts.Token);

        await _minioClient.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>(), cts.Token);
    }
}
