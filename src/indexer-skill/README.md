# Indexer Skill

Azure AI Search custom Web API skillset for rule extraction.

## Purpose
This project will contain a custom Web API skill that:
1. Receives chunk content and metadata from the Azure AI Search indexer
2. Constructs prompts using the prompt contracts defined in `/contracts/04-indexer/`
3. Calls Azure OpenAI to extract candidate rules
4. Validates the response against the RulesEngine JSON schema
5. Returns the `RulesJson` to the indexer for storage in the search index

## Key Dependencies (planned)
- Azure.AI.OpenAI
- Microsoft.Azure.Search (or Azure.Search.Documents)
- System.Text.Json
- Polly (resilience)

## Related Contracts
- [Indexer OpenAI Rule Enrichment](../../contracts/04-indexer/indexer-openai-rule-enrichment.md)
- [Indexer Skillset Design](../../contracts/04-indexer/indexer-skillset-design.md)
- [Indexer Output Schema](../../contracts/04-indexer/indexer-output-schema.md)
