# RAG-Evaluator Architecture

This document describes the architecture, design decisions, and technical implementation details of the RAG-Evaluator application.

## Table of Contents

- [Architectural Pattern](#architectural-pattern)
- [Project Structure](#project-structure)
- [Layer Responsibilities](#layer-responsibilities)
- [RAG Implementation Workflow](#rag-implementation-workflow)
- [Database Design](#database-design)
- [API Design](#api-design)
- [Technology Stack](#technology-stack)
- [Docker Deployment](#docker-deployment)
- [Current Implementation Status](#current-implementation-status)
- [Resources](#resources)

## Architectural Pattern

The application follows **Clean Architecture** (Onion Architecture) principles with clear separation of concerns:

- **Dependency Rule**: All layers depend inward toward Domain and Contract. Infrastructure and Application are sibling layers that never reference each other.
- **Domain-Centric**: Core entities and value objects are framework-agnostic with no external dependencies.
- **Testability**: Application services can be tested in isolation by mocking Contract interfaces via NSubstitute.
- **Maintainability**: Changes in one layer have minimal impact on others.
- **Centralized Abstractions**: All interface definitions (service and repository contracts) are consolidated in the Contract layer, so Application and Infrastructure both depend on the same abstractions without knowing about each other.

### Dependency Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      RagEvaluator.API                       │
│                   (Controllers, Middleware)                 │
│         References: Application, Infrastructure,            │
│                     Contract, Domain                        │
└──────────┬──────────────────────────────────┬───────────────┘
           │                                  │
           ↓                                  ↓
┌────────────────────────────┐  ┌────────────────────────────┐
│  RagEvaluator.Application  │  │ RagEvaluator.Infrastructure│
│  (Business Logic)          │  │   (Implementations)        │
│                            │  │                            │
│ • RagService               │  │ • OllamaChatService        │
│ • DocumentService          │  │ • OllamaEmbeddingService   │
│ • DocumentProcessingService│  │ • LocalFileStorageService  │
│ • QueryService             │  │ • PdfPigLoader             │
│ • MetricsService           │  │ • FixedSizeTextChunker     │
│ • SettingsService          │  │ • SemanticTextChunker      │
│ • ExperimentService        │  │ • DocumentRepository       │
│                            │  │ • DocumentChunkRepository  │
│ References: Contract,      │  │ • QueryRepository          │
│             Domain         │  │ • ExperimentRepository     │
│                            │  │                            │
│                            │  │ References: Contract,      │
│                            │  │             Domain         │
└──────────┬─────────────────┘  └──────────┬─────────────────┘
           │                               │
           ↓                               ↓
┌─────────────────────────────────────────────────────────────┐
│                   RagEvaluator.Contract                     │
│                  (Abstractions & DTOs)                      │
│                                                             │
│ • Abstractions/Services/  (IChatService, IEmbeddingService, │
│   IFileStorageService, IPdfLoader, ITextChunker)            │
│ • Abstractions/Data/  (IDocumentRepository,                 │
│   IDocumentChunkRepository, IQueryRepository,               │
│   IExperimentRepository)                                    │
│ • Dtos/  (Requests, Responses)                              │
│ • Configurations/  (RagConfiguration,                       │
│   FileStorageConfiguration)                                 │
│                                                             │
│ References: Domain                                          │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ↓
                ┌────────────────────────────┐
                │    RagEvaluator.Domain     │
                │    (Core Entities)         │
                │                            │
                │ • Entities/                │
                │ • Value Objects/           │
                │ • Enums/                   │
                │                            │
                │ References: (none)         │
                └────────────────────────────┘
```

**Key Points**:
- **API** references all layers, but only `Program.cs` touches Infrastructure (for DI wiring). Controllers themselves only depend on Application services and Contract DTOs
- **Application and Infrastructure are siblings** - they both depend on Contract and Domain but never on each other
- **Infrastructure** implements interfaces defined in Contract (e.g., `OllamaChatService` implements `IChatService`)
- **Application** orchestrates workflows using Contract abstractions (e.g., `RagService` depends on `IChatService`, not `OllamaChatService`)
- **Domain** has zero dependencies - purely entities, enums, and value objects
- **Contract** serves as the central interface hub, depending only on Domain for types used in interface signatures (e.g., `SearchResult`, `Document`)

## Project Structure

```
RAG-Evaluator/
├── RagEvaluator.API/                           # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── DocumentController.cs               # Upload PDFs, manage docs
│   │   ├── ExperimentController.cs             # Experiment batch processing
│   │   ├── HealthController.cs                 # Service health check
│   │   ├── QueryController.cs                  # Ask questions, RAG queries
│   │   └── SettingsController.cs               # Runtime RAG configuration
│   ├── Middleware/
│   │   └── ExceptionHandler.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── RagEvaluator.Application/                   # Business Logic & Orchestration
│   ├── Mappers/
│   │   ├── DocumentMapper.cs                   # Document → DTO mapping
│   │   ├── ExperimentMapper.cs                 # Experiment → DTO mapping
│   │   ├── ExperimentMetricsAggregator.cs      # Experiment metrics aggregation (mean, stddev, distributions)
│   │   ├── QueryMapper.cs                      # Query → DTO mapping
│   │   └── PromptTemplateResolver.cs           # Prompt template resolution by language
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IRagService.cs
│   │   │   ├── IDocumentService.cs             # Document CRUD operations
│   │   │   ├── IDocumentProcessingService.cs   # PDF processing, chunking, embedding, chunk search
│   │   │   ├── IExperimentService.cs           # Experiment batch processing
│   │   │   ├── IQueryService.cs
│   │   │   ├── IMetricsService.cs              # Similarity & evaluation metrics
│   │   │   └── ISettingsService.cs             # Runtime settings management
│   │   ├── RagService.cs                       # Core RAG orchestration
│   │   ├── DocumentService.cs                  # Document CRUD operations
│   │   ├── DocumentProcessingService.cs        # PDF processing, chunking, embedding, chunk search
│   │   ├── ExperimentService.cs                # Experiment creation, processing & aggregation
│   │   ├── QueryService.cs                     # Query handling & persistence
│   │   ├── MetricsService.cs                   # Cosine similarity, MRR, Precision@K, etc.
│   │   └── SettingsService.cs                  # Runtime RAG configuration management
│   ├── Workers/
│   │   ├── ExperimentWorker.cs                 # BackgroundService for async experiment processing
│   │   └── ExperimentQueue.cs                  # Channel<T>-based in-memory queue
│   └── Validators/
│
├── RagEvaluator.Contract/                      # DTOs, Abstractions & Shared Contracts
│   ├── Abstractions/
│   │   ├── Services/
│   │   │   ├── IChatService.cs                 # Chat/LLM service interface
│   │   │   ├── IEmbeddingService.cs            # Embedding generation interface
│   │   │   ├── IFileStorageService.cs          # File storage interface
│   │   │   ├── IPdfLoader.cs                   # PDF loading interface
│   │   │   └── ITextChunker.cs                 # Text chunking interface
│   │   └── Data/
│   │       ├── IDocumentRepository.cs          # Document repository interface
│   │       ├── IDocumentChunkRepository.cs     # Vector chunk repository interface
│   │       ├── IExperimentRepository.cs        # Experiment repository interface
│   │       └── IQueryRepository.cs             # Query repository interface
│   ├── Configurations/
│   │   ├── FileStorageConfiguration.cs         # File storage settings
│   │   └── RagConfiguration.cs
│   ├── Dtos/
│   │   ├── Requests/
│   │   │   ├── AskQuestionRequest.cs
│   │   │   ├── UploadDocumentRequest.cs
│   │   │   ├── AnnotateResultsRequest.cs       # Relevance + response quality + ground truth annotations
│   │   │   ├── CreateExperimentRequest.cs      # Experiment batch creation (queries + repeat count)
│   │   │   └── UpdateSettingsRequest.cs        # Runtime settings partial update
│   │   └── Responses/
│   │       ├── QueryResponse.cs
│   │       ├── QuerySummaryResponse.cs         # Query history list item
│   │       ├── SearchResultDto.cs
│   │       ├── DocumentResponse.cs
│   │       ├── DocumentChunkResponse.cs        # Document chunk details
│   │       ├── DocumentFileInfo.cs             # File info for downloads
│   │       ├── ExperimentResponse.cs           # Full experiment with query groups & aggregated metrics
│   │       ├── ExperimentSummaryResponse.cs    # Experiment list item with progress & config
│   │       ├── ReprocessResponse.cs            # Reprocess result (docs processed, chunks created)
│   │       └── SettingsResponse.cs             # Current settings + available options
│
├── RagEvaluator.Domain/                        # Domain Models & Business Rules
│   ├── Entities/
│   │   ├── Document.cs                         # Document aggregate root
│   │   ├── DocumentSummary.cs                  # Lightweight document (for list views)
│   │   ├── DocumentChunk.cs                    # Text chunk with vector embedding
│   │   ├── Experiment.cs                       # Experiment with config snapshot & progress
│   │   ├── Query.cs                            # User query entity
│   │   ├── QueryResult.cs                      # Retrieved chunk result with relevance
│   │   └── QueryRelevantDocument.cs             # Ground truth relevant document for Recall@K
│   ├── Enums/
│   │   ├── DocumentStatus.cs                   # Document processing status
│   │   ├── ExperimentStatus.cs                 # Experiment status (Running, Completed)
│   │   ├── ChunkingStrategy.cs                 # Chunking strategy selection (FixedSize, Semantic)
│   │   ├── PromptTemplate.cs                   # Prompt template types (Basic, Instructed, LanguageAware)
│   │   ├── RelevanceGrade.cs                   # Graded relevance scale (0-3) for NDCG
│   │   └── ResponseQuality.cs                  # LLM response quality evaluation (0-3)
│   └── ValueObjects/
│       ├── SearchResult.cs                     # Search result with similarity score
│       ├── ChunkSearchMatch.cs                 # Raw chunk match (before similarity calculation)
│       └── QueryMetrics.cs                     # RAG metrics container (MRR, P@K, R@K, NDCG@K)
│
├── RagEvaluator.Infrastructure/                # Data Access & External Services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs             # EF Core DbContext with pgvector
│   │   ├── DocumentRepository.cs               # Document repository implementation
│   │   ├── Repositories/
│   │   │   ├── DocumentChunkRepository.cs      # Vector chunk repository with pgvector
│   │   │   ├── ExperimentRepository.cs         # Experiment persistence repository
│   │   │   └── QueryRepository.cs              # Query persistence repository
│   │   ├── Configurations/
│   │   │   ├── DocumentConfiguration.cs
│   │   │   ├── DocumentChunkConfiguration.cs   # pgvector mapping
│   │   │   ├── ExperimentConfiguration.cs      # Experiment entity mapping
│   │   │   ├── QueryConfiguration.cs           # Query entity mapping
│   │   │   ├── QueryResultConfiguration.cs     # QueryResult entity mapping
│   │   │   └── QueryRelevantDocumentConfiguration.cs # Ground truth mapping
│   │   └── Migrations/
│   └── Services/
│       ├── LocalFileStorageService.cs          # Local file system storage
│       ├── PdfPigLoader.cs                     # PDF text extraction (PdfPig)
│       ├── FixedSizeTextChunker.cs             # Fixed-size text splitting
│       ├── SemanticTextChunker.cs              # Embedding-based semantic splitting
│       ├── OllamaChatService.cs                # Ollama chat service
│       └── OllamaEmbeddingService.cs           # Ollama embedding service
│
├── RagEvaluator.WebUi/                         # React Frontend (Vite)
│   ├── src/
│   │   ├── components/
│   │   ├── assets/
│   │   ├── api/
│   │   ├── utils/
│   │   ├── index.css
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── package.json
│   ├── vite.config.js
│   └── Dockerfile
│
├── RagEvaluator.Test/                          # Unit Tests (xUnit, NSubstitute)
│
├── docker-compose.yml
├── .env.example
└── ARCHITECTURE.md
```

## Layer Responsibilities

### 1. API Layer (`RagEvaluator.API`)

**Purpose**: HTTP entry point, request/response handling, composition root

**Responsibilities**:

- RESTful API endpoints
- Request validation and model binding
- Exception handling middleware
- Swagger/OpenAPI documentation
- CORS configuration (permissive for development)
- Dependency injection wiring (composition root in `Program.cs`)
- Auto-migration on startup (development only)

**Key Components**:

- Controllers (thin, delegate to Application layer)
- `ExceptionHandler` middleware for global error handling
- `Program.cs` - composition root that wires all DI registrations

**Dependencies**: → Application, Infrastructure, Contract, Domain (API references all layers because `Program.cs` is the composition root; controllers only use Application and Contract)

### 2. Application Layer (`RagEvaluator.Application`)

**Purpose**: Business logic orchestration and use cases

**Responsibilities**:

- RAG pipeline orchestration (document processing, query answering)
- Service coordination across Contract abstractions
- DTO mapping (entity → response)
- Retrieval metric calculations (MRR, Precision@K, Recall@K, NDCG@K)
- Background experiment processing

**Key Components**:

- Service interfaces and implementations (`Services/`)
- Mappers for entity-to-DTO conversion (`Mappers/`)
- `ExperimentWorker` + `ExperimentQueue` for background processing (`Workers/`)

**Implemented Services**:

- `RagService` - Core RAG orchestration
  - `ProcessDocumentAsync()` - orchestrates document upload, extraction, chunking, embedding
  - `AskQuestionAsync()` - orchestrates RAG query (embed question, search chunks, generate answer)
  - `IsInitializedAsync()` - checks if Ollama services are available (used by health endpoint)
- `DocumentService` - Document CRUD operations
  - `CreateDocumentAsync()` - creates document entity and saves file to storage
  - `GetByIdAsync()` / `GetAllAsync()` - document retrieval
  - `GetDocumentFileInfoAsync()` - file info for downloads
  - `UpdateStatusAsync()` - updates document processing status
  - `DeleteAsync()` - deletes document, file, and associated chunks
- `DocumentProcessingService` - PDF processing, chunking, embedding, and chunk search
  - `ProcessDocumentContentAsync()` - extracts text, chunks, embeds, and stores chunks
  - `ReprocessAllDocumentsAsync()` - re-chunks and re-embeds all documents using stored content and current config
  - `GetChunksByDocumentIdAsync()` - retrieves document chunks
  - `SearchChunksAsync()` - vector similarity search across all chunks
- `QueryService` - Query handling, persistence, and annotation
  - `CreateQueryAsync()` - creates and persists a query with configuration snapshot
  - `CompleteQueryAsync()` - populates query with answer, embedding, response time, and retrieved chunks
  - `GetByIdAsync()` / `GetAllAsync()` - query retrieval and history
  - `AnnotateResultsAsync()` - updates query results with relevance grades, response quality, and ground truth documents; calculates metrics
  - `DeleteAsync()` - deletes a query
- `MetricsService` - Similarity and retrieval evaluation metrics
  - `CosineSimilarity()` / `CosineDistance()` - vector similarity calculations
  - `MeanReciprocalRank()` - MRR for retrieval evaluation
  - `PrecisionAtK()` / `RecallAtK()` - precision and recall metrics
  - `NormalizedDiscountedCumulativeGainAtK()` - NDCG for ranking quality
  - `CalculateQueryMetrics()` - calculates all metrics for a query from its results
- `SettingsService` - Runtime RAG configuration management
  - `GetSettings()` - returns current configuration with available options
  - `UpdateSettingsAsync()` - validates and applies partial config updates; triggers embedding service reinitialization when the model changes
- `ExperimentService` - Experiment batch processing and aggregation
  - `CreateExperimentAsync()` - creates experiment with config snapshot, enqueues for background processing
  - `ProcessExperimentAsync()` - runs all queries × repeatCount, links results to experiment, updates progress
  - `GetByIdAsync()` - returns experiment with query groups and aggregated metrics
  - `GetAllAsync()` / `DeleteAsync()` - list and delete operations
- `ExperimentWorker` - `BackgroundService` that reads from `ExperimentQueue` and delegates to `ExperimentService.ProcessExperimentAsync()`. Processes one experiment at a time.
- `ExperimentQueue` - In-memory `Channel<T>`-based queue for passing experiment work items from the API to the background worker.

**Dependencies**: → Domain, Contract

### 3. Contract Layer (`RagEvaluator.Contract`)

**Purpose**: Shared abstractions, DTOs, and configuration across all layers

**Responsibilities**:

- Service interface definitions (IChatService, IEmbeddingService, IFileStorageService, IPdfLoader, ITextChunker)
- Repository interface definitions (IDocumentRepository, IDocumentChunkRepository, IQueryRepository, IExperimentRepository)
- Request/response DTOs for API communication
- Configuration models for runtime settings

**Key Components**:

- `Abstractions/Services/` - Infrastructure service interfaces
- `Abstractions/Data/` - Repository interfaces
- `Configurations/` - `RagConfiguration` (runtime-mutable RAG settings), `FileStorageConfiguration`
- `Dtos/Requests/` - API request DTOs (AskQuestionRequest, UploadDocumentRequest, AnnotateResultsRequest, CreateExperimentRequest, UpdateSettingsRequest)
- `Dtos/Responses/` - API response DTOs (QueryResponse, QuerySummaryResponse, DocumentResponse, ExperimentResponse, ExperimentSummaryResponse, ReprocessResponse, SettingsResponse, etc.)

**Dependencies**: → Domain (for entity types and value objects used in interface signatures)

### 4. Domain Layer (`RagEvaluator.Domain`)

**Purpose**: Core data definitions (framework-agnostic)

**Responsibilities**:

- Entity definitions
- Value objects
- Enums

**Key Components**:

- Entities: `Document`, `DocumentSummary`, `DocumentChunk`, `Experiment`, `Query`, `QueryResult`, `QueryRelevantDocument`
- Enums: `DocumentStatus`, `ExperimentStatus`, `ChunkingStrategy`, `PromptTemplate`, `RelevanceGrade`, `ResponseQuality`
- Value Objects: `SearchResult`, `ChunkSearchMatch`, `QueryMetrics`

**Dependencies**: None

### 5. Infrastructure Layer (`RagEvaluator.Infrastructure`)

**Purpose**: Implementations of Contract interfaces - data access and external service integration

**Responsibilities**:

- Data persistence via EF Core with PostgreSQL and pgvector
- Ollama LLM integration (chat and embeddings) via Semantic Kernel
- PDF text extraction via PdfPig
- Local file storage for uploaded documents
- Text chunking algorithms (fixed-size and semantic)

**Key Components**:

- `Data/` - `ApplicationDbContext`, EF entity configurations, repositories
- `Services/` - Implementations of Contract service interfaces

**Implemented Services**:

- `OllamaChatService` - Chat completion via Semantic Kernel, implements `IChatService`
- `OllamaEmbeddingService` - Embedding generation via Semantic Kernel, implements `IEmbeddingService`. Supports runtime reinitialization for model switching
- `PdfPigLoader` - PDF text extraction using ContentOrderTextExtractor with whitespace normalization, implements `IPdfLoader`
- `LocalFileStorageService` - Local file system storage with configurable directory, implements `IFileStorageService`
- `FixedSizeTextChunker` - Character-based text chunking with configurable size and overlap, implements `ITextChunker`
- `SemanticTextChunker` - Embedding-based chunking that splits at topic boundaries via cosine similarity drops between consecutive line embeddings, implements `ITextChunker`

**Repositories**:

- `DocumentRepository` - Document CRUD with status filtering
- `DocumentChunkRepository` - Chunk persistence with pgvector similarity search (raw SQL with `<=>` operator)
- `QueryRepository` - Query persistence with eager loading of results, relevant documents, and experiment association
- `ExperimentRepository` - Experiment persistence with eager loading of queries

**Vector Storage Architecture**:
- Domain layer uses `float[]` for embeddings (no external dependencies)
- Infrastructure layer converts to pgvector `Vector` type via EF Core value converter
- Similarity search uses raw SQL with pgvector's cosine distance operator (`<=>`) for ordering
- Repository returns `ChunkSearchMatch` with raw embeddings; similarity scores calculated by `MetricsService` in Application layer

**Dependencies**: → Domain, Contract

### 6. WebUI Layer (`RagEvaluator.WebUi`)

**Purpose**: User interface (React SPA)

**Responsibilities**:

- User interface components
- API communication via Axios
- Client-side routing
- Form handling and validation

## RAG Implementation Workflow

### Document Processing Pipeline

```
1. PDF Upload (Controller)
   → 2. RagService.ProcessDocumentAsync() (Application Layer)
      → 3. DocumentService.CreateDocumentAsync() - Save file and create document entity (status: Pending)
      → 4. DocumentService.UpdateStatusAsync(Processing)
      → 5. DocumentProcessingService.ProcessDocumentContentAsync()
         → 6. PdfPigLoader.LoadPdf() - Extract text using ContentOrderTextExtractor
         → 7. Join pages into single content string
         → 8. ITextChunker.CreateDocumentChunksAsync() - Split into chunks
               • fixed-size: Character-based splitting (ChunkSize, ChunkOverlap)
               • semantic: Embedding-based splitting at topic boundaries (SimilarityThreshold)
         → 9. For each chunk:
            → 10. IEmbeddingService.GenerateEmbeddingAsync("search_document: " + chunk)
            → 11. Create DocumentChunk entity with embedding, strategy, model info
         → 12. DocumentChunkRepository.AddRangeAsync() - Persist all chunks to PostgreSQL
         → 13. Update Document status to Completed with content stored for future reprocessing
      → On failure: UpdateStatusAsync(Failed) and rethrow
   → 14. Return DocumentResponse (DTO)
```

### Document Reprocessing Pipeline

```
1. Settings Change or Manual Trigger
   → 2. DocumentProcessingService.ReprocessAllDocumentsAsync() (Application Layer)
      → 3. DocumentChunkRepository.DeleteAllAsync() - Remove all existing chunks
      → 4. Fetch all documents with Status == Completed
      → 5. Set all documents to Status = Processing
      → 6. For each document:
         → 7. ITextChunker.CreateDocumentChunksAsync(document.Content) - Re-chunk stored content
         → 8. For each chunk:
            → 9. OllamaEmbeddingService.GenerateEmbeddingAsync("search_document: " + chunk)
            → 10. Create DocumentChunk entity with current config (strategy, model)
         → 11. DocumentChunkRepository.AddRangeAsync() - Persist new chunks
         → 12. Update document ChunkCount, ProcessedAt, Status = Completed
   → 13. Return ReprocessResponse (documents processed, total chunks, config used)
```

### Query Processing Pipeline

```
1. Question Submission (Controller)
   → 2. RagService.AskQuestionAsync() (Application Layer)
      → 3. Start timing with Stopwatch
      → 4. PromptTemplateResolver.Resolve() - Resolve system prompt from template + query language
      → 5. QueryService.CreateQueryAsync() - Create query object with configuration snapshot
           and generate query embedding via IEmbeddingService ("search_query: " + question)
           (in-memory only, not persisted yet)
      → 6. DocumentProcessingService.SearchChunksAsync() - Find top K similar chunks
           (delegates to DocumentChunkRepository, uses pgvector cosine distance for ordering)
      → 7. Build context from retrieved chunks
      → 8. IChatService.GenerateResponseAsync() - Generate answer with system prompt and context
      → 9. Stop timing, calculate response time
      → 10. QueryService.CompleteQueryAsync() - Populate and persist query with answer,
            response time, and QueryResults to database
   → 11. Return QueryResponse with answer + sources
```

### Experiment Processing Pipeline

```
1. Experiment Submission (Controller)
   → 2. ExperimentService.CreateExperimentAsync() (Application Layer)
      → 3. Create Experiment entity with config snapshot from current RagConfiguration
      → 4. Persist to database (status: Running)
      → 5. Enqueue (experimentId, queries) to ExperimentQueue (Channel<T>)
   → 6. Return 202 Accepted with ExperimentSummaryResponse

7. ExperimentWorker picks up work item from queue
   → 8. ExperimentService.ProcessExperimentAsync()
      → 9. For each repeat (1..repeatCount):
         → For each query in queries:
            → 10. RagService.AskQuestionAsync() - Runs full RAG pipeline
            → 11. Load resulting Query, set ExperimentId, save
            → 12. Increment CompletedQueryCount, update experiment
      → 13. Set status to Completed, set CompletedAt

14. GET /api/experiments/{id} → ExperimentMapper.ToResponse()
    → Groups queries by (Question, Language, TopK)
    → ExperimentMetricsAggregator computes aggregated metrics per group and overall:
       • Mean/StdDev response time
       • Mean MRR, Precision@K, Recall@K, NDCG@K
       • Response quality distribution (categorical count)
       • Language switching rate
```

## Database Design

### Relational Database (PostgreSQL + EF Core)

Used for structured data and metadata:

**Tables**:

```sql
-- Documents table
CREATE TABLE Documents (
    Id UUID PRIMARY KEY,
    FileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(500),
    FileSize BIGINT,
    MimeType VARCHAR(100),
    Content TEXT,                -- Full extracted PDF text
    Language VARCHAR(50),        -- Document language (en, de)
    Course VARCHAR(100),         -- Course name for categorization
    PageCount INT,
    ChunkCount INT,
    UploadedAt TIMESTAMP NOT NULL,
    ProcessedAt TIMESTAMP,
    Status VARCHAR(50)           -- Pending, Processing, Completed, Failed
);

-- DocumentChunks table (vector embeddings with pgvector)
CREATE TABLE DocumentChunks (
    Id UUID PRIMARY KEY,
    Text TEXT NOT NULL,
    Embedding VECTOR NOT NULL,   -- pgvector type for similarity search
    ChunkingStrategy VARCHAR(100) NOT NULL,
    EmbeddingModel VARCHAR(100) NOT NULL,
    DocumentId UUID NOT NULL REFERENCES Documents(Id) ON DELETE CASCADE
);
CREATE INDEX IX_DocumentChunks_DocumentId ON DocumentChunks(DocumentId);

-- Experiments table (batch experiment with config snapshot)
CREATE TABLE Experiments (
    Id UUID PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    RepeatCount INT NOT NULL,
    Status TEXT NOT NULL,                -- ExperimentStatus enum: 'Running', 'Completed'
    CreatedAt TIMESTAMP NOT NULL,
    CompletedAt TIMESTAMP,
    EmbeddingModel VARCHAR(100) NOT NULL,
    ChunkingStrategy VARCHAR(100) NOT NULL,
    ChatModel VARCHAR(100) NOT NULL,
    ChunkSize INT NOT NULL,
    ChunkOverlap INT NOT NULL,
    SimilarityThreshold DOUBLE PRECISION NOT NULL,
    PromptTemplate VARCHAR(100) NOT NULL,
    TotalQueryCount INT NOT NULL,
    CompletedQueryCount INT NOT NULL
);

-- Queries table (query history with response and metrics)
CREATE TABLE Queries (
    Id UUID PRIMARY KEY,
    Question TEXT NOT NULL,
    Language VARCHAR(10) NOT NULL,
    TopK INT NOT NULL,
    SystemPrompt TEXT NOT NULL,
    ChunkingStrategy VARCHAR(100) NOT NULL,
    EmbeddingModel VARCHAR(100) NOT NULL,
    ChatModel VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    Answer TEXT NOT NULL,
    QueryEmbedding VECTOR NOT NULL,      -- pgvector type for offline analysis
    ResponseTimeMs INT NOT NULL,
    ExperimentId UUID REFERENCES Experiments(Id) ON DELETE SET NULL,  -- Nullable FK to experiment
    ResponseQuality INT,                 -- Nullable (ResponseQuality enum: 0=CorrectAndComplete, 1=VagueOrIncomplete, 2=Incorrect, 3=Hallucinated)
    HasLanguageSwitching BOOLEAN,        -- Nullable (language switching detection)
    MRR DOUBLE PRECISION,                -- Nullable metrics (calculated after relevance labeling)
    PrecisionAtK DOUBLE PRECISION,
    RecallAtK DOUBLE PRECISION,
    NDCGAtK DOUBLE PRECISION
);
CREATE INDEX IX_Queries_ExperimentId ON Queries(ExperimentId);

-- QueryResults table (retrieved chunks with relevance labeling)
CREATE TABLE QueryResults (
    Id UUID PRIMARY KEY,
    QueryId UUID NOT NULL REFERENCES Queries(Id) ON DELETE CASCADE,
    DocumentChunkId UUID NOT NULL,       -- Reference only (denormalized data below)
    DocumentId UUID NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    ChunkText TEXT NOT NULL,
    ChunkingStrategy VARCHAR(100) NOT NULL,
    EmbeddingModel VARCHAR(100) NOT NULL,
    Rank INT NOT NULL,
    SimilarityScore DOUBLE PRECISION NOT NULL,
    IsRelevant BOOLEAN,                  -- Nullable (for relevance labeling)
    RelevanceGrade INT                   -- Nullable (RelevanceGrade enum: 0=NotRelevant, 1=MarginallyRelevant, 2=FairlyRelevant, 3=HighlyRelevant)
);
CREATE INDEX IX_QueryResults_QueryId ON QueryResults(QueryId);
CREATE INDEX IX_QueryResults_DocumentId ON QueryResults(DocumentId);

-- QueryRelevantDocuments table (ground truth for Recall@K calculation)
CREATE TABLE QueryRelevantDocuments (
    QueryId UUID NOT NULL REFERENCES Queries(Id) ON DELETE CASCADE,
    DocumentId UUID NOT NULL,
    PRIMARY KEY (QueryId, DocumentId)
);
CREATE INDEX IX_QueryRelevantDocuments_DocumentId ON QueryRelevantDocuments(DocumentId);
```

### Vector Store Implementation

**PostgreSQL with pgvector** (Current Implementation)

- SQL + vector search in one database
- ACID compliance with cascade deletes
- Cosine similarity search via `<=>` operator
- EF Core integration with value converter (`float[]` ↔ `Vector`)
- Supports multiple documents with cross-document similarity search

## API Design

### RESTful Endpoints

#### Documents API

```
POST   /api/documents/upload        # Upload PDF document
GET    /api/documents               # List all documents
GET    /api/documents/{id}          # Get document details
GET    /api/documents/{id}/download # Download document file
DELETE /api/documents/{id}          # Delete document
GET    /api/documents/{id}/chunks   # Get document chunks
POST   /api/documents/reprocess     # Reprocess all documents with current config
```

#### Query API

```
POST   /api/query                   # Ask question using RAG
GET    /api/query/history           # Get query history
GET    /api/query/{id}              # Get specific query
PATCH  /api/query/{id}/results      # Annotate results with relevance, response quality, and ground truth documents
DELETE /api/query/{id}              # Delete a query
```

#### Experiments API

```
POST   /api/experiments             # Create experiment (batch queries x repeatCount, background processing)
GET    /api/experiments             # List all experiments with progress and config
GET    /api/experiments/{id}        # Get experiment with query groups and aggregated metrics
DELETE /api/experiments/{id}        # Delete experiment (queries preserved via SET NULL)
```

#### Settings API

```
GET    /api/settings                # Get current runtime RAG configuration and available options
PATCH  /api/settings                # Update runtime RAG configuration (partial update)
```

#### Health API

```
GET    /api/health                  # Check if RAG services are ready
```

### Request/Response Examples

**Upload Document Request**:

```
POST /api/documents/upload
Content-Type: multipart/form-data

file: <PDF file>
language: "en" or "de"
course: "Software Engineering I"
```

**Upload Document Response**:

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "fileName": "document.pdf",
  "language": "en",
  "course": "Software Engineering I",
  "pageCount": 15,
  "chunkCount": 38,
  "uploadedAt": "2025-01-04T12:00:00Z",
  "status": "Completed"
}
```

**Ask Question Request**:

```json
{
  "question": "What is the main conclusion?",
  "topK": 3,
  "language": "en"
}
```

**Ask Question Response**:

```json
{
  "queryId": "456e7890-e89b-12d3-a456-426614174001",
  "question": "What is the main conclusion?",
  "answer": "The main conclusion is...",
  "sources": [
    {
      "id": "789e0123-e89b-12d3-a456-426614174002",
      "text": "...relevant text chunk...",
      "similarity": 0.892,
      "documentId": "123e4567-e89b-12d3-a456-426614174000",
      "fileName": "document.pdf",
      "chunkingStrategy": "FixedSize",
      "embeddingModel": "nomic-embed-text-v2-moe"
    }
  ],
  "timestamp": "2025-01-04T12:05:00Z"
}
```

## Technology Stack

### Backend

- **Framework**: ASP.NET Core 10.0
- **Architecture**: Clean Architecture (Onion Architecture)
- **AI/ML Framework**: Microsoft Semantic Kernel 1.70.0
- **LLM Provider**: Ollama (local models, configurable via `.env` and runtime Settings API)
  - **Embedding Models**: nomic-embed-text-v2-moe (default), nomic-embed-text (configurable, hot-swappable at runtime)
  - **Chat Model**: qwen2.5:14b
- **PDF Processing**: PdfPig 0.1.13
- **Vector Store**: PostgreSQL with pgvector extension
  - Persistent storage with cosine similarity search
  - EF Core integration via Pgvector.EntityFrameworkCore
- **Database**: PostgreSQL 18 (pgvector/pgvector:0.8.1-pg18)
- **ORM**: Entity Framework Core 10.0 with Npgsql
- **API Documentation**: Swagger/OpenAPI (Swashbuckle.AspNetCore 10.1.0)
- **Testing**: xUnit 3, NSubstitute

### Frontend

- **Framework**: React 19
- **Language**: JavaScript
- **Build Tool**: Vite 7
- **Routing**: React Router DOM 7
- **UI Library**: Tailwind CSS 4
- **HTTP Client**: Axios
- **UI Libraries**: React Dropzone, React Toastify, Heroicons

### DevOps

- **Containerization**: Docker & Docker Compose
- **CI/CD**: GitHub Actions

## Docker Deployment

### Container Architecture

The application uses 4 Docker containers orchestrated via Docker Compose:

| Container | Image | Port Mapping | Purpose |
|-----------|-------|--------------|---------|
| ragevaluator-api | Custom (.NET 10) | 5000:8080 | ASP.NET Core Web API |
| ragevaluator-web-ui | Custom (Nginx + React) | 3000:80 | React frontend |
| postgres | pgvector/pgvector:0.8.1-pg18 | 5432:5432 | PostgreSQL with pgvector |
| ollama | ollama/ollama:0.13.5 | 11434:11434 | Local LLM service |

### Ollama Initialization

The Ollama container uses a custom initialization script (`ollama-init.sh`) that:

1. Starts the Ollama service in the background
2. Waits for the service to be ready
3. Checks for required models (configured via `.env`) and pulls them if missing:
   - All embedding models listed in `OLLAMA_EMBEDDING_MODELS` (comma-separated, e.g., `nomic-embed-text-v2-moe,nomic-embed-text`)
   - `qwen2.5:14b` - Chat completion model (approximately 9 GB)
4. Models are persisted in the `ollama_data` Docker volume

**First Startup**: Initial container startup takes 5-10 minutes to download models (approximately 10 GB total).

**Subsequent Startups**: Nearly instant as models are cached in the persistent volume.

### Environment Configuration

The API container is configured via environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=ragevaluator;...
  - RagConfiguration__OllamaEndpoint=http://ollama:11434/v1
  - RagConfiguration__ChatModel=${OLLAMA_CHAT_MODEL}
  - RagConfiguration__AvailableEmbeddingModels=${OLLAMA_EMBEDDING_MODELS}
  - RagConfiguration__ChunkingStrategy=${RAG_CHUNKING_STRATEGY}
  - RagConfiguration__ChunkSize=${RAG_CHUNK_SIZE}
  - RagConfiguration__ChunkOverlap=${RAG_CHUNK_OVERLAP}
  - RagConfiguration__SimilarityThreshold=${RAG_SIMILARITY_THRESHOLD}
  - RagConfiguration__PromptTemplate=${RAG_PROMPT_TEMPLATE}
  - RagConfiguration__PromptBasic=${RAG_PROMPT_BASIC}
  - RagConfiguration__PromptInstructed=${RAG_PROMPT_INSTRUCTED}
  - RagConfiguration__PromptLanguageAwareEn=${RAG_PROMPT_LANGUAGE_AWARE_EN}
  - RagConfiguration__PromptLanguageAwareDe=${RAG_PROMPT_LANGUAGE_AWARE_DE}
  - RagConfiguration__AvailableCourses=${AVAILABLE_COURSES}
  - FileStorageConfiguration__BaseDirectory=/app/uploads
```

**Prompt Templates**: Three prompt strategies are available via `RAG_PROMPT_TEMPLATE` in `.env`: `Basic` (basic English prompt), `Instructed` (English prompt with explicit language instruction), and `LanguageAware` (prompt in the query's native language, selected automatically based on the query language). Each template's text is independently configurable via `RAG_PROMPT_BASIC`, `RAG_PROMPT_INSTRUCTED`, `RAG_PROMPT_LANGUAGE_AWARE_EN`, and `RAG_PROMPT_LANGUAGE_AWARE_DE`. All RAG parameters can also be changed at runtime via the Settings API without restarting the container.

**Available Courses**: Course names for document categorization are configured via `AVAILABLE_COURSES` in `.env` as a comma-separated list (e.g., `Datenmanagement,Software Engineering I`). These appear in the upload UI dropdown and are returned by the Settings API.

### Docker Networking

Containers communicate via Docker's internal network:
- API connects to Ollama at `http://ollama:11434`
- API connects to PostgreSQL at `postgres:5432`
- External access via port mappings (5000, 3000, etc.)

## In Progress / Planned

- [ ] Refine which embedding models will be used and refine the prompt templates
- [ ] Analytics and metrics Dashboard

## Resources

### Architecture & Best Practices
- [Clean Architecture - Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)

### AI/ML & RAG
- [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Ollama Documentation](https://ollama.ai/)
- [RAG (Retrieval-Augmented Generation) Overview](https://arxiv.org/abs/2005.11401)

### Frontend & DevOps
- [React Documentation](https://react.dev/)
- [Docker Documentation](https://docs.docker.com/)
- [Swagger/OpenAPI Specification](https://swagger.io/specification/)
