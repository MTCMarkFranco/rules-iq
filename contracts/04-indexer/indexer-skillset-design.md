# Indexer Skillset Design

This document describes the design of the Azure AI Search Indexer skillset for rule enrichment.

## Skillset Architecture

### Option A: Azure OpenAI Skill (Built-in)
Use the built-in Azure OpenAI skill in the indexer pipeline to call an Azure OpenAI deployment with the prompt contract defined in `indexer-openai-rule-enrichment.md`.

**Pros:**
- No custom code required
- Managed by Azure AI Search
- Simple configuration

**Cons:**
- Limited control over prompt construction
- May not support complex pre/post-processing
- Token limits per call

**Configuration:**
```json
{
  "@odata.type": "#Microsoft.Skills.Custom.AmlSkill",
  "name": "rule-extraction-skill",
  "description": "Extract RulesEngine rules from policy chunks",
  "context": "/document/pages/*",
  "uri": "[Azure OpenAI endpoint]",
  "inputs": [
    { "name": "content", "source": "/document/pages/*/content" },
    { "name": "document_id", "source": "/document/metadata_storage_path" },
    { "name": "page_number", "source": "/document/pages/*/pageNumber" }
  ],
  "outputs": [
    { "name": "RulesJson", "targetName": "rulesJson" }
  ]
}
```

### Option B: Custom Web API Skill (Recommended)
Build a custom Web API skill that:
1. Receives the chunk content and metadata
2. Constructs the full prompt using the prompt contract
3. Calls Azure OpenAI with the prompt
4. Validates the response against the expected schema
5. Returns the `RulesJson` to the indexer

**Pros:**
- Full control over prompt construction
- Can add validation and retry logic (Polly)
- Can log and audit extraction decisions
- Can add pre-processing (e.g., entity detection before rule extraction)

**Cons:**
- Requires deploying and maintaining a Web API
- Additional infrastructure cost

**Web API Contract:**
```
POST /api/extract-rules
Content-Type: application/json

{
  "values": [
    {
      "recordId": "1",
      "data": {
        "content": "Applicants must be at least 18 years old...",
        "document_id": "doc123",
        "source_uri": "https://contoso.com/policies/eligibility.pdf",
        "page_number": 3,
        "char_range": { "start": 123, "end": 456 },
        "workflow_hint": "EligibilityRules"
      }
    }
  ]
}
```

**Response:**
```json
{
  "values": [
    {
      "recordId": "1",
      "data": {
        "rulesJson": "{\"hasRules\": true, \"WorkflowName\": \"EligibilityRules\", ...}"
      }
    }
  ]
}
```

## Recommendation
**Use Option B (Custom Web API Skill)** for production workloads because:
- You need deterministic, validated rule extraction
- You need audit logging of every extraction decision
- You need retry/resilience via Polly
- You need to enforce the prompt contract schema on every response
- The OpenAI skill alone cannot guarantee schema compliance

## Skillset Pipeline Order
1. **Document Cracking** — extract text from PDF/Word
2. **Text Split** — chunk into pages/sections
3. **Language Detection** — identify language per chunk
4. **Entity Recognition** (optional) — pre-identify entities
5. **Rule Extraction** (Custom Web API Skill) — apply prompt contract
6. **Embedding** — vectorize chunk content
7. **Version Stamping** — assign `RulesetVersion` and `SourceDocumentVersion` to each rule row
8. **Index Projection** — write `Content`, `Vectorized_Content`, `RulesJson`, version fields to index

## Document Version Update Pipeline

When a new version of a source document is ingested (e.g., OSFI B-21 Rev 2025 replaces Rev 2024):

### Orchestration Flow
1. **Detect document identity** — match the incoming document to an existing `SourceDocumentId` (by URI, title, or explicit mapping).
2. **Assign new version** — increment `SourceDocumentVersion` (e.g., `"2024.1"` → `"2025.1"`) and `RulesetVersion` (e.g., `"v2.1.0"` → `"v3.0.0"`).
3. **Run the full pipeline** — crack, chunk, extract, normalize, embed, and version-stamp the new document.
4. **Replace index rows** — delete or overwrite all existing rows for the same `SourceDocumentId` with the new version's rows.
5. **Archive old version** (optional) — copy old rows to an archive store before deletion for audit.
6. **Update `RulesetPublishedTimestamp`** — set to the current timestamp on all new rows.

### Versioning Rules
- `SourceDocumentVersion` is a free-form string assigned by the pipeline operator (e.g., `"2024.1"`, `"B-21 Rev 2025"`).
- `RulesetVersion` follows [Semantic Versioning](https://semver.org/):
  - **Major** — document replaced or significant rule changes
  - **Minor** — new rules added, no existing rules removed
  - **Patch** — minor corrections or metadata updates
- The pipeline orchestrator is responsible for version assignment; the AI enrichment skill simply propagates the version it receives.

## First Pipeline Run — Loan Eligibility (Canada)

> **The loan eligibility meta-contract is the FIRST domain to be pushed through the indexing pipeline after infrastructure deployment.**

### Source Documents
Upload the following 8 PDFs from `C:\rules-iq\meta-contracts\loan-eligibility\` to the `policy-documents` blob container in `sadatafileshubcanada`:

1. `OSFI-Guideline-B20-Residential-Mortgage-Underwriting.pdf`
2. `OSFI-Guideline-B21-Mortgage-Insurance-Underwriting.pdf`
3. `FCAC-CG9-Mortgage-Prepayment-Penalty-Disclosure.pdf`
4. `FCAC-Commissioner-Guidance-Overview.pdf`
5. `FCAC-Guideline-Appropriate-Products-Services-Banks.pdf`
6. `FCAC-Guideline-Mortgage-Loans-Exceptional-Circumstances.pdf`
7. `FSRA-Ontario-CU0063INT-Residential-Mortgage-Lending.pdf`
8. `FSRA-Ontario-Guidance-Overview-Credit-Unions.pdf`

### Execution Steps
1. Upload all 8 PDFs to blob storage using managed identity auth (az cli or SDK — no keys).
2. Trigger the indexer `idx-policy-rules` — this runs the full skillset pipeline.
3. Monitor indexer status via `az rest` or the Search REST API until all documents are processed.
4. Validate the `rules-index` contains rules extracted from each document — expect rules in categories: Minimum Age, Residency, Income, GDS/TDS, Employment, Credit Score, LTV, Stress Test, Down Payment, Provincial Exceptions.
5. Run a semantic search query (e.g., "mortgage stress test qualifying rate") to confirm semantic ranker returns relevant rules.

### Initial Version Assignment
- `SourceDocumentVersion`: use the document's own revision identifier (e.g., `"B-20 Rev 2024"`, `"CG-9 2022"`).
- `RulesetVersion`: `"v1.0.0"` — this is the initial extraction.
- `RulesetPublishedTimestamp`: set to the indexer run timestamp.

### Validation Criteria
- [ ] All 8 documents are cracked, chunked, and processed without indexer errors.
- [ ] Extracted rules are valid RulesEngine JSON conforming to the workflow schema.
- [ ] Each rule has traceability metadata (source_uri, page_number, char_range).
- [ ] Vector embeddings (3072-dim) are present for all indexed chunks.
- [ ] Semantic search returns relevant results for loan eligibility queries.
