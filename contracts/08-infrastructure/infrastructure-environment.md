# Contract: Infrastructure Environment & Deployment

## Purpose
Define the complete Azure infrastructure for the Rules-IQ platform, including all services, managed identities, RBAC permissions, service-to-service trusts, and data-plane configuration. **No API keys are used anywhere** — all authentication is via Microsoft Entra ID managed identities and RBAC.

---

## Core Tenets

### 1. Zero API Keys — Managed Identity + RBAC Only
- **All** service-to-service communication MUST use **system-assigned managed identities** with Azure RBAC role assignments.
- API key authentication is **disabled** on all services (`disableLocalAuth: true`, `allowSharedKeyAccess: false`).
- The deploying user's identity is used for initial setup; runtime services authenticate via their managed identity.

### 2. Azure CLI as Primary Provisioning Tool
- Use `az cli` against the **current logged-in subscription** for all imperative operations (data-plane indexer creation, role assignments, etc.).
- All infrastructure is codified in **Bicep** for reproducibility and stored in `infra/`.

### 3. AI Foundry Agent IQ v2 API
- Use the **Azure AI Foundry Agent Service (v2 API)** for agent orchestration.
- Agents connect to **already-deployed models** on the existing Azure OpenAI (AIServices) resource — no new model deployments needed.
- All services MUST use the **same embedding model** (`text-embedding-3-large`) for consistency.

---

## Current Subscription Context

| Property | Value |
|----------|-------|
| Subscription | `ME-MngEnvMCAP490549-marfra-1` |
| Subscription ID | `28d10200-70b0-476c-b004-c6ae29265897` |
| Tenant ID | `d7d6e19e-5176-4dea-a576-1681f77e0243` |
| Tenant Domain | `MngEnvMCAP490549.onmicrosoft.com` |
| Logged-in User | `dev@MngEnvMCAP490549.onmicrosoft.com` |

---

## Resource Group

| Property | Value |
|----------|-------|
| Name | `rg-rules-iq` |
| Location | `eastus` |
| Purpose | Contains all Rules-IQ resources |

---

## Services & Configuration

### 1. Azure OpenAI (AIServices) — Existing Resource

| Property | Value |
|----------|-------|
| Name | `rg-openai-hub` |
| Resource Group | `RG-OpenAI` |
| Location | `eastus` |
| Kind | `AIServices` |
| Endpoint | `https://rg-openai-hub.cognitiveservices.azure.com/` |
| Local Auth | **Disabled** (`disableLocalAuth: true`) |

**Model Deployments (already deployed):**

| Deployment Name | Model | Usage |
|----------------|-------|-------|
| `gpt-4.1` | GPT-4.1 | Rule extraction, normalization, evaluation planning |
| `gpt-4o` | GPT-4o | Fallback / lighter-weight inference |
| `text-embedding-3-large` | text-embedding-3-large | **Standard embedding model for all services** |

> **IMPORTANT:** All services that produce or consume embeddings MUST use `text-embedding-3-large` (3072 dimensions). This includes AI Search vectorizer, the indexer skill, and any runtime vector queries.

---

### 2. Azure AI Search — Existing Resource (Reconfigure)

| Property | Value |
|----------|-------|
| Name | `ai-search-hub-canada` |
| Resource Group | `RG-OpenAI` |
| Location | `eastus` |
| SKU | `standard` |
| Semantic Ranker | **Must be enabled** (`free` tier under Standard SKU) |
| Local Auth | **Must be disabled** (`disableLocalAuth: true`) |

**Required Configuration Changes:**
1. Enable Semantic Ranker: `az search service update --name ai-search-hub-canada --resource-group RG-OpenAI --semantic-search free`
2. Disable API key auth: `az search service update --name ai-search-hub-canada --resource-group RG-OpenAI --disable-local-auth true --auth-options aadOrApiKey`

