# University Enrollment Assistant

An ASP.NET Core 8 + React (Vite) app that answers free-text questions about
university enrollment data through a single chat-style UI. There is no CRUD
UI or REST resource model — the only API surface is `POST /enrollments/query`,
which resolves natural-language questions against seeded enrollment records
using an LLM (via OpenRouter).

## What you can ask

The assistant answers from a fixed seeded dataset — five programmes
(Computer Science, Mechanical Engineering, Business Administration, Law,
Nursing) across four faculties, with yearly student counts from 2016–2025.
Questions are unconstrained natural language, but they generally fall into
these shapes:

- **Single-value lookups** — e.g. *"How many students were in Business
  Administration in 2023?"* Answered as plain text, no chart.
- **Trends over time** — e.g. *"How has Computer Science enrollment changed
  since 2016?"* Rendered as a **line chart** (one line per programme/faculty
  across years).
- **Comparisons at a point in time** — e.g. *"Compare enrollment across all
  programmes in 2023."* Rendered as a **bar chart**.
- **Multi-row breakdowns** that aren't a clean trend or comparison — e.g.
  *"List all programmes and their enrollment for every year."* Rendered as a
  **table**.
- **Follow-up questions** — e.g. *"What about Nursing?"* after a previous
  question. The conversation history is sent back to the model so it can
  resolve pronouns and implied subjects, but it's instructed to always
  re-derive numbers from the raw data rather than trust prior answers.
- **Out-of-scope questions** — anything not answerable from the seeded data
  (e.g. asking about a programme that doesn't exist) gets a plain-text
  "can't answer that from this data" style response.

The model itself decides `chartType` (`table` / `line` / `bar` / `none`) and
returns the exact supporting rows alongside the answer; the frontend renders
whichever chart type comes back (or nothing, for single-value answers).

## Architecture

```
reactapp1.client/  (React + Vite SPA)
        |
        | POST /enrollments/query  { question, history }
        v
ReactApp1.Server/  (ASP.NET Core 8 minimal API)
        |
        | 1. load all rows from SQLite (Dapper)
        | 2. serialize rows to CSV as context
        | 3. send system prompt + CSV + history + question
        v
   OpenRouter (OpenAI .NET SDK, pointed at openrouter.ai)
        |
        | structured JSON: { answer, chartType, rows }
        v
   EnrollmentQueryResult  -->  chat bubble + optional chart/table
```

**Backend** (`ReactApp1.Server/`)
- No controllers — all HTTP endpoints are minimal APIs mapped directly in
  `Program.cs`. The only route is `POST /enrollments/query`.
- All business logic and the LLM integration live in `EnrollmentService.cs`
  (`IEnrollmentService` / `EnrollmentService`), which also owns SQLite setup
  (Dapper, `Microsoft.Data.Sqlite`) and seeds the `Enrollments` table on
  first run if empty.
- **Query flow** (`AnswerQuestionAsync`): loads all enrollment rows →
  serializes to CSV → builds a system prompt instructing the model to answer
  only from that data and re-derive numbers itself (not trust prior turns) →
  sends the system prompt + conversation history (capped at
  `MaxHistoryTurns`) + the new question to OpenRouter via the official
  `OpenAI` .NET SDK, pointed at OpenRouter's endpoint instead of OpenAI's.
  Uses `ChatResponseFormat.CreateJsonSchemaFormat` (strict mode) so the model
  returns structured JSON: `answer` (text), `chartType`
  (`table`/`line`/`bar`/`none`), and `rows` (the exact supporting records,
  not invented). This is what lets the frontend render a chart/table
  alongside the text answer when there's more than one relevant row.
- This is a RAG-style approach in the loosest sense: the "retrieval" is
  "serialize the whole (small) dataset as CSV" rather than a vector store,
  since the dataset is small enough to fit entirely in context.

**Frontend** (`reactapp1.client/`)
- A single chat component, no routing or state library. `App.jsx` holds the
  displayed chat messages and a separate `historyRef` (only successful
  exchanges, so LLM errors never pollute the conversation context sent back
  to the model).
- `EnrollmentViz.jsx` renders the optional line/bar chart or table from a
  response's `chartType`/`rows` as plain inline SVG/HTML — deliberately no
  charting library dependency.

## Running locally

Backend (`ReactApp1.Server/`):

```
dotnet user-secrets set "OpenRouter:ApiKey" "<key>"   # required for real answers
dotnet run --launch-profile https                     # runs backend + auto-launches Vite via SpaProxy
```

This is the normal way to run the full app — it matches what Visual
Studio's F5 does with `ReactApp1.Server` as the startup project. Without an
API key configured, the query endpoint returns a friendly "not configured"
message instead of erroring.

Frontend (`reactapp1.client/`), only useful with the backend already running:

```
npm run dev     # Vite dev server standalone
npm run build   # production build
npm run lint    # ESLint
```

> **Dev proxy gotcha:** `vite.config.js` resolves its `/enrollments` proxy
> target from `ASPNETCORE_HTTPS_PORT` / `ASPNETCORE_URLS`, which are
> normally injected automatically when SpaProxy spawns `npm run dev` as a
> child process of `dotnet run` on the server project. Running the client
> standalone (or as its own startup project) will likely misconfigure the
> proxy target and cause `502` responses from Vite.

There are no automated tests in this repo.

## Configuration

- `OpenRouter:ApiKey` and `OpenRouter:Model` (default `openai/gpt-4o-mini`)
  in `appsettings.json` / user-secrets / env vars.
- **Never put a real key in `appsettings.json`** — it's tracked in git. Use
  `dotnet user-secrets` locally.
- This repo publishes to Azure Container Apps (see
  `ReactApp1.Server/Properties/PublishProfiles/`). In any deployed
  environment, set the key as a container secret/env var named
  `OPENROUTER__ApiKey` (double underscore maps to the `OpenRouter:ApiKey`
  config section). `dotnet user-secrets` only applies when
  `ASPNETCORE_ENVIRONMENT=Development` and never travels with a publish — a
  freshly deployed instance needs the env var set explicitly, or the
  endpoint responds with an "AI assistant is not configured" message
  instead of erroring.
