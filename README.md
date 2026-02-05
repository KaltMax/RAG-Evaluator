# RAG-Evaluator

A full-stack application using C#/.NET and React to evaluate RAG-based search with local LLMs.

## Documentation

- **[Architecture Documentation](ARCHITECTURE.md)** - Detailed architecture, design patterns, and technical decisions
- **This README** - Quick start guide and setup instructions

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) with WSL2 backend (Windows)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (recommended) or VS Code
- **NVIDIA GPU** with CUDA support (12GB+ VRAM recommended for Qwen2.5-14b)
  - RTX 3060 (12GB) or better
  - NVIDIA drivers installed
  - [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html) for Docker GPU access

### Running with Docker Compose (Recommended)

The fastest way to get started is using Docker Compose, which sets up all services automatically:

#### Option 1: Visual Studio

1. Open `RagEvaluator.sln` in Visual Studio
2. Select **docker-compose** from the startup dropdown
3. Press **F5** to run

#### Option 2: Command Line

```bash
docker-compose up
```

This will start:
- **API** on `http://localhost:5000`
- **Web UI** on `http://localhost:3000`
- **PostgreSQL** on `localhost:5432`
- **Ollama** on `localhost:11434`

**Note on First Startup**: The first time you run this, Ollama will automatically download required models (approximately 10 GB total: `nomic-embed-text-v2-moe` for embeddings and `qwen2.5:14b` for chat). This may take 10-30 minutes depending on your internet connection. Subsequent startups will be instant as models are persisted in the `ollama_data` volume. Models, system prompt, and chunking strategy are configurable via `.env`.

**GPU Verification**: After startup, verify GPU access with:
```bash
docker exec ragevaluator-ollama nvidia-smi
```
You should see your NVIDIA GPU listed. If not, ensure Docker Desktop is using WSL2 backend and NVIDIA Container Toolkit is installed.

### Troubleshooting

**Windows Line Ending Issues**: If you see `\r': command not found` errors from `ollama-init.sh`, the `.gitattributes` file enforces Unix (LF) line endings. If you cloned before this was added:
```bash
dos2unix ollama-init.sh
git add ollama-init.sh
```

**GPU Not Detected**: Ensure Docker Desktop is configured for WSL2 (Settings → General → "Use WSL 2 based engine"). Install NVIDIA drivers in Windows and verify with `nvidia-smi` in PowerShell.

## Project Structure

```
RAG-Evaluator/
├── RagEvaluator.API/               # ASP.NET Core Web API
├── RagEvaluator.Application/       # Business logic & metrics (MetricsService)
├── RagEvaluator.Contract/          # DTOs and contracts
├── RagEvaluator.Domain/            # Domain entities
├── RagEvaluator.Infrastructure/    # Data access & external services
├── RagEvaluator.WebUi/             # React frontend
├── RagEvaluator.Tests/             # Unit and integration tests
├── docker-compose.yml              # Production compose config
├── docker-compose.override.yml     # Development overrides
└── ARCHITECTURE.md                 # Detailed architecture docs
```

## Docker Configuration

### Services

The application uses 4 Docker containers:

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| **ragevaluator-api** | Custom (.NET 10) | 5000 | REST API backend |
| **ragevaluator-web-ui** | Custom (Nginx + React) | 3000 | Frontend SPA |
| **postgres** | pgvector/pgvector:0.8.1-pg18 | 5432 | Database with vector support |
| **ollama** | ollama/ollama:0.13.5 | 11434 | Local LLM service |

## Development

### Project Dependencies (Clean Architecture)

The project follows Clean Architecture dependency rules:

```
RagEvaluator.API
  → Application
  → Infrastructure
  → Contract

RagEvaluator.Application
  → Domain
  → Contract

RagEvaluator.Infrastructure
  → Domain
  → Application

RagEvaluator.Domain
  → (No dependencies)

RagEvaluator.Contract
  → (No dependencies)
```

### Running Tests

```bash
dotnet test
```

## API Endpoints

Once running, the API is available at `http://localhost:5000`:

### Documents
- `POST /api/documents/upload` - Upload PDF document with language for RAG processing
- `GET /api/documents` - List all documents
- `GET /api/documents/{id}` - Get document details
- `GET /api/documents/{id}/chunks` - Get document chunks with embeddings
- `GET /api/documents/{id}/download` - Download a previously uploaded document
- `DELETE /api/documents/{id}` - Delete a document

