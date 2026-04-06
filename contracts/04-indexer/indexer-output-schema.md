# Indexer Output Schema

This document defines the Azure AI Search index schema for the rules transformation pipeline.

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
| `PageNumber` | `Edm.Int32` | Page number in the source document | No | Yes | Yes |
| `SemanticLabel` | `Edm.String` | Semantic section label (e.g., "Eligibility Criteria") | Yes | Yes | Yes |
| `IndexedTimestamp` | `Edm.DateTimeOffset` | When this row was indexed | No | Yes | Yes |

## RulesJson Field Format

The `RulesJson` field stores a serialized JSON string with the following structure:

```json
{
  "hasRules": true,
  "WorkflowName": "EligibilityRules",
  "Rules": [
    {
      "RuleName": "MinimumAgeRequirement",
      "Expression": "input.Age >= 18",
      "SuccessEvent": "Age OK",
      "ErrorMessage": "Customer must be 18 or older",
      "Metadata": {
        "SourceDocumentId": "doc123",
        "SourceUri": "https://contoso.com/policies/eligibility.pdf",
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
