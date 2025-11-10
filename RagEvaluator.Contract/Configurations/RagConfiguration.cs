namespace RagEvaluator.Contract.Configurations
{
    /// <summary>
    /// Represents the configuration settings for a Retrieval-Augmented Generation (RAG) workflow, including model
    /// endpoints, chunking parameters, and system prompt customization.
    /// </summary>
    public class RagConfiguration
    {
        public string OllamaEndpoint { get; set; } = "http://localhost:11434/v1";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
        public string ChatModel { get; set; } = "llama3.2:1b";
        public int ChunkSize { get; set; } = 1000;
        public int ChunkOverlap { get; set; } = 200;
        public int TopK { get; set; } = 3;
        public string SystemPrompt { get; set; } = "You are a helpful assistant. Answer the question based on the provided context. Be concise and accurate. If the context does not contain the answer, respond with 'I don't know.'";
    }
}
