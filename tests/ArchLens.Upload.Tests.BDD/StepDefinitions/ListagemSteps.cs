using System.Net.Http.Headers;
using System.Text.Json;
using ArchLens.Upload.Tests.BDD.Hooks;
using Reqnroll;

namespace ArchLens.Upload.Tests.BDD.StepDefinitions;

[Binding]
public sealed class ListagemSteps(ScenarioContext scenarioContext)
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Given("que existem {int} diagramas cadastrados no sistema")]
    public async Task DadoQueExistemNDiagramasCadastradosNoSistema(int count)
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");

        for (int i = 0; i < count; i++)
        {
            var fileContent = new byte[1024];
            Array.Copy(PngSignature, fileContent, PngSignature.Length);
            // Ensure each file has unique content by writing the index
            BitConverter.GetBytes(i).CopyTo(fileContent, PngSignature.Length);
            Random.Shared.NextBytes(fileContent.AsSpan(PngSignature.Length + 4));

            using var content = new MultipartFormDataContent();
            var fileBytes = new ByteArrayContent(fileContent);
            fileBytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileBytes, "file", $"diagram_{i}.png");

            var response = await client.PostAsync("/diagrams", content);
            response.EnsureSuccessStatusCode();

            // Store the last created diagram id
            if (i == count - 1)
            {
                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (json.TryGetProperty("diagramId", out var diagramId))
                {
                    scenarioContext.Set(diagramId.GetString()!, "LastDiagramId");
                }
            }
        }
    }

    [Given("que existe um diagrama cadastrado no sistema")]
    public async Task DadoQueExisteUmDiagramaCadastradoNoSistema()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");

        var fileContent = new byte[1024];
        Array.Copy(PngSignature, fileContent, PngSignature.Length);
        Random.Shared.NextBytes(fileContent.AsSpan(PngSignature.Length));

        using var content = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileBytes, "file", "test_diagram.png");

        var response = await client.PostAsync("/diagrams", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (json.TryGetProperty("diagramId", out var diagramId))
        {
            scenarioContext.Set(diagramId.GetString()!, "DiagramId");
        }
    }

    [Given("que o diagrama foi excluído")]
    public async Task DadoQueODiagramaFoiExcluido()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var diagramId = scenarioContext.Get<string>("DiagramId");
        var response = await client.DeleteAsync($"/diagrams/{diagramId}");
        response.EnsureSuccessStatusCode();
    }

    [When("eu envio uma requisição GET para o diagrama existente")]
    public async Task QuandoEuEnvioUmaRequisicaoGetParaODiagramaExistente()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var diagramId = scenarioContext.Get<string>("DiagramId");
        var response = await client.GetAsync($"/diagrams/{diagramId}");
        scenarioContext.Set(response, "Response");
    }

    [When("eu envio uma requisição GET para o status do diagrama existente")]
    public async Task QuandoEuEnvioUmaRequisicaoGetParaOStatusDoDiagramaExistente()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var diagramId = scenarioContext.Get<string>("DiagramId");
        var response = await client.GetAsync($"/diagrams/{diagramId}/status");
        scenarioContext.Set(response, "Response");
    }

    [When("eu envio uma requisição DELETE para o diagrama existente")]
    public async Task QuandoEuEnvioUmaRequisicaoDeleteParaODiagramaExistente()
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var diagramId = scenarioContext.Get<string>("DiagramId");
        var response = await client.DeleteAsync($"/diagrams/{diagramId}");
        scenarioContext.Set(response, "Response");
    }
}
