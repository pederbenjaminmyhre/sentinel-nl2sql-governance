# The Sentinel — NL2SQL Governance & Validation Layer

## Project Overview

A zero-trust intermediary layer that intercepts AI-generated SQL, validates it through four sequential security gates, and executes it only against a read-only replica if all checks pass. Deployed to Azure as a serverless architecture for a professional portfolio.

---

## Solution Architecture

```
Client (React SPA)
    │
    ▼
Azure API Management (APIM) ── API keys, rate limiting, CORS
    │
    ▼
Azure Functions (.NET 9, Isolated Worker) ── "The Sentinel"
    │
    ├──► LLM Service (Azure OpenAI / Ollama) ── NL-to-SQL translation
    │        ▲
    │        └── Self-correction loop (max 2 retries)
    │
    ├──► Gate Pipeline (sequential)
    │      G1: Lexical Shape Validation (TSqlParser)
    │      G2: Allow-List Filter (safe_schema.json)
    │      G3: Semantic Verification (lightweight LLM)
    │      G4: Execution Sandboxing (timeout + row limit)
    │
    └──► Azure SQL Read-Only Named Replica ── query execution
            │
            ▼
         Results returned to client with audit metadata
```

---

## Directory Structure

```
/
├── CLAUDE.md                          # This file
├── src/
│   ├── Sentinel.Api/                  # Azure Functions project (.NET 9)
│   │   ├── Sentinel.Api.csproj
│   │   ├── Program.cs                 # Host builder, DI registration
│   │   ├── host.json                  # Function host config (timeout, logging)
│   │   ├── local.settings.json        # Local dev settings (gitignored)
│   │   ├── Functions/
│   │   │   ├── QueryFunction.cs       # HTTP trigger: accepts NL prompt, returns results
│   │   │   └── HealthFunction.cs      # Health check endpoint
│   │   ├── Gates/
│   │   │   ├── IGate.cs               # Interface: Task<GateResult> Evaluate(QueryContext)
│   │   │   ├── GateResult.cs          # Pass/Fail + reason + gate name
│   │   │   ├── GatePipeline.cs        # Runs gates sequentially, short-circuits on failure
│   │   │   ├── LexicalGate.cs         # G1: TSqlParser DDL/DML detection
│   │   │   ├── AllowListGate.cs       # G2: Schema allow-list check
│   │   │   ├── SemanticGate.cs        # G3: LLM intent-vs-SQL verification
│   │   │   └── SandboxGate.cs         # G4: Timeout + row-limit enforcement
│   │   ├── Parsing/
│   │   │   ├── SqlGuard.cs            # TSqlParser wrapper: returns allowed/blocked + details
│   │   │   └── SchemaExtractor.cs     # Extracts table/column refs from parsed AST
│   │   ├── Services/
│   │   │   ├── ILlmService.cs         # Interface for NL-to-SQL + semantic verification
│   │   │   ├── AzureOpenAiService.cs  # Azure OpenAI implementation
│   │   │   ├── ISqlExecutor.cs        # Interface for sandboxed query execution
│   │   │   ├── SqlExecutor.cs         # Executes against read-only replica with timeout
│   │   │   └── AuditLogger.cs         # Structured logging for every query attempt
│   │   ├── Models/
│   │   │   ├── QueryContext.cs         # User prompt + generated SQL + gate results
│   │   │   ├── QueryRequest.cs        # Inbound DTO: natural language prompt
│   │   │   ├── QueryResponse.cs       # Outbound DTO: results + audit summary
│   │   │   └── AuditEntry.cs          # Prompt, SQL, gate statuses, timestamp
│   │   └── Configuration/
│   │       ├── SentinelOptions.cs     # Typed config: timeouts, row limits, retry count
│   │       └── SafeSchema.cs          # Deserialized safe_schema.json model
│   │
│   └── Sentinel.Tests/                # xUnit test project
│       ├── Sentinel.Tests.csproj
│       ├── Gates/
│       │   ├── LexicalGateTests.cs    # DDL/DML blocking, SELECT passthrough, obfuscation
│       │   ├── AllowListGateTests.cs  # Allowed tables pass, blocked tables fail
│       │   ├── SemanticGateTests.cs   # Mock LLM: matching/mismatching intent
│       │   ├── SandboxGateTests.cs    # Timeout enforcement, row-limit injection
│       │   └── GatePipelineTests.cs   # Sequential execution, short-circuit behavior
│       ├── Parsing/
│       │   ├── SqlGuardTests.cs       # Comprehensive SQL parsing tests
│       │   └── SchemaExtractorTests.cs
│       └── Services/
│           └── SqlExecutorTests.cs
│
├── config/
│   └── safe_schema.json               # Allow-list: permitted tables, views, columns
│
├── infra/
│   ├── main.bicep                     # All Azure resources (Function, SQL, KV, APIM, AppConfig)
│   ├── parameters.json                # Environment-specific parameter values
│   └── modules/
│       ├── function-app.bicep
│       ├── sql-replica.bicep
│       ├── key-vault.bicep
│       ├── api-management.bicep
│       └── app-configuration.bicep
│
├── .github/
│   └── workflows/
│       ├── ci.yml                     # Build + test on every PR
│       └── deploy.yml                 # Deploy to Azure on merge to main
│
├── .gitignore
├── Sentinel.sln                       # Solution file
├── Polished Requirements/             # HTML requirements document
└── Rough Requirements/                # Original requirement notes
```

