using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IrisAI.Agent.Models;

namespace IrisAI.Agent.Services;

public sealed class IrisApiClient
{
	private readonly HttpClient _http;
	private readonly IConfiguration _configuration;
	private readonly DiagnosticsLog _log;

	private readonly JsonSerializerOptions _jsonOptions =
		new()
		{
			PropertyNameCaseInsensitive = true
		};

	public IrisApiClient(
		HttpClient http,
		IConfiguration configuration,
		DiagnosticsLog log)
	{
		_http = http;
		_configuration = configuration;
		_log = log;
	}

	public async Task<IrisCustomer?> GetCustomerAsync(
		string nationalId,
		CancellationToken ct)
	{
		var url = BuildUrl(
			"/api/v1/customers/search?nationalID=" +
			Uri.EscapeDataString(nationalId));

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisCustomerSearchResponse>(
				request,
				ct);

		return response?.Values.FirstOrDefault();
	}

	public async Task<List<IrisProduct>> GetProductsAsync(
		CancellationToken ct)
	{
		var url =
			BuildUrl("/api/v1/Products?pageNo=1&pageSize=1000");

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisProductsResponse>(
				request,
				ct);

		return response?.Items ?? new List<IrisProduct>();
	}

	// IRIS resolves the customer's accounts by National ID (CNIC).
	public async Task<List<IrisAccount>> GetCustomerAccountsAsync(
		string nationalId,
		CancellationToken ct)
	{
		var url = BuildUrl(
			"/api/v1/customers/" +
			Uri.EscapeDataString(nationalId) +
			"/Accounts");

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisAccountsResponse>(
				request,
				ct);

		return response?.Values ?? new List<IrisAccount>();
	}

	// GET /api/v1/customers/{cnic}/Cards - all cards issued to the customer.
	public async Task<List<IrisCard>> GetCustomerCardsAsync(
		string nationalId,
		CancellationToken ct)
	{
		var url = BuildUrl(
			"/api/v1/customers/" +
			Uri.EscapeDataString(nationalId) +
			"/Cards");

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisCardsResponse>(request, ct);

		return response?.Values ?? new List<IrisCard>();
	}

	// GET /api/v1/Cards/{cardId} - single card detail.
	public async Task<IrisCard?> GetCardAsync(
		string cardId,
		CancellationToken ct)
	{
		var url = BuildUrl(
			"/api/v1/Cards/" +
			Uri.EscapeDataString(cardId));

		using var request =
			CreateRequest(HttpMethod.Get, url);

		return await SendAsync<IrisCard>(request, ct);
	}

	// GET /api/v1/branches
	public async Task<List<IrisBranch>> GetBranchesAsync(
		CancellationToken ct)
	{
		var url =
			BuildUrl("/api/v1/branches?page=1&pageSize=1000");

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisBranchesResponse>(request, ct);

		return response?.Values ?? new List<IrisBranch>();
	}

	// GET /api/v1/currencies
	public async Task<List<IrisCurrency>> GetCurrenciesAsync(
		CancellationToken ct)
	{
		var url = BuildUrl("/api/v1/currencies");

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisCurrenciesResponse>(request, ct);

		return response?.Values ?? new List<IrisCurrency>();
	}

	// GET /api/v1/customers?customerName=&mobileNumber=&emailAddress=
	public async Task<List<IrisCustomer>> SearchCustomersAsync(
		string? customerName,
		string? mobileNumber,
		string? emailAddress,
		CancellationToken ct)
	{
		var query = new List<string> { "page=1", "pageSize=25" };

		if (!string.IsNullOrWhiteSpace(customerName))
		{
			query.Add("customerName=" + Uri.EscapeDataString(customerName));
		}

		if (!string.IsNullOrWhiteSpace(mobileNumber))
		{
			query.Add("mobileNumber=" + Uri.EscapeDataString(mobileNumber));
		}

		if (!string.IsNullOrWhiteSpace(emailAddress))
		{
			query.Add("emailAddress=" + Uri.EscapeDataString(emailAddress));
		}

		var url = BuildUrl(
			"/api/v1/customers?" + string.Join("&", query));

		using var request =
			CreateRequest(HttpMethod.Get, url);

		var response =
			await SendAsync<IrisCustomerSearchResponse>(request, ct);

		return response?.Values ?? new List<IrisCustomer>();
	}

	public async Task<IrisCreateCardResponse?> CreateCardAsync(
		string customerId,
		CreateDebitCardRequest requestBody,
		CancellationToken ct)
	{
		var url = BuildUrl(
			"/api/v1/customers/" +
			Uri.EscapeDataString(customerId) +
			"/DebitCards");

		using var request =
			CreateRequest(HttpMethod.Post, url);

		request.Content =
			new StringContent(
				JsonSerializer.Serialize(
					requestBody,
					_jsonOptions),
				Encoding.UTF8,
				"application/json");

		return await SendAsync<IrisCreateCardResponse>(
			request,
			ct);
	}

