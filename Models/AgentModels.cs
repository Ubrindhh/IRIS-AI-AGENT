using System.Text.Json;
using System.Text.Json.Serialization;

namespace IrisAI.Agent.Models;

public sealed class ChatMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string? Content { get; set; }

	[JsonPropertyName("tool_name")]
	public string? ToolName { get; set; }

	[JsonPropertyName("tool_calls")]
	public List<OllamaToolCall>? ToolCalls { get; set; }
}

public sealed class OllamaChatResponse
{
	[JsonPropertyName("message")]
	public OllamaMessage? Message { get; set; }

	[JsonPropertyName("done")]
	public bool Done { get; set; }
}

public sealed class OllamaMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string? Content { get; set; }

	[JsonPropertyName("tool_name")]
	public string? ToolName { get; set; }

	[JsonPropertyName("tool_calls")]
	public List<OllamaToolCall>? ToolCalls { get; set; }
}

public sealed class OllamaToolCall
{
	[JsonPropertyName("id")]
	public string? Id { get; set; }

	[JsonPropertyName("function")]
	public OllamaFunction? Function { get; set; }
}

public sealed class OllamaFunction
{
	[JsonPropertyName("index")]
	public int Index { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("arguments")]
	public JsonElement Arguments { get; set; }
}

public sealed class AgentSession
{
	public List<ChatMessage> Messages { get; } = new();

	public bool ConfirmationReceived { get; set; }
}

public sealed record AgentResponse(
	string SessionId,
	string Message,
	bool ToolUsed,
	string? ToolName,
	object? ToolResult);