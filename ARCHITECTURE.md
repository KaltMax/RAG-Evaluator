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
│                      RagEvaluator.API                        │
│                   (Controllers, Middleware)                  │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  RagEvaluator.Application                    │
│            (Business Logic & Orchestration)                  │
│  Services: RagService, DocumentService, QueryService         │
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
                                │ • SimpleVectorStore        │
                                │ • DocumentRepository       │
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
│   ├── Extensions/
│   │   └── ServiceCollectionExtension.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── RagEvaluator.Application/            # Business Logic & Orchestration
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IRagService.cs
│   │   │   ├── IDocumentService.cs
│   │   │   └── IQueryService.cs
│   │   ├── RagService.cs                # Core RAG orchestration
│   │   ├── DocumentService.cs           # Document management
│   │   └── QueryService.cs              # Query handling (placeholder)
│   ├── Validators/
│   │   └── AskQuestionValidator.cs
│   └── Extensions/
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
│   │       └── IVectorStore.cs          # Vector store interface
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
│   │   │   └── ErrorResponse.cs
│   │   └── PaginationDto.cs
│   └── Logger/
│       ├── ILoggerWrapper.cs
│       └── LoggerWrapper.cs
│
├── RagEvaluator.Domain/                 # Domain Models & Business Rules
│   ├── Entities/
│   │   ├── Document.cs                  # Document aggregate root
│   │   ├── VectorEntry.cs               # Vector storage entity
│   │   ├── Query.cs                     # User query entity (placeholder)
│   │   └── ChatHistory.cs               # Conversation history (placeholder)
│   ├── ValueObjects/
│   │   ├── DocumentMetadata.cs
│   │   ├── SearchResult.cs
│   │   └── Embedding.cs
│   └── Exceptions/
│       ├── DocumentNotFoundException.cs
│       └── VectorStoreException.cs
│
├── RagEvaluator.Infrastructure/         # Data Access & External Services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # EF Core DbContext
│   │   ├── DocumentRepository.cs        # Document repository implementation
│   │   ├── Configurations/
│   │   │   ├── DocumentConfiguration.cs
│   │   │   └── QueryConfiguration.cs    # (placeholder)
│   │   └── Migrations/
│   ├── Services/
│   │   ├── LocalFileStorageService.cs   # Local file system storage
│   │   ├── PdfLoader.cs                 # PDF text extraction (PdfPig)
│   │   ├── TextChunker.cs               # Text splitting
│   │   ├── SimpleVectorStore.cs         # In-memory vector store
│   │   ├── OllamaChatService.cs         # Ollama chat service
│   │   ├── OllamaEmbeddingService.cs    # Ollama embedding service
│   │   └── PgVectorStore.cs             # PostgreSQL vector store (placeholder)
│   ├── External/
│   │   └── OllamaClient.cs              # Ollama API client (placeholder)
│   └── Extensions/
│       └── InfrastructureExtensions.cs
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

- Entities: `Document`, `VectorEntry`, `Query`, `ChatHistory`
- Value Objects: `DocumentMetadata`, `SearchResult`, `Embedding`
- Domain exceptions: `DocumentNotFoundException`, `VectorStoreException`

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
- `PdfLoader` - PDF text extraction using PdfPig
- `TextChunker` - Text chunking with configurable size and overlap
- `SimpleVectorStore` - In-memory vector store with cosine similarity
- `OllamaEmbeddingService` - Ollama embedding generation via Semantic Kernel
- `OllamaChatService` - Ollama chat completion via Semantic Kernel
- `PgVectorStore` - PostgreSQL vector store (placeholder for future implementation)

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
      → 3. PdfLoader.LoadPdf() - Extract text from pages
      → 4. TextChunker.SplitDocuments() - Split into chunks (1000 chars, 200 overlap)
      → 5. For each chunk:
         → 6. OllamaEmbeddingService.GenerateEmbeddingAsync() - Create vector
         → 7. SimpleVectorStore.AddEntry() - Store chunk + embedding
      → 8. Store DocumentMetadata (Domain Value Object)
   → 9. Return DocumentResponse (DTO)
```

### Query Processing Pipeline

```
1. Question Submission (Controller)
   → 2. RagService.AskQuestionAsync() (Application Layer)
      → 3. OllamaEmbeddingService.GenerateEmbeddingAsync() - Embed question
      → 4. SimpleVectorStore.Search() - Find top K similar chunks (cosine similarity)
      → 5. Build context from retrieved chunks
      → 6. OllamaChatService.GenerateResponseAsync() - Generate answer with context
   → 7. Return QueryResponse with answer + sources (DTOs)
