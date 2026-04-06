# Agentic Rules Transformation Engine — Rules-IQ

A complete, deterministic rules transformation engine that converts unstructured policy documents into executable [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) rules using Azure AI Search, Azure OpenAI, and a Blazor Server UI.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for infrastructure deployment)
- [Node.js 18+](https://nodejs.org/) (for Playwright E2E tests)

## Quick Start

### 1. Build the Solution

```powershell
cd src
dotnet build RulesIQ.sln
```

All 6 projects should compile with **0 warnings, 0 errors**.

### 2. Run the Blazor UI Locally (No Azure Required)

```powershell
cd src/ui-adapter/RulesIQ.UIAdapter
dotnet run
```

Open the URL shown in the terminal (e.g. `https://localhost:7001`). Then:

1. Click **Evaluate** in the sidebar (or the card on the home page).
2. Click **Load Test Persona** to populate the form with the canonical test applicant.
3. Click **Evaluate Compliance**.
4. The dashboard will show the compliance result — the **GDS rule will fail** (41.5% exceeds the 39% limit), demonstrating the system correctly identifies policy violations.
5. Click **Show Rules Snapshot** to see the full evaluation JSON.
6. Click **Export JSON** to view the complete evaluation output with `ComplianceScore`, `VersionFingerprint`, and `RulesSnapshot`.

> The evaluation page includes a built-in sample workflow with 7 Canadian loan eligibility rules that run **entirely in-memory** via Microsoft RulesEngine — no Azure services are needed for this step.

### 3. Deploy Azure Infrastructure

Ensure you are logged into the correct Azure subscription:

```powershell
az login
az account set --subscription "ME-MngEnvMCAP490549-marfra-1"
```

Run the master deployment script:

```powershell
cd infra/scripts
.\deploy.ps1
```

This executes the following phases in order:

| Phase | What It Does |
|-------|-------------|
| 1 | Creates resource group `rg-rules-iq`, managed identity, hub storage |
| 2 | Configures existing AI Search (semantic ranker, disable local auth) and blob storage |
| 3 | Deploys App Service (indexer skill host), AI Foundry Hub + Project |
| 3.5 | Creates Entra ID app registration, configures Easy Auth v2 (v1 issuer, dual audiences) |
| 3.6 | Builds and deploys the indexer-skill .NET 8 Web API to App Service |
| 4 | Applies all RBAC role assignments (managed identity, zero API keys) |
| 4.5 | Uploads policy documents to blob storage (handles `publicNetworkAccess: Disabled`) |
| 5 | Creates AI Search data-plane objects (index, data source, skillset, indexer) |
| 6 | Creates AI Foundry agents (extraction, normalization, evaluation planning) |

You can skip phases with flags: `-SkipBicep`, `-SkipAppAuth`, `-SkipAppDeploy`, `-SkipUpload`, `-SkipDataPlane`, `-SkipAgents`.

### 4. Upload Policy Documents and Run the Indexer

Upload the 8 Canadian regulatory PDFs to blob storage:

```powershell
cd infra/scripts
.\upload-policy-docs.ps1
```

The script automatically handles storage accounts with `publicNetworkAccess: Disabled` by temporarily enabling access for the upload and re-disabling it afterward. It also creates the `policy-documents` container if it doesn't exist.

The indexer runs on a 24-hour schedule, or trigger it manually:

```powershell
$token = az account get-access-token --resource "https://search.azure.com" --query accessToken -o tsv
Invoke-RestMethod `
    -Uri "https://ai-search-hub-canada.search.windows.net/indexers/ixr-policy-rules/run?api-version=2024-07-01" `
    -Method POST `
    -Headers @{ "Authorization" = "Bearer $token" }
```

### 5. Run Playwright End-to-End Tests

With the Blazor UI running (step 2), in a separate terminal:

```powershell
cd tests/e2e
npm install
npx playwright install chromium
npx playwright test
```

The tests validate:
- Canonical test persona submission and compliance result display
- GDS rule failure (41.5% > 39%) with error message and source document link
- Compliance color coding (yellow/red, not green)
- Ruleset version and evaluation timestamp visibility
- Rules Snapshot expandability and JSON export validity
- Navigation between Home, Evaluate, and Rules pages

View the test report:

```powershell
npx playwright show-report
```

## Folder Structure

```
/contracts                     # Prompt contracts (16 files across 8 phases)
    /01-ingestion              #   Document chunking and preprocessing
    /02-extraction             #   Policy chunk → candidate rules
    /03-normalization          #   Deduplication and validation
    /04-indexer                #   Azure AI Search enrichment
    /05-ui                     #   RulesEngineEditor traceability
    /06-runtime                #   Deterministic evaluation planning
    /07-meta                   #   Domain-specific meta-contracts
    /08-infrastructure         #   Azure environment specification
/examples
    /input                     # Sample policy chunks and indexer rows
    /output                    # Sample extracted rules, workflows, evaluations
/src
    /shared-models             # DTOs, enums, constants, AutoMapper profiles
    /infrastructure            # Azure clients, Polly resilience, DI
    /indexer-skill             # ASP.NET Core Web API (AI Search custom skill)
    /rules-normalizer          # Rule deduplication and normalization engine
    /runtime-engine            # Microsoft RulesEngine execution + compliance scoring
    /ui-adapter                # Blazor Server UI with compliance dashboard
/infra
    /modules                   # Bicep modules (7 files)
    /scripts                   # PowerShell deployment scripts (8 files)
    /definitions               # AI Search index/skillset/indexer + agent definitions
/tests
    /e2e                       # Playwright end-to-end tests
/meta-contracts
    /loan-eligibility          # 8 Canadian regulatory PDFs (OSFI, FCAC, FSRA)
```

## Architecture

This is **not** a RAG system. This is a **compiler**.

```
Policy Documents → Chunking → AI Extraction → Normalization → Search Index → UI
                                                                      ↓
                                                              Runtime Evaluation
                                                                      ↓
                                                          Compliance Score + Audit Trail
```

AI handles **natural language interpretation** (offline, at compile time).
Software engineering handles **deterministic execution** (at runtime).

This delivers:

- **Determinism** — rules execute identically every time
- **Auditability** — every rule traces back to source policy text with page/char range
- **Repeatability** — same input always yields same rules
- **Explainability** — extraction and normalization notes document reasoning
- **Version control** — rules are diffable JSON with semantic versioning
- **Compliance scoring** — every evaluation produces a percentage with pass/fail per rule
- **No runtime hallucination** — AI is used offline only, never at decision time

## Key Technologies

- [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) — deterministic rule evaluation
- [RulesEngineEditor](https://www.nuget.org/packages/RulesEngineEditor/) — Blazor UI for rule management
- [Azure AI Search](https://learn.microsoft.com/en-us/azure/search/) — indexing, vector search, and enrichment
- [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/) — natural language interpretation (GPT-4.1)
- [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-studio/) — agent orchestration (v2 API)
- [AutoMapper](https://automapper.org/) — object mapping for rule inputs
- [Polly](https://github.com/App-vNext/Polly) — resilience and fault handling
- [Playwright](https://playwright.dev/) — end-to-end testing

## Canonical Test Persona

The solution includes a built-in test persona (a fictitious Ontario mortgage applicant) used for end-to-end validation:

| Field | Value | Expected Result |
|-------|-------|----------------|
| Age | 34 | ✅ Pass (≥ 18) |
| Province | ON | Ontario — tests federal + FSRA rules |
| Residency | PermanentResident | ✅ Pass |
| Annual Income | $92,000 | ✅ Pass (≥ $25,000) |
| **GDS** | **41.5%** | **❌ Fail (exceeds 39%)** |
| TDS | 43.0% | ✅ Pass (≤ 44%) |
| Credit Score | 710 | ✅ Pass (≥ 650) |
| Employment | 28 months | ✅ Pass (≥ 6 months) |

The GDS ratio intentionally exceeds the OSFI B-20 limit, producing a compliance score below 100%.
