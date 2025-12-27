# RAG-Evaluator WebUI

Modern web interface for the RAG-Evaluator system built with React, Vite, and Tailwind CSS.

## Features

### Implemented

- **Document Search** - Query uploaded documents using AI-powered RAG (Retrieval-Augmented Generation)
  - Ask questions in natural language
  - Get AI-generated answers with source citations
  - View similarity scores for retrieved chunks
  - Adjustable top-K results (1, 3, 5, 10)

- **Document Upload** - Upload PDF documents for processing
  - Drag-and-drop interface powered by react-dropzone
  - PDF-only validation
  - Optional document descriptions
  - Real-time upload progress
  - View processed document metadata (pages, chunks, etc.)

- **Responsive Navigation**
  - Collapsible sidebar (icon-only on mobile, full width on desktop)
  - Active route highlighting
  - Clean, modern dark theme

### Placeholder Pages

- **Statistics** - To be implemented
- **Settings** - To be implemented

## Tech Stack

- **React 19** - UI framework
- **Vite 7** - Build tool with HMR
- **Tailwind CSS 4** - Utility-first styling
- **React Router DOM 7** - Client-side routing
- **Axios** - HTTP client for API calls
- **React Dropzone** - Drag-and-drop file uploads
- **React Toastify** - Toast notifications
- **Heroicons** - Icon library
- **Vitest** - Unit testing with coverage

## Getting Started

### Prerequisites

- Node.js 18+ and npm
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
├── api/                    # API service layer
│   ├── axiosConfig.js     # Axios instance configuration
│   ├── PostQueryService.js # Query API service
│   └── UploadDocumentsService.js # Upload API service
├── components/             # React components
│   ├── Header.jsx         # Top navigation bar
│   ├── Sidebar.jsx        # Collapsible sidebar navigation
│   ├── SearchView.jsx     # Main search page
│   ├── Searchbar.jsx      # Search input component
│   ├── SearchResults.jsx  # Search results display
│   ├── UploadDocuments.jsx # Document upload page
│   ├── Statistics.jsx     # Statistics page (placeholder)
│   └── Settings.jsx       # Settings page (placeholder)
├── assets/                 # Static assets
│   └── rag-evaluator.svg  # Application logo
├── App.jsx                 # Main app component with routing
├── main.jsx               # Application entry point
└── index.css              # Global styles (Tailwind imports)
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

The frontend communicates with the backend API through two main services:

### Query Service
- **Endpoint**: `POST /api/query`
- **Purpose**: Submit questions and get AI-generated answers
- **Request**: `{ Question: string, TopK: number }`
- **Response**: `{ queryId, question, answer, sources[], timestamp }`

### Upload Service
- **Endpoint**: `POST /api/documents/upload`
- **Purpose**: Upload PDF documents for processing
- **Request**: Multipart form data with `file` and optional `description`
- **Response**: `{ documentId, fileName, description, pageCount, chunkCount, uploadedAt }`