**Standard SKU Capabilities Required:**
- Semantic Ranker (semantic search)
- Vector search (HNSW / exhaustive KNN)
- Custom Web API skills in skillsets
- Indexer with blob data source
- Minimum 1 replica, 1 partition (default)

---

### 3. Azure Blob Storage — Existing Resource (Reconfigure)

| Property | Value |
|----------|-------|
| Name | `sadatafileshubcanada` |
| Resource Group | `RG-OpenAI` |
| Location | `eastus` |
| SKU | `Standard_LRS` |
| Shared Key Access | **Must be disabled** |

**Required Configuration Changes:**
1. Disable shared key access: `az storage account update --name sadatafileshubcanada --resource-group RG-OpenAI --allow-shared-key-access false`
2. Create blob container for policy documents: `az storage container create --name policy-documents --account-name sadatafileshubcanada --auth-mode login`

> **IMPORTANT:** This storage account has `publicNetworkAccess: Disabled`. Uploading policy documents from a developer machine requires temporarily enabling public access. The `upload-policy-docs.ps1` script handles this automatically — it enables access, uploads, and re-disables access in a `finally` block.

**Container Structure:**
```
policy-documents/
  ├── osfi-b20/
  │     └── osfi-b20-2024.pdf
  ├── osfi-b21/
  │     └── osfi-b21-2025.pdf
  └── internal-policies/
        └── personal-loan-eligibility-2024.pdf
```

---

### 4. Azure AI Foundry Hub + Project — New Resources

| Property | Value |
|----------|-------|
| Hub Name | `rulesiq-ai-hub` |
| Project Name | `rulesiq-agent-project` |
| Resource Group | `rg-rules-iq` |
| Location | `eastus` |
| Kind | Hub = `hub`, Project = `project` |

**AI Foundry Configuration:**
- The hub connects to the existing `rg-openai-hub` AIServices resource (not a new one).
- The project uses the **Agent IQ v2 API** (`/agents/v1.0` endpoint on the project).
- Agents use already-deployed models via the connected AIServices resource.
- The hub is backed by storage account `sarulesiqhub` (new, for hub metadata only).
- A connected AI Search service enables grounding agents on indexed rules.

**Agent Configuration (v2 API):**

| Agent | Model Deployment | Purpose |
|-------|-----------------|---------|
| `rule-extraction-agent` | `gpt-4.1` | Extract candidate rules from policy chunks |
| `rule-normalization-agent` | `gpt-4.1` | Normalize, deduplicate, and validate rules |
| `evaluation-planning-agent` | `gpt-4.1` | Plan runtime evaluation for documents/requests |

All agents use the same AI Foundry project and the same connected OpenAI resource. Agent definitions are created via the v2 REST API or Azure AI Foundry SDK.

---

### 5. Custom Web API Skill Host — New Resource

| Property | Value |
|----------|-------|
| Name | `app-rulesiq-indexer-skill` |
| Resource Group | `rg-rules-iq` |
| Location | `eastus` |
| Type | Azure App Service (Linux, .NET 8) |
| Plan | `asp-rulesiq` (B1 or S1) |
| Managed Identity | **System-assigned** |

This hosts the custom Web API skill called by the AI Search indexer to extract rules from document chunks.

#### Entra ID App Registration (WebApiSkill Authentication)

The AI Search indexer authenticates to the custom Web API skill via its **system-assigned managed identity**. This requires an Entra ID app registration on the App Service so AI Search can acquire a token for the skill endpoint.

| Property | Value |
|----------|-------|
| App Registration Name | `app-rulesiq-indexer-skill` |
| Application (Client) ID | `512c962b-55aa-4d13-a9fd-e4fa5888c1e5` |
| Identifier URI | `api://512c962b-55aa-4d13-a9fd-e4fa5888c1e5` |
| Tenant ID | `d7d6e19e-5176-4dea-a576-1681f77e0243` |