---

## Technical Requirements

### Runtime & Frameworks

| Component | Technology | Version |
|-----------|-----------|---------|
| Validation Layer | .NET (Isolated Worker Azure Functions) | .NET 9 |
| Lexical Parser | Microsoft.SqlServer.TransactSql.ScriptDom | Latest |
| LLM Integration | Azure.AI.OpenAI | Latest |
| Database Client | Microsoft.Data.SqlClient | Latest |
| Test Framework | xUnit + Moq | Latest |
| IaC | Bicep | Latest |
| CI/CD | GitHub Actions | N/A |

### NuGet Packages (Sentinel.Api)

```
Microsoft.Azure.Functions.Worker
Microsoft.Azure.Functions.Worker.Sdk
Microsoft.Azure.Functions.Worker.Extensions.Http
Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
Microsoft.SqlServer.TransactSql.ScriptDom
Microsoft.Data.SqlClient
Azure.AI.OpenAI
Azure.Identity
Azure.Security.KeyVault.Secrets
Microsoft.Extensions.Configuration.AzureAppConfiguration
Microsoft.ApplicationInsights.WorkerService
```

### NuGet Packages (Sentinel.Tests)

```
xUnit
xUnit.runner.visualstudio
Moq
Microsoft.NET.Test.Sdk
FluentAssertions
```

---

## Implementation Plan

### Phase 1: Project Scaffolding & Core Parsing

**Goal:** Establish the solution structure, build the SQL parsing engine, and prove Gate 1.

1. **Create solution and projects**
   - `Sentinel.sln` with `Sentinel.Api` (.NET 9 isolated worker function) and `Sentinel.Tests` (xUnit)
   - Add all NuGet package references
   - Create `host.json`, `local.settings.json`, `.gitignore`

2. **Implement `SqlGuard` (Parsing/SqlGuard.cs)**
   - Use `TSqlParser.Create(SqlVersion.Sql160)` to parse input SQL
   - Walk the AST using a `TSqlFragmentVisitor`
   - Return `blocked = true` if any node is: `DeleteStatement`, `DropTableStatement`, `TruncateTableStatement`, `UpdateStatement`, `InsertStatement`, `CreateTableStatement`, `AlterTableStatement`, `ExecuteStatement`, `MergeStatement`
   - Return `allowed = true` only for pure `SELECT` queries (including CTEs)
   - Return parse errors with line/column detail

