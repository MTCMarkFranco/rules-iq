# Prompt Contract: Rule–Document Traceability Mapping (UI Metadata)

## Core Tenets

### UI Foundation
The RulesEngine UI MUST be based on the **RulesEngineEditor** Blazor component library (or a fork of the repository if modifications are required):
- **NuGet:** https://www.nuget.org/packages/RulesEngineEditor/
- **Repository:** https://github.com/alexreich/RulesEngineEditor
- The editor supports real-time evaluation, drag-and-drop rule ordering, import/export of workflow JSON, nested rule support, and Entity Framework persistence.
- If the NuGet package is insufficient (e.g., custom compliance views, version display panels), fork the repository and maintain changes in a separate branch. Document all deviations.

### Live Index Integration
The UI MUST retrieve rules dynamically from the Azure AI Search index at runtime — rules MUST NOT be hardcoded in the UI layer:
- **Evaluate page**: Calls `IRuleRetrievalService.RetrieveWorkflowAsync(workflowName)` to fetch the current rules from the search index before executing evaluation via `IRuleEvaluationService`.
- **Rule Browser page**: Calls `IRuleRetrievalService.RetrieveWorkflowAsync(workflowName)` on page load to display the current indexed rules dynamically.
- If no rules are found in the index, the UI MUST display a clear error message (not an empty or broken state).
- This ensures that when policy PDFs are re-indexed with updated rules, the UI immediately reflects the changes without code changes or redeployment.

### Compliance Visibility
The UI MUST display a compliance score (percentage) for every document/request evaluation, showing how compliant the input is against the evaluated rules.

### Version Traceability
The UI MUST display the ruleset version used for each evaluation, and allow users to see exactly which rules (and which versions of those rules) were applied.

## Inputs
- **Context:**
  - The RulesEngine UI (via `RulesEngineEditor` NuGet package) must display:
    - A rule.
    - A link back to the originating unstructured document.
    - Inline context (snippet) from the original chunk.
- **Provided at runtime:**
  - `rule`: a single normalized rule object (as per the normalization contract).
  - `sourceDocuments`: array of source metadata objects:
    - `SourceDocumentId` (string)
    - `SourceUri` (string)
    - `PageNumber` (int, if available)
    - `CharRange` (object with `Start` and `End`, if available)
    - `RawChunkText` (string, the original chunk text)
  - `ui_base_url`: base URL for the document viewer or content system (e.g., `https://contoso.com/docs/view`).

## Expected Output
- A **JSON object** representing UI-friendly metadata:

```json
{
  "RuleName": "MinimumAgeRequirement",
  "DisplayLabel": "Minimum Age Requirement (18+)",
  "SourceLinks": [
    {
      "Label": "Policy Document - Page 3",
      "Url": "https://contoso.com/docs/view?docId=doc123&page=3&start=123&end=456",
      "PageNumber": 3,
      "Snippet": "Applicants must be at least 18 years of age to qualify for this program..."
    }
  ],
  "TraceabilitySummary": "This rule was derived from 1 source document (Policy Document, page 3). The original text explicitly states a minimum age of 18.",
  "RulesetVersion": "v2.1.0",
  "RulesetPublishedTimestamp": "2025-03-15T10:30:00Z"
}
```

- **`DisplayLabel`**:
  - Short, human-readable label for the rule (e.g., "Minimum Age Requirement (18+)").
  - Must be understandable by non-technical business users.
- **`SourceLinks`**:
  - For each `sourceDocument`, construct:
    - `Label`: e.g., "Policy Doc A - Page 3".
    - `Url`: constructed from `ui_base_url` + query parameters (e.g., `?docId=...&page=...&start=...`).
    - `PageNumber`: the page number, if available.
    - `Snippet`: a short excerpt from `RawChunkText` around the `CharRange`.
- **`TraceabilitySummary`**:
  - One or two sentences explaining where this rule came from and how many documents/pages support it.
- **`RulesetVersion`**:
  - The semantic version of the ruleset this rule belongs to (e.g., `"v2.1.0"`).
  - Displayed in the UI alongside each rule so users know which version of the policy produced it.
- **`RulesetPublishedTimestamp`**:
  - ISO 8601 timestamp of when this ruleset version was published/indexed.
  - Helps users understand the currency of the rules.

## Constraints
- **No URL guessing beyond pattern:**
  - You MUST only construct URLs using the provided `ui_base_url` and the given identifiers.
  - Do NOT invent URL patterns or endpoints.
- **Snippet brevity:**
  - Snippets SHOULD be 1–3 sentences or approximately 200 characters around the rule text.
  - Truncate with ellipses (`...`) if the original text is longer.
- **Human readability:**
  - `DisplayLabel` and `Label` MUST be understandable by non-technical business users.
  - Avoid technical jargon in display fields.
- **Accuracy:**
  - Snippets must faithfully represent the original policy text.
  - Do NOT paraphrase or summarize — extract verbatim where possible.

## Edge Cases
- **Multiple source documents:**
  - Include multiple `SourceLinks` entries and mention the count in `TraceabilitySummary`.
- **Missing page number or char range:**
  - Omit those fields from the link parameters but still provide a best-effort `Snippet` if `RawChunkText` is available.
  - URL should omit missing query parameters gracefully.
- **Very long chunks:**
  - Truncate snippets with ellipses (`...`) while preserving the key rule phrase.
- **No RawChunkText available:**
  - Set `Snippet` to `"[Original text not available]"` and note in `TraceabilitySummary`.

## Acceptance Criteria
- [ ] Each rule can be traced back to at least one document via `SourceLinks`.
- [ ] Snippets clearly show the original policy language that led to the rule.
- [ ] URLs follow the expected pattern and are constructed only from provided inputs.
- [ ] `TraceabilitySummary` is concise and understandable by a business user.
- [ ] `DisplayLabel` is human-readable and describes the rule's intent.
- [ ] No invented URLs or fabricated snippet text.
- [ ] `RulesetVersion` and `RulesetPublishedTimestamp` are present and accurately reflect the ruleset version.
