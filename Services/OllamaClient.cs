using System.Net.Http.Json;
using System.Text.Json;
using IrisAI.Agent.Models;

namespace IrisAI.Agent.Services;

public sealed class OllamaClient
{
	private readonly HttpClient _http;
	private readonly IConfiguration _configuration;
	private readonly DiagnosticsLog _log;

	public OllamaClient(
		HttpClient http,
		IConfiguration configuration,
		DiagnosticsLog log)
	{
		_http = http;
		_configuration = configuration;
		_log = log;
	}

	private string BaseUrl =>
		(_configuration["Ollama:BaseUrl"]
			?? "http://localhost:11434")
		.TrimEnd('/');

	private string Model =>
		_configuration["Ollama:Model"]
		?? "qwen3:4b";

	// How long Ollama keeps the model resident after a request. Keeping it
	// loaded between turns avoids the multi-second reload penalty on every
	// message. Default: 30 minutes.
	private string KeepAlive =>
		_configuration["Ollama:KeepAlive"]
		?? "30m";

	public async Task<OllamaChatResponse> ChatAsync(
		IReadOnlyList<ChatMessage> messages,
		object[] tools,
		CancellationToken cancellationToken)
	{
		var numPredict =
			_configuration.GetValue(
				"Ollama:NumPredict",
				150);

		var numCtx =
			_configuration.GetValue(
				"Ollama:NumCtx",
				2048);

		// The system prompt and latest messages are trimmed by
		// AgentService before reaching this client.
		var payload = new
		{
			model = Model,
			messages,
			tools,
			stream = false,

			// Keep the model resident between turns.
			keep_alive = KeepAlive,

			// Disable the reasoning channel where the model supports it.
			// NOTE: some models/Ollama versions ignore this flag, so
			// AgentService also strips any reasoning that leaks into the
			// content before it reaches the UI.
			think = false,

			options = new
			{
				temperature = 0,

				// Limit response generation (keeps replies concise/fast).
				num_predict = numPredict,

				// Reduce context processing overhead.
				num_ctx = numCtx,

				// Faster, tighter sampling.
				top_p = 0.9,
				top_k = 20
			}
		};

		using var request =
			new HttpRequestMessage(
				HttpMethod.Post,
				BaseUrl + "/api/chat");

		request.Content =
			JsonContent.Create(payload);

		using var response =
			await _http.SendAsync(
				request,
				HttpCompletionOption.ResponseHeadersRead,
				cancellationToken);

		var body =
			await response.Content
				.ReadAsStringAsync(
					cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_log.Error(
				"Ollama",
				$"Chat request returned HTTP {(int)response.StatusCode}.",
				$"Model: {Model}\nResponse: {body}");

			throw new InvalidOperationException(
				$"Ollama returned HTTP " +
				$"{(int)response.StatusCode}: {body}");
		}

		var result =
			JsonSerializer.Deserialize<
				OllamaChatResponse>(
				body,
				new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

		return result
			?? throw new InvalidOperationException(
				"Ollama returned an empty response.");
	}

	/*
     * Load the model into memory at application start so the first user
     * message does not pay the cold-start cost. Failures are ignored -
     * this is a best-effort optimisation.
     */
	public async Task WarmUpAsync(
		CancellationToken cancellationToken)
	{
		try
		{
			var payload = new
			{
				model = Model,
				prompt = "ping",
				stream = false,
				keep_alive = KeepAlive,
				options = new
				{
					num_predict = 1
				}
			};

			using var request =
				new HttpRequestMessage(
					HttpMethod.Post,
					BaseUrl + "/api/generate");

			request.Content =
				JsonContent.Create(payload);

			using var response =
				await _http.SendAsync(
					request,
					HttpCompletionOption.ResponseHeadersRead,
					cancellationToken);

			Console.WriteLine(
				$"Ollama warm-up completed for model '{Model}'. " +
				$"Status: {(int)response.StatusCode}.");
		}
		catch (Exception ex)
		{
			_log.Warn(
				"Ollama",
				$"Warm-up skipped for model '{Model}': {ex.Message}",
				ex.ToString());
		}
	}

	public async Task<bool> IsHealthyAsync(
		CancellationToken cancellationToken)
	{
		try
		{
			using var response =
				await _http.GetAsync(
					BaseUrl + "/api/tags",
					cancellationToken);

			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
