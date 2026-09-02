using IrisAI.Agent.Services;
using IrisAI.Agent.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<OllamaClient>(client =>
{
	client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<IrisApiClient>(client =>
{
	client.Timeout = TimeSpan.FromMinutes(1);
});

builder.Services.AddSingleton<DiagnosticsLog>();
builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton<IrisTools>();

var app = builder.Build();

// Best-effort: load the Ollama model at startup so the first user
// message does not pay the cold-start cost.
_ = Task.Run(async () =>
{
	using var scope = app.Services.CreateScope();
	var ollama = scope.ServiceProvider.GetRequiredService<OllamaClient>();
	await ollama.WarmUpAsync(CancellationToken.None);
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async (
	OllamaClient ollama,
	CancellationToken ct) =>
{
	var healthy = await ollama.IsHealthyAsync(ct);

	return healthy
		? Results.Ok(new
		{
			status = "healthy",
			ollama = "connected"
		})
		: Results.Json(
			new
			{
				status = "unhealthy",
				ollama = "disconnected"
			},
			statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/logs", (DiagnosticsLog log) =>
	Results.Ok(new
	{
		count = log.Count,
		entries = log.Recent(200)
	}));

app.MapPost("/api/logs/clear", (DiagnosticsLog log) =>
{
	log.Clear();
	return Results.Ok(new { cleared = true });
});

app.MapPost("/api/agent/chat", async (
	ChatRequest request,
	AgentService agent,
	DiagnosticsLog log,
	CancellationToken ct) =>
{
	if (request is null)
	{
		return Results.BadRequest(new
		{
			error = "Request body is required."
		});
	}

	if (string.IsNullOrWhiteSpace(request.Message))
	{
		return Results.BadRequest(new
		{
			error = "Message is required."
		});
	}

	try
	{
		var result = await agent.RunAsync(
			request.SessionId,
			request.Message.Trim(),
			ct);

		return Results.Ok(result);
	}
	catch (OperationCanceledException) when (ct.IsCancellationRequested)
	{
		// Client disconnected before the response was ready. Not an error.
		return Results.StatusCode(499);
	}
	catch (TaskCanceledException) when (!ct.IsCancellationRequested)
	{
		log.Error(
			"Agent",
			"Request timed out waiting for the AI service.",
			$"Message: {request.Message}");

		return Results.Json(
			new
			{
				error = "The request timed out.",
				message = "The AI service took too long to respond."
			},
			statusCode: StatusCodes.Status504GatewayTimeout);
	}
	catch (HttpRequestException ex)
	{
		log.Error(
			"Agent",
			"A dependency (Ollama or IRIS) is unreachable.",
			ex.ToString());

		return Results.Json(
			new
			{
				error = "A required service is unavailable.",
				message = "Please try again."
			},
			statusCode: StatusCodes.Status503ServiceUnavailable);
	}
	catch (Exception ex)
	{
		log.Error(
			"Agent",
			"Unhandled error while processing the request.",
			ex.ToString());

		return Results.Json(
			new
			{
				error = "Unable to process the request.",
				message = "An unexpected error occurred."
			},
			statusCode: StatusCodes.Status500InternalServerError);
	}
});

app.Run();

public sealed record ChatRequest(
	string? SessionId,
	string Message);