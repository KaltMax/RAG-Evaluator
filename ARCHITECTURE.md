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

The application follows **Clean Architecture** (Onion Architecture) principles with clear separation of concerns:

- **Dependency Rule**: Dependencies point inward (Infrastructure → Application → Domain)
- **Domain-Centric**: Business logic independent of frameworks and external concerns
- **Testability**: Each layer can be tested independently
- **Maintainability**: Changes in one layer have minimal impact on others

## Project Structure

```
RAG-Evaluator/
├── RagEvaluator.API/                    # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── DocumentsController.cs       # Upload PDFs, manage docs
│   │   └── QueryController.cs           # Ask questions, RAG queries
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
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
│   │   └── QueryService.cs              # Query handling
│   ├── Validators/
│   │   └── AskQuestionValidator.cs
│   └── Extensions/
│
├── RagEvaluator.Contract/               # DTOs & Shared Interfaces
│   ├── Requests/
│   │   ├── AskQuestionRequest.cs
│   │   ├── UploadDocumentRequest.cs
│   │   └── UpdateConfigurationRequest.cs
│   ├── Responses/
│   │   ├── QueryResponse.cs
│   │   ├── SearchResultDto.cs
│   │   ├── DocumentResponse.cs
│   │   └── ErrorResponse.cs
│   └── Models/
│       ├── RagConfiguration.cs          # Configuration model
│       └── PaginationDto.cs
│
├── RagEvaluator.Domain/                 # Domain Models & Business Rules
│   ├── Entities/
│   │   ├── Document.cs                  # Document aggregate root
│   │   ├── VectorEntry.cs               # Vector storage entity
│   │   ├── Query.cs                     # User query entity
│   │   └── ChatHistory.cs               # Conversation history
│   ├── ValueObjects/
│   │   ├── DocumentMetadata.cs
│   │   ├── SearchResult.cs
│   │   └── Embedding.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IDocumentRepository.cs
│   │   └── IVectorStore.cs
│   └── Exceptions/
│       ├── DocumentNotFoundException.cs
│       └── VectorStoreException.cs
│
├── RagEvaluator.Infrastructure/         # Data Access & External Services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # EF Core DbContext
│   │   ├── Configurations/
│   │   │   ├── DocumentConfiguration.cs
│   │   │   └── QueryConfiguration.cs
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── DocumentRepository.cs
│   │   └── QueryRepository.cs
│   ├── Services/
│   │   ├── PdfLoader.cs                 # PDF text extraction
│   │   ├── TextChunker.cs               # Text splitting
│   │   ├── SimpleVectorStore.cs         # In-memory vector store
│   │   └── PgVectorStore.cs             # PostgreSQL vector store (future)
│   ├── External/
│   │   └── OllamaClient.cs              # Ollama API client
│   └── Extensions/
│       └── InfrastructureExtensions.cs
│
├── RagEvaluator.WebUi/                  # React Frontend (Vite)
│   ├── public/
│   ├── src/
│   │   ├── components/
│   │   │   ├── DocumentUpload/
│   │   │   ├── QueryInterface/
│   │   │   ├── ChatHistory/
│   │   │   └── SourceViewer/
│   │   ├── pages/
│   │   │   ├── Home.jsx
│   │   │   ├── Documents.jsx
│   │   │   └── Settings.jsx
│   │   ├── services/
│   │   │   └── api.js                   # API client
│   │   ├── hooks/
│   │   ├── utils/
│   │   ├── App.jsx
│   │   ├── main.jsx
│   │   └── index.css
│   ├── package.json
│   ├── vite.config.js
│   ├── nginx.conf
│   └── Dockerfile
│
└── tests/
    ├── RagEvaluator.UnitTests/
    │   ├── Application/
    │   ├── Domain/
    │   └── Infrastructure/
    ├── RagEvaluator.IntegrationTests/
    │   ├── API/
    │   └── Infrastructure/
    └── RagEvaluator.E2ETests/
        └── Scenarios/
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

**Dependencies**: → Domain, Contract

### 3. Contract Layer (`RagEvaluator.Contract`)

**Purpose**: Data transfer objects and shared contracts

**Responsibilities**:

- API request/response models
- DTOs for data transfer between layers
- Shared interfaces (if needed)
- Validation attributes

**Key Components**:

- Request DTOs
- Response DTOs
- API models
- Enums and constants

**Dependencies**: None (shared by all layers)

### 4. Domain Layer (`RagEvaluator.Domain`)

**Purpose**: Core business entities and rules (framework-agnostic)

**Responsibilities**:

- Domain entities and aggregates
- Value objects
- Domain interfaces (repository contracts)
- Domain exceptions
- Business invariants and rules

**Key Components**:

- Entities: `Document`, `VectorEntry`, `Query`, `ChatHistory`
- Value Objects: `DocumentMetadata`, `SearchResult`, `Embedding`
- Repository interfaces
- Domain exceptions

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
- Repository implementations
- PDF processing services
- Vector store implementations
- External API clients

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
    UserId UUID -- Future: multi-tenant support
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

**Option 1: In-Memory (Current)**

- Fast, simple
- No persistence
- Good for development/demos

**Option 2: PostgreSQL with pgvector**

- SQL + vector search in one database
- ACID compliance
- Good for moderate scale

**Option 3: Dedicated Vector Database**

- Qdrant, Milvus, or Weaviate
- Optimized for vector operations
- Scalable for production

## API Design

### RESTful Endpoints

#### Documents API

```
POST   /api/documents              # Upload document
GET    /api/documents              # List all documents
GET    /api/documents/{id}         # Get document details
DELETE /api/documents/{id}         # Delete document
GET    /api/documents/{id}/chunks  # Get document chunks
```

#### Query API

```
POST   /api/query                  # Ask question
GET    /api/query/history          # Get query history
GET    /api/query/{id}             # Get specific query
```

### Request/Response Examples

**Ask Question Request**:

```json
{
  "documentId": "123e4567-e89b-12d3-a456-426614174000",
  "question": "What is the main conclusion?",
  "topK": 3,
  "includeMetadata": true
}
```

**Ask Question Response**:

```json
{
  "answer": "The main conclusion is...",
  "sources": [
    {
      "chunkId": "chunk_0",
      "text": "...relevant text...",
      "similarity": 0.892,
      "metadata": {
        "page": 5,
        "chapter": "Conclusion"
      }
    }
  ],
  "responseTimeMs": 1234,
  "documentId": "123e4567-e89b-12d3-a456-426614174000"
}
```

## Technology Stack

### Backend

- **Framework**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0
- **Database**: PostgreSQL 16
- **Vector Search**: pgvector (or in-memory)
- **AI/ML**: Microsoft Semantic Kernel
- **LLM**: Ollama (local models)
- **PDF Processing**: iText7
- **Testing**: xUnit, FluentAssertions, Moq

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

## Future Enhancements

- [ ] Multi-document querying
- [ ] Document versioning
- [ ] Collaborative features (sharing, annotations)
- [ ] Advanced search filters
- [ ] Conversation memory/context
- [ ] Support for more document formats (DOCX, TXT, MD)
- [ ] Real-time streaming responses (SignalR)
- [ ] Multi-language support (i18n)
- [ ] Admin dashboard
- [ ] Usage analytics

## Resources

- [Clean Architecture - Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [React Documentation](https://react.dev/)
- [Docker Documentation](https://docs.docker.com/)
