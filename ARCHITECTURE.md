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
                                │ • PdfLoader                │
                                │ • TextChunker              │
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
│   │   └── DocumentMapper.cs            # Document → DTO mapping
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IRagService.cs
│   │   │   ├── IDocumentService.cs
│   │   │   ├── IQueryService.cs
│   │   │   └── IMetricsService.cs       # Similarity & evaluation metrics
│   │   ├── RagService.cs                # Core RAG orchestration
│   │   ├── DocumentService.cs           # Document management
│   │   ├── QueryService.cs              # Query handling (placeholder)
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
│   │       └── IDocumentChunkRepository.cs # Vector chunk repository interface
│   ├── Configurations/
│   │   ├── FileStorageConfiguration.cs  # File storage settings
│   │   └── RagConfiguration.cs
│   ├── Dtos/
│   │   ├── Requests/
│   │   │   ├── AskQuestionRequest.cs
│   │   │   └── UploadDocumentRequest.cs
│   │   ├── Responses/
│   │   │   ├── QueryResponse.cs
│   │   │   ├── SearchResultDto.cs
│   │   │   ├── DocumentResponse.cs
│   │   │   ├── DocumentFileInfo.cs      # File info for downloads
│   │   │   └── ErrorResponse.cs
│   │   └── PaginationDto.cs
│   └── Logger/
│       ├── ILoggerWrapper.cs
│       └── LoggerWrapper.cs
│
├── RagEvaluator.Domain/                 # Domain Models & Business Rules
│   ├── Entities/
│   │   ├── Document.cs                  # Document aggregate root
│   │   ├── DocumentSummary.cs           # Lightweight document (for list views)
│   │   ├── DocumentChunk.cs             # Text chunk with vector embedding
│   │   └── Query.cs                     # User query entity (placeholder)
│   ├── ValueObjects/
│   │   ├── SearchResult.cs              # Search result with similarity score
│   │   └── ChunkSearchMatch.cs          # Raw chunk match (before similarity calculation)
│   └── Exceptions/
│       ├── DocumentNotFoundException.cs
│       └── VectorStoreException.cs
│
├── RagEvaluator.Infrastructure/         # Data Access & External Services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # EF Core DbContext with pgvector
│   │   ├── DocumentRepository.cs        # Document repository implementation
│   │   ├── Repositories/
│   │   │   └── DocumentChunkRepository.cs # Vector chunk repository with pgvector
│   │   ├── Configurations/
│   │   │   ├── DocumentConfiguration.cs
│   │   │   └── DocumentChunkConfiguration.cs # pgvector mapping
│   │   └── Migrations/
│   └── Services/
│       ├── LocalFileStorageService.cs   # Local file system storage
│       ├── PdfLoader.cs                 # PDF text extraction (PdfPig)
│       ├── TextChunker.cs               # Text splitting
│       ├── OllamaChatService.cs         # Ollama chat service
│       └── OllamaEmbeddingService.cs    # Ollama embedding service
│
├── RagEvaluator.WebUi/                  # React Frontend (Vite)
│   ├── src/
│   │   ├── components/
│   │   ├── assets/
│   │   ├── api/
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
- Caching strategies

**Key Components**:

- Service interfaces and implementations
- Command/Query handlers (optional CQRS)
- Business validators
- Application-specific DTOs mapping

**Implemented Services**:

- `IRagService` - Core RAG orchestration (business logic)
  - `ProcessDocumentAsync()` - Orchestrates document upload workflow
  - `AskQuestionAsync()` - Orchestrates RAG query workflow
  - `IsInitializedAsync()` - Checks service availability
  - `GetDocumentCountAsync()` - Returns document count
- `IDocumentService` - Document management operations
- `IQueryService` - Query handling and history management
- `IMetricsService` - Similarity and retrieval evaluation metrics
  - `CosineSimilarity()` / `CosineDistance()` - Vector similarity calculations
  - `MeanReciprocalRank()` - MRR for retrieval evaluation
  - `PrecisionAtK()` / `RecallAtK()` - Precision and recall metrics
  - `NormalizedDiscountedCumulativeGainAtK()` - NDCG for ranking quality

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

