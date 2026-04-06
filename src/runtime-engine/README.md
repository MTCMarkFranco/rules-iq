# Runtime Engine

Deterministic rule execution pipeline using Microsoft RulesEngine.

## Purpose
This project will contain the logic to:
1. Pull relevant rules from the Azure AI Search index
2. Map document/input data to the RulesEngine input model using AutoMapper
3. Execute rules deterministically using the Microsoft RulesEngine
4. Return structured results with pass/fail outcomes per rule

## Key Dependencies (planned)
- RulesEngine (Microsoft)
- AutoMapper
- Polly (resilience for search index queries)
- Azure.Search.Documents

## Related Contracts
- [Runtime Evaluation Plan](../../contracts/06-runtime/runtime-evaluation-plan.md)
- [Runtime Mapping Strategy](../../contracts/06-runtime/runtime-mapping-strategy.md)
