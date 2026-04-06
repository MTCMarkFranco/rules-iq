# Meta-Contract: Loan Eligibility (Canada)

## Purpose
Define how to extract, normalize, and validate rules for **Canadian Loan Eligibility** from unstructured policy documents. This meta-contract governs the behavior of the extraction pipeline when processing lending policy documents under Canadian federal and provincial regulations.

## Domain
- **Domain Name:** Loan Eligibility
- **Industry:** Banking / Financial Services
- **Jurisdiction:** Canada (Federal + Provincial)
- **Regulatory Bodies:** OSFI (Office of the Superintendent of Financial Institutions), FCAC (Financial Consumer Agency of Canada), provincial regulators

## Inputs
- `policy_chunk`: a single chunk of lending policy text.
- `province_context` (optional): e.g., "Ontario", "British Columbia", "Quebec".
- `loan_type` (optional): e.g., "personal loan", "mortgage", "line of credit", "auto loan".
- `regulatory_context`: applicable regulatory framework (e.g., "OSFI B-20", "FCAC Code of Conduct").
- `lender_type` (optional): "federally regulated" or "provincially regulated".
- `metadata`: standard chunk metadata (document_id, source_uri, page_number, char_range).
- `source_document_version` (optional): version of the source document (e.g., "B-20 Rev 2024", "B-21 Rev 2025").
- `ruleset_version` (optional): semantic version of the ruleset (e.g., "v2.1.0").

## Expected Output
A RulesEngine workflow with Canadian lending rules, such as:

```json
{
  "hasRules": true,
  "workflow": {
    "WorkflowName": "CanadianLoanEligibility",
    "RulesetVersion": "v2.1.0",
    "SourceDocumentVersion": "B-20 Rev 2024",
    "Rules": [
      {
        "RuleName": "MinimumAgeRequirement",
        "Expression": "input.Age >= 18 || (input.Province == \"AB\" && input.Age >= 18)",
        "SuccessEvent": "Applicant meets minimum age requirement",
        "ErrorMessage": "Applicant does not meet minimum age requirement",
        "Metadata": { ... }
      },
      {
        "RuleName": "MaxGrossDebtServiceRatio",
        "Expression": "input.GDS <= 39",
        "SuccessEvent": "GDS ratio within limit",
        "ErrorMessage": "GDS ratio exceeds 39% limit",
        "Metadata": { ... }
      },
      {
        "RuleName": "MaxTotalDebtServiceRatio",
        "Expression": "input.TDS <= 44",
        "SuccessEvent": "TDS ratio within limit",
        "ErrorMessage": "TDS ratio exceeds 44% limit",
        "Metadata": { ... }
      }
    ]
  },
  "extractionNotes": "Extracted 3 rules from OSFI B-20 guideline section on debt service ratios."
}
```

**Note:** When OSFI updates a guideline (e.g., B-20 is revised), the pipeline should:
1. Ingest the new PDF with an incremented `source_document_version` (e.g., `"B-20 Rev 2025"`).
2. Assign a new `ruleset_version` (e.g., `"v3.0.0"`).
3. Replace all existing rules for that `SourceDocumentId` in the index.

## Rule Categories
The following categories of rules are expected when processing Canadian lending policy documents:

1. **Minimum Age** — age of majority by province (18 or 19 depending on province)
2. **Residency Requirements** — Canadian citizen, permanent resident, or valid work permit
3. **Income Thresholds** — minimum income requirements, income verification
4. **Debt-to-Income Ratios** — Gross Debt Service (GDS) ratio, Total Debt Service (TDS) ratio
5. **Employment Verification** — employment status, minimum employment duration
6. **Credit Score Minimums** — minimum credit score thresholds
7. **Loan-to-Value (LTV) Ratio** — maximum LTV for mortgages
8. **Stress Test** — qualifying rate under OSFI B-20 (mortgage stress test)
9. **Down Payment Requirements** — minimum down payment percentages
10. **Provincial Exceptions** — province-specific variations in lending rules

## Entity Model
Expected `input` object properties for Canadian loan eligibility:

| Property | Type | Description |
|----------|------|-------------|
| `input.Age` | int | Applicant's age in years |
| `input.Province` | string | Canadian province code (e.g., "ON", "BC", "QC", "AB") |
| `input.ResidencyStatus` | string | "Citizen", "PermanentResident", "WorkPermit", "Other" |
| `input.AnnualIncome` | decimal | Gross annual income in CAD |
| `input.GDS` | decimal | Gross Debt Service ratio as percentage |
| `input.TDS` | decimal | Total Debt Service ratio as percentage |
| `input.CreditScore` | int | Credit score (Equifax/TransUnion scale) |
| `input.EmploymentStatus` | string | "Employed", "SelfEmployed", "Unemployed", "Retired" |
| `input.EmploymentDurationMonths` | int | Months at current employment |
| `input.LoanAmount` | decimal | Requested loan amount in CAD |
| `input.PropertyValue` | decimal | Property value in CAD (for mortgages) |
| `input.DownPaymentPercent` | decimal | Down payment as percentage of property value |
| `input.LTV` | decimal | Loan-to-Value ratio as percentage |
| `input.LoanType` | string | "PersonalLoan", "Mortgage", "LineOfCredit", "AutoLoan" |
| `input.LenderType` | string | "FederallyRegulated", "ProvinciallyRegulated" |

## Constraints
- Must follow Canadian regulatory language (OSFI, FCAC).
- Must not invent financial thresholds not present in the policy text.
- Must flag ambiguous regulatory text in `extractionNotes`.
- Provincial variations must be explicitly handled (e.g., age of majority differs by province).
- Stress test rules must reference the qualifying rate as defined in OSFI B-20, not invented rates.
- Currency is always CAD unless explicitly stated otherwise.

## Edge Cases
- **Provincial differences:**
  - Age of majority is 18 in most provinces but 19 in BC, NB, NS, NL, NT, NU, YT.
  - Quebec has distinct civil law that may affect lending rules.
  - Handle by including province-specific conditions in expressions or creating separate rules per province.
- **Federally regulated vs. provincially regulated lenders:**
  - OSFI B-20 applies to federally regulated lenders (banks).
  - Provincial rules may differ for credit unions, private lenders.
  - Tag rules with `lender_type` in metadata.
- **Secured vs. unsecured loans:**
  - Mortgage rules (LTV, stress test, down payment) do not apply to unsecured personal loans.
  - Ensure rules are scoped to the correct `loan_type`.
- **Self-employed applicants:**
  - Income verification rules differ for self-employed vs. salaried applicants.
  - May require separate rules or branched expressions.
- **Co-applicants:**
  - Some policies allow combined income for eligibility.
  - If the policy references co-applicants, create rules that reference combined inputs.

## Acceptance Criteria
- [ ] Rules reflect Canadian lending standards (OSFI, FCAC).
- [ ] No hallucinated financial thresholds (interest rates, ratios, minimums) not present in the source policy.
- [ ] All rules traceable to source documents.
- [ ] Provincial exceptions are handled or explicitly flagged.
- [ ] Debt service ratio rules (GDS/TDS) match OSFI B-20 guidelines when applicable.
- [ ] Stress test rules reference the correct qualifying rate methodology.
- [ ] Entity model properties are correctly named and typed.
- [ ] Rules are scoped to the correct loan type and lender type.
