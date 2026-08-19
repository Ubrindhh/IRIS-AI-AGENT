# IRIS AI Agent – Working Prototype

A standalone AI Agent prototype for the IRIS CMS. The existing IRIS ASP.NET MVC 5 / .NET Framework 4.8 application is not modified.

## Architecture

```text
User
  |
  v
IRIS AI Agent (.NET 9)
  |
  +-- Agent orchestration / conversation state
  |
  +-- Tool layer
  |     +-- get_customer
  |     +-- get_card_status
  |     +-- create_card
  |
  +-- Ollama HTTP API
          |
          v
       Qwen3:4b
          |
          v
   Optional IRIS REST APIs
          |
          v
       IRIS CMS
```

The prototype keeps AI concerns outside the existing IRIS application. Tools are the controlled boundary between the agent and IRIS.

## Prerequisites

- Windows/Linux/macOS
- .NET 9 SDK
- Ollama
- Qwen3:4b

## 1. Start Ollama

```bash
ollama pull qwen3:4b
ollama run qwen3:4b
```

Verify:

```bash
curl http://localhost:11434/api/tags
```

## 2. Run the agent

From the repository root:

```bash
dotnet restore
dotnet run
```

The API starts on:

```text
http://localhost:5099
```

## 3. Health check

```bash
curl http://localhost:5099/health
```

Expected:

```json
{"status":"healthy","ollama":"connected"}
```

## 4. Chat with the agent

```bash
curl -X POST http://localhost:5099/api/agent/chat ^
  -H "Content-Type: application/json" ^
  -d "{\"message\":\"What is the card status for customer 123456789?\"}"
```

The agent can use the `get_card_status` tool.

## 5. Card creation demo

First request:

```json
{
  "message": "I want to create a card for customer 123456789 using product 9961"
}
```

The agent should collect the required information and ask for confirmation before creation.

Continue using the returned `sessionId`:

```json
{
  "sessionId": "<returned-session-id>",
  "message": "confirm"
}
```

In demo mode the `create_card` tool returns a simulated card ID. No production IRIS data is changed.

## 6. Connect to IRIS

The prototype contains an `IrisApiClient` adapter. Set:

```json
"IrisApi": {
  "Enabled": true,
  "BaseUrl": "http://localhost:8080/IRIS5-CLONE-CMSAPI",
  "Endpoints": {
    "GetCustomer": "<your endpoint>",
    "GetCardStatus": "<your endpoint>",
    "CreateCard": "<your endpoint>"
  }
}
```

Do not expose Oracle directly to the agent. The agent should call controlled IRIS APIs, which continue to enforce existing business rules, validation and authorization.

## Tool safety

- Tool arguments are validated by application code.
- Card creation is blocked until explicit confirmation is received.
- The model is instructed not to invent identifiers or product codes.
- Demo mode is enabled by default.
- Existing IRIS business logic remains the system of record.

## Prototype scope

Implemented:

- Standalone AI Agent API
- Local Ollama/Qwen3 integration
- Conversation sessions
- Function/tool calling
- Customer lookup tool
- Card status tool
- Create-card tool with confirmation gate
- Optional IRIS REST adapter
- Health endpoint
- Demo-safe mode

Future:

- Persistent conversation store
- Authentication/authorization integration
- Audit trail
- More IRIS tools
- MCP server
- RAG over IRIS documentation
- Observability and metrics
- Production deployment

## Technology

- ASP.NET Core / .NET 9
- C#
- Ollama
- Qwen3:4b
- REST
- System.Text.Json

## Submission note

This is intentionally a separate agent service. The existing IRIS CMS is not modified by this prototype.
