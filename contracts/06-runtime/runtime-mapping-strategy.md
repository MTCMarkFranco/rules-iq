# Runtime Mapping Strategy

This document describes the strategy for mapping data from document chunks and external systems into the RulesEngine input model at runtime.

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
    ↓ [RulesEngine.ExecuteAllRulesAsync]
Rule Results
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
