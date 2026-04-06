# Rule Deduplication Guidelines

Use these guidelines when consolidating candidate rules from multiple chunks or documents into a single normalized workflow.

## Deduplication Strategy

### 1. Identical Expressions
- **Merge** rules with exactly identical `Expression` strings.
- Keep the most descriptive `RuleName`.
- Aggregate all `Metadata.SourceDocuments` from both rules.
- Document the merge in `NormalizationNotes`.

### 2. Semantically Equivalent Expressions
- Rules that express the same condition with different syntax (e.g., `input.Age >= 18` vs `18 <= input.Age`) SHOULD be merged.
- Normalize the expression to the most readable form.
- Preserve all source metadata.

### 3. Subset/Superset Rules
- If one rule is strictly a subset of another (e.g., `input.Age >= 18` vs `input.Age >= 18 && input.Age <= 65`):
  - Keep both rules as separate rules — they serve different purposes.
  - Note the relationship in `NormalizationNotes`.

### 4. Conflicting Rules
- If two rules express contradictory conditions for the same entity:
  - **Prefer the stricter rule** when safety or compliance is implied.
  - Document the conflict, the resolution, and the rationale in `NormalizationNotes`.
  - If resolution is unclear, keep both rules and flag for human review.

## Naming Normalization

### Consistent Rule Names
- Use PascalCase (e.g., `MinimumAgeRequirement`, not `min_age_rule`).
- Use descriptive, stable names derived from the rule's intent.
- If multiple candidate rules have different names for the same condition, normalize to one name and list aliases.

### Workflow Names
- Use a single `WorkflowName` per domain (e.g., `LoanEligibility`, `TravelInsuranceCoverage`).
- If merging multiple candidate workflows, unify under `targetWorkflowName` if provided.

## Metadata Aggregation
- Every normalized rule MUST include `Metadata.SourceDocuments` listing all chunks that contributed to it.
- Never discard metadata during merging.
- If a rule was derived from 5 chunks, all 5 must appear in `SourceDocuments`.
- Every `SourceDocuments` entry MUST preserve `SourceDocumentVersion` if it was present in the candidate rule.

## Version-Aware Deduplication
When deduplicating rules across different versions of the same source document:
- Rules from the **newer** `SourceDocumentVersion` take precedence over the older version.
- If a rule exists in both versions with identical expressions, merge and keep the newer version's metadata.
- If a rule was removed in the newer version (not present), do NOT carry it forward.
- Document all version-based deduplication decisions in `NormalizationNotes`.

## Conflict Documentation
When documenting conflicts in `NormalizationNotes`, include:
- The conflicting rule expressions
- The source documents for each
- The resolution chosen
- The rationale for the resolution

## Output Validation
After deduplication, validate:
- [ ] No two rules have the same `RuleName`
- [ ] No two rules have identical `Expression` strings (they should have been merged)
- [ ] All rules have valid C# boolean expressions
- [ ] All rules have at least one `SourceDocuments` entry
- [ ] `NormalizationNotes` documents all merges, discards, and conflict resolutions
