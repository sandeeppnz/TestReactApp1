# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

An ASP.NET Core 8 + React (Vite) app: a "University Enrollment Assistant" chat UI backed by a single natural-language query endpoint. There is no CRUD UI — the only API surface is `POST /enrollments/query`, which answers free-text questions about seeded enrollment data (Year, Programme, Faculty, StudentCount) using an LLM via OpenRouter, with a RAG-style approach (the full dataset is serialized to CSV and passed as context).

## Commands

Backend (`ReactApp1.Server/`):
- `dotnet build` — build the server project.
- `dotnet run --launch-profile https` — run the backend + auto-launch the Vite dev server via SpaProxy (this is the normal way to run the full app; matches what Visual Studio's F5 does with `ReactApp1.Server` as the startup project).
- `dotnet user-secrets set "OpenRouter:ApiKey" "<key>"` — required for the LLM to actually respond (see Configuration below). Without it, the query endpoint returns a friendly "not configured" message instead of erroring.

Frontend (`reactapp1.client/`):
- `npm run dev` — Vite dev server standalone (only useful with the backend already running and reachable — see Dev proxy gotcha below).
- `npm run build` — production build.
- `npm run lint` — ESLint (flat config, `eslint.config.js`).

There are no automated tests in this repo (neither project has a test suite configured).

## Architecture

**Backend is a single service, no controllers.** All HTTP endpoints are minimal APIs mapped directly in `ReactApp1.Server/Program.cs`. Business logic and the OpenRouter/LLM integration live in `ReactApp1.Server/EnrollmentService.cs` (`IEnrollmentService` / `EnrollmentService`), which also owns SQLite setup (Dapper, `Microsoft.Data.Sqlite`) and seeds the `Enrollments` table on first run if empty.

**Query flow (`AnswerQuestionAsync`)**: loads all enrollment rows → serializes to CSV → builds a system prompt instructing the model to answer only from that data and to re-derive numbers itself (not trust prior turns) → sends the system prompt + conversation history (`ChatTurn` list, capped at `MaxHistoryTurns`) + the new question to OpenRouter via the official `OpenAI` .NET SDK, pointed at OpenRouter's endpoint (`https://openrouter.ai/api/v1`) instead of OpenAI's. Uses `ChatResponseFormat.CreateJsonSchemaFormat` (strict mode) so the model returns structured JSON: `answer` (text), `chartType` (`table`/`line`/`bar`/`none`), and `rows` (the exact supporting records, not invented). This lets the frontend render a chart/table alongside the text answer when there's more than one relevant row.

**Frontend is a single chat component**, no routing/state library. `App.jsx` holds chat messages and a separate `historyRef` (only successful exchanges, so LLM errors never pollute conversation context sent back to the model). `EnrollmentViz.jsx` renders the optional line/bar chart or table from a response's `chartType`/`rows` as plain inline SVG/HTML — deliberately no charting library dependency.

**Configuration**: `OpenRouter:ApiKey` and `OpenRouter:Model` (default `openai/gpt-4o-mini`) in `appsettings.json`/user-secrets/env vars. **Never put a real key in `appsettings.json`** — it's tracked in git. Use `dotnet user-secrets` locally; in any deployed environment (this repo publishes to Azure Container Apps — see `ReactApp1.Server/Properties/PublishProfiles/`), set it as a container secret/env var named `OPENROUTER__ApiKey` (double underscore maps to the `OpenRouter:ApiKey` config section). `dotnet user-secrets` only applies when `ASPNETCORE_ENVIRONMENT=Development` and never travels with a publish — a freshly published/deployed instance needs the env var set explicitly or the endpoint will respond with an "AI assistant is not configured" message instead of erroring.

**Dev proxy gotcha**: `reactapp1.client/vite.config.js` resolves its `/enrollments` proxy target from `ASPNETCORE_HTTPS_PORT` / `ASPNETCORE_URLS` env vars at Vite startup, falling back to a hardcoded `https://localhost:32773` if neither is set. Those env vars are normally injected automatically by ASP.NET Core's SpaProxy when it spawns `npm run dev` as a child process of `dotnet run` on the server project — so running the client (`npm run dev`) standalone, or running the client project as its own startup project instead of the server, will likely misconfigure the proxy target and cause `502` responses from Vite. If you see a `502` from the frontend, first check that the backend was actually the thing that launched Vite (or manually export the matching env var before `npm run dev`).

**No Book/BookService remnants**: the app was refactored from a Book/Library demo to this enrollment domain; if you see references to "Book" anywhere they're stale and should be updated, not treated as intentional.
