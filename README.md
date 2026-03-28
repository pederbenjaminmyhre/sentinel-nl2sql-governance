# The Sentinel — NL2SQL Governance & Validation Layer

A **zero-trust intermediary layer** that intercepts AI-generated SQL, validates it through four sequential security gates, and executes only approved `SELECT` queries against a read-only database replica.

> *"Don't trust the LLM to write safe SQL. Trust the architecture to enforce safe SQL."*

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Client (Browser / API Consumer)                                 │
└──────────────────┬───────────────────────────────────────────────┘
                   │  POST /api/query { "prompt": "..." }
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│  Azure API Management                                            │
│  ├─ API key enforcement                                          │
│  ├─ Rate limiting (60 req/min)                                   │
│  └─ CORS policy                                                  │
└──────────────────┬───────────────────────────────────────────────┘
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│  Azure Functions (.NET 9, Isolated Worker)                       │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  LLM Service (Azure OpenAI)                             │     │
│  │  ├─ Translates natural language → candidate SQL         │     │
│  │  └─ Self-correction loop (max 2 retries)                │     │
│  └─────────────────────────────────────────────────────────┘     │
│                         │                                        │
│                         ▼                                        │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  Gate Pipeline (sequential, short-circuits on failure)  │     │
│  │                                                         │     │
│  │  ┌─ G1  Lexical Validation ─────────────────────────┐   │     │
│  │  │  TSqlParser AST analysis                         │   │     │
│  │  │  Blocks: DELETE, DROP, UPDATE, INSERT, EXEC ...  │   │     │
│  │  └──────────────────────────────────────────────────┘   │     │
│  │  ┌─ G2  Allow-List Filter ──────────────────────────┐   │     │
│  │  │  Schema-aware table/column validation             │   │     │
│  │  │  Only pre-approved objects can be queried         │   │     │
│  │  └──────────────────────────────────────────────────┘   │     │
│  │  ┌─ G3  Semantic Verification ──────────────────────┐   │     │
│  │  │  Second LLM compares intent vs. generated SQL    │   │     │
│  │  │  Catches hallucinated but syntactically valid SQL │   │     │
│  │  └──────────────────────────────────────────────────┘   │     │
│  │  ┌─ G4  Execution Sandbox ──────────────────────────┐   │     │
│  │  │  30-second timeout, TOP 1000 row limit           │   │     │
│  │  │  Read-only replica, no write path exists         │   │     │
│  │  └──────────────────────────────────────────────────┘   │     │
│  └─────────────────────────────────────────────────────────┘     │
│                         │                                        │
│  Audit Logger ──► Application Insights / Log Analytics           │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│  Azure SQL Database — Read-Only Named Replica                    │
│  ├─ Row-Level Security (RLS)                                     │
│  ├─ Dynamic Data Masking                                         │
│  └─ Restricted service account (SELECT only)                     │
└──────────────────────────────────────────────────────────────────┘
```

---

## The Four Sentinel Gates

| Gate | Name | What It Does | Blocks |
|------|------|-------------|--------|
| **G1** | Lexical Validation | Parses SQL into an AST via `TSqlParser` | Any DDL/DML: `DELETE`, `DROP`, `UPDATE`, `INSERT`, `EXEC`, `CREATE`, `ALTER`, `TRUNCATE`, `MERGE`, `GRANT` |
| **G2** | Allow-List Filter | Checks all table/column references against `safe_schema.json` | Queries referencing tables or columns not in the approved schema |
| **G3** | Semantic Verification | A second LLM pass comparing user intent vs. generated SQL | Hallucinated queries that are syntactically valid but logically wrong |
| **G4** | Execution Sandbox | Runs the query with a 30s timeout and TOP 1000 row limit | Denial-of-service via expensive joins or full table scans |

Each gate produces a **pass/fail result** logged to the audit trail. The pipeline **short-circuits** on the first failure.

---

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- SQL Server ([LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) or Docker)

### Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

### Run Locally

```bash
# Start LocalDB (Windows)
sqllocaldb start MSSQLLocalDB

# Seed the demo database
sqlcmd -S "(localdb)\MSSQLLocalDB" -i scripts/seed-demo-db.sql

# Configure local settings (edit src/Sentinel.Api/local.settings.json)
# Set your Azure OpenAI endpoint and API key, or use demo mode

