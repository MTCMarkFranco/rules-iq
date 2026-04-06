# Prompt Contract: Normalize & Validate Candidate Rules (RulesEngine Workflow)

## Inputs
- **Context:**
  - You receive **candidate rules** generated from policy chunks.
  - Your job is to normalize, validate, and consolidate them into a **final RulesEngine workflow** that is syntactically and semantically coherent.
- **Provided at runtime:**
  - `candidateWorkflows`: array of workflow objects from previous step (each with `WorkflowName`, `Rules`, `Metadata`, `extractionNotes`).
  - `targetWorkflowName`: optional string to unify multiple candidate workflows under a single workflow name.
  - `domain_context`: optional domain description (e.g., "mortgage underwriting").
- **RulesEngine expectations (high level):**
  - A workflow is a JSON object with:
    - `WorkflowName` (string)
    - `Rules` (array of rule objects)
  - Rules may include:
    - `RuleName`, `Expression`, `SuccessEvent`, `ErrorMessage`, `RuleExpressionType`, `LocalParams`, `Actions`.

## Expected Output
- A **single JSON object**:

```json
{
  "WorkflowName": "EligibilityRules",
  "Rules": [
    {
      "RuleName": "MinimumAgeRequirement",
      "Expression": "input.Age >= 18",
      "SuccessEvent": "Age OK",
      "ErrorMessage": "Customer must be 18 or older",
      "RuleExpressionType": "LambdaExpression",
      "LocalParams": [],
      "Actions": [],
      "Metadata": {
        "SourceDocuments": [
          {
            "SourceDocumentId": "doc123",
            "SourceUri": "https://contoso.com/policies/eligibility.pdf",
            "PageNumber": 3,
            "CharRange": {
              "Start": 123,
              "End": 456
            }
          }
        ]
      }
    }
  ],
  "NormalizationNotes": "Merged 3 equivalent age rules into 1. Resolved naming conflict between MinAgeRule and AgeCheck."
}
```

- **`WorkflowName`**:
  - If `targetWorkflowName` is provided, use it.
  - Otherwise, infer a stable, descriptive name from the candidate workflows.
- **`Rules`**:
  - Deduplicated, normalized, and merged where appropriate.
  - Each rule MUST be syntactically valid and executable by RulesEngine.
- **`Metadata`**:
  - Aggregated from all contributing candidate rules (multiple source documents/chunks may map to the same normalized rule).
- **`NormalizationNotes`**:
  - Explanation of:
    - How rules were merged or deduplicated.
    - Any conflicts resolved.
    - Any rules discarded and why.

## Constraints
- **No semantic drift:**
  - You MUST preserve the original intent of the policy text.
  - You MUST NOT weaken or strengthen conditions without clear justification.
- **Deduplication:**
  - If multiple candidate rules express the same condition with minor wording differences, you SHOULD merge them into a single rule.
- **Conflict resolution:**
  - If two rules conflict:
    - Prefer the stricter rule when safety/compliance is implied.
    - Document the conflict and resolution in `NormalizationNotes`.
- **Expression validity:**
  - Expressions MUST be valid C# boolean expressions referencing `input`.
  - You MUST NOT introduce side effects or external dependencies.
- **Metadata preservation:**
  - You MUST preserve traceability back to all source chunks that contributed to each rule.
- **RuleExpressionType:**
  - Default to `"LambdaExpression"` unless there is a clear reason to use another supported type.

## Edge Cases
- **Empty candidate set:**
  - If `candidateWorkflows` is empty or all have `hasRules = false`, output a workflow with:
    - `Rules = []`
    - `NormalizationNotes` explaining that no executable rules were found.
- **Overlapping but not identical rules:**
  - Example: `input.Age >= 18` and `input.Age >= 21`.
  - Treat as distinct rules unless the policy clearly indicates one supersedes the other.
- **Inconsistent naming:**
  - Example: `MinAgeRule`, `MinimumAgeRequirement`, `AgeCheck`.
  - Normalize to a single, descriptive `RuleName` and list aliases in `NormalizationNotes`.

## Acceptance Criteria
- [ ] The output workflow is syntactically valid for the Microsoft RulesEngine.
- [ ] Duplicate or trivially equivalent rules are merged without losing traceability.
- [ ] Conflicting rules are either both preserved with clear notes or resolved with justification in `NormalizationNotes`.
- [ ] Every rule includes at least one `SourceDocuments` entry in `Metadata`.
- [ ] No rule introduces invented conditions or thresholds not present in the candidate rules.
- [ ] `RuleExpressionType` is set consistently and appropriately (default `"LambdaExpression"`).
