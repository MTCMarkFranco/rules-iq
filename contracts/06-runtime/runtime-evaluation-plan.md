# Prompt Contract: Apply RulesEngine Rules to New Document Chunk (Deterministic Evaluation Plan)

## Core Tenets

### Rules Engine Foundation
The rules engine MUST be based on the **Microsoft RulesEngine** NuGet package (or a fork of the repository if modifications are required):
- **NuGet:** https://www.nuget.org/packages/RulesEngine/
- **Repository:** https://github.com/microsoft/RulesEngine
- All workflow and rule JSON MUST conform to the [RulesEngine workflow schema](https://github.com/microsoft/RulesEngine/blob/main/schema/workflow-schema.json).
- If the NuGet package is insufficient (e.g., custom expression types, extended metadata), fork the repository and maintain changes in a separate branch. Document all deviations.

### Version Fingerprinting
Every evaluation result MUST be fingerprinted with the version of the ruleset used at the time of execution. This ensures auditability and point-in-time reproducibility.

### Compliance Scoring
Every evaluation MUST produce a compliance score representing the percentage of rules that passed out of the total rules evaluated.

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
  },
  "VersionFingerprint": {
    "RulesetVersion": "v2.1.0",
    "RulesetPublishedTimestamp": "2025-03-15T10:30:00Z",
    "SourceDocumentVersions": [
      {
        "SourceDocumentId": "policy-doc-2024-001",
        "DocumentVersion": "2024.1",
        "IngestedTimestamp": "2025-03-14T08:00:00Z"
      }
    ],
    "EvaluationTimestamp": "2025-03-16T14:22:00Z"
  },
  "ComplianceScore": {
    "TotalRulesEvaluated": 7,
    "RulesPassed": 5,
    "RulesFailed": 2,
    "CompliancePercentage": 71.43,
    "FailedRules": [
      {
        "RuleName": "MaxGrossDebtServiceRatio",
        "ErrorMessage": "Applicant GDS ratio must not exceed 39%"
      },
      {
        "RuleName": "MinimumCreditScore",
        "ErrorMessage": "Applicant credit score must be at least 650"
      }
    ]
  },
  "RulesSnapshot": {
    "Description": "Complete snapshot of all rules used in this evaluation for audit trail",
    "WorkflowName": "EligibilityRules",
    "RulesetVersion": "v2.1.0",
    "Rules": [
      {
        "RuleName": "MinimumAgeRequirement",
        "Expression": "input.Age >= 18",
        "Result": "Passed"
      },
      {
        "RuleName": "MaxGrossDebtServiceRatio",
        "Expression": "input.GDS <= 39",
        "Result": "Failed"
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
- **`VersionFingerprint`**:
  - Records the exact ruleset version, source document versions, and timestamps used for this evaluation.
  - `RulesetVersion`: the semantic version of the ruleset at evaluation time.
  - `SourceDocumentVersions`: array listing each source document and when it was ingested.
  - `EvaluationTimestamp`: when the evaluation was executed.
- **`ComplianceScore`**:
  - `TotalRulesEvaluated`: total number of rules executed.
  - `RulesPassed` / `RulesFailed`: counts of passed and failed rules.
  - `CompliancePercentage`: `(RulesPassed / TotalRulesEvaluated) * 100`, rounded to 2 decimal places.
  - `FailedRules`: array of failed rule names and their error messages for quick review.
- **`RulesSnapshot`**:
  - A complete record of every rule (name, expression, result) used in this evaluation.
  - This is the **audit fingerprint** — it allows point-in-time reconstruction of exactly which rules were applied and what version they were.
  - The UI and downstream consumers use this to display the full rule context alongside results.

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
- [ ] The output includes a `VersionFingerprint` with the ruleset version, source document versions, and evaluation timestamp.
- [ ] The output includes a `ComplianceScore` with total, passed, failed counts and a percentage.
- [ ] The output includes a `RulesSnapshot` containing every rule used and its result, stamped with the ruleset version.
- [ ] The JSON output is structured for UI consumption — the AI agent produces this as the primary output format.

## First Evaluation Scenario — Loan Eligibility (Canada)

> **The loan eligibility domain is the FIRST runtime evaluation to execute after the indexing pipeline has processed the 8 Canadian regulatory PDFs.**

### Canonical Test Persona
Use this fictitious applicant as the primary end-to-end validation case. See [meta-loan-eligibility-canada.md](../07-meta/meta-loan-eligibility-canada.md) for the full entity model.

```json
{
  "Age": 34,
  "Province": "ON",
  "ResidencyStatus": "PermanentResident",
  "AnnualIncome": 92000.00,
  "GDS": 41.5,
  "TDS": 43.0,
  "CreditScore": 710,
  "EmploymentStatus": "Employed",
  "EmploymentDurationMonths": 28,
  "LoanAmount": 485000.00,
  "PropertyValue": 625000.00,
  "DownPaymentPercent": 22.4,
  "LTV": 77.6,
  "LoanType": "Mortgage",
  "LenderType": "FederallyRegulated"
}
```

### Expected Evaluation Behavior
1. **Rule Retrieval** — query the `rules-index` with the evaluation context (loan type: Mortgage, jurisdiction: Ontario, lender type: FederallyRegulated). Expect rules from OSFI B-20, B-21, FCAC, and FSRA Ontario documents.
2. **Input Mapping** — map the test persona properties directly to the RulesEngine `input` object (1:1 mapping, no external lookups needed for this test case).
3. **Rule Execution** — execute the `CanadianLoanEligibility` workflow via Microsoft RulesEngine.
4. **Expected Failure** — the `MaxGrossDebtServiceRatio` rule MUST fail because `GDS = 41.5` exceeds the 39% limit defined in OSFI B-20.
5. **Compliance Score** — the result MUST produce a `CompliancePercentage` less than 100%, with `MaxGrossDebtServiceRatio` listed in `FailedRules`.
6. **Version Fingerprint** — the result MUST include `RulesetVersion: "v1.0.0"` and source document versions for all contributing documents.
7. **Rules Snapshot** — the result MUST contain a complete snapshot of all evaluated rules with their expressions and pass/fail results.

### Validation Criteria
- [ ] The runtime engine retrieves relevant rules from the search index using the evaluation context.
- [ ] The Canonical Test Persona input maps correctly to the RulesEngine input model.
- [ ] The `MaxGrossDebtServiceRatio` rule fails as expected.
- [ ] The compliance percentage is less than 100%.
- [ ] The output JSON conforms to the schema defined in this contract (MappingPlan, ExecutionPlan, ResiliencePlan, VersionFingerprint, ComplianceScore, RulesSnapshot).
- [ ] The result is consumable by the UI adapter for display in the compliance dashboard.