- Entities: `Document`, `DocumentSummary`, `DocumentChunk`, `Query`
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

**Dependencies**: None (pure domain logic)

### 5. Infrastructure Layer (`RagEvaluator.Infrastructure`)

**Purpose**: External concerns and implementation details

**Responsibilities**:

- Data persistence (Entity Framework Core)
- External service integration (Ollama)
- File system operations (PDF loading)
- Caching implementation
- Vector store implementation
- Email/notification services

**Key Components**:

- EF Core DbContext and configurations
- Repository implementations (DocumentRepository)
- PDF processing services
- Vector store implementations
- External API clients

**Implemented Services**:

- `LocalFileStorageService` - Local file system storage with configurable directory
- `PdfLoader` - PDF text extraction using PdfPig with ContentOrderTextExtractor for proper reading order
- `TextChunker` - Text chunking with configurable size and overlap
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
      → 3. PdfLoader.LoadPdf() - Extract text using ContentOrderTextExtractor
      → 4. TextChunker.SplitDocuments() - Split into chunks (1000 chars, 200 overlap)
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
      → 3. OllamaEmbeddingService.GenerateEmbeddingAsync("search_query: " + question)
      → 4. DocumentChunkRepository.SearchAsync() - Find top K similar chunks
         (uses pgvector cosine distance for ordering, returns ChunkSearchMatch)
      → 5. MetricsService.CosineSimilarity() - Calculate similarity scores
      → 6. Build context from retrieved chunks
      → 7. OllamaChatService.GenerateResponseAsync() - Generate answer with context
   → 8. Return QueryResponse with answer + sources (includes similarity, fileName, chunkingStrategy, embeddingModel)
```

### Dependency Inversion in Action

The Contract layer defines **what** needs to be done (interface abstractions):
- Service interfaces: `IChatService`, `IEmbeddingService`, `IFileStorageService`, `IPdfLoader`, `ITextChunker`
- Data interfaces: `IDocumentRepository`, `IDocumentChunkRepository`

The Infrastructure layer defines **how** it's done (concrete implementations):
- `OllamaChatService`, `OllamaEmbeddingService`, `LocalFileStorageService`, `PdfLoader`, `TextChunker`
- `DocumentRepository`, `DocumentChunkRepository` (with pgvector integration)

The Application layer consumes these abstractions:
- `RagService` uses IChatService, IEmbeddingService, IDocumentChunkRepository, IMetricsService, etc.
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

-- Queries table (chat history - placeholder)
CREATE TABLE Queries (
    Id UUID PRIMARY KEY,
    DocumentId UUID REFERENCES Documents(Id),
    Question TEXT NOT NULL,
    Answer TEXT,
    SourceCount INT,
    CreatedAt TIMESTAMP NOT NULL,
    ResponseTimeMs INT,
    UserId UUID
);
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
POST   /api/documents/upload       # Upload PDF document (IMPLEMENTED)
GET    /api/documents              # List all documents (IMPLEMENTED)
GET    /api/documents/{id}         # Get document details (IMPLEMENTED)
GET    /api/documents/{id}/download # Download document file (IMPLEMENTED)
DELETE /api/documents/{id}         # Delete document (IMPLEMENTED)
GET    /api/documents/{id}/chunks  # Get document chunks (scaffolded)
```

#### Query API

```
POST   /api/query                  # Ask question using RAG (IMPLEMENTED)
GET    /api/query/history          # Get query history (scaffolded)
GET    /api/query/{id}             # Get specific query (scaffolded)
```

**Implementation Status**: Core RAG functionality (upload and query) is fully implemented. Document CRUD endpoints (list, get, delete, download) are fully implemented. Query history endpoints are scaffolded.

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
  "topK": 3
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
- **AI/ML Framework**: Microsoft Semantic Kernel 1.66.0
- **LLM Provider**: Ollama (local models, configurable via `.env`)
  - **Embedding Model**: nomic-embed-text-v2-moe (multilingual MoE, uses asymmetric prefixes)
  - **Chat Model**: qwen2.5:14b
