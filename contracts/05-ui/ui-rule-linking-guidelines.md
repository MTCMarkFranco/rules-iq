# UI Rule Linking Guidelines

This document provides guidelines for how rules should be displayed and linked in the RulesEngineEditor UI.

## Core Tenets

### UI Foundation
The UI MUST be built on the **RulesEngineEditor** Blazor component library (or a fork if modifications are required):
- **NuGet:** https://www.nuget.org/packages/RulesEngineEditor/
- **Repository:** https://github.com/alexreich/RulesEngineEditor
- Supports Blazor WebAssembly (client-side) and Blazor Server (server-side) hosting models.
- Supports 2-way binding of Workflows, Entity Framework persistence, real-time evaluation, and import/export of compliant JSON.
- Custom extensions (compliance panels, version displays) should be built as Blazor components that integrate with the `RulesEngineEditorPage` component.

### Compliance Scoring Display
The UI MUST prominently display a compliance score (percentage) for every document/request evaluation. Users must immediately see how compliant their input is against the rules.

### Version Display
The UI MUST display the ruleset version and publication timestamp for every evaluation and every rule, ensuring full transparency into which rules were applied.

## Display Requirements

### Rule Card Layout
Each rule in the UI should display:
1. **Rule Name** — the `DisplayLabel` from the traceability mapping
2. **Expression** — the C# boolean expression, formatted for readability
3. **Status** — Success/Error event descriptions
4. **Source Link(s)** — clickable links back to the original policy document
5. **Snippet** — inline preview of the original policy text

### Source Document Linking
- Every rule MUST have at least one visible link to its source document.
- Links should open the document viewer at the specific page and highlight the relevant text range.
- If multiple sources exist, display all links in a collapsible list.
- Link format: `[Document Name - Page N]`

### Snippet Display
- Show the snippet inline beneath the rule expression.
- Use a visually distinct style (e.g., blockquote, light background) to differentiate policy text from rule logic.
- Truncate snippets longer than 200 characters with an expandable "Show more" control.
- If no snippet is available, display: "[Original text not available — see source link]"

## Workflow Organization
- Group rules by `WorkflowName` in the UI.
- Within a workflow, sort rules alphabetically by `RuleName` unless a custom order is specified.
- Show workflow-level metadata: total rule count, source document count, last updated timestamp.
- Display the **Ruleset Version** (e.g., `v2.1.0`) prominently at the workflow level.
- Show the **Ruleset Published Timestamp** so users know when the rules were last updated.

## Compliance Score Display
When displaying evaluation results, the UI MUST show:

### Summary Panel
- **Compliance Percentage** — displayed as a large, prominent value (e.g., "71.43% Compliant")
- **Visual indicator** — color-coded: green (≥ 80%), yellow (50–79%), red (< 50%)
- **Rule counts** — "5 of 7 rules passed"

### Per-Rule Results
- Each rule card shows its result: ✅ Passed or ❌ Failed
- Failed rules display their `ErrorMessage` prominently
- Rules are sortable by: name, result (failed first), or category

### Version Context
- The evaluation result panel shows:
  - **Ruleset Version** used (e.g., `v2.1.0`)
  - **Evaluation Timestamp** (when the document/request was processed)
  - **Source Document Versions** (which policy document versions contributed to the rules)
- Users can expand a "Rules Snapshot" section to see the complete list of rules that were evaluated, their expressions, and results.

### AI Agent Output
- The AI agent that processes documents/requests MUST produce output as a structured JSON object.
- This JSON object MUST include:
  - `ComplianceScore` (with percentage, total, passed, failed counts)
  - `VersionFingerprint` (ruleset version, source document versions, evaluation timestamp)
  - `RulesSnapshot` (complete list of rules used, their versions, and results)
- The UI interprets this JSON to render the compliance dashboard.

