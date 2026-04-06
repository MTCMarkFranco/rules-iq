# Prompt Contract: Apply RulesEngine Rules to New Document Chunk (Deterministic Evaluation Plan)

## Inputs
- **Context:**
  - At runtime, application code will:
    - Pull relevant rules from the search index.
    - Execute them using the Microsoft RulesEngine.
    - Use frameworks like AutoMapper and Polly for mapping and resilience.
  - Your job is NOT to execute rules, but to **plan and describe** how to apply them deterministically to a given input model.
- **Provided at runtime:**
  - `rulesWorkflow`: a RulesEngine workflow JSON object.
  - `inputModelDescription`: description of the .NET input model (properties, types, semantics).
  - `document_chunk`: the text being evaluated (e.g., a claim, application, or request).
  - `evaluation_goal`: e.g., "determine eligibility", "flag policy violations".

## Expected Output
- A **JSON object** describing an evaluation plan:

```json
{
  "MappingPlan": {
    "InputProperties": [
      {
        "PropertyName": "Age",
        "Source": "DocumentChunk",
        "ExtractionStrategy": "Parse integer from 'Applicant Age: 25' using regex (\\d+)",
        "Notes": "Extract age from applicant section of the document chunk."
      },
      {
        "PropertyName": "Income",
        "Source": "ExternalSystem",
        "ExtractionStrategy": "Retrieve from CRM via applicant ID lookup",
        "Notes": "Annual gross income in CAD."
      },
      {
        "PropertyName": "Country",
        "Source": "Default",
        "ExtractionStrategy": "Default value: 'CA'",
        "Notes": "Assumed Canada for this workflow."
      }
    ]
  },
  "ExecutionPlan": {
    "WorkflowName": "EligibilityRules",
    "ExpectedOutcomes": [
      {
        "RuleName": "MinimumAgeRequirement",
        "OnSuccess": "Applicant meets minimum age requirement",
        "OnFailure": "Applicant does not meet minimum age — flag for review"
      },
      {
        "RuleName": "MaxDebtToIncomeRatio",
        "OnSuccess": "DTI ratio within acceptable range",
        "OnFailure": "DTI ratio exceeds limit — loan not eligible"
      }
    ]
  },
  "ResiliencePlan": {
    "UsePolly": true,
    "Policies": [
      {
        "Name": "ExternalDataRetryPolicy",
        "Type": "Retry",
        "Notes": "Retry up to 3 times with exponential backoff for external CRM lookups."
      },
      {
        "Name": "SearchIndexCircuitBreaker",
        "Type": "CircuitBreaker",
        "Notes": "Circuit breaker for Azure AI Search queries — open after 5 consecutive failures."
      }
    ]
  }
}
```

- **`MappingPlan`**:
  - How to map fields from `document_chunk` and external systems into the `input` object used by RulesEngine.
  - Each property specifies its source (`DocumentChunk`, `ExternalSystem`, or `Default`) and extraction strategy.
- **`ExecutionPlan`**:
  - Which workflow to run and what outcomes to expect from each rule.
  - `OnSuccess` and `OnFailure` describe the semantic meaning of the outcome (not the technical result).
- **`ResiliencePlan`**:
  - Where resilience patterns (e.g., Polly) should be applied.
  - Resilience applies to **external data lookups**, not to rule evaluation itself (rule evaluation is deterministic and does not need retry).

## Constraints
- **No actual execution:**
  - You MUST NOT simulate or guess rule outcomes.
  - You describe the plan; you do not predict results.
- **Clarity over code:**
  - You describe the plan in structured JSON; you do not generate full implementation code here.
- **Stable mapping:**
  - Mapping strategies MUST be deterministic and repeatable (e.g., "extract Age from field `Applicant.Age` in JSON payload").
- **Resilience scope:**
  - Polly policies apply to I/O operations (search queries, external API calls), NOT to rule evaluation.
  - Rule evaluation is deterministic and synchronous — no retry needed.

## Edge Cases
- **Missing input fields:**
  - If a rule references `input.Property` that is not present in `inputModelDescription`, call this out in `MappingPlan.Notes` and suggest how to resolve (e.g., default value, external lookup, or flag for manual entry).
- **Multiple workflows:**
  - If multiple workflows are present, specify the order of execution and note any dependencies between them.
- **No rules retrieved:**
  - If the search index returns no rules for the given context, the plan should note this and suggest fallback behavior.

## Acceptance Criteria
- [ ] The plan clearly describes how to map document data into the RulesEngine input model.
- [ ] The plan identifies all rules and their expected semantic outcomes (success/failure meaning).
- [ ] The plan suggests where resilience patterns are appropriate without over-engineering.
- [ ] Missing input fields are identified and documented with resolution strategies.
- [ ] No rule outcomes are predicted or simulated — only the plan is described.
