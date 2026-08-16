using System.Threading;
using System.Threading.Tasks;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Minimal LLM abstraction for SQL completion and analysis.
/// The library ships a built-in OpenAI-compatible implementation
/// (<see cref="OpenAiCompatibleLlmService"/>); hosts embedding the explorer
/// (e.g. NVS) can inject their own implementation via
/// <c>MainWindowViewModel.LlmService</c> to reuse the host's LLM configuration.
/// </summary>
public interface ILlmService
{
    /// <summary>Whether the service has enough configuration to send requests.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a single system + user prompt pair and returns the assistant's reply.
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);

    /// <summary>
    /// Sends a single system + user prompt pair, delivering content tokens to
    /// <paramref name="onToken"/> as they arrive and returning the full reply.
    /// Implementations that support streaming should override this; the default
    /// implementation falls back to <see cref="ChatAsync"/> (single delivery).
    /// </summary>
    async Task<string> ChatStreamingAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onToken,
        CancellationToken ct = default)
    {
        var reply = await ChatAsync(systemPrompt, userPrompt, ct).ConfigureAwait(false);
        onToken?.Invoke(reply);
        return reply;
    }
}
