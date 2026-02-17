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

**Note on First Startup**: The first time you run this, Ollama will automatically download required models (approximately 10 GB total: `nomic-embed-text-v2-moe` for embeddings and `qwen2.5:14b` for chat). This may take 10-30 minutes depending on your internet connection. Subsequent startups will be instant as models are persisted in the `ollama_data` volume. Embedding models, chunking strategy, prompt template, and other RAG parameters are configurable via `.env` and can be changed at runtime via the Settings API.

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
├── RagEvaluator.Application/       # Business logic, metrics & background processing
├── RagEvaluator.Contract/          # DTOs and contracts
├── RagEvaluator.Domain/            # Domain entities
├── RagEvaluator.Infrastructure/    # Data access & external services
├── RagEvaluator.WebUi/             # React frontend
├── RagEvaluator.Test/              # Unit tests (xUnit, NSubstitute)
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
  → Contract

RagEvaluator.Domain
  → (No dependencies)

RagEvaluator.Contract
  → Domain
```

### Running Tests

```bash
dotnet test
```

## API Endpoints

Once running, the API is available at `http://localhost:5000`:

### Documents
- `POST /api/documents/upload` - Upload PDF document with language and course for RAG processing
- `GET /api/documents` - List all documents
- `GET /api/documents/{id}` - Get document details
- `GET /api/documents/{id}/chunks` - Get document chunks with embeddings
- `GET /api/documents/{id}/download` - Download a previously uploaded document
- `DELETE /api/documents/{id}` - Delete a document
- `POST /api/documents/reprocess` - Reprocess all documents with current chunking and embedding configuration

### Query
- `POST /api/query` - Ask questions using RAG (Retrieval-Augmented Generation)
- `GET /api/query/history` - Get query history
- `GET /api/query/{id}` - Get specific query details
- `PATCH /api/query/{id}/results` - Annotate query results with relevance labels, response quality, ground truth documents, and calculate metrics

### Experiments
- `POST /api/experiments` - Create and start a new experiment (batch of queries × repeat count, runs in background)
- `GET /api/experiments` - List all experiments with progress and config summary
- `GET /api/experiments/{id}` - Get experiment details with query groups and aggregated metrics
- `DELETE /api/experiments/{id}` - Delete an experiment

### Settings
- `GET /api/settings` - Get current runtime RAG configuration and available options
- `PATCH /api/settings` - Update runtime RAG configuration (embedding model, chunking strategy, prompt template, chunk size/overlap, similarity threshold)

### Health
- `GET /api/health` - Check if RAG services (Ollama) are ready

### Swagger UI
- `http://localhost:5000/swagger` - Interactive API documentation and testing

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
  -F 'language=en' \
  -F 'course=Software Engineering I'
```

Response:
```json
{
  "id": "guid",
  "fileName": "your-document.pdf",
  "language": "en",
  "course": "Software Engineering I",
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
      "chunkingStrategy": "FixedSize | Semantic",
      "embeddingModel": "nomic-embed-text-v2-moe"
    }
  ],
  "timestamp": "2025-01-04T12:05:00Z",
  "experimentId": null,
  "experimentName": null
}
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 10.0
- **Architecture**: Clean Architecture (Onion Architecture)
- **AI/ML**: Microsoft Semantic Kernel 1.66.0
- **LLM Provider**: Ollama (local models, configurable via `.env` and runtime Settings API)
  - Embedding Models: `nomic-embed-text-v2-moe` (default), `nomic-embed-text` (configurable, hot-swappable at runtime)
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
