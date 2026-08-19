using System.Collections.Concurrent;
using IrisAI.Agent.Models;

namespace IrisAI.Agent.Services;

public sealed class ConversationStore
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public AgentSession GetOrCreate(string sessionId)
        => _sessions.GetOrAdd(sessionId, _ => new AgentSession());
}
