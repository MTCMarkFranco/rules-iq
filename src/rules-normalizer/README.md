# Rules Normalizer

Deterministic rule consolidation and normalization engine.

## Purpose
This project will contain the logic to:
1. Receive candidate rules from multiple chunks/documents
2. Deduplicate equivalent rules
3. Resolve naming conflicts
4. Merge metadata from multiple sources
5. Produce a single, normalized RulesEngine workflow

## Key Dependencies (planned)
- RulesEngine (Microsoft)
- System.Text.Json

## Related Contracts
- [Rule Normalization](../../contracts/03-normalization/rule-normalization.md)
- [Rule Deduplication Guidelines](../../contracts/03-normalization/rule-deduplication-guidelines.md)