> **IMPORTANT:** The identifier URI MUST use the `api://{appId}` format with the actual GUID — friendly names like `api://app-rulesiq-indexer-skill` are rejected by most tenant policies.

**Creation Commands:**
```powershell
# Create the app registration
$appId = az ad app create --display-name "app-rulesiq-indexer-skill" --query appId -o tsv

# Set the identifier URI (must use the actual GUID)
az ad app update --id $appId --identifier-uris "api://$appId"

# Create a service principal for the app registration
az ad sp create --id $appId
```

#### Easy Auth v2 (App Service Authentication)

The App Service MUST have **Easy Auth v2** (Microsoft Identity Platform / Entra ID) enabled so the AI Search system-assigned managed identity can authenticate when calling the custom WebApiSkill.

| Property | Value |
|----------|-------|
| Auth Provider | Microsoft Entra ID (v2) |
| Client ID | `512c962b-55aa-4d13-a9fd-e4fa5888c1e5` |
| Issuer URL | `https://sts.windows.net/d7d6e19e-5176-4dea-a576-1681f77e0243/` |
| Allowed Audiences | `api://512c962b-55aa-4d13-a9fd-e4fa5888c1e5`, `512c962b-55aa-4d13-a9fd-e4fa5888c1e5` |
| Unauthenticated Action | Return 401 |

> **CRITICAL:** The issuer URL MUST use the **v1 endpoint** (`https://sts.windows.net/{tenantId}/`), NOT the v2.0 endpoint (`https://login.microsoftonline.com/{tenantId}/v2.0`). AI Search's system-assigned managed identity acquires tokens via the v1 endpoint, and a v2.0-only issuer will reject these tokens.

> **CRITICAL:** Both `api://{appId}` AND the raw `{appId}` GUID MUST be listed as allowed audiences. AI Search may present either format in the token's `aud` claim.

**Configuration Command:**
```powershell
$appId = "512c962b-55aa-4d13-a9fd-e4fa5888c1e5"
$tenantId = "d7d6e19e-5176-4dea-a576-1681f77e0243"

az webapp auth update `
    --name app-rulesiq-indexer-skill `
    --resource-group rg-rules-iq `
    --enabled true `
    --action LoginWithAzureActiveDirectory `
    --aad-client-id $appId `
    --aad-issuer "https://sts.windows.net/$tenantId/" `
    --aad-allowed-token-audiences "api://$appId" "$appId"
```

After configuring Easy Auth, **restart the App Service** for the changes to take effect:
```powershell
az webapp restart --name app-rulesiq-indexer-skill --resource-group rg-rules-iq
```

#### App Code Deployment

The custom Web API skill (.NET 8) must be built and deployed to the App Service:

```powershell
# Build and publish
dotnet publish src/indexer-skill/RulesIQ.IndexerSkill/RulesIQ.IndexerSkill.csproj `
    -c Release -o ./publish

# Create zip archive
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy to App Service
az webapp deploy `
    --name app-rulesiq-indexer-skill `
    --resource-group rg-rules-iq `
    --src-path ./publish.zip `
    --type zip

# Clean up
Remove-Item -Path ./publish -Recurse -Force
Remove-Item -Path ./publish.zip -Force
```

> **NOTE:** The controller returns `string.Empty` (not `null`) for `WorkflowName` and `RulesJson` when `hasRules = false`. This prevents index projection warnings on chunks without rules. Null values in projected fields cause AI Search to emit warnings for every non-rule chunk.

---

### 6. Storage Account for AI Foundry Hub — New Resource

| Property | Value |
|----------|-------|
| Name | `sarulesiqhub` |
| Resource Group | `rg-rules-iq` |
| Location | `eastus` |
| SKU | `Standard_LRS` |
| Kind | `StorageV2` |
| Shared Key Access | `false` |

---

### 7. User-Assigned Managed Identity — New Resource

