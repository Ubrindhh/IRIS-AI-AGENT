using IrisAI.Agent.Services;
using IrisAI.Agent.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddHttpClient<IrisApiClient>();
builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton<IrisTools>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "IRIS AI Agent",
    status = "running",
    model = app.Configuration["Ollama:Model"] ?? "qwen3:4b"
}));

app.MapGet("/health", async (OllamaClient ollama, CancellationToken ct) =>
{
    var healthy = await ollama.IsHealthyAsync(ct);
    return healthy
        ? Results.Ok(new { status = "healthy", ollama = "connected" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/api/agent/chat", async (
    ChatRequest request,
    AgentService agent,
    CancellationToken ct) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(new { error = "message is required" });

    var result = await agent.RunAsync(
        request.SessionId,
        request.Message.Trim(),
        ct);

    return Results.Ok(result);
});

app.Run();

public sealed record ChatRequest(string? SessionId, string Message);