	private HttpRequestMessage CreateRequest(
		HttpMethod method,
		string url)
	{
		var request =
			new HttpRequestMessage(method, url);

		request.Headers.Accept.Add(
			new MediaTypeWithQualityHeaderValue(
				"application/json"));

		var authorizationToken =
			_configuration["IrisApi:AuthorizationToken"];

		if (!string.IsNullOrWhiteSpace(
				authorizationToken))
		{
			request.Headers.Authorization =
				new AuthenticationHeaderValue(
					"Bearer",
					authorizationToken.Trim());
		}

		var consumerCustomId =
			_configuration["IrisApi:XConsumerCustomId"];

		if (!string.IsNullOrWhiteSpace(
				consumerCustomId))
		{
			request.Headers.Add(
				"X-Consumer-Custom-Id",
				consumerCustomId.Trim());
		}

		return request;
	}

	private async Task<T?> SendAsync<T>(
		HttpRequestMessage request,
		CancellationToken ct)
	{
		var stopwatch =
			Stopwatch.StartNew();

		try
		{
			await LogRequestAsync(request);

			using var response =
				await _http.SendAsync(
					request,
					ct);

			var responseBody =
				response.Content == null
					? string.Empty
					: await response.Content
						.ReadAsStringAsync(ct);

			stopwatch.Stop();

			LogResponse(
				response,
				responseBody,
				stopwatch.ElapsedMilliseconds);

			if (!response.IsSuccessStatusCode)
			{
				var summary =
					$"{request.Method} {request.RequestUri?.AbsolutePath} " +
					$"returned HTTP {(int)response.StatusCode}.";

				var detail =
					$"URL: {request.RequestUri}\n" +
					$"Elapsed: {stopwatch.ElapsedMilliseconds} ms\n" +
					$"Response: {responseBody}";

				// 404 is usually just "record not found" - a normal outcome.
				if ((int)response.StatusCode == 404)
				{
					_log.Warn("IRIS API", summary, detail);
				}
				else
				{
					_log.Error("IRIS API", summary, detail);
				}

				throw new InvalidOperationException(
					$"IRIS API returned HTTP " +
					$"{(int)response.StatusCode}: " +
					responseBody);
			}

			if (string.IsNullOrWhiteSpace(
					responseBody))
			{
				return default;
			}

			return JsonSerializer.Deserialize<T>(
				responseBody,
				_jsonOptions);
		}
		catch (InvalidOperationException)
		{
			// Already recorded above (non-success HTTP status).
			throw;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			_log.Error(
				"IRIS API",
				$"{request.Method} {request.RequestUri?.AbsolutePath} failed: {ex.Message}",
				$"URL: {request.RequestUri}\n" +
				$"Elapsed: {stopwatch.ElapsedMilliseconds} ms\n" +
				$"{ex}");

			Console.WriteLine();
			Console.WriteLine(
				"========== IRIS API EXCEPTION ==========");

			Console.WriteLine(
				$"URL: {request.RequestUri}");

			Console.WriteLine(
				$"Method: {request.Method}");

			Console.WriteLine(
				$"Elapsed: {stopwatch.ElapsedMilliseconds} ms");

			Console.WriteLine(
				$"Exception Type: {ex.GetType().FullName}");

			Console.WriteLine(
				$"Message: {ex.Message}");

			Console.WriteLine(
				"========================================");

			throw;
		}
	}

	private async Task LogRequestAsync(
		HttpRequestMessage request)
	{
		Console.WriteLine();
		Console.WriteLine(
			"========== IRIS API REQUEST ==========");

		Console.WriteLine(
			$"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

		Console.WriteLine(
			$"Method: {request.Method}");

		Console.WriteLine(
			$"URL: {request.RequestUri}");

		Console.WriteLine();
		Console.WriteLine("Headers:");

		foreach (var header in request.Headers)
		{
			var value =
				string.Join(", ", header.Value);

			Console.WriteLine(
				$"{header.Key}: " +
				$"{MaskHeader(header.Key, value)}");
		}

		if (request.Content != null)
		{
			Console.WriteLine();
			Console.WriteLine("Request Body:");

			var body =
				await request.Content
					.ReadAsStringAsync();

			Console.WriteLine(body);
		}
		else
		{
			Console.WriteLine();
			Console.WriteLine(
				"Request Body: <EMPTY>");
		}

		Console.WriteLine(
			"======================================");
	}

	private static void LogResponse(
		HttpResponseMessage response,
		string responseBody,
		long elapsedMilliseconds)
	{
		Console.WriteLine();
		Console.WriteLine(
			"========== IRIS API RESPONSE =========");

		Console.WriteLine(
			$"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

		Console.WriteLine(
			$"Status Code: {(int)response.StatusCode}");

		Console.WriteLine(
			$"Status: {response.StatusCode}");

		Console.WriteLine(
			$"Elapsed Time: {elapsedMilliseconds} ms");

		Console.WriteLine();
		Console.WriteLine(
			"Response Body:");

		Console.WriteLine(
			string.IsNullOrWhiteSpace(responseBody)
				? "<EMPTY>"
				: responseBody);

		Console.WriteLine(
			"======================================");
	}

	private string BuildUrl(
		string path)
	{
		var baseUrl =
			_configuration["IrisApi:BaseUrl"];

		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			throw new InvalidOperationException(
				"IrisApi:BaseUrl is not configured.");
		}

		return
			baseUrl.TrimEnd('/') +
			"/" +
			path.TrimStart('/');
	}

	private static string MaskHeader(
		string headerName,
		string value)
	{
		if (headerName.Equals(
				"Authorization",
				StringComparison.OrdinalIgnoreCase))
		{
			return "Bearer [MASKED]";
		}

		return value;
	}
}