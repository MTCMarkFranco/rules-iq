# Agentic Rules Transformation Engine — Prompt Contract Suite

This repository contains a complete set of **prompt contracts**, **meta-contracts**, and **example artifacts** for building an end-to-end deterministic rules transformation engine using:

- Microsoft `.NET RulesEngine`
- RulesEngineEditor UI (NuGet)
- Azure AI Search Indexer
- Azure OpenAI (OpenAI Skillset or custom Web API skill)
- Deterministic rule extraction pipeline

## Folder Structure

```
/contracts
    /01-ingestion
    /02-extraction
    /03-normalization
    /04-indexer
    /05-ui
    /06-runtime
    /07-meta
/examples
    /input
    /output
/src
    /indexer-skill
    /rules-normalizer
    /ui-adapter
    /runtime-engine
    /shared-models
    /infrastructure
README.md
```

## Purpose

This repository defines the **prompt contracts** used by an agentic pipeline that:

1. Ingests unstructured policy documents
2. Chunks and preprocesses them
3. Extracts deterministic candidate rules
4. Normalizes and validates them
5. Stores them in an Azure AI Search index
6. Links them to the RulesEngineEditor UI
7. Executes them deterministically at runtime

## Architecture

This is **not** a RAG system. This is a **compiler**.

```
Policy → Spec → Rules → UI
```

AI handles **natural language interpretation**. Software engineering handles **deterministic execution**.

This hybrid approach delivers:

- **Determinism** — rules execute identically every time
- **Auditability** — every rule traces back to source policy text
- **Repeatability** — same input always yields same rules
- **Explainability** — extraction notes document reasoning
- **Version control** — rules are diffable JSON
- **No runtime hallucination** — AI is used offline, not at decision time

## Meta-Contracts

The `/07-meta` folder contains:

- A **meta-contract template** for creating new domain-specific rule extraction contracts
- A **Loan Eligibility (Canada)** meta-contract as a working example

## Examples

The `/examples` folder contains:

- Sample input chunks
- Sample indexer rows
- Sample extracted rules
- Sample normalized workflows
- Sample UI traceability metadata

These examples demonstrate how the contracts behave end-to-end.

## Source Scaffolding

The `/src` folder contains empty scaffolding for the future codebase:

| Folder | Purpose |
|--------|---------|
| `/indexer-skill` | Azure AI Search skillset Web API or OpenAI skill |
| `/rules-normalizer` | Deterministic rule consolidation engine |
| `/ui-adapter` | RulesEngineEditor UI integration |
| `/runtime-engine` | Rule execution pipeline using .NET RulesEngine |
| `/shared-models` | DTOs, AutoMapper profiles, shared contracts |
| `/infrastructure` | Polly policies, DI setup, logging, configuration |

## Usage

These prompt contracts are designed to be used:

- In Azure AI Search indexer skillsets
- In agentic pipelines
- In offline rule compilation workflows
- In deterministic rule governance systems

They ensure **traceability**, **determinism**, **auditability**, and **regulatory compliance**.

## Key Technologies

- [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) — deterministic rule evaluation
- [RulesEngineEditor](https://www.nuget.org/packages/RulesEngineEditor/) — UI for rule management
- [Azure AI Search](https://learn.microsoft.com/en-us/azure/search/) — indexing and enrichment
- [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/) — natural language interpretation
- [AutoMapper](https://automapper.org/) — object mapping for rule inputs
- [Polly](https://github.com/App-vNext/Polly) — resilience and fault handling