```

### Dependency Inversion in Action

The Contract layer defines **what** needs to be done (interface abstractions):
- Service interfaces: `IChatService`, `IEmbeddingService`, `IFileStorageService`, `IPdfLoader`, `ITextChunker`
- Data interfaces: `IVectorStore`, `IDocumentRepository`

The Infrastructure layer defines **how** it's done (concrete implementations):
- `OllamaChatService`, `OllamaEmbeddingService`, `LocalFileStorageService`, `PdfLoader`, `TextChunker`, `SimpleVectorStore`

The Application layer consumes these abstractions:
- `RagService` uses IChatService, IEmbeddingService, IVectorStore, etc.
- No direct dependency on Infrastructure implementations

This allows:
- Testing Application layer with mocks
- Swapping implementations (e.g., SimpleVectorStore → PgVectorStore)
- Framework independence
- Centralized interface management in the Contract layer

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
    PageCount INT,
    ChunkCount INT,
    UploadedAt TIMESTAMP NOT NULL,
    ProcessedAt TIMESTAMP,
    Status VARCHAR(50), -- Pending, Processing, Completed, Failed
);

-- Queries table (chat history)
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

-- Query Sources (many-to-many)
CREATE TABLE QuerySources (
    QueryId UUID REFERENCES Queries(Id),
    ChunkId VARCHAR(100),
    Similarity FLOAT,
    TextPreview TEXT,
    PRIMARY KEY (QueryId, ChunkId)
);
```

### Vector Store Options

**PostgreSQL with pgvector**

- SQL + vector search in one database
- ACID compliance
- Good for moderate scale

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
description: "Optional description"
```

**Upload Document Response**:

```json
{
  "documentId": "123e4567-e89b-12d3-a456-426614174000",
  "fileName": "document.pdf",
  "description": "Optional description",
  "pageCount": 15,
  "chunkCount": 38,
  "uploadedAt": "2025-01-04T12:00:00Z"
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
      "id": 0,
      "text": "...relevant text chunk...",
      "similarity": 0.892,
      "metadata": {
        "documentId": "123e4567-e89b-12d3-a456-426614174000",
        "fileName": "document.pdf"
      }
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
- **LLM Provider**: Ollama (local models)
  - **Embedding Model**: nomic-embed-text
  - **Chat Model**: qwen2.5:14b
- **PDF Processing**: PdfPig 0.1.9
- **Vector Store**:
  - In-memory (SimpleVectorStore) - Current implementation with cosine similarity
  - PostgreSQL with pgvector - Planned for persistence
- **Database**: PostgreSQL 18
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
| ragevaluator-api | Custom (.NET 9) | 5000:8080 | ASP.NET Core Web API |
| ragevaluator-web-ui | Custom (Nginx + React) | 3000:80 | React frontend |
| postgres | postgres:18 | 5432:5432 | PostgreSQL database |
| ollama | ollama/ollama:0.13.5 | 11434:11434 | Local LLM service |

### Ollama Initialization

The Ollama container uses a custom initialization script (`ollama-init.sh`) that:

1. Starts the Ollama service in the background
2. Waits for the service to be ready
3. Checks for required models and pulls them if missing:
   - `nomic-embed-text` - Text embedding model (approximately 274 MB)
   - `qwen2.5:14b` - Chat completion model (approximately 1.3 GB)
4. Models are persisted in the `ollama_data` Docker volume

**First Startup**: Initial container startup takes 5-10 minutes to download models (approximately 1.5 GB total).

**Subsequent Startups**: Nearly instant as models are cached in the persistent volume.

### Environment Configuration

The API container is configured via environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=ragevaluator;...
  - RagConfiguration__OllamaEndpoint=http://ollama:11434/v1
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
- [x] In-memory vector store with cosine similarity
- [x] Ollama integration via Microsoft Semantic Kernel
- [x] Automatic model downloading on first startup
- [x] Swagger UI for API testing
- [x] Docker Compose orchestration
- [x] Dependency Inversion with interface-based services
- [x] Domain Value Objects (DocumentMetadata, SearchResult, etc.)

### In Progress / Planned

- [x] Database persistence (EF Core + PostgreSQL)
  - [x] Document metadata storage
  - [ ] Query history tracking
- [x] Repository pattern implementations (DocumentRepository)
- [x] Document API endpoints (list, get, delete, download)
- [ ] Query history API endpoints
- [ ] React frontend UI components
- [ ] PostgreSQL vector store (pgvector)
- [ ] Unit and integration tests
- [ ] Multi-document querying
- [ ] Real-time streaming responses (SignalR)
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
