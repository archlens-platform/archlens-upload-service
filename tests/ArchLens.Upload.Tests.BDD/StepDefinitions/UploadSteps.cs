using System.Net.Http.Headers;
using ArchLens.Upload.Tests.BDD.Hooks;
using Reqnroll;

namespace ArchLens.Upload.Tests.BDD.StepDefinitions;

[Binding]
public sealed class UploadSteps(ScenarioContext scenarioContext)
{
    // PNG magic bytes: 0x89 0x50 0x4E 0x47
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    // JPG magic bytes: 0xFF 0xD8 0xFF
    private static readonly byte[] JpgSignature = [0xFF, 0xD8, 0xFF, 0xE0];

    [Given("que eu tenho um arquivo PNG válido chamado {string}")]
    public void DadoQueEuTenhoUmArquivoPngValidoChamado(string fileName)
    {
        var fileContent = new byte[1024];
        Array.Copy(PngSignature, fileContent, PngSignature.Length);
        Random.Shared.NextBytes(fileContent.AsSpan(PngSignature.Length));
        scenarioContext.Set(fileContent, "FileContent");
        scenarioContext.Set(fileName, "FileName");
        scenarioContext.Set("image/png", "ContentType");
    }

    [Given("que eu tenho um arquivo JPG válido chamado {string}")]
    public void DadoQueEuTenhoUmArquivoJpgValidoChamado(string fileName)
    {
        var fileContent = new byte[1024];
        Array.Copy(JpgSignature, fileContent, JpgSignature.Length);
        Random.Shared.NextBytes(fileContent.AsSpan(JpgSignature.Length));
        scenarioContext.Set(fileContent, "FileContent");
        scenarioContext.Set(fileName, "FileName");
        scenarioContext.Set("image/jpeg", "ContentType");
    }

    [Given("que eu tenho um arquivo com extensão não suportada chamado {string}")]
    public void DadoQueEuTenhoUmArquivoComExtensaoNaoSuportadaChamado(string fileName)
    {
        var fileContent = new byte[100];
        Random.Shared.NextBytes(fileContent);
        scenarioContext.Set(fileContent, "FileContent");
        scenarioContext.Set(fileName, "FileName");
        scenarioContext.Set("text/plain", "ContentType");
    }

    [Given("que eu tenho um arquivo com assinatura inválida chamado {string}")]
    public void DadoQueEuTenhoUmArquivoComAssinaturaInvalidaChamado(string fileName)
    {
        // File has .png extension but no valid PNG signature
        var fileContent = new byte[100];
        fileContent[0] = 0x00;
        fileContent[1] = 0x00;
        fileContent[2] = 0x00;
        fileContent[3] = 0x00;
        scenarioContext.Set(fileContent, "FileContent");
        scenarioContext.Set(fileName, "FileName");
        scenarioContext.Set("image/png", "ContentType");
    }

    [Given("que eu já fiz upload desse arquivo anteriormente")]
    public async Task DadoQueEuJaFizUploadDesseArquivoAnteriormente()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var fileContent = scenarioContext.Get<byte[]>("FileContent");
        var fileName = scenarioContext.Get<string>("FileName");
        var contentType = scenarioContext.Get<string>("ContentType");

        using var content = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileBytes, "file", fileName);

        await client.PostAsync("/diagrams", content);
    }

    [When("eu envio o upload do arquivo para {string}")]
    public async Task QuandoEuEnvioOUploadDoArquivoPara(string endpoint)
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var fileContent = scenarioContext.Get<byte[]>("FileContent");
        var fileName = scenarioContext.Get<string>("FileName");
        var contentType = scenarioContext.Get<string>("ContentType");

        using var content = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileBytes, "file", fileName);

        var response = await client.PostAsync(endpoint, content);
        scenarioContext.Set(response, "Response");
    }
}
