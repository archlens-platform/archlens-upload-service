using System.Text.Json;
using FluentAssertions;
using Reqnroll;

namespace ArchLens.Upload.Tests.BDD.StepDefinitions;

[Binding]
public sealed class CommonSteps(ScenarioContext scenarioContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Then("a resposta deve ter status code {int}")]
    public void EntaoARespostaDeveTerStatusCode(int statusCode)
    {
        var response = scenarioContext.Get<HttpResponseMessage>("Response");
        ((int)response.StatusCode).Should().Be(statusCode);
    }

    [Then("a resposta deve conter a mensagem {string}")]
    public async Task EntaoARespostaDeveConterAMensagem(string expectedMessage)
    {
        var response = scenarioContext.Get<HttpResponseMessage>("Response");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainEquivalentOf(expectedMessage);
    }

    [Then("a resposta deve conter o campo {string}")]
    public async Task EntaoARespostaDeveConterOCampo(string fieldName)
    {
        var response = scenarioContext.Get<HttpResponseMessage>("Response");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        json.TryGetProperty(fieldName, out _).Should().BeTrue($"expected field '{fieldName}' in response body");
    }

    [Then("a resposta deve conter o campo {string} com valor {string}")]
    public async Task EntaoARespostaDeveConterOCampoComValor(string fieldName, string expectedValue)
    {
        var response = scenarioContext.Get<HttpResponseMessage>("Response");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        json.TryGetProperty(fieldName, out var value).Should().BeTrue($"expected field '{fieldName}' in response body");
        value.ToString().Should().BeEquivalentTo(expectedValue);
    }

    [When("eu envio uma requisição GET para {string}")]
    public async Task QuandoEuEnvioUmaRequisicaoGetPara(string endpoint)
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var response = await client.GetAsync(endpoint);
        scenarioContext.Set(response, "Response");
    }

    [When("eu envio uma requisição DELETE para {string}")]
    public async Task QuandoEuEnvioUmaRequisicaoDeletePara(string endpoint)
    {
        var client = scenarioContext.Get<HttpClient>("HttpClient");
        var response = await client.DeleteAsync(endpoint);
        scenarioContext.Set(response, "Response");
    }
}