3. **Implement `SchemaExtractor` (Parsing/SchemaExtractor.cs)**
   - Visit `NamedTableReference` nodes to extract all referenced table names
   - Visit `ColumnReferenceExpression` nodes to extract column references
   - Return a `HashSet<string>` of fully-qualified object names (schema.table, schema.table.column)

4. **Implement `LexicalGate` (Gates/LexicalGate.cs)**
   - Inject `SqlGuard`
   - Return `GateResult.Pass()` if SqlGuard reports allowed
   - Return `GateResult.Fail("G1-LEXICAL", reason)` with the specific blocked statement type

5. **Write tests for SqlGuard and LexicalGate**
   - `SELECT * FROM Orders` → allowed
   - `DELETE FROM Orders` → blocked
   - `DROP TABLE Users` → blocked
   - `SELECT 1; DROP TABLE Users` → blocked (batch with hidden DDL)
   - `WITH cte AS (SELECT ...) SELECT * FROM cte` → allowed
   - Obfuscation: `/* comment */ DROP /* comment */ TABLE Users` → blocked
   - Empty/null input → blocked

### Phase 2: Allow-List Gate

**Goal:** Implement schema-aware filtering so only pre-approved tables and columns are queryable.

1. **Define `safe_schema.json` format**
   ```json
   {
     "allowedObjects": [
       {
         "schema": "dbo",
         "table": "Orders",
         "columns": ["OrderId", "CustomerId", "OrderDate", "TotalAmount"]
       },
       {
         "schema": "dbo",
         "table": "Products",
         "columns": ["ProductId", "Name", "Category", "Price"]
       }
     ]
   }
   ```

2. **Implement `SafeSchema` model and `SentinelOptions`**
   - Deserialize `safe_schema.json` into typed config
   - Register in DI via `IOptions<SafeSchema>`

3. **Implement `AllowListGate` (Gates/AllowListGate.cs)**
   - Inject `SchemaExtractor` and `IOptions<SafeSchema>`
   - Parse SQL → extract table/column references
   - Compare against allow-list (case-insensitive)
   - Return `GateResult.Fail("G2-ALLOWLIST", "Table 'Salary' is not in the approved schema")` on violation

4. **Write tests**
   - Query referencing only allowed tables → pass
   - Query joining an unauthorized table (`Salary`, `UserCredentials`) → fail
   - Query referencing an unauthorized column on an allowed table → fail
   - Star-select (`SELECT *`) on an allowed table → pass (columns resolved at execution)

### Phase 3: LLM Integration & Semantic Gate

**Goal:** Wire up LLM for NL-to-SQL translation, implement the self-correction loop, and build Gate 3.

1. **Define `ILlmService` interface**
   ```csharp
   Task<string> GenerateSqlAsync(string naturalLanguagePrompt, SafeSchema schema);
   Task<bool> VerifyIntentAsync(string originalPrompt, string generatedSql);
   Task<string> CorrectSqlAsync(string sql, string error);
   ```

2. **Implement `AzureOpenAiService`**
   - `GenerateSqlAsync`: System prompt includes the safe schema as context; instructs LLM to generate SELECT-only T-SQL
   - `VerifyIntentAsync`: Asks a second (or same) LLM: "Does this SQL answer the user's question? Respond YES or NO with a one-line reason."
   - `CorrectSqlAsync`: Sends the SQL + parse error back to LLM for correction
   - Use `Azure.AI.OpenAI` client with `Azure.Identity` `DefaultAzureCredential`

3. **Implement self-correction loop in `QueryFunction`**
   - Generate SQL → run through G1 and G2
   - If G1 or G2 fail with a correctable error, call `CorrectSqlAsync`
   - Retry up to 2 times; on 3rd failure, return error to client

4. **Implement `SemanticGate` (Gates/SemanticGate.cs)**
   - Inject `ILlmService`
   - Call `VerifyIntentAsync(originalPrompt, generatedSql)`
   - Pass if LLM confirms alignment; fail with `"G3-SEMANTIC"` if mismatch detected