| Property | Value |
|----------|-------|
| Name | `id-rulesiq` |
| Resource Group | `rg-rules-iq` |
| Location | `eastus` |

This identity is assigned to the AI Foundry Hub and Project for accessing OpenAI and AI Search. System-assigned identities on individual services are also used where appropriate.

---

## Managed Identity & RBAC Role Assignments

### Service-to-Service Trust Map

```
┌────────────────────────┐     Cognitive Services OpenAI User     ┌──────────────────────┐
│  AI Search Indexer      │ ─────────────────────────────────────► │  Azure OpenAI        │
│  (system-assigned MI)   │                                        │  (rg-openai-hub)     │
│  ai-search-hub-canada   │     Cognitive Services OpenAI User     │                      │
│                         │ ─────────────────────────────────────► │  (for embeddings)    │
└────────────────────────┘                                        └──────────────────────┘
         │                                                                   ▲
         │  Storage Blob Data Reader                                         │
         ▼                                                                   │
┌────────────────────────┐                                    Cognitive Services OpenAI User
│  Blob Storage           │                                                  │
│  sadatafileshubcanada   │                              ┌──────────────────────┐
│  (policy-documents)     │                              │  App Service          │
└────────────────────────┘                              │  (indexer-skill)      │
         ▲                                               │  app-rulesiq-         │
         │  Storage Blob Data Reader                     │  indexer-skill        │
         │                                               └──────────────────────┘
┌────────────────────────┐                                          │
│  App Service            │     Search Index Data Contributor       │
│  (indexer-skill)        │ ◄──────────────────────────── (AI Search calls out to skill)
│  app-rulesiq-           │
│  indexer-skill          │
└────────────────────────┘

┌────────────────────────┐     Cognitive Services OpenAI User     ┌──────────────────────┐
│  AI Foundry Project     │ ─────────────────────────────────────► │  Azure OpenAI        │
│  (id-rulesiq)           │                                        │  (rg-openai-hub)     │
│  rulesiq-agent-project  │     Search Index Data Reader           │                      │
│                         │ ─────────────────────────────────────► └──────────────────────┘
│                         │     ┌──────────────────────┐
│                         │ ──► │  AI Search            │
│                         │     │  ai-search-hub-canada │
└────────────────────────┘     └──────────────────────┘
```

### Complete RBAC Assignment Table

| # | Principal (Who) | Principal Type | Role | Scope (On What) | Purpose |
|---|----------------|----------------|------|-----------------|---------|
| 1 | `ai-search-hub-canada` (system MI) | ServicePrincipal | `Cognitive Services OpenAI User` | `rg-openai-hub` (AIServices) | AI Search indexer calls OpenAI for embeddings via integrated vectorizer |
| 2 | `ai-search-hub-canada` (system MI) | ServicePrincipal | `Storage Blob Data Reader` | `sadatafileshubcanada` (Storage) | AI Search indexer reads PDFs from blob storage |
| 3 | `app-rulesiq-indexer-skill` (system MI) | ServicePrincipal | `Cognitive Services OpenAI User` | `rg-openai-hub` (AIServices) | Custom Web API skill calls OpenAI for rule extraction |
| 4 | `app-rulesiq-indexer-skill` (system MI) | ServicePrincipal | `Search Index Data Reader` | `ai-search-hub-canada` (Search) | Skill may query index for context during extraction |
| 5 | `id-rulesiq` (user-assigned MI) | ServicePrincipal | `Cognitive Services OpenAI User` | `rg-openai-hub` (AIServices) | AI Foundry agents call OpenAI models |
| 6 | `id-rulesiq` (user-assigned MI) | ServicePrincipal | `Search Index Data Reader` | `ai-search-hub-canada` (Search) | AI Foundry agents query the rules index |
| 7 | `id-rulesiq` (user-assigned MI) | ServicePrincipal | `Search Index Data Contributor` | `ai-search-hub-canada` (Search) | AI Foundry agents can update index (versioned rule updates) |
| 8 | `id-rulesiq` (user-assigned MI) | ServicePrincipal | `Storage Blob Data Contributor` | `sarulesiqhub` (Storage) | AI Foundry hub writes project metadata |
| 9 | `id-rulesiq` (user-assigned MI) | ServicePrincipal | `Storage Blob Data Reader` | `sadatafileshubcanada` (Storage) | AI Foundry agents read source policy documents |
| 10 | `dev@MngEnvMCAP490549.onmicrosoft.com` (user) | User | `Search Service Contributor` | `ai-search-hub-canada` (Search) | Developer can manage indexes, indexers, skillsets via CLI |
| 11 | `dev@MngEnvMCAP490549.onmicrosoft.com` (user) | User | `Search Index Data Contributor` | `ai-search-hub-canada` (Search) | Developer can create/update/delete index data |
| 12 | `dev@MngEnvMCAP490549.onmicrosoft.com` (user) | User | `Cognitive Services OpenAI User` | `rg-openai-hub` (AIServices) | Developer can test OpenAI calls |
| 13 | `dev@MngEnvMCAP490549.onmicrosoft.com` (user) | User | `Storage Blob Data Contributor` | `sadatafileshubcanada` (Storage) | Developer can upload PDFs |