### Query
- `POST /api/query` - Ask questions using RAG (Retrieval-Augmented Generation)
- `GET /api/query/history` - Get query history
- `GET /api/query/{id}` - Get specific query details
- `PATCH /api/query/{id}/results` - Annotate query results with relevance labels, response quality, ground truth documents, and calculate metrics

### Health
- `GET /api/health` - Check if RAG services (Ollama) are ready

### Swagger UI
- `http://localhost:5000/swagger` - Interactive API documentation and testing

**Current Implementation Status**: The core RAG functionality is fully implemented with document upload (including language selection and content extraction) and question answering with language selection. Document chunks are persisted in PostgreSQL using pgvector for efficient similarity search across multiple documents. Document management endpoints (list, get, delete, download) are fully implemented. Query history including LLM responses, retrieved chunks, and query embeddings is persisted via the `Query` and `QueryResult` entities for reproducible evaluation. The `QueryResult` entity uses denormalized chunk data to preserve historical accuracy even when documents are re-chunked or deleted. Relevance annotation is supported via `PATCH /api/query/{id}/results` endpoint with a graded scale (`RelevanceGrade` enum: NotRelevant, MarginallyRelevant, FairlyRelevant, HighlyRelevant). Response quality evaluation is also supported with a `ResponseQuality` enum (CorrectAndComplete, VagueOrIncomplete, Incorrect, Hallucinated) and a language switching detection flag. Ground truth document selection enables proper Recall@K calculation by allowing users to specify which documents should ideally contain relevant information for a query. The frontend includes an annotation UI with relevance badges for each retrieved chunk, response quality evaluation buttons, ground truth document selector, and displays retrieval metrics (MRR, Precision@K, Recall@K, NDCG@K, Response Time) after annotation submission. The frontend supports multi-file upload (up to 20 files) with per-file language selection. A dedicated `MetricsService` in the Application layer handles similarity calculations (cosine similarity) and provides retrieval evaluation metrics. System prompts are configurable via `.env` file.

## Using the API

### 1. Upload a PDF Document

Navigate to Swagger UI at `http://localhost:5000/swagger` and use the `POST /api/documents/upload` endpoint:

```bash
# Example using curl
curl -X 'POST' \
  'http://localhost:5000/api/documents/upload' \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -F 'file=@your-document.pdf' \
  -F 'language=en'
```

Response:
```json
{
  "id": "guid",
  "fileName": "your-document.pdf",
  "language": "en",
  "pageCount": 10,
  "chunkCount": 25,
  "uploadedAt": "2025-01-04T12:00:00Z",
  "status": "Completed"
}
```

### 2. Ask Questions

Use the `POST /api/query` endpoint:

```bash
curl -X 'POST' \
  'http://localhost:5000/api/query' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "question": "What is the main topic of the document?",
  "topK": 3,
  "language": "en"
}'
```

Response:
```json
{
  "queryId": "guid",
  "question": "What is the main topic of the document?",
  "answer": "The document discusses...",
  "sources": [
    {
      "id": "guid",
      "text": "Relevant chunk text...",
      "similarity": 0.89,
      "documentId": "guid",
      "fileName": "your-document.pdf",
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
- **AI/ML**: Microsoft Semantic Kernel 1.66.0
- **LLM Provider**: Ollama (local models, configurable via `.env`)
  - Embedding Model: `nomic-embed-text-v2-moe` (multilingual, MoE architecture)
  - Chat Model: `qwen2.5:14b`
- **PDF Processing**: PdfPig 0.1.9
- **Database**: PostgreSQL 18 with Entity Framework Core 10.0
- **Vector Store**: PostgreSQL with pgvector extension (cosine similarity search)
- **Evaluation Metrics**: MetricsService (Cosine Similarity, MRR, Precision@K, Recall@K, NDCG@K)
- **API Documentation**: Swagger/OpenAPI (Swashbuckle.AspNetCore)

### Frontend
- **Framework**: React 19
- **Build Tool**: Vite 7
- **Styling**: Tailwind CSS 4
- **Routing**: React Router DOM 7
- **HTTP Client**: Axios
- **UI Libraries**: React Dropzone, React Toastify, Heroicons
- **Testing**: Vitest with coverage

For detailed frontend documentation, see [RagEvaluator.WebUi/README.md](RagEvaluator.WebUi/README.md)

### DevOps
- Docker & Docker Compose
- GitHub Actions

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed overview of the architecture, design patterns, and technical decisions.
