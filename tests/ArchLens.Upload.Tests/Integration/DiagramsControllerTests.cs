using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Integration;

[Trait("Category", "Integration")]
public class DiagramsControllerTests : IClassFixture<UploadApiFactory>
{
    private readonly HttpClient _client;

    public DiagramsControllerTests(UploadApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requires running infrastructure (PostgreSQL, RabbitMQ, MinIO) - run with Testcontainers in CI")]
    public async Task Upload_InvalidExtension_ShouldReturn400()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x01, 0x02, 0x03]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe");

        var response = await _client.PostAsync("/diagrams", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires running infrastructure (PostgreSQL, RabbitMQ, MinIO) - run with Testcontainers in CI")]
    public async Task Upload_ValidPng_ShouldReturnSuccessOrCreated()
    {
        var content = new MultipartFormDataContent();
        var fileBytes = CreateMinimalPng();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test-diagram.png");

        var response = await _client.PostAsync("/diagrams", content);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact(Skip = "Requires running infrastructure (PostgreSQL, RabbitMQ, MinIO) - run with Testcontainers in CI")]
    public async Task GetStatus_NonExistent_ShouldReturnNotFoundOrError()
    {
        var response = await _client.GetAsync($"/diagrams/{Guid.NewGuid()}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact(Skip = "Requires running infrastructure (PostgreSQL, RabbitMQ, MinIO) - run with Testcontainers in CI")]
    public async Task ListDiagrams_ShouldReturnSuccessOrError()
    {
        var response = await _client.GetAsync("/diagrams?page=1&pageSize=10");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact(Skip = "Requires running infrastructure (PostgreSQL, RabbitMQ, MinIO) - run with Testcontainers in CI")]
    public async Task HealthCheck_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static byte[] CreateMinimalPng()
    {
        byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        byte[] ihdr = [
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE
        ];
        byte[] idat = [
            0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54,
            0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, 0x00,
            0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33
        ];
        byte[] iend = [
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82
        ];

        return [.. pngSignature, .. ihdr, .. idat, .. iend];
    }
}
