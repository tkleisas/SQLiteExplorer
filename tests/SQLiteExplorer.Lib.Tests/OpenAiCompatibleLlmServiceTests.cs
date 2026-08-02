using System.Net;
using System.Text;
using System.Text.Json;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class OpenAiCompatibleLlmServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public string? LastRequestBody;
        public HttpStatusCode StatusCode = HttpStatusCode.OK;
        public string ResponseBody = """
            {"choices":[{"message":{"role":"assistant","content":"SELECT 1;"}}]}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private static LlmSettings EnabledSettings() => new()
    {
        Enabled = true,
        Endpoint = "https://example.test/v1",
        ApiKey = "secret-key",
        Model = "test-model",
        Temperature = 0.5
    };

    [Fact]
    public void IsConfigured_RequiresEnabledEndpointAndModel()
    {
        Assert.False(new OpenAiCompatibleLlmService(new LlmSettings()).IsConfigured);
        Assert.False(new OpenAiCompatibleLlmService(new LlmSettings { Enabled = true, Endpoint = "", Model = "m" }).IsConfigured);
        Assert.False(new OpenAiCompatibleLlmService(new LlmSettings { Enabled = true, Endpoint = "e", Model = "" }).IsConfigured);
        Assert.True(new OpenAiCompatibleLlmService(EnabledSettings()).IsConfigured);
    }

    [Fact]
    public async Task ChatAsync_SendsModelMessagesTemperatureAndBearerKey()
    {
        var handler = new FakeHandler();
        var service = new OpenAiCompatibleLlmService(EnabledSettings(), () => handler);

        var reply = await service.ChatAsync("system prompt", "user prompt");

        Assert.Equal("SELECT 1;", reply);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://example.test/v1/chat/completions", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("secret-key", handler.LastRequest.Headers.Authorization?.Parameter);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("test-model", root.GetProperty("model").GetString());
        Assert.Equal(0.5, root.GetProperty("temperature").GetDouble());
        Assert.False(root.GetProperty("stream").GetBoolean());

        var messages = root.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("system prompt", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("user prompt", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task ChatAsync_NoApiKey_OmitsAuthorizationHeader()
    {
        var handler = new FakeHandler();
        var settings = EnabledSettings();
        settings.ApiKey = "";
        var service = new OpenAiCompatibleLlmService(settings, () => handler);

        await service.ChatAsync("s", "u");

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task ChatAsync_HttpError_ThrowsFriendlyMessageWithBody()
    {
        var handler = new FakeHandler
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ResponseBody = """{"error":"invalid api key"}"""
        };
        var service = new OpenAiCompatibleLlmService(EnabledSettings(), () => handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync("s", "u"));

        Assert.Contains("401", ex.Message);
        Assert.Contains("invalid api key", ex.Message);
    }

    [Fact]
    public async Task ChatAsync_NotConfigured_Throws()
    {
        var service = new OpenAiCompatibleLlmService(new LlmSettings());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync("s", "u"));
    }

    [Theory]
    [InlineData("https://api.test/v1", "https://api.test/v1/chat/completions")]
    [InlineData("https://api.test/v1/", "https://api.test/v1/chat/completions")]
    [InlineData("https://api.test/v1/chat/completions", "https://api.test/v1/chat/completions")]
    public void BuildCompletionsUrl_AppendsPathUnlessAlreadyPresent(string endpoint, string expected)
    {
        Assert.Equal(expected, OpenAiCompatibleLlmService.BuildCompletionsUrl(endpoint));
    }
}
