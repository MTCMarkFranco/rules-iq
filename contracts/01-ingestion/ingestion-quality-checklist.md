# Ingestion Quality Checklist

Use this checklist to validate the quality of document ingestion before passing chunks to the extraction phase.

## Text Extraction Quality
- [ ] Text extracted without OCR corruption or garbled characters
- [ ] Special characters (currency symbols, mathematical operators, legal symbols) preserved correctly
- [ ] Encoding is consistent (UTF-8 expected)

## Structural Preservation
- [ ] Headings preserved with correct hierarchy (H1, H2, H3, etc.)
- [ ] Paragraph breaks maintained
- [ ] List items (ordered and unordered) captured cleanly
- [ ] Tables captured with row/column structure intact
- [ ] Footnotes and endnotes associated with correct content

## Metadata Accuracy
- [ ] Page numbers mapped correctly to chunk positions
- [ ] Character ranges (`char_range`) are accurate and verifiable
- [ ] `document_id` is unique and deterministic
- [ ] `source_uri` resolves to the original document

## Semantic Boundaries
- [ ] Chunks respect section boundaries (no mid-section splits unless forced by token limit)
- [ ] Semantic labels (`semantic_label`) are meaningful and consistent
- [ ] Related content (e.g., a rule and its exceptions) is kept together when possible

## Chunk Stability
- [ ] Re-ingesting the same document with the same parameters produces identical chunks
- [ ] Chunk IDs are deterministic and reproducible
- [ ] No duplicate chunks produced

## Edge Case Handling
- [ ] Scanned documents with OCR: quality issues documented in `notes`
- [ ] Multi-language documents: language identified per chunk if mixed
- [ ] Redacted or blank pages: handled gracefully (empty chunks or skipped with note)
- [ ] Very large tables: split at row boundaries with clear notes

## Pre-Extraction Gate
**Do not pass chunks to the extraction phase unless all critical items above are satisfied.**
Non-critical items (e.g., minor OCR artifacts in non-rule text) may be noted and passed through.
