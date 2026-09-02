# IRIS AI Agent

A conversational assistant that lets bank staff service customer **debit cards**
through plain language, while every action that touches a card stays
**deterministic, validated, and explicitly confirmed**.

It sits in front of the **IRIS CMS** core-banking API and uses a **local LLM**
(via Ollama) only for free-form conversation — never for the operations that
create or modify a card.

```
Browser (chat UI)  →  /api/agent/chat  →  AgentService
                                            ├─ OllamaClient   → local model
                                            ├─ IrisTools      → IrisApiClient → IRIS CMS API
                                            └─ DiagnosticsLog
```

---

## Contents

- [How it works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Run it](#run-it)
- [Configuration](#configuration)
- [HTTP API](#http-api)
- [The card-creation flow](#the-card-creation-flow)
- [What staff can ask for](#what-staff-can-ask-for)
- [Project structure](#project-structure)
- [Design notes](#design-notes)
- [Known limitations](#known-limitations)

---

## How it works

Every incoming message is handled in this order:

1. **Confirmation / cancellation shortcut** — `yes` / `confirm` / `proceed` or
   `cancel` / `no` are handled directly, with no model call.
2. **Deterministic fast paths** — customer lookup by CNIC, customer search by
   name, list products / accounts / cards / branches / currencies, open a card
   by id, and the whole guided card-creation flow. Each maps to a single IRIS
   call plus a C#-built reply.
3. **Model loop** — only genuinely free-form messages reach the LLM, and its
   reply passes through a sanitizer before display.

The result: the common operations respond in ~1–3 s instead of ~20–30 s,
and the language model is never in the path of a card mutation.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| .NET SDK 9.0 | `dotnet --version` should report `9.x` |
| Ollama | Running locally, with a model pulled |
| IRIS CMS API | A reachable instance (default `http://localhost:8080/IRIS5SPRINT-CMSAPI`) |

Pull the model (matches `appsettings.json`):

```bash
ollama pull qwen2.5:3b-instruct
```

> `qwen2.5:3b-instruct` is a non-reasoning instruct model — fast, and it does
> not emit chain-of-thought. `qwen3:4b` also works but is slower and needs the
> sanitizer working harder.

---

## Run it

```bash
dotnet run
```

Then open **http://localhost:5099**.

The app warms up the model on startup so the first real message is not slowed
by a cold load.

---

## Configuration

`appsettings.json` (override per-environment in `appsettings.Development.json`
or `appsettings.Local.json`):

```jsonc
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen2.5:3b-instruct",
    "NumCtx": 2048,       // context window sent to the model
    "NumPredict": 250,    // max tokens generated (keeps replies short/fast)
    "KeepAlive": "30m"    // how long Ollama keeps the model resident
  },
  "IrisApi": {
    "Enabled": true,
    "BaseUrl": "http://localhost:8080/IRIS5SPRINT-CMSAPI",
    "AuthorizationToken": "bearer 12345",   // sent as: Authorization: Bearer <value>
    "XConsumerCustomId": "43"               // sent as: X-Consumer-Custom-Id: <value>
  }
}
```

> **Do not commit real IRIS credentials.** Use user-secrets or environment
> variables for anything beyond the local mock.

---

## HTTP API

| Method | Path | Purpose |
|---|---|---|
| `GET`  | `/health` | Reports whether Ollama is reachable |
| `POST` | `/api/agent/chat` | Send a message. Body: `{ "sessionId": "<optional>", "message": "..." }` |
| `GET`  | `/api/logs` | Recent diagnostics events (`{ count, entries }`), newest first |
| `POST` | `/api/logs/clear` | Clears the diagnostics buffer |

`/api/agent/chat` response:

```json
{
  "sessionId": "…",
  "message": "the reply shown to the user",
  "toolUsed": true,
  "toolName": "get_customer",
  "toolResult": { }
}
```

Errors are mapped to `503` (dependency unavailable), `504` (timeout), or `500`
(unexpected); the body always carries a safe `message`, and the detail is
captured in `/api/logs`.

---

## The card-creation flow

Guided, correctable, and confirmed. The model is not involved.

```
Staff:  Find customer with CNIC 4220170172913
IRIS :  Customer found: USMAN BALOCH RIND.
        Account 12345678910111121314 (Current), branch 0047.
        Which card product would you like?

Staff:  MasterCard
IRIS :  What name should be printed on the card?

Staff:  TEST USER
IRIS :  Pending debit-card request:
        Customer: USMAN BALOCH RIND …
        Product: MasterCard (7015) …
        Delivery Branch: 0047
        Reply yes / confirm / proceed to create this card, or cancel.

Staff:  delivery branch 123
IRIS :  "123" is not a known delivery branch. Reply with one of these:
        - Saddarr (0047)  - MainBranch (007)  - TPS Branch (1234)

Staff:  1234
IRIS :  Pending debit-card request: … Delivery Branch: 1234 …

Staff:  yes
IRIS :  Card created successfully. Card ID: 643  Card Number: 565656******0130
```

Behaviour worth noting:

- **Fields are resolved and validated** — the product is matched against the
  catalogue (longest-name match wins), the account is auto-selected, the
  delivery branch is checked against the live branch list (name *or* code),
  and the numeric account-type id is used where IRIS expects it.
- **Explicit lookups work mid-flow** — asking "list the customer accounts"
  while the flow waits for a product answers the question without losing the
  flow.
- **Failures keep the request pending** — if IRIS rejects the create, the
  reason is shown and you can correct one field (`product MasterCard`,
  `name on card John Ali`, a branch code) and confirm again.
- **Cancel any time** — `cancel` / `no` discards the pending request but keeps
  the identified customer.

The UI also renders inline **Confirm & create** / **Cancel** buttons on the
review message.

---

## What staff can ask for

| Area | Examples |
|---|---|
| Customers | `Find customer with CNIC …` · `Find customer named USMAN` |
| Accounts | `List the customer accounts` |
| Products | `List card products` |
| Reference | `List branches` · `List currencies` |
| Cards | `List the customer cards` · `Show card 634` |
| Create | `I want to create a debit card` → guided flow |

Underlying IRIS endpoints:

```
GET  /api/v1/customers/search?nationalID={cnic}
GET  /api/v1/customers?customerName=&mobileNumber=&emailAddress=
GET  /api/v1/Products?pageNo=1&pageSize=1000
GET  /api/v1/customers/{cnic}/Accounts
GET  /api/v1/customers/{cnic}/Cards
GET  /api/v1/Cards/{cardId}
GET  /api/v1/branches?page=1&pageSize=1000
GET  /api/v1/currencies
POST /api/v1/customers/{customerId}/DebitCards
```

---

## Project structure

```
Program.cs                     Minimal-API host: DI, endpoints, startup warm-up
Services/
  AgentService.cs              Orchestration — fast-path router, guided card
                               flow, model loop, response sanitizer
  OllamaClient.cs              Local-LLM transport, warm-up, keep-alive
  IrisApiClient.cs             Typed IRIS REST calls + request/response logging
  ConversationStore.cs         In-memory session store
  DiagnosticsLog.cs            In-memory ring buffer of errors / warnings
Tools/
  IrisTools.cs                 Tool definitions + handlers (10 tools)
Models/
  AgentModels.cs               Chat, session, and pending-card models
  IrisModels.cs                IRIS request / response DTOs
wwwroot/
  index.html · css/chat.css · js/chat.js    Single-page chat UI
docs/
  IRIS CMS API.postman_collection.json      Endpoint catalogue
```

---

## Design notes

- **Deterministic-first.** The LLM decides nothing about money or cards. It
  only helps with conversational phrasing when no fast path applies.
- **`create_card` only stages.** Actual creation is a separate code path
  (`CreateConfirmedCardAsync`) gated on an explicit confirmation flag.
- **Response sanitizer.** Small local models leak reasoning even with
  thinking disabled. A multi-stage cleaner strips reasoning, planning,
  tool-selection narration and any echo of the system prompt; a final gate
  replaces the reply entirely if it still reads like deliberation.
- **Guided flow state.** `AwaitingCardField` on the session lets a bare reply
  ("MasterCard", "1234") be understood as the answer to the last question.
- **Graceful failure.** A rejected card is not thrown away — the pending
  request survives so a single field can be corrected.
- **Diagnostics built in.** Every IRIS / model / tool failure is recorded with
  full detail and is viewable from the UI (the *Diagnostics* panel) and at
  `/api/logs`.
- **Session persistence.** The browser keeps the transcript in `localStorage`,
  so a refresh does not lose context.

---

## Known limitations

| Area | Detail |
|---|---|
| Auth | `/api/agent/chat` has no authentication yet |
| Sessions | Held in memory — lost on restart, not shared across instances |
| Secrets | IRIS credentials live in `appsettings.json` |
| Write operations | Block / replace / renew a card, create customer & account are
  scoped from the Postman collection but not implemented — they need
  request-body examples from IRIS |
| Model | Any small local model can still occasionally produce an odd reply for
  open-ended questions; the sanitizer degrades those to a safe fallback |
