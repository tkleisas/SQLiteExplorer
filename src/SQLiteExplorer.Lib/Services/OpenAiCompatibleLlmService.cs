using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// <see cref="ILlmService"/> over an OpenAI-compatible chat completions endpoint.
/// Works with OpenAI, OpenRouter, DeepSeek, Ollama, LM Studio and similar providers.
/// </summary>
public class OpenAiCompatibleLlmService : ILlmService
{
    private readonly LlmSettings _settings;
    private readonly Func<HttpMessageHandler>? _handlerFactory;

    public OpenAiCompatibleLlmService(LlmSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Test constructor allowing the HTTP transport to be faked.</summary>
    internal OpenAiCompatibleLlmService(LlmSettings settings, Func<HttpMessageHandler> handlerFactory)
    {
        _settings = settings;
        _handlerFactory = handlerFactory;
    }

    public bool IsConfigured =>
        _settings.Enabled
        && !string.IsNullOrWhiteSpace(_settings.Endpoint)
        && !string.IsNullOrWhiteSpace(_settings.Model);

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        using var request = BuildRequest(systemPrompt, userPrompt, stream: false);

        try
        {
            using var client = CreateClient();
            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            await ThrowIfFailedAsync(response, ct).ConfigureAwait(false);

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ExtractContent(responseJson);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("LLM request timed out.");
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChatStreamingAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onToken,
        CancellationToken ct = default)
    {
        using var request = BuildRequest(systemPrompt, userPrompt, stream: true);

        try
        {
            using var client = CreateClient();
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct).ConfigureAwait(false);
            await ThrowIfFailedAsync(response, ct).ConfigureAwait(false);

            return await ReadSseContentAsync(response, onToken, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("LLM request timed out.");
        }
    }

    private HttpRequestMessage BuildRequest(string systemPrompt, string userPrompt, bool stream)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = _settings.Model,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = _settings.Temperature,
            ["stream"] = stream
        };
        if (_settings.ThinkingMode)
        {
            requestBody["reasoning_effort"] = _settings.ThinkingEffort;
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildCompletionsUrl(_settings.Endpoint))
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        return request;
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = (await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)).Trim();
        if (detail.Length > 500)
        {
            detail = detail[..500] + "…";
        }
        throw new InvalidOperationException(
            string.IsNullOrEmpty(detail)
                ? $"LLM request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                : $"LLM request failed: {(int)response.StatusCode} {response.ReasonPhrase} — {detail}");
    }

    /// <summary>
    /// Reads an OpenAI-compatible SSE stream, invoking <paramref name="onToken"/>
    /// for each content delta and returning the accumulated reply.
    /// </summary>
    internal static async Task<string> ReadSseContentAsync(
        HttpResponseMessage response,
        Action<string>? onToken,
        CancellationToken ct)
    {
        var full = new StringBuilder();

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line.AsSpan(5).Trim();
            if (data.SequenceEqual("[DONE]"))
            {
                break;
            }

            try
            {
                using var doc = JsonDocument.Parse(data.ToString());
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    continue;
                }

                var delta = choices[0].TryGetProperty("delta", out var d)
                    ? d
                    : choices[0].GetProperty("message");

                if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    var token = content.GetString() ?? string.Empty;
                    if (token.Length > 0)
                    {
                        full.Append(token);
                        onToken?.Invoke(token);
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed chunks; keep reading.
            }
        }

        return full.ToString();
    }

    internal static string ExtractContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var content)
            ? content.GetString() ?? string.Empty
            : string.Empty;
    }

    internal static string BuildCompletionsUrl(string endpoint)
    {
        var baseUrl = endpoint.TrimEnd('/');
        return baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}/chat/completions";
    }

    private HttpClient CreateClient()
    {
        var client = _handlerFactory is null
            ? new HttpClient()
            : new HttpClient(_handlerFactory());
        client.Timeout = TimeSpan.FromSeconds(60);
        return client;
    }
}
