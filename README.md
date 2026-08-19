# IRIS AI Agent

## AI-Powered Banking & Card Assistant

IRIS AI Agent is a standalone .NET 9 AI Agent prototype that provides a natural-language interface for banking and card-related operations.

The agent uses **Ollama + Qwen3:4b** for local AI processing and controlled business tools for IRIS capabilities.

The prototype includes a browser-based chatbot so users can interact with the AI Agent without using Postman.

## 1. Overview

The objective is to introduce an AI-powered interaction layer around the existing IRIS CMS without modifying the existing IRIS application.

The AI Agent can:

- Understand natural-language requests
- Maintain conversation sessions
- Select business tools using native LLM tool calling
- Execute controlled application tools
- Retrieve customer information
- Retrieve card status
- Collect missing information
- Handle card-creation workflows
- Require confirmation before sensitive write operations
- Return natural-language responses

The existing IRIS CMS remains the authoritative business and data layer.

## 2. Architecture Principle

The AI Agent does **not** directly access Oracle.

```text
User
  |
  v
Browser Chatbot
  |
  v
IRIS AI Agent (.NET 9)
  |
  +--------------------+
  |                    |
  v                    v
Ollama              IRIS Tools
Qwen3:4b                |
                        v
                 IRIS API Adapter
                        |
                        v
                 Existing IRIS CMS
                        |
                        v
                      Oracle
```

The current prototype uses a **Demo Adapter** for business operations.

## 3. Prototype Features

### Browser Chatbot

Users interact with the AI Agent using natural language instead of manually creating API requests in Postman.

Example:

```text
What is the card status for customer 123456789?
```

Response:

```text
Customer 123456789's card status is Active.
```

### Local AI

```text
Ollama
   |
   v
Qwen3:4b
```

No OpenAI API key is required for the prototype.

### Native Tool Calling

```text
User
 |
 v
Qwen3:4b
 |
 | get_card_status(customerId)
 v
Tool Executor
 |
 v
Tool Result
 |
 v
Qwen3:4b
 |
 v
Final Answer
```

The model selects the tool; application code executes it.

## 4. Available Tools

### get_customer

Retrieves customer information.

Example:

```text
get_customer(customerId = "123456789")
```

Example demo result:

```json
{
  "success": true,
  "source": "demo",
  "customerId": "123456789",
  "name": "Demo Customer",
  "status": "Active"
}
```

### get_card_status

Retrieves the current card status.

```text
get_card_status(customerId = "123456789")
```

Example:

```json
{
  "success": true,
  "source": "demo",
  "customerId": "123456789",
  "cardStatus": "Active"
}
```

### create_card

Controlled card creation operation.

Required information:

```text
Customer ID
Product Code
Name on Card
Delivery Branch
```

Example demo result:

```json
{
  "success": true,
  "source": "demo",
  "cardId": "DEMO-20260819071020",
  "customerId": "123456789",
  "productCode": "9961",
  "nameOnCard": "John Doe",
  "deliveryBranch": "Main Branch"
}
```

## 5. Agentic Conversation Flow

```text
User:
I want to create a card for customer 123456789 using product 9961.
        |
        v
AI Agent
        |
        v
get_customer()
        |
        v
Collect missing information
        |
        v
User:
John Doe, Main Branch.
        |
        v
AI Agent:
Please confirm the card creation.
        |
        v
User:
Confirm.
        |
        v
create_card()
        |
        v
Tool Result
        |
        v
AI Agent:
Your card has been successfully created.
```

This demonstrates a multi-turn, tool-using AI Agent rather than a simple chatbot.

## 6. Technology Stack

| Component | Technology |
|---|---|
| Frontend | HTML / CSS / JavaScript |
| Backend | ASP.NET Core / .NET 9 |
| Language | C# |
| LLM Runtime | Ollama |
| Model | Qwen3:4b |
| Communication | HTTP / REST |
| Agent State | In-memory conversation store |
| Business Tools | C# application services |
| Existing System | IRIS CMS |
| Existing Database | Oracle |

## 7. Prerequisites

Install:

