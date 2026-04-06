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