- **PDF Processing**: PdfPig 0.1.9
- **Vector Store**: PostgreSQL with pgvector extension
  - Persistent storage with cosine similarity search
  - EF Core integration via Pgvector.EntityFrameworkCore
- **Database**: PostgreSQL 18 (pgvector/pgvector:0.8.1-pg18)
- **ORM**: Entity Framework Core 10.0 with Npgsql
- **API Documentation**: Swagger/OpenAPI (Swashbuckle.AspNetCore 9.0.6)
- **Testing**: xUnit, FluentAssertions, NSubstitute (planned)

### Frontend

- **Framework**: React 18+
- **Language**: JavaScript
- **Build Tool**: Vite
- **State Management**: TanStack Query + Context
- **Routing**: React Router v6
- **UI Library**: Tailwind CSS / Material-UI
- **HTTP Client**: Axios
- **Form Handling**: React Hook Form + Zod

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
  - FileStorageConfiguration__BaseDirectory=/app/uploads
```

**Note**: Configuration uses double underscores (`__`) to override nested JSON configuration in ASP.NET Core.

### Docker Networking

Containers communicate via Docker's internal network:
- API connects to Ollama at `http://ollama:11434`
- API connects to PostgreSQL at `postgres:5432`
- External access via port mappings (5000, 3000, etc.)

## Security Considerations

### Authentication & Authorization

- ASP.NET Core Identity for user management
- JWT tokens for API authentication
- Role-based access control (RBAC)
- Rate limiting for API endpoints

### Data Protection

- Encrypt sensitive data at rest
- HTTPS/TLS for data in transit
- Secure file upload validation
- Input sanitization for LLM prompts

### API Security

- CORS configuration
- API key authentication (optional)
- Request size limits
- Anti-forgery tokens

## Scalability Considerations

### Horizontal Scaling

- Stateless API design
- External session storage (Redis)
- Load balancer configuration
- Distributed caching

### Performance Optimization

- Background job processing (Hangfire/Quartz)
- Response caching
- Database indexing
- Connection pooling
- CDN for static assets

### Monitoring & Observability

- Application Performance Monitoring (APM)
- Distributed tracing (OpenTelemetry)
- Structured logging (Serilog)
- Health checks
- Metrics collection (Prometheus)

## Current Implementation Status

### Completed

- [x] Clean Architecture project structure
- [x] Core RAG pipeline (upload PDF, ask questions)
- [x] File storage service abstraction and local implementation
- [x] Document download endpoint
- [x] PDF text extraction with PdfPig
- [x] Text chunking with configurable size/overlap
- [x] PostgreSQL vector store with pgvector (cosine similarity search)
- [x] Ollama integration via Microsoft Semantic Kernel
- [x] Automatic model downloading on first startup
- [x] Swagger UI for API testing
- [x] Docker Compose orchestration
- [x] Dependency Inversion with interface-based services
- [x] Domain Value Objects (SearchResult with fileName, chunkingStrategy, embeddingModel)
- [x] Document content extraction and storage
- [x] Document language selection (en/de) with validation
- [x] DTO mapping pattern (Application layer)
- [x] DocumentSummary projection for optimized list queries
- [x] Database persistence (EF Core + PostgreSQL)
  - [x] Document metadata storage
  - [x] Document content storage
  - [x] DocumentChunk persistence with pgvector
- [x] Repository pattern implementations (DocumentRepository, DocumentChunkRepository)
- [x] MetricsService for similarity and evaluation metrics (CosineSimilarity, MRR, Precision@K, Recall@K, NDCG@K)
- [x] Document API endpoints (list, get, delete, download)
- [x] React frontend UI components
  - [x] Multi-file upload (up to 20 files)
  - [x] Per-file language selection
  - [x] Document list with language column
  - [x] Search results with source details (chunking strategy, embedding model)
- [x] Multi-document querying (similarity search across all documents)

### In Progress / Planned

- [ ] Query history tracking and API endpoints
- [ ] Metrics calculation integration in query workflow
- [ ] Metrics persistence in database for each query
- [ ] Unit and integration tests
- [ ] Analytics and metrics Dashboard
- [ ] Configurable chunking strategies (for RAG evaluation)
- [ ] Multiple embedding model support (for RAG evaluation)

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