- .NET 9 SDK
- Ollama
- Git
- Modern web browser

Verify:

```cmd
dotnet --version
ollama --version
```

## 8. Install Ollama

Install Ollama, then pull the model:

```cmd
ollama pull qwen3:4b
```

Verify:

```cmd
ollama list
```

You should see:

```text
qwen3:4b
```

Verify the Ollama API:

```cmd
curl http://localhost:11434/api/tags
```

## 9. Clone the Repository

```cmd
git clone <YOUR-GITHUB-REPOSITORY-URL>
cd IrisAI-Agent
```

## 10. Project Structure

```text
IrisAI-Agent/
|
+-- Models/
|
+-- Services/
|   +-- AgentService.cs
|   +-- OllamaClient.cs
|   +-- ConversationStore.cs
|   +-- IrisApiClient.cs
|
+-- Tools/
|   +-- IrisTools.cs
|
+-- wwwroot/
|   +-- index.html
|   +-- css/
|   |   +-- chat.css
|   +-- js/
|       +-- chat.js
|
+-- Program.cs
+-- appsettings.json
+-- IrisAI.Agent.csproj
+-- README.md
```

## 11. Configuration

Example `appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:4b"
  },
  "IrisApi": {
    "Enabled": false
  }
}
```

## 12. Demo Mode

The submitted prototype uses:

```json
"IrisApi": {
  "Enabled": false
}
```

Business tools therefore return simulated/demo responses.

This prevents accidental changes to a real IRIS environment.

Demo responses contain:

```json
"source": "demo"
```

## 13. Build and Run

From the project directory:

```cmd
dotnet restore
dotnet clean
dotnet build
dotnet run
```

Open the URL displayed by ASP.NET Core, for example:

```text
http://localhost:5099/
```

## 14. Health Check

The application exposes:

```http
GET /health
```

Example:

```text
http://localhost:5099/health
```

Expected response:

```json
{
  "name": "IRIS AI Agent",
  "status": "running",
  "model": "qwen3:4b"
}
```

Ollama connectivity:

```http
GET /health/ollama
```

Expected:

```json
{
  "status": "healthy",
  "ollama": "connected"
}
```

## 15. Chat API

The browser chatbot calls:

```http
POST /api/agent/chat
```

Example:

```json
{
  "sessionId": "",
  "message": "What is the card status for customer 123456789?"
}
```

The application creates a session when `sessionId` is empty.

Subsequent messages use the same session ID.

## 16. Card Status Demo

Enter:

```text
What is the card status for customer 123456789?
```

The agent selects:

```text
get_card_status
```

with:

```text
customerId = 123456789
```

Expected response:

```text
Customer 123456789's card status is Active.
```

## 17. Card Creation Demo

Enter:

```text
I want to create a card for customer 123456789 using product 9961.
```

The agent verifies the customer and collects missing information.

Provide:

```text
Name on card: John Doe
Delivery branch: Main Branch
```

The agent should require confirmation before the write operation.

Then:

```text
Confirm
```

The agent invokes:

```text
create_card
```

The demo tool returns a simulated result.

## 18. Security Model

The LLM does not receive:

- Oracle credentials
- Direct database access
- Arbitrary SQL access
- Arbitrary HTTP execution
- Arbitrary URLs
- Arbitrary HTTP headers
- Arbitrary code execution

Instead:

```text
LLM
 |
 | Tool Request
 v
Application Tool Executor
 |
 | Validation
 v
Business/API Boundary
 |
 v
IRIS
 |
 v
Oracle
```

## 19. Existing IRIS Integration

The intended production architecture is:

```text
Browser
   |
   v
IRIS AI Agent
   |
   v
AI Tool
   |
   v
Existing IRIS REST API
   |
   v
Existing IRIS Business Logic
   |
   v
Existing Repository/Data Layer
   |
   v
Oracle
```

The AI Agent is an additional capability, not a replacement for IRIS.

Existing IRIS remains responsible for:

- Business rules
- Validation
- Authorization
- Data access
- Transaction processing
- Database operations

## 20. Why Ollama + Qwen3?

Ollama allows the LLM to run locally.

