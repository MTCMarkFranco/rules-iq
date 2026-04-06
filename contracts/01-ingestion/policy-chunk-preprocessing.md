# Prompt Contract: Policy Chunk Preprocessing

## Inputs
- **`raw_document_text`**: full text extracted from PDF, Word, or HTML.
- **`document_metadata`**:
  - `document_id` (string)
  - `source_uri` (string URL or file path)
  - `ingestion_timestamp` (ISO 8601)
  - `file_type` (string, e.g., "pdf", "docx", "html", "md")
- **`chunking_parameters`**:
  - `max_tokens` (int, maximum tokens per chunk)
  - `semantic_boundaries` (array of boundary types: headings, paragraphs, sections)
  - `overlap_size` (int, number of overlapping tokens between adjacent chunks)

## Expected Output
A JSON object:

```json
{
  "chunks": [
    {
      "chunk_id": "string",
      "content": "string",
      "page_number": 3,
      "char_range": { "start": 123, "end": 456 },
      "semantic_label": "Eligibility Criteria"
    }
  ],
  "notes": "string"
}
```

- **`chunks`**: array of chunk objects, each with:
  - `chunk_id`: deterministic, stable identifier derived from document_id + position
  - `content`: the raw text of the chunk
  - `page_number`: page in the original document (if available)
  - `char_range`: start/end character offsets in the original document
  - `semantic_label`: inferred section heading or topic label
- **`notes`**: free-text explanation of any preprocessing decisions (e.g., "Merged two short paragraphs into one chunk")

## Constraints
- Preserve semantic boundaries where possible (do not split mid-sentence or mid-paragraph unless exceeding `max_tokens`).
- Never merge unrelated sections into a single chunk.
- Maintain deterministic chunk IDs — the same document with the same parameters must always produce the same chunk IDs.
- Strip formatting artifacts (headers, footers, page numbers embedded in text) but preserve structural markers (headings, list items).
- Normalize whitespace but preserve paragraph breaks.
- If OCR was used, note quality issues in `notes`.

## Edge Cases
- **Missing page numbers**: Set `page_number` to `null` and note in `notes`.
- **OCR noise**: Preserve the text as-is but flag quality concerns in `notes`.
- **Tables or lists spanning pages**: Keep the table/list in a single chunk if it fits within `max_tokens`; otherwise, split at row boundaries and note the split.
- **Very short documents**: If the entire document fits in one chunk, produce a single chunk.
- **Empty or corrupted text**: Return an empty `chunks` array and explain in `notes`.

## Acceptance Criteria
- [ ] Chunk boundaries are stable across re-ingestion of the same document with the same parameters.
- [ ] No chunk exceeds `max_tokens`.
- [ ] Semantic labels are meaningful and consistent across similar documents.
- [ ] Character ranges (`char_range`) are accurate and can be used to locate the chunk in the original text.
- [ ] The output JSON is syntactically valid.
