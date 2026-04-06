# Meta-Contract Template (Domain-Specific Rule Extraction)

Use this template to create a new domain-specific rule extraction contract. Copy this file, rename it to `meta-[domain-name].md`, and fill in each section.

## Domain
Describe the domain (e.g., "Loan Eligibility in Canada", "Travel Insurance Coverage", "HR Leave Policy").

**Domain Name:** [e.g., Loan Eligibility]

**Industry:** [e.g., Banking, Insurance, Healthcare, HR]

**Jurisdiction:** [e.g., Canada, Ontario, Federal]

**Regulatory Bodies:** [e.g., OSFI, FCAC, provincial regulators]

## Inputs
- `policy_chunk`: a single chunk of policy text from a domain-specific document.
- `domain_context`: short description of the business domain.
- `regulatory_context`: applicable regulatory framework(s).
- `exceptions_context`: known exceptions or special cases in this domain.
- `metadata`: standard chunk metadata (document_id, source_uri, page_number, char_range).

## Expected Output
A RulesEngine workflow with domain-specific rule types, following the standard schema:

```json
{
  "hasRules": true,
  "workflow": {
    "WorkflowName": "[DomainWorkflowName]",
    "Rules": [
      {
        "RuleName": "[DomainSpecificRuleName]",
        "Expression": "input.[Property] [operator] [value]",
        "SuccessEvent": "[description]",
        "ErrorMessage": "[description]",
        "Metadata": { ... }
      }
    ]
  },
  "extractionNotes": "string"
}
```

## Rule Categories
List the categories of rules expected in this domain. Examples:

- Category 1: [description]
- Category 2: [description]
- Category 3: [description]

## Entity Model
Describe the expected `input` object properties for this domain:

| Property | Type | Description |
|----------|------|-------------|
| `input.Property1` | type | description |
| `input.Property2` | type | description |

## Constraints
- No invented thresholds or conditions not present in the policy text.
- No hallucinated conditions or requirements.
- Must follow regulatory language specific to this domain and jurisdiction.
- Must flag ambiguous regulatory text in `extractionNotes`.
- Expressions must be valid C# boolean expressions.

## Edge Cases
List domain-specific edge cases and exceptions:

- Edge case 1: [description and handling strategy]
- Edge case 2: [description and handling strategy]

## Acceptance Criteria
- [ ] Rules reflect domain-specific standards and regulations.
- [ ] No hallucinated financial, legal, or regulatory thresholds.
- [ ] All rules traceable to source documents.
- [ ] Domain-specific terminology is used correctly.
- [ ] Jurisdiction-specific exceptions are handled or flagged.
- [ ] Output conforms to the standard RulesEngine workflow schema.
