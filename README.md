# IRIS AI Agent

## 1. Overview

IRIS AI Agent is a standalone .NET 9 prototype that provides a natural-language interface to controlled IRIS banking/card capabilities.

The prototype uses:

- ASP.NET Core / .NET 9
- Ollama as the local LLM runtime
- Qwen3:4b as the language model
- Tool calling for controlled business operations
- Session-based conversation state
- Demo adapters for safe presentation/testing

The existing IRIS CMS application remains outside the AI service and is treated as the authoritative business/data layer.

## 2. Prototype Capabilities

The prototype demonstrates:

1. Natural-language user interaction
2. LLM-based intent understanding
3. Native Ollama/Qwen3 tool calling
4. Tool execution by application code
5. Multi-turn conversation state
6. Customer lookup
7. Card status lookup
8. Card creation workflow
9. Missing-information collection
10. Demo-mode business execution

Example:

    User
      ↓
    AI Agent
      ↓
    Qwen3:4b
      ↓
    Tool selection
      ↓
    Tool Executor
      ↓
    Demo IRIS capability
      ↓
    Tool result
      ↓
    Qwen3:4b
      ↓
    Final response

## 3. Architecture

    ┌──────────────────┐
    │       User       │
    └────────┬─────────┘
             │ Natural language
             ▼
    ┌─────────────────────────┐
    │     IRIS AI Agent       │
    │         .NET 9          │
    │                         │
    │ Agent Orchestrator      │
    │ Conversation State      │
    │ Tool Executor           │
    └───────────┬─────────────┘
                │
        ┌───────┴────────┐
        │                │
        ▼                ▼
    ┌─────────┐     ┌─────────────┐
    │ Ollama  │     │ IRIS Tools  │
    │ Qwen3   │     │ Demo Adapter│
    │  :4b    │     └──────┬──────┘
    └─────────┘            │
                           ▼
                    Existing IRIS CMS
                    (future/live adapter)
                           │
                           ▼
                         Oracle

The AI service does not connect directly to Oracle.

## 4. Prerequisites

### Required

- Windows/Linux/macOS
- .NET 9 SDK
- Ollama
- Qwen3:4b model
- Git

Verify .NET:

    dotnet --version

Verify Ollama:

    ollama --version

## 5. Install and Configure Ollama

Install Ollama for your operating system.

Then pull the model:

    ollama pull qwen3:4b

Verify:

    ollama list

You should see:

    qwen3:4b

Verify the Ollama API:

    curl http://localhost:11434/api/tags

## 6. Clone the Repository

    git clone <YOUR-GITHUB-REPOSITORY-URL>
    cd IrisAI-Agent

## 7. Configure the Application

Review `appsettings.json`.

Example:

    {
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "qwen3:4b"
      },
      "IrisApi": {
        "Enabled": false
      }
    }

### Demo mode

For the submitted prototype, keep:

    "IrisApi": {
      "Enabled": false
    }

This prevents accidental changes to a real IRIS environment and allows the complete agent/tool workflow to be demonstrated safely.

The tools return clearly marked demo results.

## 8. Build and Run

From the project directory:

    dotnet restore
    dotnet clean
    dotnet build
    dotnet run

The API will start on the URL displayed by ASP.NET Core.

## 9. Health Check

Use:

    GET /health

Expected result:

    {
      "status": "healthy",
      "ollama": "connected"
    }

## 10. Chat Endpoint

Use:

    POST /api/agent/chat

Example request:

    {
      "sessionId": "",
      "message": "What is the card status for customer 123456789?"
    }

The agent should select:

    get_card_status

and return a response similar to:

    {
      "sessionId": "...",
      "message": "Customer 123456789's card status is Active.",
      "toolUsed": true,
      "toolName": "get_card_status",
      "toolResult": {
        "success": true,
        "source": "demo",
        "customerId": "123456789",
        "cardStatus": "Active"
      }
    }

## 11. Card Creation Demonstration

