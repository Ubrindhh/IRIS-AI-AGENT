using System.ComponentModel;
using System.Text.Json;
using IrisAI.Agent.Services;

namespace IrisAI.Agent.Tools;

public sealed class IrisTools
{
    private readonly IrisApiClient _irisApi;
    private readonly IConfiguration _configuration;

    public IrisTools(IrisApiClient irisApi, IConfiguration configuration)
    {
        _irisApi = irisApi;
        _configuration = configuration;
    }

    public object[] GetDefinitions() =>
    [
        new
        {
            type = "function",
            function = new
            {
                name = "get_customer",
                description = "Get an existing customer from IRIS by customer identifier.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        customerId = new { type = "string", description = "Existing customer identifier." }
                    },
                    required = new[] { "customerId" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "get_card_status",
                description = "Get the current card status for an existing customer.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        customerId = new { type = "string", description = "Existing customer identifier." }
                    },
                    required = new[] { "customerId" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "create_card",
                description = "Create a card for an existing customer. Only call after the user has explicitly confirmed the pending request.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        customerId = new { type = "string" },
                        productCode = new { type = "string" },
                        nameOnCard = new { type = "string" },
                        deliveryBranch = new { type = "string" }
                    },
                    required = new[] { "customerId", "productCode" }
                }
            }
        }
    ];

    public async Task<object> ExecuteAsync(
        string name,
        string argumentsJson,
        bool confirmationReceived,
        CancellationToken cancellationToken)
    {
        using var json = JsonDocument.Parse(argumentsJson);
        var root = json.RootElement;

        switch (name)
        {
            case "get_customer":
                return await GetCustomerAsync(root, cancellationToken);

            case "get_card_status":
                return await GetCardStatusAsync(root, cancellationToken);

            case "create_card":
                if (!confirmationReceived)
                {
                    return new
                    {
                        success = false,
                        blocked = true,
                        reason = "Explicit user confirmation is required before card creation."
                    };
                }

                return await CreateCardAsync(root, cancellationToken);

            default:
                return new { success = false, error = $"Unknown tool: {name}" };
        }
    }

    private async Task<object> GetCustomerAsync(JsonElement root, CancellationToken ct)
    {
        var id = GetRequired(root, "customerId");

        if (_configuration.GetValue("IrisApi:Enabled", false))
            return await _irisApi.GetCustomerAsync(id, ct);

        return new
        {
            success = true,
            source = "demo",
            customerId = id,
            name = "Demo Customer",
            status = "Active"
        };
    }

    private async Task<object> GetCardStatusAsync(JsonElement root, CancellationToken ct)
    {
        var id = GetRequired(root, "customerId");

        if (_configuration.GetValue("IrisApi:Enabled", false))
            return await _irisApi.GetCardStatusAsync(id, ct);

        return new
        {
            success = true,
            source = "demo",
            customerId = id,
            cardStatus = "Active"
        };
    }

    private async Task<object> CreateCardAsync(JsonElement root, CancellationToken ct)
    {
        var customerId = GetRequired(root, "customerId");
        var productCode = GetRequired(root, "productCode");
        var nameOnCard = GetOptional(root, "nameOnCard");
        var deliveryBranch = GetOptional(root, "deliveryBranch");

        if (_configuration.GetValue("IrisApi:Enabled", false))
        {
            return await _irisApi.CreateCardAsync(
                customerId,
                productCode,
                nameOnCard,
                deliveryBranch,
                ct);
        }

        return new
        {
            success = true,
            source = "demo",
            cardId = "DEMO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            customerId,
            productCode,
            nameOnCard,
            deliveryBranch,
            message = "Card creation simulated successfully."
        };
    }

    private static string GetRequired(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Tool argument '{name}' is required.");

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"Tool argument '{name}' is required.");

        return text.Trim();
    }

    private static string? GetOptional(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
}
