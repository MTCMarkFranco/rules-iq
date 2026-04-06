# Rule Extraction Ambiguity Checklist

Use this checklist when reviewing a policy chunk to determine whether it contains enforceable, machine-executable rules. Apply this checklist **before** generating candidate rules.

## Actionability Assessment
- [ ] Does the chunk contain enforceable logic (conditions, thresholds, requirements)?
- [ ] Can the logic be expressed as a boolean expression (true/false outcome)?
- [ ] Is the chunk actionable without requiring additional context from other sections?

## Threshold Clarity
- [ ] Are numeric thresholds explicit (e.g., "18 years", "$75,000", "3 business days")?
- [ ] Are comparison operators implied or explicit (e.g., "at least" = `>=`, "no more than" = `<=`)?
- [ ] Are units of measurement clear (dollars, days, years, percent)?

## Exception Handling
- [ ] Are exceptions clearly stated and bounded?
- [ ] Are exceptions conditional (e.g., "except when...") or absolute?
- [ ] Can exceptions be expressed as separate rules or rule branches?

## Language Determinism
- [ ] Is the language deterministic (avoid: "may", "could", "where appropriate", "at the discretion of")?
- [ ] Are definitions referenced elsewhere provided or can they be resolved?
- [ ] Is there a single unambiguous interpretation?

## Cross-Reference Resolution
- [ ] Are cross-references to other sections (e.g., "See Section 3.2") resolvable from available chunks?
- [ ] Are cross-references to external documents or regulations identified?
- [ ] If cross-references cannot be resolved, is this documented in `extractionNotes`?

## Entity Identification
- [ ] Are the entities involved in the rule clearly identifiable (e.g., "applicant", "loan", "property")?
- [ ] Can entities be mapped to an `input` object model (e.g., `input.Age`, `input.Income`)?
- [ ] Are composite entities (e.g., "household income") clearly defined?

## Ambiguity Flags
If any of the following are true, the chunk should be flagged and documented in `extractionNotes`:
- [ ] The chunk uses subjective language ("reasonable", "appropriate", "sufficient")
- [ ] The chunk describes a process, not a rule (e.g., "The committee will review...")
- [ ] The chunk is purely definitional (e.g., "For the purposes of this policy, 'income' means...")
- [ ] The chunk contains conflicting statements
- [ ] The chunk is a table of contents, index, or appendix reference

## Decision Gate
- **If all Actionability and Threshold items pass**: Proceed with rule extraction (`hasRules = true`).
- **If any Ambiguity Flags are raised**: Extract with caution and document in `extractionNotes`.
- **If the chunk fails Actionability**: Mark `hasRules = false` and explain in `extractionNotes`.
