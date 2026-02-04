# RAG-Evaluator Architecture

This document describes the architecture, design decisions, and technical implementation details of the RAG-Evaluator application.

## Table of Contents

- [Architectural Pattern](#architectural-pattern)
- [Project Structure](#project-structure)
- [Layer Responsibilities](#layer-responsibilities)
- [Database Design](#database-design)
- [API Design](#api-design)
- [Technology Stack](#technology-stack)
- [Security Considerations](#security-considerations)
- [Scalability Considerations](#scalability-considerations)
- [Future Enhancements](#future-enhancements)
- [Resources](#resources)

## Architectural Pattern

The application follows **Clean Architecture** (Onion Architecture) principles with clear separation of concerns and a pragmatic, centralized approach to interface management:

- **Dependency Rule**: Dependencies point inward (Infrastructure → Application → Domain)
- **Domain-Centric**: Business logic independent of frameworks and external concerns
- **Testability**: Each layer can be tested independently
- **Maintainability**: Changes in one layer have minimal impact on others
- **Centralized Abstractions**: All interface definitions are consolidated in the Contract layer for simplified dependency management

### Dependency Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      RagEvaluator.API                       │
│                   (Controllers, Middleware)                 │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  RagEvaluator.Application                   │
│            (Business Logic & Orchestration)                 │
│  Services: RagService, DocumentService, QueryService,       │
│            MetricsService                                   │
└──────────┬────────────────────────────────┬─────────────────┘
           │                                │
           ↓                                ↓
┌──────────────────────┐        ┌─────────────────────────────┐
│ RagEvaluator.Domain  │        │  RagEvaluator.Contract      │
│  (Core Entities)     │←───────│  (ALL Abstractions)         │
│                      │        │                             │
│ • Entities           │        │ • Abstractions/Services/    │
│ • Value Objects      │        │ • Abstractions/Data/        │
│ • Exceptions         │        │ • Dtos/                     │
└──────────────────────┘        │ • Configurations/           │
                                │ • Logger/                   │
                                └────────────┬────────────────┘
                                             │
                                             ↓
                                ┌────────────────────────────┐
                                │ RagEvaluator.Infrastructure│
                                │   (Implementations)        │
                                │                            │
                                │ • OllamaChatService        │
                                │ • OllamaEmbeddingService   │
                                │ • LocalFileStorageService  │
                                │ • PdfPigLoader             │
                                │ • FixedSizeTextChunker     │
                                │ • DocumentRepository       │
                                │ • DocumentChunkRepository  │
                                └────────────────────────────┘
```

**Key Points**:
- Infrastructure implements interfaces defined in Contract
- Application orchestrates workflows using Contract abstractions
- Domain is dependency-free and purely focused on business logic
- Contract serves as the central "interface hub" for the entire application

## Project Structure

```
RAG-Evaluator/
├── RagEvaluator.API/                    # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── DocumentController.cs        # Upload PDFs, manage docs
│   │   ├── HealthController.cs          # Service health check
│   │   └── QueryController.cs           # Ask questions, RAG queries
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── RagEvaluator.Application/            # Business Logic & Orchestration
│   ├── Mappers/
│   │   ├── DocumentMapper.cs            # Document → DTO mapping
│   │   └── QueryMapper.cs               # Query → DTO mapping
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IRagService.cs
│   │   │   ├── IDocumentService.cs
│   │   │   ├── IQueryService.cs
│   │   │   └── IMetricsService.cs       # Similarity & evaluation metrics
│   │   ├── RagService.cs                # Core RAG orchestration
│   │   ├── DocumentService.cs           # Document processing & management
│   │   ├── QueryService.cs              # Query handling & persistence
│   │   └── MetricsService.cs            # Cosine similarity, MRR, Precision@K, etc.
│   └── Validators/
│
├── RagEvaluator.Contract/               # DTOs, Abstractions & Shared Contracts
│   ├── Abstractions/
│   │   ├── Services/
│   │   │   ├── IChatService.cs          # Chat/LLM service interface
│   │   │   ├── IEmbeddingService.cs     # Embedding generation interface
│   │   │   ├── IFileStorageService.cs   # File storage interface
│   │   │   ├── IPdfLoader.cs            # PDF loading interface
│   │   │   └── ITextChunker.cs          # Text chunking interface
│   │   └── Data/
│   │       ├── IDocumentRepository.cs   # Document repository interface
│   │       ├── IDocumentChunkRepository.cs # Vector chunk repository interface
│   │       └── IQueryRepository.cs      # Query repository interface
│   ├── Configurations/
│   │   ├── FileStorageConfiguration.cs  # File storage settings
│   │   └── RagConfiguration.cs
│   ├── Dtos/
│   │   ├── Requests/
│   │   │   ├── AskQuestionRequest.cs
│   │   │   ├── UploadDocumentRequest.cs
│   │   │   └── AnnotateResultsRequest.cs       # Relevance + response quality annotations
│   │   └── Responses/
│   │       ├── QueryResponse.cs
│   │       ├── QuerySummaryResponse.cs  # Query history list item
│   │       ├── SearchResultDto.cs
│   │       ├── DocumentResponse.cs
│   │       ├── DocumentChunkResponse.cs # Document chunk details
│   │       └── DocumentFileInfo.cs      # File info for downloads
│   └── Logger/
│       ├── ILoggerWrapper.cs
│       └── LoggerWrapper.cs
│
├── RagEvaluator.Domain/                 # Domain Models & Business Rules
│   ├── Entities/
│   │   ├── Document.cs                  # Document aggregate root
│   │   ├── DocumentSummary.cs           # Lightweight document (for list views)
│   │   ├── DocumentChunk.cs             # Text chunk with vector embedding
│   │   ├── Query.cs                     # User query entity
│   │   └── QueryResult.cs               # Retrieved chunk result with relevance
│   ├── Enums/
│   │   ├── DocumentStatus.cs            # Document processing status
│   │   ├── RelevanceGrade.cs            # Graded relevance scale (0-3) for NDCG
│   │   └── ResponseQuality.cs           # LLM response quality evaluation (0-3)
│   ├── ValueObjects/
│   │   ├── SearchResult.cs              # Search result with similarity score
│   │   ├── ChunkSearchMatch.cs          # Raw chunk match (before similarity calculation)
│   │   └── QueryMetrics.cs              # RAG metrics container (MRR, P@K, R@K, NDCG@K)
│   └── Exceptions/
│       ├── DocumentNotFoundException.cs
│       └── VectorStoreException.cs
│
├── RagEvaluator.Infrastructure/         # Data Access & External Services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # EF Core DbContext with pgvector
│   │   ├── DocumentRepository.cs        # Document repository implementation
│   │   ├── Repositories/
│   │   │   ├── DocumentChunkRepository.cs # Vector chunk repository with pgvector
│   │   │   └── QueryRepository.cs       # Query persistence repository
│   │   ├── Configurations/
│   │   │   ├── DocumentConfiguration.cs
│   │   │   ├── DocumentChunkConfiguration.cs # pgvector mapping
│   │   │   ├── QueryConfiguration.cs    # Query entity mapping
│   │   │   └── QueryResultConfiguration.cs # QueryResult entity mapping
│   │   └── Migrations/
│   └── Services/
│       ├── LocalFileStorageService.cs   # Local file system storage
│       ├── PdfPigLoader.cs              # PDF text extraction (PdfPig)
│       ├── FixedSizeTextChunker.cs      # Text splitting
│       ├── OllamaChatService.cs         # Ollama chat service
│       └── OllamaEmbeddingService.cs    # Ollama embedding service
│
├── RagEvaluator.WebUi/                  # React Frontend (Vite)
│   ├── src/
│   │   ├── components/
│   │   ├── assets/
│   │   ├── api/
|   |   ├── utils/
│   │   ├── index.css
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── package.json
│   ├── vite.config.js
│   └── Dockerfile
│
├── RagEvaluator.Tests/                  # Test Project
│
├── docker-compose.yml
├── .env.example
└── ARCHITECTURE.md
```

## Layer Responsibilities

### 1. API Layer (`RagEvaluator.API`)

**Purpose**: HTTP entry point, request/response handling

**Responsibilities**:

- RESTful API endpoints
- Request validation and model binding
- Authentication and authorization
- Exception handling middleware
- API versioning
- Swagger/OpenAPI documentation
- CORS configuration

**Key Components**:

- Controllers (thin, delegate to Application layer)
- Middleware for cross-cutting concerns
- Filters for validation and error handling
- Dependency injection configuration

**Dependencies**: → Application, Contract

### 2. Application Layer (`RagEvaluator.Application`)

**Purpose**: Business logic orchestration and use cases

**Responsibilities**:

- RAG pipeline orchestration
- Business workflows and use cases
- Service coordination
- Business validation
- Transaction management

**Key Components**:

- Service interfaces and implementations
- Command/Query handlers (optional CQRS)
- Business validators
- Application-specific DTOs mapping

**Implemented Services**:

- `IRagService` - Core RAG orchestration (business logic)
  - `ProcessDocumentAsync()` - Orchestrates document upload workflow
  - `AskQuestionAsync()` - Orchestrates RAG query workflow
  - `IsInitializedAsync()` - Checks if Ollama services are available (used by health endpoint)
- `IDocumentService` - Document processing and management operations
  - `ProcessDocumentAsync()` - Orchestrates PDF processing workflow
  - `GetDocumentChunksAsync()` - Retrieves document chunks
- `IQueryService` - Query handling, persistence, and history management
  - `CreateQuery()` - Creates a query object with configuration snapshot (no persistence)
  - `CompleteQueryAsync()` - Populates query with answer, embedding, response time, retrieved chunks, and persists to database
  - `GetQueryByIdAsync()` - Retrieves query by ID
  - `GetQueryHistoryAsync()` - Returns paginated query history
  - `AnnotateResultsAsync()` - Updates query results with relevance grades and response quality evaluation
  - `CalculateMetricsAsync()` - Calculates MRR, Precision@K, Recall@K, NDCG@K based on relevance labels
- `IMetricsService` - Similarity and retrieval evaluation metrics
  - `CosineSimilarity()` / `CosineDistance()` - Vector similarity calculations
  - `MeanReciprocalRank()` - MRR for retrieval evaluation
  - `PrecisionAtK()` / `RecallAtK()` - Precision and recall metrics
  - `NormalizedDiscountedCumulativeGainAtK()` - NDCG for ranking quality
  - `CalculateQueryMetrics()` - Calculates all metrics for a query from its results

**Dependencies**: → Domain, Contract

### 3. Contract Layer (`RagEvaluator.Contract`)

**Purpose**: Shared abstractions, DTOs, and contracts across all layers

**Responsibilities**:

- **Service Abstractions**: All service interface definitions (IChatService, IEmbeddingService, etc.)
- **Data Abstractions**: Repository and data access interfaces (IVectorStore, IDocumentRepository)
- **Configuration Models**: Application configuration structures
- **DTOs**: Request/response models for API communication
- **Logging Abstractions**: Logger wrapper interfaces and implementations
- **Shared Contracts**: Validation attributes and common enums

**Key Components**:

- `Abstractions/Services/` - Service interfaces (IChatService, IEmbeddingService, IFileStorageService, IPdfLoader, ITextChunker)
- `Abstractions/Data/` - Data access interfaces (IVectorStore, IDocumentRepository)
- `Configurations/` - Configuration models (RagConfiguration, FileStorageConfiguration)
- `Dtos/Requests/` - API request DTOs
- `Dtos/Responses/` - API response DTOs
- `Logger/` - Logger abstraction and implementation

**Dependencies**: → Domain (for value objects like SearchResult used in interfaces)

### 4. Domain Layer (`RagEvaluator.Domain`)

**Purpose**: Core business entities and rules (framework-agnostic)

**Responsibilities**:

- Domain entities and aggregates
- Value objects
- Domain exceptions
- Business invariants and rules

**Key Components**:

- Entities: `Document`, `DocumentSummary`, `DocumentChunk`, `Query`, `QueryResult`
- Value Objects: `SearchResult`, `ChunkSearchMatch`
- Domain exceptions: `DocumentNotFoundException`, `VectorStoreException`

**DocumentChunk Entity Fields**:
- `Id` - Unique identifier (GUID)
- `Text` - The text content of the chunk
- `Embedding` - Vector embedding as `float[]` (converted to pgvector in Infrastructure)
- `ChunkingStrategy` - Strategy used to create this chunk (e.g., "fixed-size")
- `EmbeddingModel` - Model used to generate the embedding (e.g., "nomic-embed-text-v2-moe")
- `DocumentId` - Foreign key to parent Document

**Document Entity Fields**:
- `Id`, `FileName`, `FilePath`, `FileSize`, `MimeType`
- `Content` - Full extracted text from PDF (for search/analysis)
- `Language` - Document language (`en`, `de`)
- `PageCount`, `ChunkCount`, `UploadedAt`, `ProcessedAt`, `Status`

**Query Entity Fields**:
- `Id`, `Question`, `Language`, `TopK` - Query parameters
- `SystemPrompt`, `ChunkingStrategy`, `EmbeddingModel`, `ChatModel` - Configuration tracking
- `Answer` - LLM-generated response
- `QueryEmbedding` - Vector embedding as `float[]` for offline analysis
- `ResponseTimeMs`, `CreatedAt` - Performance and timing
- `ResponseQuality` - LLM response quality evaluation (nullable, `ResponseQuality` enum: CorrectAndComplete=0, VagueOrIncomplete=1, Incorrect=2, Hallucinated=3)
- `HasLanguageSwitching` - Flag indicating unexpected language switching in response (nullable)
- `MRR`, `PrecisionAtK`, `RecallAtK`, `NDCGAtK` - Retrieval metrics (nullable, calculated after relevance labeling)
- `Results` - Navigation to `QueryResult` collection

**QueryResult Entity Fields**:
- `Id`, `QueryId` - Primary key and foreign key to Query
- `DocumentChunkId`, `DocumentId`, `FileName`, `ChunkText`, `ChunkingStrategy`, `EmbeddingModel` - Denormalized chunk data (preserved for reproducible evaluation even if original chunks are modified or deleted)
- `Rank`, `SimilarityScore` - Retrieval position and cosine similarity
- `IsRelevant`, `RelevanceGrade` - Relevance labeling for metrics calculation (nullable). `RelevanceGrade` is a `RelevanceGrade` enum (NotRelevant=0, MarginallyRelevant=1, FairlyRelevant=2, HighlyRelevant=3)

**Dependencies**: None (pure domain logic)

### 5. Infrastructure Layer (`RagEvaluator.Infrastructure`)

**Purpose**: External concerns and implementation details

**Responsibilities**:

- Data persistence (Entity Framework Core)
- External service integration (Ollama)
- File system operations (PDF loading)
- Vector store implementation

**Key Components**:

- EF Core DbContext and configurations
- Repository implementations (DocumentRepository)
- PDF processing services
- Vector store implementations
- External API clients

**Implemented Services**:

- `LocalFileStorageService` - Local file system storage with configurable directory
- `PdfPigLoader` - PDF text extraction using PdfPig with ContentOrderTextExtractor for proper reading order
- `FixedSizeTextChunker` - Text chunking with configurable size and overlap
- `DocumentChunkRepository` - PostgreSQL vector store with pgvector for similarity search
- `OllamaEmbeddingService` - Ollama embedding generation via Semantic Kernel
- `OllamaChatService` - Ollama chat completion via Semantic Kernel

**Vector Storage Architecture**:
- Domain layer uses `float[]` for embeddings (no external dependencies)
- Infrastructure layer converts to pgvector `Vector` type via EF Core value converter
- Similarity search uses raw SQL with pgvector's cosine distance operator (`<=>`) for ordering
- Repository returns `ChunkSearchMatch` with raw embeddings; similarity scores calculated by `MetricsService` in Application layer

**Dependencies**: → Domain, Application

### 6. WebUI Layer (`RagEvaluator.WebUi`)

**Purpose**: User interface (React SPA)

**Responsibilities**:

- User interface components
- State management
- API communication
- Client-side routing
- Form handling and validation

**Technology Stack**:

- React 18+
- JavaScript
- Vite (build tool)
- TanStack Query (data fetching)
- React Router (routing)
- Tailwind CSS / Material-UI (UI components)

## RAG Implementation Workflow

### Document Processing Pipeline

```
1. PDF Upload (Controller)
   → 2. RagService.ProcessDocumentAsync() (Application Layer)
      → 3. PdfPigLoader.LoadPdf() - Extract text using ContentOrderTextExtractor
      → 4. FixedSizeTextChunker.SplitDocuments() - Split into chunks (1000 chars, 200 overlap)
      → 5. For each chunk:
         → 6. OllamaEmbeddingService.GenerateEmbeddingAsync("search_document: " + chunk)
         → 7. Create DocumentChunk entity with embedding, strategy, model info
      → 8. DocumentChunkRepository.AddRangeAsync() - Persist all chunks to PostgreSQL
      → 9. Update Document status to Completed
   → 10. Return DocumentResponse (DTO)
```

### Query Processing Pipeline

```
1. Question Submission (Controller)
   → 2. RagService.AskQuestionAsync() (Application Layer)
      → 3. Start timing with Stopwatch
      → 4. QueryService.CreateQuery() - Create query object with configuration snapshot (no DB)
      → 5. OllamaEmbeddingService.GenerateEmbeddingAsync("search_query: " + question)
      → 6. DocumentChunkRepository.SearchAsync() - Find top K similar chunks
         (uses pgvector cosine distance for ordering, returns ChunkSearchMatch)
      → 7. Build context from retrieved chunks
      → 8. OllamaChatService.GenerateResponseAsync() - Generate answer with context
      → 9. Stop timing, calculate response time
      → 10. QueryService.CompleteQueryAsync() - Populate and persist query with answer, embedding, response time, and QueryResults
   → 11. Return QueryResponse with answer + sources (includes similarity, fileName, chunkingStrategy, embeddingModel)
```

### Dependency Inversion in Action

The Contract layer defines **what** needs to be done (interface abstractions):
- Service interfaces: `IChatService`, `IEmbeddingService`, `IFileStorageService`, `IPdfLoader`, `ITextChunker`
- Data interfaces: `IDocumentRepository`, `IDocumentChunkRepository`

The Infrastructure layer defines **how** it's done (concrete implementations):
- `OllamaChatService`, `OllamaEmbeddingService`, `LocalFileStorageService`, `PdfPigLoader`, `FixedSizeTextChunker`
- `DocumentRepository`, `DocumentChunkRepository` (with pgvector integration)

The Application layer consumes these abstractions:
- `RagService` uses IChatService
- `DocumentService` uses IPdfLoader, ITextChunker, IFileStorageService, IEmbeddingService
- `QueryService` uses IQueryRepository, IEmbeddingService, IQueryRepository
- `MetricsService` provides similarity and evaluation metric calculations (CosineSimilarity, MRR, Precision@K, Recall@K, NDCG@K)
- No direct dependency on Infrastructure implementations

This allows:
- Testing Application layer with mocks
- Swapping implementations (e.g., PostgreSQL → another vector database)
- Framework independence
- Centralized interface management in the Contract layer
- Domain layer remains free of infrastructure dependencies (uses `float[]` for embeddings)

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
    ResponseQuality INT,                 -- Nullable (ResponseQuality enum: 0=CorrectAndComplete, 1=VagueOrIncomplete, 2=Incorrect, 3=Hallucinated)
    HasLanguageSwitching BOOLEAN,        -- Nullable (language switching detection)
    MRR DOUBLE PRECISION,                -- Nullable metrics (calculated after relevance labeling)
    PrecisionAtK DOUBLE PRECISION,
    RecallAtK DOUBLE PRECISION,
    NDCGAtK DOUBLE PRECISION
);

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
POST   /api/documents/upload        # Upload PDF document (IMPLEMENTED)
GET    /api/documents               # List all documents (IMPLEMENTED)
GET    /api/documents/{id}          # Get document details (IMPLEMENTED)
GET    /api/documents/{id}/download # Download document file (IMPLEMENTED)
DELETE /api/documents/{id}          # Delete document (IMPLEMENTED)
GET    /api/documents/{id}/chunks   # Get document chunks (IMPLEMENTED)
```

#### Query API

```
POST   /api/query                   # Ask question using RAG (IMPLEMENTED)
GET    /api/query/history           # Get query history (IMPLEMENTED)
GET    /api/query/{id}              # Get specific query (IMPLEMENTED)
PATCH  /api/query/{id}/results      # Annotate results with relevance, response quality, and calculate metrics (IMPLEMENTED)
```

#### Health API

```
GET    /api/health                   # Check if RAG services are ready (IMPLEMENTED)
```

**Implementation Status**: Core RAG functionality (upload and query) is fully implemented. Relevance annotation, response quality evaluation, and metrics calculation are fully implemented. Document CRUD endpoints (list, get, delete, download, chunks) are fully implemented. Query history endpoints are fully implemented with persistence.

### Request/Response Examples

**Upload Document Request**:

```
POST /api/documents/upload
Content-Type: multipart/form-data

file: <PDF file>
language: "en" or "de"
```

**Upload Document Response**:

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "fileName": "document.pdf",
  "language": "en",
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
      "chunkingStrategy": "fixed-size",
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
- **LLM Provider**: Ollama (local models, configurable via `.env`)
  - **Embedding Model**: nomic-embed-text-v2-moe (multilingual MoE, uses asymmetric prefixes)
  - **Chat Model**: qwen2.5:14b
- **PDF Processing**: PdfPig 0.1.13
- **Vector Store**: PostgreSQL with pgvector extension
  - Persistent storage with cosine similarity search
  - EF Core integration via Pgvector.EntityFrameworkCore
- **Database**: PostgreSQL 18 (pgvector/pgvector:0.8.1-pg18)
- **ORM**: Entity Framework Core 10.0 with Npgsql
- **API Documentation**: Swagger/OpenAPI (Swashbuckle.AspNetCore 10.1.0)
- **Testing**: xUnit, FluentAssertions, NSubstitute (planned)

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
- **API Documentation**: Swagger/OpenAPI

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
   - `nomic-embed-text-v2-moe` - Text embedding model (approximately 958 MB)
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
  - RagConfiguration__EmbeddingModel=${OLLAMA_EMBEDDING_MODEL}
  - RagConfiguration__ChatModel=${OLLAMA_CHAT_MODEL}
  - RagConfiguration__SystemPrompt=${RAG_SYSTEM_PROMPT}
  - RagConfiguration__ChunkingStrategy=${RAG_CHUNKING_STRATEGY}
  - FileStorageConfiguration__BaseDirectory=/app/uploads
```

**System Prompt**: The RAG system prompt can be customized via the `RAG_SYSTEM_PROMPT` environment variable in `.env`.

### Docker Networking

Containers communicate via Docker's internal network:
- API connects to Ollama at `http://ollama:11434`
- API connects to PostgreSQL at `postgres:5432`
- External access via port mappings (5000, 3000, etc.)

## Current Implementation Status

### Completed

- [x] **Architecture**: Clean Architecture with Dependency Inversion, Repository pattern, DTO mapping
- [x] **Document Processing**: PDF extraction (PdfPig), text chunking, file storage, language selection (en/de)
- [x] **Database**: PostgreSQL + pgvector, EF Core persistence (Documents, Chunks, Queries, QueryResults)
- [x] **RAG Pipeline**: Ollama integration (Semantic Kernel), multi-document similarity search, query embedding storage
- [x] **Metrics**: CosineSimilarity, MRR, Precision@K, Recall@K, NDCG@K
- [x] **API**: Full CRUD for documents and queries, Swagger UI, Docker Compose orchestration
- [x] **Frontend**: React UI with multi-file upload, language selection, search results with source details
- [x] **Configuration**: System prompt, embedding model, and chunking strategy via `.env`
- [x] **Relevance Annotation**: API endpoint for labeling query results with graded relevance (RelevanceGrade enum), automatic metrics calculation
- [x] **Relevance Annotation UI**: Frontend UI for annotating query results with relevance badges, metrics display panel (MRR, Precision@K, NDCG@K, Response Time)

### In Progress / Planned
- [ ] Solution on how to evaluate Recall@K (requires tracking total relevant documents per query, which most likely will require the gold-standard test set)
- [ ] Improve FixedSizeTextChunker
- [ ] Semantic Chunking Strategy implementation
- [ ] Configurable chunking strategies (for RAG evaluation)
- [ ] Multiple embedding model support (for RAG evaluation)
- [ ] Configurable System Prompt templates (for different use cases)
- [ ] Settings page in WebUI for runtime configuration
- [ ] Analytics and metrics Dashboard
- [ ] Unit and integration tests
- [ ] Logging and Error Handling with custom exceptions

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
