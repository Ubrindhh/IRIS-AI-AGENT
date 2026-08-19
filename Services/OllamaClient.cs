using System.Net.Http.Json;
using System.Text.Json;
using IrisAI.Agent.Models;

namespace IrisAI.Agent.Services;

public sealed class OllamaClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public OllamaClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<OllamaChatResponse> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        object[] tools,
        CancellationToken cancellationToken)
    {
        var endpoint = (_configuration["Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/') + "/api/chat";
        var model = _configuration["Ollama:Model"] ?? "qwen3:4b";

        var payload = new
        {
            model,
            messages,
            tools,
            stream = false,
            think = false,
            options = new { temperature = 0 }
        };

        using var response = await _http.PostAsJsonAsync(endpoint, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama returned HTTP {(int)response.StatusCode}: {body}");

        var result = JsonSerializer.Deserialize<OllamaChatResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException("Ollama returned an empty response.");
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = (_configuration["Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/') + "/api/tags";
            using var response = await _http.GetAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