---

## AI Search Index & Indexer Objects (Created from Code/CLI)

These objects are created **after** infrastructure provisioning using az cli REST calls or the Azure.Search.Documents SDK. They are NOT Bicep resources — they are data-plane objects.

### Data Source
```json
{
  "name": "ds-policy-documents",
  "type": "azureblob",
  "credentials": { "connectionString": "ResourceId=/subscriptions/28d10200-70b0-476c-b004-c6ae29265897/resourceGroups/RG-OpenAI/providers/Microsoft.Storage/storageAccounts/sadatafileshubcanada;" },
  "container": { "name": "policy-documents" },
  "dataDeletionDetectionPolicy": null,
  "dataChangeDetectionPolicy": { "@odata.type": "#Microsoft.Azure.Search.HighWaterMarkChangeDetectionPolicy", "highWaterMarkColumnName": "metadata_storage_last_modified" }
}
```

> **Note:** The `connectionString` uses the `ResourceId=` format for managed identity auth — no keys.

### Index
The index schema is defined in the [indexer-output-schema.md](../contracts/04-indexer/indexer-output-schema.md) contract. Key configuration:

- Vector field: `Vectorized_Content` — `Collection(Edm.Single)`, 3072 dimensions, HNSW algorithm
- Vector profile: uses Azure OpenAI vectorizer pointing to `text-embedding-3-large` deployment
- Semantic configuration: enabled, uses `Content` as content field, `SemanticLabel` as title field

