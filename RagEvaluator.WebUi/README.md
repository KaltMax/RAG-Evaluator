# RAG-Evaluator WebUI

Modern web interface for the RAG-Evaluator system built with React, Vite, and Tailwind CSS.

## Features

### Implemented

- **Document Search** - Query uploaded documents using AI-powered RAG (Retrieval-Augmented Generation)
  - Ask questions in natural language
  - Get AI-generated answers with source citations
  - View similarity scores for retrieved chunks
  - Adjustable top-K results
  - Language selection (English/German)

- **Relevance Annotation** - Evaluate retrieval quality with graded relevance labels
  - Annotate each retrieved chunk with relevance grades (Not Relevant, Marginally Relevant, Fairly Relevant, Highly Relevant)
  - Color-coded relevance badges for quick selection
  - Automatic metrics calculation after annotation submission
  - Metrics display panel showing MRR, Precision@K, Recall@k, NDCG@K, and Response Time

- **Response Quality Evaluation** - Evaluate LLM response quality
  - Rate response quality (Correct & Complete, Vague/Incomplete, Incorrect, Hallucinated)
  - Track language switching issues in responses
  - Color-coded quality buttons for quick selection

- **Query History** - Review and analyze past queries
  - Sortable by Question, Language, Created At, Experiment, and Status (ascending/descending)
  - Collapsible cards with query question and ID
  - Experiment association display (experiment name or "None" for standalone queries)
  - Full answer display with system prompt
  - Query parameters (Top-K, Language, Chat Model, Embedding Model, Chunking Strategy)
  - Evaluation metrics display (MRR, Precision@K, Recall@K, NDCG@K, Response Time, Response Quality, Language Switching)
  - Status badges (Evaluated/Pending)
  - Inline annotation for pending queries via "Annotate" button (renders SearchResults evaluation UI)

- **Document Upload** - Upload PDF documents for processing
  - Drag-and-drop interface powered by react-dropzone
  - Multi-file upload support (up to 20 files at once)
  - Per-file language selection (English/German)
  - PDF-only validation
  - Real-time upload progress
  - View processed document metadata (pages, chunks, language, etc.)

- **Document List**
  - View all uploaded documents in a sortable table
  - Sortable columns: file name, size, pages, chunks, language, status, upload date (ascending/descending)
  - See metadata: file name, size, pages, chunks, language, status, upload date
  - Download documents
  - Delete documents

- **Responsive Navigation**
  - Collapsible sidebar (icon-only on mobile, full width on desktop)
  - Active route highlighting
  - Clean, modern dark theme

- **Experiments** - Run batch experiments to evaluate RAG performance
  - Upload a JSON file defining experiment name, repeat count, and queries
  - Client-side validation of experiment structure
  - Preview experiment details and query list before submission
  - Submit to create experiment via API

- **Settings** - Configure RAG system parameters at runtime
  - Embedding model selection from available models
  - Chunking strategy selection (FixedSize with chunk size/overlap, Semantic with similarity threshold)
  - Prompt template selection (Basic, Instructed, LanguageAware) with prompt text preview
  - Draft/save/discard workflow with dirty state detection
  - Partial update API (only changed fields sent)
  - Automatic document reprocessing when embedding/chunking settings change

- **Statistics** - Compare experiment results across RAG configurations
  - Select experiments for side-by-side comparison (up to 12)
  - Overall comparison table with best-value highlighting per metric
  - Retrieval metrics bar chart with standard deviation error bars (MRR, Precision@K, Recall@K, NDCG@K)
  - Response quality distribution chart (stacked horizontal bars)
  - Language comparison: side-by-side retrieval metrics for English vs German queries
  - Per-query breakdown: expandable accordion with per-experiment metric comparison

## Tech Stack

- **React 19** - UI framework
- **Vite 7** - Build tool with HMR
- **Tailwind CSS 4** - Utility-first styling
- **React Router DOM 7** - Client-side routing
- **Axios** - HTTP client for API calls
- **React Dropzone** - Drag-and-drop file uploads
- **React Toastify** - Toast notifications
- **Recharts** - Charting library (bar charts, error bars, stacked bars)
- **PropTypes** - Runtime type checking for React props
- **Heroicons** - Icon library
- **Vitest** - Unit testing with coverage

## Getting Started

### Prerequisites

- Node.js 20+ and npm
- Backend API running on `http://localhost:5000` (or configure `VITE_API_BASE_URL`)

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

The app will be available at `http://localhost:5173` with hot module replacement enabled.

### Production Build

```bash
npm run build
```

Built files will be output to the `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

### Running Tests

```bash
# Run tests with coverage
npm run test

# Run tests with UI
npm run test:ui
```

### Linting

```bash
npm run lint
```

## Configuration

### Environment Variables

Create a `.env` file in the root directory (see `.env.example`):

```env
# API Base URL
# For production with nginx: Use relative path (default: /api)
# For local development without nginx: Use full URL (e.g., http://localhost:5000/api)
VITE_API_BASE_URL=/api
```

### API Proxy (Development)

The Vite dev server is configured to proxy API requests to `http://localhost:5000`. This is defined in `vite.config.js`:

