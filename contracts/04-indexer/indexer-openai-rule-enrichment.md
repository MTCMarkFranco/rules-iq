# Prompt Contract: Azure AI Search Indexer Enrichment → Rules Column

## Inputs
- **Context:**
  - You are called from an Azure AI Search Indexer as part of a skillset (OpenAI skill or custom Web API skill).
  - Each call processes a single **index row** (chunk).
  - The goal is to decide whether this chunk should produce rules and, if so, emit a RulesEngine workflow fragment.
- **Provided at runtime:**
  - `content`: the text of the chunk being indexed.
  - `document_id` (string): unique identifier for the source document.
  - `source_uri` (string): URL or path to the source document.
  - `page_number` (int, if available): page in the original document.
  - `char_range` (object, if available): `{ "start": int, "end": int }`.
  - Optional: `existing_rules_column` (string, if re-indexing or updating).
  - Optional: `workflow_hint` (string, e.g., "EligibilityRules", "CoverageRules").
  - Optional: `source_document_version` (string, e.g., "2024.1", "B-21 Rev 2025").
  - Optional: `ruleset_version` (string, e.g., "v2.1.0") — assigned by the pipeline orchestrator.
- **Index schema (conceptual):**
  - `Content` (string) — the raw chunk text
  - `Vectorized_Content` (vector) — embedding of the chunk
  - `RulesJson` (string) — JSON representation of workflow or rule collection

## Expected Output
- A **JSON object** to be written into the `RulesJson` column:

```json
{
  "hasRules": true,
  "WorkflowName": "EligibilityRules",
  "RulesetVersion": "v2.1.0",
  "SourceDocumentVersion": "2024.1",
  "Rules": [
    {
      "RuleName": "MinimumAgeRequirement",
      "Expression": "input.Age >= 18",
      "SuccessEvent": "Age OK",
      "ErrorMessage": "Customer must be 18 or older",
      "Metadata": {
        "SourceDocumentId": "doc123",
        "SourceUri": "https://contoso.com/policies/eligibility.pdf",
        "SourceDocumentVersion": "2024.1",
        "PageNumber": 3,
        "CharRange": {
          "Start": 123,
          "End": 456
        }
      }
    }
  ]
}
```

- If `hasRules = false`, `WorkflowName` MUST be `null` and `Rules` MUST be an empty array `[]`.
- If `hasRules = true`, `WorkflowName` MUST be set (use `workflow_hint` if provided, otherwise infer from content).
- If `ruleset_version` is provided, include it as `RulesetVersion` in the output. If not provided, omit the field (the pipeline orchestrator will assign it).
- If `source_document_version` is provided, include it as `SourceDocumentVersion` in the output and in each rule's `Metadata`.

## Constraints
- **Deterministic decision:**
  - You MUST decide `hasRules` based solely on the chunk content and clear policy language.
  - Do NOT mark `hasRules = true` for vague or non-actionable text.
- **No cross-chunk reasoning:**
  - You only see one chunk at a time; you MUST NOT assume context from other chunks.
- **Schema stability:**
  - The shape of the JSON MUST remain stable across calls so the index schema is predictable.
- **Idempotency:**
  - Given the same input chunk and metadata, you MUST produce the same output JSON.
- **Expression format:**
  - Expressions MUST be valid C# boolean expressions referencing `input`.
  - No external service calls, no side effects.

## Edge Cases
- **Chunk references rules defined elsewhere:**
  - Example: "See Section 3.2 for eligibility criteria."
  - Output: `hasRules = false`, explainable by the fact that the chunk is a pointer, not a rule.
- **Chunk partially describes a rule:**
  - If critical parameters are missing, prefer `hasRules = false` and let another chunk carry the rule.
- **Existing rules present (document version update):**
  - If `existing_rules_column` is provided, you MAY:
    - Preserve it if the chunk content has not changed.
    - Replace it if the chunk content has changed significantly (this decision is typically made by the orchestrating code, not you).
  - When a new version of a source document is ingested, the pipeline orchestrator will increment `ruleset_version` and `source_document_version`. The enrichment skill should include these in the output so the index is properly versioned.
- **Empty or whitespace-only content:**
  - Output: `hasRules = false`, `WorkflowName = null`, `Rules = []`.

## Acceptance Criteria
- [ ] For chunks with clear, enforceable conditions, `hasRules = true` and at least one rule is emitted.
- [ ] For chunks that are purely narrative or referential, `hasRules = false`.
- [ ] The JSON is always valid and matches the expected schema.
- [ ] The same input always yields the same `RulesJson` output.
- [ ] No hallucinated rules — every rule traces directly to text in the chunk.