Start a new conversation:

    {
      "sessionId": "",
      "message": "I want to create a card for customer 123456789 using product 9961."
    }

The agent may first verify the customer and/or request missing information.

Provide:

    Name on card: John Doe
    Delivery branch: Main Branch

The agent should collect the required fields and require confirmation before the write operation.

Use the same `sessionId` for subsequent messages.

Example:

    {
      "sessionId": "<SESSION_ID>",
      "message": "Confirm"
    }

The demo tool returns a simulated card creation result such as:

    {
      "success": true,
      "source": "demo",
      "cardId": "DEMO-...",
      "customerId": "123456789",
      "productCode": "9961",
      "nameOnCard": "John Doe",
      "deliveryBranch": "Main Branch"
    }

## 12. Tool Calling

The prototype exposes controlled business tools rather than allowing the LLM to execute arbitrary HTTP requests or SQL.

Current prototype tools include:

- `get_customer`
- `get_card_status`
- `create_card`

The LLM selects a tool; application code validates and executes it.

The LLM does not receive database credentials and does not directly access Oracle.

## 13. Project Structure

    IrisAI-Agent/
    ├── Models/
    ├── Services/
    ├── Tools/
    ├── Program.cs
    ├── appsettings.json
    └── README.md

Key components:

- `AgentService` — agent orchestration and conversation loop
- `OllamaClient` — communication with Ollama
- `ConversationStore` — session state
- `IrisTools` — controlled business tools
- `IrisApiClient` — boundary for future live IRIS API integration

## 14. Existing IRIS Integration

The prototype intentionally keeps the existing IRIS CMS separate.

The intended production flow is:

    IRIS AI Agent
          ↓
    IRIS REST API
          ↓
    Existing IRIS business services
          ↓
    Existing repositories
          ↓
    Oracle

The prototype currently uses a demo adapter.

A production deployment should enable the IRIS API adapter only after authentication, authorization, validation, audit logging, and environment-specific configuration are implemented and tested.

## 15. Security Considerations

The prototype follows these principles:

- No direct LLM-to-database access
- No arbitrary SQL generated by the model
- No arbitrary URL execution
- Controlled tool definitions
- Application-side argument validation
- Explicit confirmation for sensitive write operations
- Demo mode by default
- No credentials committed to source control

## 16. Limitations

This is a working prototype, not a production banking deployment.

Current limitations include:

- Demo tool data
- In-memory conversation state
- No production identity provider integration
- No distributed session store
- No production audit trail
- No rate limiting
- No RAG/knowledge base
- No MCP server
- No multi-agent orchestration

## 17. Future Roadmap

### Phase 1 — Prototype

- Ollama
- Qwen3:4b
- Agent orchestration
- Tool calling
- Conversation state
- Demo tools

### Phase 2 — IRIS Integration

- Connect tools to existing IRIS APIs
- Authentication
- Authorization
- Audit logging
- Production error handling

### Phase 3 — Enterprise Agent

- Persistent conversation state
- Redis/SQL-backed state
- Observability
- RAG
- Knowledge/document retrieval

### Phase 4 — Standardized Tool Integration

- MCP server
- Reusable IRIS tools
- Multiple AI clients/agents

### Phase 5 — Advanced Agent Architecture

- Workflow orchestration
- Specialized agents
- Human-in-the-loop approvals
- Advanced monitoring and governance

## 18. Submission Demo

Recommended demonstration sequence:

1. Start Ollama.
2. Verify `qwen3:4b`.
3. Start the AI Agent.
4. Call `/health`.
5. Ask for a customer's card status.
6. Show that the agent selects `get_card_status`.
7. Start a card creation request.
8. Show customer verification.
9. Provide missing information.
10. Confirm the card creation.
11. Show the controlled `create_card` tool execution.
12. Show the simulated result.

## 19. License / Internal Prototype

This repository is intended as a prototype/demo submission for the IRIS AI Agent initiative.
