using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Repository for document chunks with vector embeddings and similarity search.
    /// </summary>
    public interface IDocumentChunkRepository
    {
        /// <summary>
        /// Gets a document chunk by its unique identifier.
        /// </summary>
        Task<DocumentChunk?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all document chunks belonging to a specific document.
        /// </summary>
        Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId);
        
        /// <summary>
        /// Gets the total count of all document chunks in the repository.
        /// </summary>
        Task<int> GetCountAsync();
        
        /// <summary>
        /// Adds a new document chunk to the repository.
        /// </summary>
        Task AddAsync(DocumentChunk chunk);
        
        /// <summary>
        /// Adds multiple document chunks to the repository in a single operation.
        /// </summary>
        Task AddRangeAsync(IEnumerable<DocumentChunk> chunks);
        
        /// <summary>
        /// Deletes all document chunks associated with a specific document.
        /// </summary>
        Task DeleteByDocumentIdAsync(Guid documentId);

        /// <summary>
        /// Searches for similar chunks using vector similarity.
        /// Returns raw matches ordered by similarity (closest first) without calculated similarity scores.
        /// </summary>
        Task<IReadOnlyList<ChunkSearchMatch>> SearchAsync(float[] queryEmbedding, int topK = 3);
    }
}