5. **Write tests with mocked `ILlmService`**
   - Intent matches SQL → pass
   - Intent does not match SQL → fail
   - Self-correction: first attempt fails G1, corrected attempt passes

### Phase 4: Execution Sandbox & Audit

**Goal:** Implement safe query execution with timeouts/row limits, and full audit logging.

1. **Implement `ISqlExecutor` / `SqlExecutor`**
   - Open connection to read-only replica using `Managed Identity` (DefaultAzureCredential) or connection string from Key Vault for local dev
   - Inject `TOP 1000` if not already present (modify parsed AST or prepend `SET ROWCOUNT 1000`)
   - Set `CommandTimeout = 30` seconds
   - Return `DataTable` or JSON-serializable result set
   - Catch `SqlException` for timeout → return structured error

2. **Implement `SandboxGate` (Gates/SandboxGate.cs)**
   - This gate wraps execution: it IS the execution step
   - Enforces timeout and row limit at the code level (defense-in-depth alongside DB-level restrictions)
   - Return `GateResult.Fail("G4-SANDBOX", "Query exceeded 30-second timeout")` on violation

3. **Implement `GatePipeline`**
   - Accepts `IEnumerable<IGate>` via DI (ordered: G1 → G2 → G3 → G4)
   - Iterates sequentially; short-circuits on first failure
   - Returns list of all `GateResult` objects (for audit trail)

4. **Implement `AuditLogger`**
   - Creates structured `AuditEntry` for every query attempt
   - Logs to Application Insights with custom dimensions: prompt, SQL, each gate status, elapsed time
   - Local dev: logs to console in JSON format

5. **Write tests**
   - Pipeline short-circuits: G1 fails → G2/G3/G4 never called
   - Pipeline passes all gates → results returned
   - Timeout simulation → SandboxGate returns failure
   - Audit entry contains all expected fields

### Phase 5: HTTP API & Models

**Goal:** Wire everything into the Azure Function HTTP trigger with proper request/response models.

1. **Define request/response models**
   ```csharp
   // QueryRequest.cs
   public record QueryRequest(string Prompt);

   // QueryResponse.cs
   public record QueryResponse(
       bool Success,
       string? GeneratedSql,
       object? Results,
       AuditSummary Audit
   );

   // AuditSummary — subset of AuditEntry for client consumption
   public record AuditSummary(
       string[] GateResults,  // ["G1:PASS", "G2:PASS", "G3:PASS", "G4:PASS"]
       double ElapsedMs
   );
   ```

2. **Implement `QueryFunction` (Functions/QueryFunction.cs)**
   - HTTP POST trigger at `/api/query`
   - Deserialize `QueryRequest`
   - Call `ILlmService.GenerateSqlAsync` → build `QueryContext`
   - Run self-correction loop (G1/G2 check → correct → retry)
   - Run full `GatePipeline`
   - Return `QueryResponse` with 200 (success) or 422 (gate failure) or 400 (bad input)

3. **Implement `HealthFunction`**
   - HTTP GET at `/api/health`
   - Verify DB connectivity, LLM reachability
   - Return 200 with component status

4. **Configure DI in `Program.cs`**
   - Register all gates in order
   - Register `GatePipeline`, `SqlGuard`, `SchemaExtractor`
   - Register `ILlmService` → `AzureOpenAiService`
   - Register `ISqlExecutor` → `SqlExecutor`
   - Bind `SentinelOptions` and `SafeSchema` from configuration
   - Add Application Insights telemetry

### Phase 6: Infrastructure-as-Code

**Goal:** Define all Azure resources in Bicep for one-command deployment.

1. **`main.bicep`** — orchestrator that invokes modules:
   - Azure Function App (Consumption plan, .NET 9 isolated, system-assigned Managed Identity)
   - Azure SQL Database (Hyperscale) with Named Replica (read-only)
   - Azure Key Vault (Function App identity gets `Secret:Get` policy)
   - Azure API Management (Consumption tier, single API with rate-limit policy)
   - Azure App Configuration (stores `safe_schema.json`)
   - Application Insights + Log Analytics Workspace