```js
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true,
      secure: false,
    },
  },
}
```

### Production Deployment

In production, nginx handles routing:
- Frontend static files served from `/`
- API requests to `/api/*` proxied to backend at `http://ragevaluator-api:8080/api/`

See `nginx.conf` for the complete configuration.

## Project Structure

```
src/
├── api/                                # API service layer
│   ├── axiosConfig.js                  # Axios instance configuration with shared error handling
│   ├── documentService.js              # Document API (CRUD, upload, download, reprocess)
│   ├── queryService.js                 # Query API (post, history, get by ID, delete, annotate)
│   ├── experimentService.js            # Experiment API (create, list, get by ID)
│   └── settingsService.js              # Settings API (get, update)
├── utils/                              # Utility functions
│   ├── formatDate.js                   # Date formatting utility
│   ├── formatLanguage.js               # Language code to name formatting
│   ├── formatMetric.js                 # Metric formatting utility (3 decimals)
│   ├── formatResponseTime.js           # Response time formatting (ms/s)
│   ├── relevanceGrades.js              # Relevance grade definitions and helpers
│   ├── formatFileSize.js               # File size formatting utility (B/KB/MB)
│   ├── responseQualityOptions.js       # Response quality options, helpers, and colors
│   ├── sortByKey.js                    # Shared sorting utility for tables and lists
│   ├── metricHelpers.js                # Shared metric definitions, formatting, and best-value logic
│   ├── statisticsPropTypes.js          # Shared PropTypes shapes for statistics components
│   └── experimentColors.js            # 12-color palette for experiment charts and tables
├── components/                         # React components
│   ├── DocumentList.jsx                # Document list page
│   ├── Header.jsx                      # Top navigation bar
│   ├── QueryHistory.jsx                # Query history with collapsible cards and inline annotation
│   ├── Sidebar.jsx                     # Collapsible sidebar navigation
│   ├── SearchView.jsx                  # Main search page
│   ├── Searchbar.jsx                   # Search input component
│   ├── SearchResults.jsx               # Search results display with annotation UI
│   ├── UploadDocuments.jsx             # Document upload page
│   ├── Experiments.jsx                 # Experiment creation page (JSON upload)
│   ├── Statistics.jsx                  # Statistics page (experiment comparison orchestrator)
│   ├── statistics/                     # Statistics sub-components
│   │   ├── ExperimentSelector.jsx      # Pill-style experiment toggle grid
│   │   ├── OverallComparisonTable.jsx  # Metric comparison table with best-value highlighting
│   │   ├── RetrievalMetricsChart.jsx   # Grouped bar chart with error bars
│   │   ├── ResponseQualityChart.jsx    # Stacked horizontal bar chart
│   │   ├── LanguageComparison.jsx      # Side-by-side EN/DE retrieval charts
│   │   └── PerQueryBreakdown.jsx       # Expandable per-question comparison accordion
│   └── Settings.jsx                    # Settings page (embedding model, chunking, prompts)
├── assets/                             # Static assets
│   └── rag-evaluator.svg               # Application logo
├── App.jsx                             # Main app component with routing
├── main.jsx                            # Application entry point
└── index.css                           # Global styles (Tailwind imports)
```

## Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start development server |
| `npm run build` | Build for production |
| `npm run preview` | Preview production build |
| `npm run lint` | Run ESLint |
| `npm run test` | Run tests with coverage |
| `npm run test:ui` | Run tests with Vitest UI |

## Docker

The WebUI is containerized and runs with nginx. See the root `docker-compose.yml` for the complete setup.

```bash
# Build and run all services
docker-compose up -d

# WebUI will be available at http://localhost:3000
```

## API Integration

Brief summary of implemented API services (see `src/api`):

| Endpoint | Description |
|----------|-------------|
| `POST /api/query` | Send `{ Question, TopK, Language }` → AI answer + sources |
| `GET /api/query/history` | Get all queries with details, parameters, and metrics |
| `GET /api/query/{id}` | Get full query details including sources and metrics |
| `PATCH /api/query/{id}/results` | Annotate results with relevance grades + response quality → calculated metrics |
| `POST /api/documents/upload` | Upload PDF with language (multipart/form-data: `file`, `language`) |
| `GET /api/documents` | List all uploaded documents |
| `GET /api/documents/{id}` | Get document details |
| `GET /api/documents/{id}/download` | Download document file |
| `DELETE /api/documents/{id}` | Delete a document |
| `POST /api/documents/reprocess` | Reprocess all documents with current chunking/embedding config |
| `GET /api/settings` | Get current RAG configuration and available options |
| `PATCH /api/settings` | Update RAG configuration (partial update, only changed fields) |
| `GET /api/experiments` | List all experiments with progress and config |
| `GET /api/experiments/{id}` | Get experiment with query groups and aggregated metrics |
| `POST /api/experiments` | Create experiment with `{ Name, RepeatCount, Queries[] }` |

Axios base URL is controlled by `VITE_API_BASE_URL` (default `/api`). Timeout is 300000 ms (5 min).