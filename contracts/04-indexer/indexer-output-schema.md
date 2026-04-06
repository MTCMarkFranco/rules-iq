# Indexer Output Schema

This document defines the Azure AI Search index schema for the rules transformation pipeline.

## Core Tenets

### Updatable and Versioned Index
The index MUST support **in-place updates** when a new version of a source document (e.g., an updated OSFI B-21 Guideline PDF) is ingested. This means:
- Re-ingesting a new version of a document MUST update all existing rules derived from that document.
- Old rule versions MUST NOT remain in the active index — they are replaced by the new version.
- Every rule and chunk MUST carry a `RulesetVersion` so consumers know which version of the rules they are using.
- A `SourceDocumentVersion` field tracks which version of the source document produced each rule.
- Historical rule versions MAY be archived (e.g., to a separate index or Cosmos DB) for audit purposes, but the active index always reflects the latest version.

### Version Fingerprinting
Every ruleset in the index MUST be stampable with a version identifier. When the runtime evaluates rules, it records the `RulesetVersion` in its output, creating an audit trail of exactly which rules were applied to any given document or request.

## Index Fields

| Field Name | Type | Description | Searchable | Filterable | Retrievable |
|------------|------|-------------|------------|------------|-------------|
| `id` | `Edm.String` | Unique row identifier (chunk_id) | No | Yes | Yes |
| `Content` | `Edm.String` | Raw text of the indexed chunk | Yes | No | Yes |
| `Vectorized_Content` | `Collection(Edm.Single)` | Vector embedding of the chunk (1536 or 3072 dimensions) | Yes (vector) | No | No |
| `RulesJson` | `Edm.String` | JSON string containing the RulesEngine workflow fragment | Yes | No | Yes |
| `HasRules` | `Edm.Boolean` | Whether this chunk produced any rules | No | Yes | Yes |
| `WorkflowName` | `Edm.String` | The workflow name for rules in this chunk | No | Yes | Yes |
| `RuleCount` | `Edm.Int32` | Number of rules extracted from this chunk | No | Yes | Yes |
| `SourceDocumentId` | `Edm.String` | Identifier of the source document | No | Yes | Yes |
| `SourceUri` | `Edm.String` | URI/path of the source document | No | No | Yes |
| `SourceDocumentVersion` | `Edm.String` | Version of the source document (e.g., "2024.1", "B-21 Rev 2025") | No | Yes | Yes |
| `RulesetVersion` | `Edm.String` | Semantic version of the ruleset (e.g., "v2.1.0") | No | Yes | Yes |
| `RulesetPublishedTimestamp` | `Edm.DateTimeOffset` | When this version of the ruleset was published | No | Yes | Yes |
| `PageNumber` | `Edm.Int32` | Page number in the source document | No | Yes | Yes |
| `SemanticLabel` | `Edm.String` | Semantic section label (e.g., "Eligibility Criteria") | Yes | Yes | Yes |
| `IndexedTimestamp` | `Edm.DateTimeOffset` | When this row was indexed | No | Yes | Yes |
| `SupersededBy` | `Edm.String` | If this row was replaced, the version that replaced it (null if current) | No | Yes | Yes |

## RulesJson Field Format

The `RulesJson` field stores a serialized JSON string with the following structure:

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
        "CharRange": { "Start": 123, "End": 456 }
      }
    }
  ]
}
```

When `hasRules = false`:
```json
{
  "hasRules": false,
  "WorkflowName": null,
  "RulesetVersion": null,
  "SourceDocumentVersion": null,
  "Rules": []
}
```

## Querying Rules from the Index

### Retrieve all rules for a workflow
```
$filter=HasRules eq true and WorkflowName eq 'EligibilityRules'
$select=RulesJson,Content,SourceUri,PageNumber
```

### Retrieve rules by source document
```
$filter=HasRules eq true and SourceDocumentId eq 'doc123'
$select=RulesJson,Content,PageNumber
```

### Vector search + rules filter
Use hybrid search to find relevant chunks by semantic similarity and filter to only those with rules:
```
$filter=HasRules eq true
```
Combined with a vector query on `Vectorized_Content`.

## Design Notes
- `RulesJson` is stored as a string (not a complex type) for maximum flexibility and compatibility.
- Application code deserializes `RulesJson` into RulesEngine workflow objects at runtime.
- `HasRules` and `WorkflowName` are extracted as top-level filterable fields for efficient querying.
- `RuleCount` enables quick analytics (e.g., "how many rules were extracted from this document?").
- `RulesetVersion` and `SourceDocumentVersion` are top-level filterable fields to support versioned queries.
- `SupersededBy` is `null` for current/active rules and set to the replacing version for archived rules.

## Document Version Update Strategy

When a new version of a source document is ingested (e.g., OSFI B-21 Guideline Rev 2025 replaces Rev 2024):

### Update Process
1. **Ingest the new PDF** through the standard pipeline (cracking → chunking → extraction → normalization).
2. **Assign a new `RulesetVersion`** (e.g., `v2.1.0` → `v3.0.0`) and `SourceDocumentVersion` (e.g., `"2024.1"` → `"2025.1"`).
3. **Identify existing index rows** for the same `SourceDocumentId` and `WorkflowName`.
4. **Replace existing rows** with the new version's rules. The `id` field may be reused (overwrite) or new IDs generated (with old rows deleted).
5. **Optionally archive** the old version by copying to an archive index or Cosmos DB before deletion.
6. **Update `RulesetPublishedTimestamp`** on all new rows.

### Querying by Version
```
$filter=HasRules eq true and RulesetVersion eq 'v3.0.0'
$select=RulesJson,Content,RulesetVersion,SourceDocumentVersion
```

### Querying Active (Latest) Rules
```
$filter=HasRules eq true and SupersededBy eq null
$select=RulesJson,Content,RulesetVersion
```

### Versioned Audit Query
To find which rules were active at a specific point in time:
```
$filter=HasRules eq true and RulesetPublishedTimestamp le 2025-03-15T00:00:00Z
$orderby=RulesetPublishedTimestamp desc
```