2. **Module files** — one per resource for clarity

3. **`parameters.json`** — environment-specific values (resource names, SKUs, region)

4. **Key Vault secrets** — SQL connection string, OpenAI API key (provisioned manually or via separate secure step)

### Phase 7: CI/CD Pipeline

**Goal:** Automated build, test, and deploy via GitHub Actions.

1. **`ci.yml`** (runs on every PR)
   - Checkout → Setup .NET 9 → Restore → Build → Test
   - Fail the PR if any test fails

2. **`deploy.yml`** (runs on merge to `main`)
   - Run CI steps
   - `az login` with OIDC federated credentials
   - `az deployment group create` with Bicep
   - `func azure functionapp publish` to deploy Function App
   - Smoke test: call `/api/health`

### Phase 8: Polish & Portfolio Readiness

**Goal:** Make the project presentable for recruiters, employers, and potential clients.

1. **README.md** with architecture diagram, quick-start, and screenshots
2. **Demo mode** — a `--demo` flag or config toggle that uses an in-memory SQLite database with sample data so reviewers can test without Azure resources
3. **Security dashboard screenshot** — query Log Analytics for blocked/passed query stats
4. **LinkedIn post assets** — the split-screen visual described in the marketing campaign

---

## Coding Conventions

- **C# style:** File-scoped namespaces, primary constructors, records for DTOs, nullable reference types enabled
- **Naming:** PascalCase for public members, `_camelCase` for private fields, `I` prefix for interfaces
- **Error handling:** Use `Result<T>` pattern (not exceptions) for gate evaluations; exceptions only for truly exceptional cases (network failures, etc.)
- **Configuration:** All magic numbers (timeout, row limit, retry count) go in `SentinelOptions`, never hardcoded
- **Testing:** Each gate tested independently with mocks for dependencies; integration tests use real `TSqlParser`
- **Git:** Conventional commits (`feat:`, `fix:`, `test:`, `infra:`, `docs:`)

---

## Key Design Decisions

1. **Gates are sequential, not parallel** — each gate can depend on prior gate context (e.g., G2 needs parsed AST from G1's parser)
2. **G4 is both a gate and the execution step** — avoids executing the query twice
3. **Self-correction loop is outside the gate pipeline** — correction targets G1/G2 failures only; G3/G4 failures are terminal
4. **`safe_schema.json` in Azure App Configuration** — allows security policy updates without code redeployment
5. **No direct SQL string manipulation** — all validation uses the parsed AST; row-limit enforcement modifies the AST or uses `SET ROWCOUNT`
6. **Managed Identity everywhere** — no connection strings or API keys in code or config files

---

## Local Development

```bash
# Prerequisites: .NET 9 SDK, Azure Functions Core Tools v4, SQL Server (LocalDB or Docker)

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Start function app locally
cd src/Sentinel.Api
func start

# Test endpoint
curl -X POST http://localhost:7071/api/query \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me all orders from last month"}'
```

For local dev, configure `local.settings.json` with:
- SQL connection string pointing to a local SQL Server instance
- LLM API key (or local Ollama endpoint)
- `safe_schema.json` loaded from the `config/` directory

---

## Azure Resources (Production)

| Resource | SKU / Tier | Estimated Monthly Cost |
|----------|-----------|----------------------|
| Azure Functions | Consumption (pay-per-execution) | ~$0-5 (portfolio traffic) |
| Azure SQL | Hyperscale (1 vCore) + Named Replica | ~$100 |
| API Management | Consumption | ~$3.50/million calls |
| Key Vault | Standard | ~$0.03/10K operations |
| App Configuration | Free tier | $0 |
| Application Insights | Free tier (5 GB/month) | $0 |
| **Total** | | **~$105/month** |

> **Cost Note:** For portfolio purposes, Azure SQL can be replaced with a Basic tier ($5/mo) without the Named Replica feature. The replica pattern would be documented but the demo would run against a single read-only-configured database.
