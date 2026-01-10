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
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code
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

**Note on First Startup**: The first time you run this, Ollama will automatically download required models (approximately 9 GB total: `nomic-embed-text` for embeddings and `qwen2.5:14b` for chat). This may take 10-30 minutes depending on your internet connection. Subsequent startups will be instant as models are persisted in the `ollama_data` volume.

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
├── RagEvaluator.API/          # ASP.NET Core Web API
├── RagEvaluator.Application/  # Business logic layer
├── RagEvaluator.Contract/     # DTOs and contracts
├── RagEvaluator.Domain/       # Domain entities
├── RagEvaluator.Infrastructure/ # Data access & external services
├── RagEvaluator.WebUi/        # React frontend
├── RagEvaluator.Tests/        # Unit and integration tests
├── docker-compose.yml         # Production compose config
├── docker-compose.override.yml # Development overrides
└── ARCHITECTURE.md            # Detailed architecture docs
```

## Docker Configuration

### Services

The application uses 4 Docker containers:

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| **ragevaluator-api** | Custom (.NET 10) | 5000 | REST API backend |
| **ragevaluator-web-ui** | Custom (Nginx + React) | 3000 | Frontend SPA |
| **postgres** | postgres:18 | 5432 | Database |
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
- `POST /api/documents/upload` - Upload PDF document for RAG processing

### Query
- `POST /api/query` - Ask questions using RAG (Retrieval-Augmented Generation)

### Swagger UI
- `http://localhost:5000/swagger` - Interactive API documentation and testing

**Current Implementation Status**: The core RAG functionality is fully implemented with document upload and question answering. Additional endpoints (list documents, query history, etc.) are scaffolded but not yet implemented.

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
  -F 'description=Optional description'
```

Response:
```json
{
  "documentId": "guid",
  "fileName": "your-document.pdf",
  "description": "Optional description",
  "pageCount": 10,
  "chunkCount": 25,
  "uploadedAt": "2025-01-04T12:00:00Z"
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
  "topK": 3
}'
```

Response:
```j
{
  "queryId": "guid",
  "question": "What is the main topic of the document?",
  "answer": "The document discusses...",
  "sources": [
    {
      "id": 0,
      "text": "Relevant chunk text...",
      "similarity": 0.89,
      "metadata": {
        "documentId": "guid",
        "fileName": "your-document.pdf"
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
- **AI/ML**: Microsoft Semantic Kernel 1.66.0
- **LLM Provider**: Ollama (local models)
  - Embedding Model: `nomic-embed-text`
  - Chat Model: `qwen2.5:14b`
- **PDF Processing**: iText7 9.3.0
- **Database**: PostgreSQL 18 (planned for persistence)
- **Vector Store**: In-memory (SimpleVectorStore) with cosine similarity
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

See [ARCHITECTURE.md](ARCHITECTURE.md) for complete technology stack details.

### Production

For production deployment:
1. Update environment variables in `docker-compose.yml`
2. Configure proper database credentials
3. Set up reverse proxy (nginx/Caddy) for HTTPS
4. Enable authentication and security features

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed overview of the architecture, design patterns, and technical decisions.
