using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Models;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Chat completion service using Ollama via Semantic Kernel
    /// </summary>
    public class OllamaChatService : IChatService
    {
        private readonly RagConfiguration _config;
        private IChatCompletionService? _chatService;
        private bool _isInitialized;

        public OllamaChatService(RagConfiguration config)
        {
            _config = config;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var kernelBuilder = Kernel.CreateBuilder();

                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: _config.ChatModel,
                    apiKey: "ollama",
                    httpClient: new System.Net.Http.HttpClient { BaseAddress = new Uri(_config.OllamaEndpoint) }
                );

                var kernel = kernelBuilder.Build();
                _chatService = kernel.GetRequiredService<IChatCompletionService>();
                _isInitialized = true;
            }
            catch
            {
                _isInitialized = false;
            }
        }

        public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
        {
            if (!_isInitialized || _chatService == null)
            {
                throw new InvalidOperationException("Chat service not initialized. Ensure Ollama is running.");
            }

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory);
            return response.Content ?? "No response generated.";
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(_isInitialized && _chatService != null);
        }
    }
}
