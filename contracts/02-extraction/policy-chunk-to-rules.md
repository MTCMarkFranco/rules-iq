# Prompt Contract: Policy Chunk → Candidate Rules (RulesEngine JSON)

## Inputs
- **Context:**
  - You are part of an offline, deterministic rules compilation pipeline.
  - The goal is to convert unstructured policy text into **candidate business rules** compatible with the Microsoft `.NET RulesEngine` JSON schema.
- **Technical References (summarized):**
  - RulesEngine repo: workflows, rules, `Expression`, `SuccessEvent`, `ErrorMessage`, `RuleName`, `RuleExpressionType`, `LocalParams`, `Actions`.
  - Rules are typically grouped into **workflows** (e.g., `"WorkflowName": "EligibilityRules"`).
- **Provided at runtime:**
  - `policy_chunk`: A single chunk of policy text (plain text) extracted from a larger document.
  - `chunk_metadata`:
    - `document_id` (string)
    - `source_uri` (string URL or identifier)
    - `page_number` (int, if available)
    - `char_range` or `offset` (start/end indices within the original document, if available)
  - `domain_context` (optional, short description of the business domain, e.g., "insurance underwriting", "loan eligibility", "HR leave policy").
- **Target schema (high level):**
  - A **workflow** with:
    - `WorkflowName` (string)
    - `Rules` (array of rule objects)
  - Each **rule** with:
    - `RuleName` (string, human readable, stable)
    - `Expression` (string, C# expression compatible with RulesEngine)
    - `SuccessEvent` (string, short description)
    - `ErrorMessage` (string, short description of failure)
    - Optional: `RuleExpressionType`, `LocalParams`, `Actions`.

## Expected Output
- A **single JSON object** with this shape:

```json
{
  "hasRules": true,
  "workflow": {
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
          "CharRange": {
            "Start": 123,
            "End": 456
          }
        }
      }
    ]
  },
  "extractionNotes": "Extracted 1 rule from eligibility criteria section."
}
```

- **`hasRules`**:
  - `true` if the chunk contains at least one enforceable, machine-executable rule.
  - `false` if the chunk is purely explanatory, definitional, or non-actionable.
- **`workflow`**:
  - If `hasRules` is `false`, `workflow` MUST be `null`.
  - If `hasRules` is `true`, `workflow` MUST be a valid RulesEngine workflow object.
- **`Rules`**:
  - Each rule MUST be atomic and executable (no vague language like "as appropriate").
  - Expressions MUST be written as C# boolean expressions referencing an abstract `input` object (e.g., `input.Age >= 18 && input.Country == "CA"`).
- **`Metadata`**:
  - MUST map each rule back to the originating chunk and document using the provided metadata.
- **`extractionNotes`**:
  - Short free-text explanation of how the rules were derived, including any assumptions or ambiguities.

## Constraints
- **Determinism over creativity:**
  - You MUST prioritize precision, traceability, and explicitness over creativity or paraphrasing.
  - If the policy language is ambiguous, you MUST:
    - Either not create a rule, OR
    - Create a rule but clearly document the ambiguity in `extractionNotes`.
- **No invented thresholds or conditions:**
  - You MUST NOT invent numbers, thresholds, or conditions that are not explicitly present or clearly implied in the text.
- **Atomic rules:**
  - Each rule SHOULD represent a single logical condition or a tightly related set of conditions.
- **Stable naming:**
  - `WorkflowName` SHOULD be derived from the policy topic (e.g., "LoanEligibility", "TravelInsuranceCoverage").
  - `RuleName` SHOULD be short, descriptive, and stable (e.g., `MinimumAgeRequirement`, `MaxLoanToIncomeRatio`).
- **Expression format:**
  - Expressions MUST be valid C# boolean expressions referencing `input` (e.g., `input.Age >= 18`).
  - You MUST NOT reference external services, APIs, or side effects.
- **No runtime decisions:**
  - You are not executing rules; you are only describing them in a deterministic schema.

## Edge Cases
- **Chunk contains no enforceable rule:**
  - Example: definitions, examples, background context.
  - Output: `hasRules = false`, `workflow = null`, `extractionNotes` explains why.
- **Chunk contains partial rule (missing key parameter):**
  - Example: "The customer must meet the minimum age requirement" with no age specified.
  - Either:
    - Do not create a rule and explain in `extractionNotes`, OR
    - Create a rule with a clearly marked placeholder (e.g., `input.Age >= MIN_AGE_PLACEHOLDER`) and explain in `extractionNotes`.
- **Conflicting statements within the same chunk:**
  - Example: "Minimum age is 18" and "In some cases, 16 may be allowed."
  - Prefer the stricter, more general rule and document the exception in `extractionNotes`.
- **Non-deterministic language:**
  - Phrases like "at the discretion of", "where appropriate", "may consider".
  - You MUST NOT convert these into hard rules; instead, explain in `extractionNotes`.

## Acceptance Criteria
- [ ] For a chunk with clear, numeric eligibility criteria, the output contains `hasRules = true` and at least one valid rule with a correct C# expression.
- [ ] For a purely explanatory chunk, the output contains `hasRules = false` and `workflow = null`.
- [ ] No rule includes invented thresholds or conditions not present in the text.
- [ ] Every rule includes `Metadata` that correctly reflects the provided `document_id`, `source_uri`, and positional info.
- [ ] The JSON is syntactically valid and conforms to the high-level RulesEngine workflow + rules structure.
- [ ] Ambiguous or partial rules are either omitted or clearly flagged in `extractionNotes`.