### Skillset
```json
{
  "name": "ss-rule-extraction",
  "description": "Extract rules from policy chunks using custom Web API + OpenAI embeddings",
  "skills": [
    {
      "@odata.type": "#Microsoft.Skills.Text.SplitSkill",
      "name": "chunk-text",
      "context": "/document",
      "textSplitMode": "pages",
      "maximumPageLength": 2000,
      "pageOverlapLength": 200,
      "inputs": [{ "name": "text", "source": "/document/content" }],
      "outputs": [{ "name": "textItems", "targetName": "pages" }]
    },
    {
      "@odata.type": "#Microsoft.Skills.Custom.WebApiSkill",
      "name": "extract-rules",
      "description": "Custom skill to extract RulesEngine rules from chunks",
      "context": "/document/pages/*",
      "uri": "https://app-rulesiq-indexer-skill.azurewebsites.net/api/extract-rules",
      "httpMethod": "POST",
      "authResourceId": "api://512c962b-55aa-4d13-a9fd-e4fa5888c1e5",
      "batchSize": 1,
      "degreeOfParallelism": 2,
      "timeout": "PT60S",
      "inputs": [
        { "name": "content", "source": "/document/pages/*" },
        { "name": "document_id", "source": "/document/metadata_storage_path" },
        { "name": "source_uri", "source": "/document/metadata_storage_path" }
      ],
      "outputs": [
        { "name": "rulesJson", "targetName": "rulesJson" },
        { "name": "hasRules", "targetName": "hasRules" },
        { "name": "ruleCount", "targetName": "ruleCount" },
        { "name": "workflowName", "targetName": "workflowName" }
      ]
    }
  ],
  "cognitiveServices": null,
  "indexProjections": {
    "selectors": [
      {
        "targetIndexName": "idx-rules-iq",
        "parentKeyFieldName": "parent_id",
        "sourceContext": "/document/pages/*",
        "mappings": [
          { "name": "Content", "source": "/document/pages/*" },
          { "name": "RulesJson", "source": "/document/pages/*/rulesJson" },
          { "name": "HasRules", "source": "/document/pages/*/hasRules" },
          { "name": "RuleCount", "source": "/document/pages/*/ruleCount" },
          { "name": "WorkflowName", "source": "/document/pages/*/workflowName" },
          { "name": "SourceDocumentId", "source": "/document/metadata_storage_path" },
          { "name": "SourceUri", "source": "/document/metadata_storage_path" }
        ]
      }
    ],
    "parameters": { "projectionMode": "skipIndexingParentDocuments" }
  }
}
```

### Indexer
```json
{
  "name": "ixr-policy-rules",
  "dataSourceName": "ds-policy-documents",
  "targetIndexName": "idx-rules-iq",
  "skillsetName": "ss-rule-extraction",
  "schedule": { "interval": "PT24H" },
  "parameters": {
    "configuration": {
      "dataToExtract": "contentAndMetadata",
      "parsingMode": "default",
      "imageAction": "none"
    }
  },
  "fieldMappings": [
    { "sourceFieldName": "metadata_storage_path", "targetFieldName": "SourceUri" },
    { "sourceFieldName": "metadata_storage_name", "targetFieldName": "SourceDocumentId" }
  ],
  "outputFieldMappings": []
}
```

---

## Deployment Order

Infrastructure must be deployed in this order due to dependencies:

### Phase 1: Resource Group + Identity
1. Create `rg-rules-iq` resource group
2. Create `id-rulesiq` user-assigned managed identity
3. Create `sarulesiqhub` storage account

### Phase 2: Reconfigure Existing Services
4. Enable system-assigned MI on `ai-search-hub-canada`
5. Enable Semantic Ranker on `ai-search-hub-canada`
6. Disable local auth on `ai-search-hub-canada`
7. Disable shared key access on `sadatafileshubcanada`
8. Create `policy-documents` blob container

### Phase 3: New Services
9. Create App Service Plan `asp-rulesiq`
10. Create App Service `app-rulesiq-indexer-skill` with system-assigned MI
11. Create AI Foundry Hub `rulesiq-ai-hub`
12. Create AI Foundry Project `rulesiq-agent-project`

### Phase 3.5: App Registration & Authentication
13. Create Entra ID app registration for `app-rulesiq-indexer-skill`
14. Set identifier URI to `api://{appId}` (using the actual GUID)
15. Create service principal for the app registration
16. Configure Easy Auth v2 on the App Service (v1 issuer, dual audiences)
17. Restart the App Service to apply Easy Auth changes

### Phase 3.6: App Code Deployment
18. Build and publish `RulesIQ.IndexerSkill` (.NET 8)
19. Deploy zip package to `app-rulesiq-indexer-skill`

### Phase 4: RBAC Assignments
20. Assign all role assignments from the RBAC table above
21. Wait ~60 seconds for RBAC propagation

### Phase 4.5: Upload Policy Documents
22. Upload PDFs to `policy-documents` container (handles `publicNetworkAccess: Disabled`)

