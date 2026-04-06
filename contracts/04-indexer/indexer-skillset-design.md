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
7. **Index Projection** — write `Content`, `Vectorized_Content`, `RulesJson` to index