Benefits:

- No external LLM API key required
- Local development
- Lower prototype cost
- Local data processing
- Easy model replacement
- Native tool-calling support

Qwen3:4b is a lightweight local model suitable for the prototype.

## 21. Why a Separate AI Agent?

The AI Agent is intentionally separated from the existing IRIS application.

Benefits:

- Existing IRIS code remains isolated
- AI development can evolve independently
- LLM provider can be changed later
- Tools can be added independently
- AI-specific security policies can be implemented separately
- Existing APIs remain the business boundary

## 22. Current Prototype Scope

### Implemented

- Standalone AI Agent
- .NET 9
- Browser chatbot
- Ollama integration
- Qwen3:4b
- Native tool calling
- Conversation sessions
- Customer lookup
- Card status lookup
- Card creation tool
- Multi-turn conversation
- Demo business adapter
- Tool execution result handling
- User-facing response cleanup

### Not Production Ready

- Production authentication
- Production authorization
- Persistent conversation storage
- Distributed session state
- Production audit logging
- Rate limiting
- Full production monitoring
- Live IRIS business API integration
- Enterprise governance

## 23. Future Roadmap

### Phase 1 — Working Prototype

```text
Ollama
+
Qwen3:4b
+
AI Agent
+
Tool Calling
+
Browser Chatbot
```

**Status: COMPLETED**

### Phase 2 — IRIS Integration

Connect tools to actual IRIS APIs.

```text
AI Agent
    |
    v
IRIS REST API
    |
    v
Existing IRIS Business Logic
```

Add:

- Authentication
- Authorization
- Validation
- Audit logging

### Phase 3 — Enterprise AI

- Persistent conversation memory
- Redis/SQL-backed sessions
- Observability
- Metrics
- Distributed deployment
- Production monitoring

### Phase 4 — Knowledge & Tool Standardization

Potential additions:

- RAG
- Enterprise document search
- MCP
- Reusable IRIS tools
- Knowledge base integration

### Phase 5 — Advanced Agent Architecture

Potential additions:

- Multiple specialized agents
- Workflow orchestration
- Human-in-the-loop approvals
- AI governance
- Agent monitoring
- Enterprise policy enforcement

## 24. Recommended Demonstration

### Demonstration 1 — Read Operation

Ask:

```text
What is the card status for customer 123456789?
```

Show:

```text
User
 ↓
Qwen3
 ↓
get_card_status
 ↓
Tool Result
 ↓
Final Answer
```

### Demonstration 2 — Business Workflow

Ask:

```text
I want to create a card for customer 123456789 using product 9961.
```

Then:

```text
John Doe, Main Branch
```

Then:

```text
Confirm
```

Show:

```text
get_customer
      ↓
collect information
      ↓
confirmation
      ↓
create_card
      ↓
demo result
```

## 25. Submission Components

### Working Prototype

GitHub repository containing the complete source code.

### Documentation

This README provides:

- Installation
- Configuration
- Ollama setup
- Build/run instructions
- Chatbot usage
- API usage
- Tool-calling explanation
- Architecture
- Security considerations
- Limitations
- Future roadmap

### Presentation

The presentation deck covers:

- Problem
- Objective
- Architecture
- Agent workflow
- Technology stack
- Working demonstration
- Security model
- Prototype scope
- Future roadmap

## 26. Final Architecture

```text
                         USER
                           |
                           v
                  +----------------+
                  | Browser Chatbot|
                  +-------+--------+
                          |
                          v
                  +----------------+
                  | IRIS AI Agent  |
                  |    .NET 9      |
                  +-------+--------+
                          |
             +------------+------------+
             |                         |
             v                         v
       +-----------+             +------------+
       |  Ollama   |             | IRIS Tools |
       | Qwen3:4b  |             +------+-----+
       +-----------+                    |
                                        v
                                +---------------+
                                | Existing IRIS |
                                | APIs / CMS    |
                                +-------+-------+
                                        |
                                        v
                                     Oracle
```

## Core Principle

> **AI handles natural-language understanding and tool selection. Existing IRIS services remain responsible for business logic and data.**
