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

	// Captured only so a model that returns reasoning in a dedicated
	// field never has it concatenated into Content. It is never sent
	// to the UI.
	[JsonPropertyName("thinking")]
	public string? Thinking { get; set; }

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

	public PendingCardRequest PendingCard { get; } =
		new PendingCardRequest();

	public bool ConfirmationReceived { get; set; }

	// Which card field the step-by-step flow last asked for, so a bare
	// reply ("Visa Product", "MUHAMMAD USMAN BALOCH") can be understood.
	// One of: "product", "nameOnCard", "deliveryBranch", "confirm", null.
	public string? AwaitingCardField { get; set; }
}

public sealed class PendingCardRequest
{
	public string? NationalId { get; set; }

	public string? CustomerId { get; set; }

	public string? CustomerName { get; set; }

	public string? ProductCode { get; set; }

	public string? ProductName { get; set; }

	public string? AccountNumber { get; set; }

	// Display name, e.g. "Current". Shown in the summary.
	public string? AccountType { get; set; }

	// Numeric account-type id, e.g. "10". Required by the IRIS
	// DebitCards endpoint (both the AccountType field and the
	// composed AccountNumber value).
	public string? AccountTypeId { get; set; }

	public string? CurrencyCode { get; set; }

	// Branch of the selected account. AccountBranchCode is the default
	// delivery branch when the user does not specify one.
	public string? AccountBranch { get; set; }

	public string? AccountBranchCode { get; set; }

	public string? NameOnCard { get; set; }

	public string? DeliveryBranch { get; set; }

	public bool IsReadyToCreate =>
		!string.IsNullOrWhiteSpace(CustomerId)
		&& !string.IsNullOrWhiteSpace(ProductCode)
		&& !string.IsNullOrWhiteSpace(AccountNumber)
		&& !string.IsNullOrWhiteSpace(AccountType)
		&& !string.IsNullOrWhiteSpace(CurrencyCode)
		&& !string.IsNullOrWhiteSpace(NameOnCard)
		&& !string.IsNullOrWhiteSpace(DeliveryBranch);

	public void Set(
		string customerId,
		string productCode,
		string accountNumber,
		string accountType,
		string currencyCode,
		string nameOnCard,
		string deliveryBranch)
	{
		CustomerId = customerId;
		ProductCode = productCode;
		AccountNumber = accountNumber;
		AccountType = accountType;
		CurrencyCode = currencyCode;
		NameOnCard = nameOnCard;
		DeliveryBranch = deliveryBranch;
	}

	public bool Matches(
		string customerId,
		string productCode,
		string accountNumber,
		string accountType,
		string currencyCode,
		string nameOnCard,
		string deliveryBranch)
		=> CustomerId == customerId
			&& ProductCode == productCode
			&& AccountNumber == accountNumber
			&& AccountType == accountType
			&& CurrencyCode == currencyCode
			&& NameOnCard == nameOnCard
			&& DeliveryBranch == deliveryBranch;

	public void Clear()
	{
		NationalId = null;
		CustomerId = null;
		CustomerName = null;
		ResetCardSelections();
	}

	// Clears everything about the card being built but keeps the
	// identified customer. Used when the customer changes.
	public void ResetCardSelections()
	{
		ProductCode = null;
		ProductName = null;
		AccountNumber = null;
		AccountType = null;
		AccountTypeId = null;
		CurrencyCode = null;
		AccountBranch = null;
		AccountBranchCode = null;
		NameOnCard = null;
		DeliveryBranch = null;
	}
}

public sealed record AgentResponse(
	string SessionId,
	string Message,
	bool ToolUsed,
	string? ToolName,
	object? ToolResult);
