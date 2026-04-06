# Runtime Mapping Strategy

This document describes the strategy for mapping data from document chunks and external systems into the RulesEngine input model at runtime.

## Core Tenets

### Rules Engine Foundation
The runtime MUST use the **Microsoft RulesEngine** NuGet package (or a fork if modifications are required):
- **NuGet:** https://www.nuget.org/packages/RulesEngine/
- **Repository:** https://github.com/microsoft/RulesEngine
- All workflow JSON consumed at runtime MUST conform to the [RulesEngine workflow schema](https://github.com/microsoft/RulesEngine/blob/main/schema/workflow-schema.json).

### Version-Aware Execution
The runtime MUST be version-aware: every execution must know which ruleset version it is running against, and stamp the results accordingly. Rules retrieved from the search index carry a `RulesetVersion` field — the runtime must propagate this through to the evaluation output.

## Mapping Architecture

### AutoMapper Integration
Use AutoMapper to define deterministic, testable mapping profiles between source data and the RulesEngine input model.

**Mapping Flow:**
```
Document Chunk (raw text)
    ↓ [Field Extraction]
Extracted Fields (dictionary or DTO)
    ↓ [AutoMapper Profile]
RulesEngine Input Model (strongly-typed object)
    ↓ [Load Versioned Rules from Index]
Versioned RulesEngine Workflow (with RulesetVersion)
    ↓ [RulesEngine.ExecuteAllRulesAsync]
Rule Results + Version Fingerprint + Compliance Score
```

### Mapping Sources
Each property in the input model comes from one of three sources:

| Source | Description | Example |
|--------|-------------|---------|
| `DocumentChunk` | Extracted directly from the text being processed | Age parsed from "Applicant Age: 25" |
| `ExternalSystem` | Retrieved from an external API or database | Income from CRM lookup |
| `Default` | A constant or computed default value | Country = "CA" for Canadian workflow |

### Field Extraction Strategies
For `DocumentChunk` sources, use deterministic extraction methods:

1. **Regex extraction** — for structured text patterns (e.g., "Age: 25", "Income: $75,000")
2. **JSON path** — if the document chunk is structured JSON
3. **Named entity recognition** — for semi-structured text (use Azure AI Language or custom NER)
4. **Key-value parsing** — for form-like documents

**Important:** Field extraction must be deterministic. The same chunk must always produce the same extracted values.

## AutoMapper Profile Example
```
Source: ExtractedFieldsDto
    - Age (int)
    - Income (decimal)
    - Country (string)
    - EmploymentStatus (string)

Destination: RuleInput
    - Age (int) ← direct map
    - Income (decimal) ← direct map
    - Country (string) ← direct map
    - IsEmployed (bool) ← map from EmploymentStatus == "Employed"
```

## Validation Before Execution
Before passing the mapped input to RulesEngine:

1. **Required field check** — verify all fields referenced by rule expressions are present.
2. **Type check** — verify field types match expected types (int, string, bool, decimal).
3. **Range check** — verify numeric values are within reasonable bounds (e.g., Age > 0, Income >= 0).
4. **Null handling** — decide on null behavior: fail-fast, default value, or skip rule.

## Resilience with Polly

### Where to Apply Polly
| Operation | Polly Policy | Rationale |
|-----------|-------------|-----------|
| Azure AI Search query | Retry + Circuit Breaker | Network I/O, transient failures |
| External CRM/API lookup | Retry + Timeout | Network I/O, slow responses |
| Azure OpenAI call (if any) | Retry + Circuit Breaker | API rate limits, transient errors |
| RulesEngine execution | **None** | Deterministic, in-memory, no I/O |
| AutoMapper mapping | **None** | In-memory transformation, no I/O |

### Where NOT to Apply Polly
- **Rule evaluation** is deterministic and in-memory — retry is meaningless.
- **AutoMapper** transformations are pure functions — they either succeed or throw due to config errors.

## Error Handling Strategy
| Error Type | Handling |
|------------|----------|
| Missing required field | Log warning, skip rule or use default, include in result metadata |
| Type mismatch | Log error, fail rule evaluation for that input |
| External system unavailable | Polly retry/circuit breaker, degrade gracefully |
| Rule expression invalid | Log error, flag rule for review, do not execute |
| No rules found in index | Return empty result set with explanation |
| Ruleset version mismatch | Log warning, use latest available version, record discrepancy in output |

## Version Fingerprinting at Runtime
After rule execution, the runtime MUST produce a version fingerprint attached to every result:

| Field | Source | Description |
|-------|--------|-------------|
| `RulesetVersion` | Index field on retrieved rules | Semantic version of the ruleset (e.g., `v2.1.0`) |
| `SourceDocumentVersions` | Index metadata | Array of source document IDs and their versions |
| `EvaluationTimestamp` | System clock | ISO 8601 timestamp of when the evaluation was executed |

This fingerprint is included in the `VersionFingerprint` section of the evaluation output JSON.

## Compliance Score Calculation
After all rules have been evaluated, the runtime MUST compute a compliance score:

```
CompliancePercentage = (RulesPassed / TotalRulesEvaluated) * 100
```

- Round to 2 decimal places.
- Include the count of total, passed, and failed rules.
- Include the list of failed rule names and their error messages.
- This is included in the `ComplianceScore` section of the evaluation output JSON.

## Rules Snapshot for Audit
The evaluation output MUST include a `RulesSnapshot` object containing:
- The complete list of rules that were evaluated (name, expression, result).
- The `RulesetVersion` at the time of evaluation.
- The `WorkflowName` that was executed.

This snapshot ensures that even if rules are updated later, the exact rules used for any historical evaluation can be reconstructed.
