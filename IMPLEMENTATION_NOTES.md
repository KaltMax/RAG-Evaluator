# Implementation Notes

Working notes for thesis writing. Not polished documentation — see README.md and ARCHITECTURE.md for that.

## Current Implementation Status

The core RAG functionality is fully implemented with document upload (including language selection and content extraction) and question answering with language selection. Document chunks are persisted in PostgreSQL using pgvector for efficient similarity search across multiple documents. Document management endpoints (list, get, delete, download) are fully implemented. Query history including LLM responses, retrieved chunks, and query embeddings is persisted via the `Query` and `QueryResult` entities for reproducible evaluation. The `QueryResult` entity uses denormalized chunk data to preserve historical accuracy even when documents are re-chunked or deleted. Relevance annotation is supported via `PATCH /api/query/{id}/results` endpoint with a graded scale (`RelevanceGrade` enum: NotRelevant, Related, HighlyRelevant). Response quality evaluation is also supported with a `ResponseQuality` enum (CorrectAndComplete, VagueOrIncomplete, Incorrect, Hallucinated) and a language switching detection flag. Ground truth document selection enables proper Recall@K calculation by allowing users to specify which documents should ideally contain relevant information for a query. The frontend includes an annotation UI with relevance badges for each retrieved chunk, response quality evaluation buttons, ground truth document selector, and displays retrieval metrics (MRR, Precision@K, Recall@K, NDCG@K, Response Time) after annotation submission. The frontend supports multi-file upload (up to 20 files) with per-file language selection. A dedicated `MetricsService` in the Application layer handles similarity calculations (cosine similarity) and provides retrieval evaluation metrics. Two chunking strategies are available: `FixedSize` (character-based with configurable size and overlap) and `Semantic` (embedding-based splitting at topic boundaries using cosine similarity between consecutive lines, configurable via `SimilarityThreshold`). The RAG system is fully configurable at runtime via the Settings API (`GET/PATCH /api/settings`), supporting multiple embedding models, chunking strategies (`FixedSize`, `Semantic`), prompt templates (`Basic`, `Instructed`, `LanguageAware`), and numeric parameters (chunk size, chunk overlap, similarity threshold). Prompt templates implement three strategies for cross-language evaluation: a basic English prompt, an English prompt with explicit language instruction, and a native-language prompt. The active embedding model can be hot-swapped at runtime with automatic service reinitialization. The Query History page displays all past queries with collapsible cards showing query details, system prompt, parameters (Top-K, Language, Chat Model, Embedding Model, Chunking Strategy), and evaluation metrics. Pending queries can be annotated inline directly from the Query History page. Experiment batch processing is supported via `POST /api/experiments`, which accepts a list of queries and a repeat count, executes them sequentially in the background using a `BackgroundService` with a `Channel<T>`-based queue, and provides aggregated metrics (mean/stddev response time, mean retrieval metrics, response quality distribution, language switching rate) per query group and overall once annotations are complete.

## Evaluation Metrics Levels

- **MRR** (Mean Reciprocal Rank) — chunk-level: reciprocal rank of the first relevant chunk
- **Precision@K** — chunk-level: proportion of relevant chunks in the top K retrieved chunks
- **Recall@K** — document-level: proportion of ground truth documents that have at least one relevant chunk in the top K results
- **NDCG@K** — chunk-level: uses graded relevance scores (0=NotRelevant, 1=Related, 2=HighlyRelevant) with logarithmic position discounting

## Testing Strategy

173 unit tests using xUnit 3 and NSubstitute, organized by layer.

### What's tested (and why)

- **Application layer services** (DocumentService, DocumentProcessingService, ExperimentService, MetricsService, QueryService, RagService, SettingsService) — these contain the core business logic, workflow orchestration, and decision-making. Each service is tested in isolation with mocked dependencies via NSubstitute.
- **ExperimentMetricsAggregator** - performs statistical calculations (mean, standard deviation, distributions) that need to be verified for correctness.
- **Infrastructure chunkers** (FixedSizeTextChunker, SemanticTextChunker) — contain real algorithmic logic (splitting strategies, cosine similarity thresholds) that can break independently of external systems.

### What's deliberately not tested (and why)

- **Repositories** — thin EF Core wrappers with no business logic. Testing them would only test EF Core itself, not application code. Integration tests against a real database would be more valuable here.
- **External service wrappers** (OllamaChatService, OllamaEmbeddingService, LocalFileStorageService, PdfPigLoader) — these delegate directly to third-party libraries (Semantic Kernel, PdfPig, System.IO). Unit testing them would require mocking the libraries themselves, which tests the mock setup rather than real behavior.
- **Domain entities, enums, value objects** — plain data classes (POCOs) with no logic to test.
- **Mappers** (DocumentMapper, QueryMapper, ExperimentMapper) — simple property-to-property assignments. Errors here surface immediately in integration or manual testing.
- **Controllers** — thin HTTP layer that delegates to services. Already covered indirectly through service tests.

### Key testing patterns

- **CancellationToken handling**: Tests use `TestContext.Current.CancellationToken` in Act steps. Persistence operations after expensive work ("point of no return") deliberately don't forward cancellation tokens — tests verify this with `CancellationToken.None`.
- **One concern per test**: Large test methods are split into focused tests with shared Arrange helpers (e.g., `ArrangeAnnotation()` returns a tuple of test fixtures).
- **Test organization**: One test class per service, grouped by method using `#region` blocks.