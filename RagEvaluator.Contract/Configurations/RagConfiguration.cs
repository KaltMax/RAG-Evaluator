using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Configurations
{
    /// <summary>
    /// Represents the configuration settings for a Retrieval-Augmented Generation (RAG) workflow, including model
    /// endpoints, chunking parameters, and system prompt customization.
    /// </summary>
    public class RagConfiguration
    {
        public string OllamaEndpoint { get; set; } = "http://localhost:11434/v1";
        public string EmbeddingModel { get; set; } = "nomic-embed-text-v2-moe";
        public string AvailableEmbeddingModels { get; set; } = "nomic-embed-text-v2-moe";
        public string ChatModel { get; set; } = "qwen2.5:14b";
        public ChunkingStrategy ChunkingStrategy { get; set; } = ChunkingStrategy.FixedSize;
        public PromptTemplate PromptTemplate { get; set; } = PromptTemplate.Basic;
        public string PromptBasic { get; set; } = "You are a helpful assistant. Answer the question based on the provided context. Be concise and accurate. If the context does not contain the answer, respond with 'I don't know.'";
        public string PromptInstructed { get; set; } = "You are a helpful assistant. Answer the question based on the provided context. Be concise and accurate. If the context does not contain the answer, respond with 'I don't know.' Always respond in the query language.";
        public string PromptLanguageAwareEn { get; set; } = "You are a helpful assistant. Answer the question based on the provided context. Be concise and accurate. If the context does not contain the answer, respond with 'I don't know.' Always respond in English.";
        public string PromptLanguageAwareDe { get; set; } = "Du bist ein hilfreicher Assistent. Beantworte die Frage basierend auf dem bereitgestellten Kontext. Sei präzise und genau. Wenn der Kontext die Antwort nicht enthält, antworte mit 'Ich weiß es nicht.' Antworte immer auf Deutsch.";
        public int ChunkSize { get; set; } = 1000;
        public int ChunkOverlap { get; set; } = 200;
        public double SimilarityThreshold { get; set; } = 0.5;
    }
}
