using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Main service for Retrieval-Augmented Generation operations
    /// Business logic orchestration layer
    /// </summary>
    public class RagService : IRagService
    {
        private readonly RagConfiguration _config;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IVectorStore _vectorStore;
        private readonly IEmbeddingService _embeddingService;
        private readonly IChatService _chatService;
        private readonly IDocumentService _documentService;
        private int _nextChunkId;

        public RagService(
            RagConfiguration config,
            IPdfLoader pdfLoader,
            ITextChunker textChunker,
            IVectorStore vectorStore,
            IEmbeddingService embeddingService,
            IChatService chatService,
            IDocumentService documentService)
        {
            _config = config;
            _pdfLoader = pdfLoader;
            _textChunker = textChunker;
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
            _chatService = chatService;
            _documentService = documentService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream pdfStream, string fileName)
        {
            if (!await _embeddingService.IsAvailableAsync())
            {
                throw new InvalidOperationException("Embedding service not available. Ensure Ollama is running with the required models.");
            }

            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(fileName, null, pdfStream.Length, "application/pdf");

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing);

                // Load and process PDF
                var pages = _pdfLoader.LoadPdf(pdfStream);
                var chunks = _textChunker.SplitDocuments(pages);

                // Generate embeddings and store chunks
                foreach (var chunk in chunks)
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk);
                    _vectorStore.AddEntry(
                        _nextChunkId++,
                        chunk,
                        embedding,
                        new Dictionary<string, object>
                        {
                            ["documentId"] = document.Id.ToString(),
                            ["fileName"] = fileName
                        }
                    );
                }

                // Update status to Completed with page and chunk counts
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Completed, pages.Count, chunks.Count);

                return new DocumentResponse
                {
                    DocumentId = document.Id,
                    FileName = fileName,
                    PageCount = pages.Count,
                    ChunkCount = chunks.Count,
                    UploadedAt = document.UploadedAt
                };
            }
            catch
            {
                // Update status to Failed on error
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Failed);
                throw;
            }
        }

        public async Task<QueryResponse> AskQuestionAsync(string question, int topK = 3)
        {
            if (!await _chatService.IsAvailableAsync() || !await _embeddingService.IsAvailableAsync())
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            var queryId = Guid.NewGuid();

            // Generate embedding for the question
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

            // Search for relevant documents
            var searchResults = _vectorStore.Search(questionEmbedding, topK);

            if (searchResults.Count == 0)
            {
                return new QueryResponse
                {
                    QueryId = queryId,
                    Question = question,
                    Answer = "No relevant documents found in the knowledge base. Please upload documents first.",
                    Sources = new List<SearchResultDto>()
                };
            }

            // Build context from search results
            var context = string.Join("\n\n", searchResults.Select(r => r.Text));

            // Generate answer using LLM
            var userMessage = $"Context:\n{context}\n\nQuestion: {question}\n\nAnswer:";
            var answer = await _chatService.GenerateResponseAsync(_config.SystemPrompt, userMessage);

            // Convert search results to DTOs
            var sourceDtos = searchResults.Select(r => new SearchResultDto
            {
                Id = r.Id,
                Text = r.Text,
                Similarity = r.Similarity,
                Metadata = r.Metadata
            }).ToList();

            return new QueryResponse
            {
                QueryId = queryId,
                Question = question,
                Answer = answer,
                Sources = sourceDtos
            };
        }

        public async Task<bool> IsInitializedAsync()
        {
            return await _embeddingService.IsAvailableAsync() && await _chatService.IsAvailableAsync();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var documents = await _documentService.GetAllAsync();
            return documents.Count;
        }
    }
}