## Traceability Panel
- Provide a dedicated "Traceability" panel or tab for each rule showing:
  - All source documents
  - All page numbers
  - All snippets
  - Extraction notes (from the extraction phase)
  - Normalization notes (from the normalization phase)

## Edit Workflow
When a user edits a rule in the UI:
- Preserve the `Metadata` and source links — do not discard traceability.
- Add a `ModifiedBy` and `ModifiedTimestamp` field to track manual changes.
- Flag rules that have been manually modified (distinct from auto-extracted rules).
- Optionally allow users to "regenerate" a rule from the original source text.

## Export Format
When exporting rules from the UI:
- Export as RulesEngine-compatible JSON (same schema used throughout the pipeline).
- Include `Metadata` in the export so traceability is preserved outside the UI.
- Include `RulesetVersion` in the export so the version lineage is preserved.
- Optionally export as a human-readable report (Markdown or PDF) for governance review.
- Compliance evaluation results should be exportable as a separate JSON document including the full `ComplianceScore`, `VersionFingerprint`, and `RulesSnapshot`.

## Playwright End-to-End Test — Loan Eligibility (Canada)

> **After the entire Rules-IQ system is built and the loan eligibility rules are indexed, a Playwright end-to-end test MUST be generated to validate the complete flow through the UI.**

### Test Scenario
The test uses the **Canonical Test Persona** defined in [meta-loan-eligibility-canada.md](../07-meta/meta-loan-eligibility-canada.md) — a fictitious Ontario mortgage applicant with a GDS ratio of 41.5% that intentionally exceeds the OSFI B-20 limit of 39%.

### Test Steps (Contract — Not a Script)
1. **Navigate** to the Rules-IQ Blazor UI home page.
2. **Select** the "Loan Eligibility" evaluation workflow.
3. **Enter** the Canonical Test Persona data into the evaluation form:
   - Age: 34, Province: ON, Residency: PermanentResident
   - Income: $92,000, GDS: 41.5%, TDS: 43.0%
   - Credit Score: 710, Employment: Employed (28 months)
   - Loan: $485,000, Property: $625,000, Down Payment: 22.4%, LTV: 77.6%
   - Loan Type: Mortgage, Lender: FederallyRegulated
4. **Submit** the evaluation.
5. **Assert** the compliance percentage is displayed prominently and is **less than 100%**.
6. **Assert** the compliance panel is color-coded **yellow** (50–79%) or **red** (< 50%) — NOT green.
7. **Assert** the `MaxGrossDebtServiceRatio` rule card shows a ❌ Failed status with the expected error message referencing the 39% GDS limit.
8. **Assert** the failed rule card has at least one source document link referencing OSFI B-20.
9. **Assert** the failed rule card displays a snippet from the OSFI B-20 policy text.
10. **Assert** the Ruleset Version (e.g., `v1.0.0`) and Evaluation Timestamp are visible in the evaluation result panel.
11. **Assert** the "Rules Snapshot" section is expandable and lists all evaluated rules with their expressions and pass/fail results.
12. **Assert** the export button produces a valid JSON file containing `ComplianceScore`, `VersionFingerprint`, and `RulesSnapshot`.
13. **Capture** a screenshot of the compliance dashboard for the test report.

### Assertions Summary
| # | Assertion | Expected |
|---|-----------|----------|
| 1 | Compliance % visible | Yes, < 100% |
| 2 | Color indicator | Yellow or Red |
| 3 | GDS rule failed | ❌ with error message |
| 4 | Source link present | OSFI B-20 link visible |
| 5 | Policy snippet shown | OSFI B-20 text excerpt |
| 6 | Version displayed | `v1.0.0` + timestamp |
| 7 | Rules Snapshot expandable | All rules listed |
| 8 | Export produces valid JSON | Schema-compliant |

> **Do NOT generate the Playwright test script until the system is fully operational.** This section is an instruction contract — the test script should be generated at that time using these assertions as the specification.
