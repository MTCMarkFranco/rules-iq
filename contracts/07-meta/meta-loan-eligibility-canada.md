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

## First Meta-Contract — Build & Validation Instructions

> **This is the FIRST meta-contract to build once the entire Rules-IQ system is deployed.**
> All pipeline components (infrastructure, indexer skill, normalizer, runtime engine, UI) must be operational before executing these steps.

### Source PDF Location
The regulatory source documents are located at:

```
C:\rules-iq\meta-contracts\loan-eligibility\
```

| # | Filename | Regulatory Body | Scope |
|---|----------|-----------------|-------|
| 1 | `OSFI-Guideline-B20-Residential-Mortgage-Underwriting.pdf` | OSFI | Federal — mortgage stress test, GDS/TDS limits |
| 2 | `OSFI-Guideline-B21-Mortgage-Insurance-Underwriting.pdf` | OSFI | Federal — mortgage insurance underwriting |
| 3 | `FCAC-CG9-Mortgage-Prepayment-Penalty-Disclosure.pdf` | FCAC | Federal — prepayment penalty disclosure |
| 4 | `FCAC-Commissioner-Guidance-Overview.pdf` | FCAC | Federal — commissioner guidance overview |
| 5 | `FCAC-Guideline-Appropriate-Products-Services-Banks.pdf` | FCAC | Federal — appropriate products and services |
| 6 | `FCAC-Guideline-Mortgage-Loans-Exceptional-Circumstances.pdf` | FCAC | Federal — exceptional circumstances |
| 7 | `FSRA-Ontario-CU0063INT-Residential-Mortgage-Lending.pdf` | FSRA | Ontario — credit union residential mortgage lending |
| 8 | `FSRA-Ontario-Guidance-Overview-Credit-Unions.pdf` | FSRA | Ontario — credit union guidance overview |

### Pipeline Execution Order
1. Upload all 8 PDFs to the `policy-documents` blob container in `sadatafileshubcanada`.
2. Run the indexer (`idx-policy-rules`) — this triggers the full pipeline: crack → chunk → extract → normalize → embed → version-stamp → index.
3. Verify the `rules-index` contains extracted rules for all 8 documents.
4. Query the index via semantic search to confirm rule retrieval quality.
5. Execute the runtime evaluation engine against the Canonical Test Persona (below) to produce a compliance result.
6. Verify the UI displays the evaluation result with compliance percentage, traceability links, and version context.

### Canonical Test Persona — Fictitious Loan Applicant
Use this fictitious applicant as the **primary end-to-end test case** across the runtime engine and Playwright tests.

| Property | Value | Notes |
|----------|-------|-------|
| `input.Age` | `34` | Above minimum age in all provinces |
| `input.Province` | `"ON"` | Ontario — tests both federal and FSRA provincial rules |
| `input.ResidencyStatus` | `"PermanentResident"` | Should pass residency checks |
| `input.AnnualIncome` | `92000.00` | CAD gross annual income |
| `input.GDS` | `41.5` | **Exceeds** the 39% GDS limit — expect failure |
| `input.TDS` | `43.0` | Within 44% TDS limit — expect pass |
| `input.CreditScore` | `710` | Above typical minimum thresholds |
| `input.EmploymentStatus` | `"Employed"` | Standard employment |
| `input.EmploymentDurationMonths` | `28` | Over 2 years at current employer |
| `input.LoanAmount` | `485000.00` | CAD |
| `input.PropertyValue` | `625000.00` | CAD |
| `input.DownPaymentPercent` | `22.4` | Above 20% — no mortgage insurance required |
| `input.LTV` | `77.6` | Below 80% — within standard LTV limits |
| `input.LoanType` | `"Mortgage"` | Triggers OSFI B-20 stress test rules |
| `input.LenderType` | `"FederallyRegulated"` | Triggers OSFI and FCAC rules |

**Expected Outcome:**
- The GDS rule (`MaxGrossDebtServiceRatio`) MUST fail (41.5 > 39).
- All other rules should pass for this persona.
- The compliance percentage should be less than 100% — confirming the system correctly identifies the GDS violation.
- The UI should display the failed rule with its error message, source document link (OSFI B-20), and the relevant policy snippet.

### Playwright End-to-End Test Instructions
After the entire system is built and the loan eligibility rules are indexed, generate a **Playwright test script** (TypeScript) that:

1. **Navigates** to the Rules-IQ Blazor UI.
2. **Submits** the Canonical Test Persona data through the loan eligibility evaluation form.
3. **Asserts** the compliance percentage is displayed and is less than 100%.
4. **Asserts** the `MaxGrossDebtServiceRatio` rule appears as failed with the expected error message.
5. **Asserts** at least one source document link (OSFI B-20) is visible on the failed rule card.
6. **Asserts** the ruleset version and evaluation timestamp are displayed.
7. **Asserts** the "Rules Snapshot" section is expandable and contains all evaluated rules.
8. **Captures** a screenshot of the compliance dashboard for the test report.

> **Do NOT generate the Playwright test now.** This instruction is a contract for when the system is fully operational. The test script should be generated at that time using this contract as the specification.

## Acceptance Criteria
- [ ] Rules reflect Canadian lending standards (OSFI, FCAC).
- [ ] No hallucinated financial thresholds (interest rates, ratios, minimums) not present in the source policy.
- [ ] All rules traceable to source documents.
- [ ] Provincial exceptions are handled or explicitly flagged.
- [ ] Debt service ratio rules (GDS/TDS) match OSFI B-20 guidelines when applicable.
- [ ] Stress test rules reference the correct qualifying rate methodology.
- [ ] Entity model properties are correctly named and typed.
- [ ] Rules are scoped to the correct loan type and lender type.
- [ ] All 8 PDFs from `C:\rules-iq\meta-contracts\loan-eligibility\` are ingested and indexed.
- [ ] Canonical Test Persona produces a compliance result with at least one failed rule (GDS).
- [ ] Playwright end-to-end test passes against the deployed UI using the Canonical Test Persona.
