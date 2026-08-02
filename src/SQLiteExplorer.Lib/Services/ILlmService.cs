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
}