### Phase 5: Data-Plane Objects (CLI/SDK)
23. Create AI Search index `idx-rules-iq`
24. Create AI Search data source `ds-policy-documents`
25. Create AI Search skillset `ss-rule-extraction`
26. Create AI Search indexer `ixr-policy-rules`

### Phase 6: AI Foundry Agents (v2 API)
27. Create agent definitions via AI Foundry v2 REST API

---

## Bicep Reproducibility

All infrastructure is codified in `infra/` with the following structure:

```
infra/
  ├── main.bicep                  # Orchestrates all modules
  ├── main.bicepparam             # Parameters file
  ├── modules/
  │     ├── resource-group.bicep  # Resource group (subscription-scoped)
  │     ├── managed-identity.bicep
  │     ├── storage.bicep
  │     ├── app-service.bicep
  │     ├── ai-foundry-hub.bicep
  │     ├── ai-foundry-project.bicep
  │     └── rbac-assignments.bicep
  ├── scripts/
  │     ├── deploy.ps1            # Master deployment script
  │     ├── configure-search.ps1  # Configure existing search service
  │     ├── setup-app-auth.ps1    # Create app registration + Easy Auth v2
  │     ├── deploy-app.ps1        # Build + deploy indexer skill to App Service
  │     ├── upload-policy-docs.ps1 # Upload PDFs (handles network access + container creation)
  │     ├── create-index.ps1      # Create AI Search index (data-plane)
  │     ├── create-indexer.ps1    # Create data source, skillset, indexer
  │     └── create-agents.ps1    # Create AI Foundry agents (v2 API)
  └── definitions/
        ├── index.json            # AI Search index definition
        ├── datasource.json       # AI Search data source definition
        ├── skillset.json         # AI Search skillset definition
        ├── indexer.json          # AI Search indexer definition
        └── agents/
              ├── rule-extraction-agent.json
              ├── rule-normalization-agent.json
              └── evaluation-planning-agent.json
```

---

## Acceptance Criteria

- [ ] All services authenticate via managed identity — zero API keys in use.
- [ ] `disableLocalAuth: true` on OpenAI, AI Search, and Storage.
- [ ] AI Search is Standard SKU with Semantic Ranker enabled.
- [ ] AI Search index uses `text-embedding-3-large` (3072 dimensions) for vector field.
- [ ] AI Search index `id` field uses `keyword` analyzer (required by index projections).
- [ ] AI Search skillset uses `projectionMode: "skipIndexingParentDocuments"` (NOT `generatedKeyAsId`).
- [ ] AI Search skillset `authResourceId` uses `api://{appId}` with the actual GUID (NOT a friendly name).
- [ ] AI Search skillset does NOT pass `page_number` as an input (SplitSkill chunks do not carry page numbers).
- [ ] AI Search skillset does NOT use `authIdentity` — relies on AI Search system-assigned MI by default.
- [ ] Entra ID app registration exists for the App Service with `api://{appId}` identifier URI.
- [ ] Easy Auth v2 is enabled on the App Service with v1 issuer and dual audiences.
- [ ] App Service has the indexer-skill .NET 8 code deployed.
- [ ] Custom Web API skill returns `string.Empty` (not `null`) for optional fields when `hasRules = false`.
- [ ] AI Search indexer, data source, skillset, and index are created from CLI/code.
- [ ] All RBAC assignments from the table are applied.
- [ ] AI Foundry hub + project are created and connected to existing OpenAI resource.
- [ ] AI Foundry agents are created via v2 API using existing model deployments.
- [ ] All embedding consumers use `text-embedding-3-large` consistently.
- [ ] Bicep in `infra/` can recreate all infrastructure from scratch (excluding existing shared resources).
- [ ] Storage upload script handles `publicNetworkAccess: Disabled` by temporarily enabling access.
- [ ] Deployment scripts are idempotent and can be re-run safely.
