# RAG-Evaluator

A full-stack application using C#/.NET and React to evaluate RAG-based search with local LLMs.

## Documentation

- **[Architecture Documentation](ARCHITECTURE.md)** - Detailed architecture, design patterns, and technical decisions
- **This README** - Quick start guide and setup instructions

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code

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
| **ragevaluator-api** | Custom (.NET 9) | 5000 | REST API backend |
| **ragevaluator-web-ui** | Custom (Nginx + React) | 3000 | Frontend SPA |
| **postgres** | postgres:16 | 5432 | Database |
| **ollama** | ollama/ollama:latest | 11434 | Local LLM service |

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
- `POST /api/documents` - Upload document
- `GET /api/documents` - List all documents
- `GET /api/documents/{id}` - Get document details
- `DELETE /api/documents/{id}` - Delete document

### Query
- `POST /api/query` - Ask question about documents
- `GET /api/query/history` - Get query history

### Swagger UI
- `http://localhost:5000/swagger` - Interactive API documentation

## Technology Stack

### Backend
- ASP.NET Core 9.0
- Entity Framework Core 9.0
- PostgreSQL 16
- Ollama (Local LLM)

### Frontend
- React 18+
- Vite
- JavaScript

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

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed deployment strategies.