# Start the function app
cd src/Sentinel.Api
func start
```

### Demo Mode

The project includes a **demo mode** that runs without Azure OpenAI or a live database. It uses an in-memory mock LLM and sample data so reviewers can test the gate pipeline immediately.

Set the environment variable or app setting:

```json
"Sentinel__DemoMode": "true"
```

In demo mode:
- The LLM service returns pre-built SQL for common queries
- The SQL executor returns sample data without a database connection
- All four gates still run with real parsing and validation

### Test the API

```bash
# Submit a natural language query
curl -X POST http://localhost:7071/api/query \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me all orders from last month"}'

# Health check
curl http://localhost:7071/api/health
```

### Example Response

```json
{
  "success": true,
  "generatedSql": "SELECT TOP 1000 OrderId, CustomerId, OrderDate, TotalAmount FROM dbo.Orders WHERE OrderDate >= DATEADD(MONTH, -1, GETDATE())",
  "results": [
    { "OrderId": 1, "CustomerId": 42, "OrderDate": "2026-03-01", "TotalAmount": 299.99 }
  ],
  "audit": {
    "gateResults": ["G1-LEXICAL:PASS", "G2-ALLOWLIST:PASS", "G3-SEMANTIC:PASS", "G4-SANDBOX:PASS"],
    "elapsedMs": 1247.3
  }
}
```

### Example Blocked Response

```json
{
  "success": false,
  "generatedSql": "DELETE FROM dbo.Orders WHERE OrderId = 1",
  "results": null,
  "audit": {
    "gateResults": ["G1-LEXICAL:FAIL (Blocked statement type: DeleteStatement)"],
    "elapsedMs": 12.1
  }
}
```

---

## Deploy to Azure

### One-Time Setup

```bash
# Create a resource group
az group create --name sentinel-rg --location eastus2

# Deploy all infrastructure
az deployment group create \
  --resource-group sentinel-rg \
  --template-file infra/main.bicep \
  --parameters baseName=sentinel \
               sqlAdminLogin=sentineladmin \
               sqlAdminPassword='<YourSecurePassword>' \
               publisherEmail='your@email.com'

# Publish the Function App
dotnet publish src/Sentinel.Api -c Release -o ./publish
func azure functionapp publish sentinel-func --dotnet-isolated
```

### CI/CD

The repo includes GitHub Actions workflows:

- **`ci.yml`** — Builds, tests, and validates Bicep on every PR
- **`deploy.yml`** — Deploys infrastructure + app on merge to `main`

Configure these GitHub secrets:
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (for OIDC login)
- `SQL_ADMIN_LOGIN`, `SQL_ADMIN_PASSWORD`
- `PUBLISHER_EMAIL`

---

## Project Structure

```
├── src/
│   ├── Sentinel.Api/              .NET 9 Azure Functions project
│   │   ├── Functions/             HTTP triggers (Query, Health)
│   │   ├── Gates/                 IGate, GatePipeline, G1–G4 implementations
│   │   ├── Parsing/              SqlGuard (TSqlParser), SchemaExtractor
│   │   ├── Services/             LLM integration, SQL executor, audit logger
│   │   ├── Models/               DTOs and domain objects
│   │   └── Configuration/        Typed options and schema models
│   └── Sentinel.Tests/           44 unit tests (xUnit + Moq + FluentAssertions)
├── config/
│   └── safe_schema.json          Allow-list: approved tables and columns
├── infra/                         Bicep IaC templates
│   ├── main.bicep                Orchestrator (6 Azure resources)
│   └── modules/                  Function App, SQL, Key Vault, APIM, App Config
├── scripts/
│   └── seed-demo-db.sql          Sample data for local development
└── .github/workflows/            CI + deploy pipelines
```

---

## Azure Resources

| Resource | Service | Purpose | Est. Cost |
|----------|---------|---------|-----------|
| Compute | Azure Functions (Consumption) | Stateless query processing | ~$0–5/mo |
| Database | Azure SQL (Basic) | Demo data store | ~$5/mo |
| Gateway | API Management (Consumption) | Rate limiting, API keys | ~$3.50/1M calls |
| Secrets | Key Vault (Standard) | Connection strings, API keys | ~$0.03/10K ops |
| Config | App Configuration (Free) | Safe schema allow-list | $0 |
| Observability | Application Insights (Free 5GB) | Audit logs, security dashboard | $0 |

**Portfolio cost: ~$10/month**

---

## Technologies

- **.NET 9** — Azure Functions Isolated Worker
- **TSqlParser** (ScriptDom) — Deterministic SQL parsing without execution
- **Azure OpenAI** — NL-to-SQL translation and semantic verification
- **Azure SQL** — Read-only replica sandboxing
- **Bicep** — Infrastructure-as-Code
- **GitHub Actions** — CI/CD automation
- **Managed Identity** — Zero passwords in code

---

## License

This project is part of a professional portfolio. All rights reserved.
