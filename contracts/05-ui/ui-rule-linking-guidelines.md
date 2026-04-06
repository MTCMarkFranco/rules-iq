# UI Rule Linking Guidelines

This document provides guidelines for how rules should be displayed and linked in the RulesEngineEditor UI.

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
- Optionally export as a human-readable report (Markdown or PDF) for governance review.
