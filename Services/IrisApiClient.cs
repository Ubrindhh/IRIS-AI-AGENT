using System.Net.Http.Json;

namespace IrisAI.Agent.Services;

public sealed class IrisApiClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public IrisApiClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<object> GetCustomerAsync(string customerId, CancellationToken ct)
        => await PostAsync("IrisApi:Endpoints:GetCustomer", new { customerId }, ct);

    public async Task<object> GetCardStatusAsync(string customerId, CancellationToken ct)
        => await PostAsync("IrisApi:Endpoints:GetCardStatus", new { customerId }, ct);

    public async Task<object> CreateCardAsync(
        string customerId,
        string productCode,
        string? nameOnCard,
        string? deliveryBranch,
        CancellationToken ct)
        => await PostAsync(
            "IrisApi:Endpoints:CreateCard",
            new { customerId, productCode, nameOnCard, deliveryBranch },
            ct);

    private async Task<object> PostAsync(string endpointKey, object body, CancellationToken ct)
    {
        var baseUrl = _configuration["IrisApi:BaseUrl"];
        var path = _configuration[endpointKey];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"IRIS API configuration is missing for {endpointKey}.");

        using var response = await _http.PostAsJsonAsync(
            baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'), body, ct);

        var content = await response.Content.ReadAsStringAsync(ct);

        return new
        {
            success = response.IsSuccessStatusCode,
            statusCode = (int)response.StatusCode,
            response = content
        };
    }
}
