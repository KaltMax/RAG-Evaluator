using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

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

        private Task InitializeAsync()
        {
            try
            {
                var kernelBuilder = Kernel.CreateBuilder()
                    .AddOllamaChatCompletion(
                        endpoint: new Uri(_config.OllamaEndpoint),
                        modelId: _config.ChatModel
                    );

                var kernel = kernelBuilder.Build();
                _chatService = kernel.GetRequiredService<IChatCompletionService>();
                _isInitialized = true;
            }
            catch
            {
                _isInitialized = false;
            }

            return Task.CompletedTask;
        }

        public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _chatService == null)
            {
                throw new InvalidOperationException("Chat service not initialized. Ensure Ollama is running.");
            }

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var settings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["think"] = _config.ChatModelThinking
                }
            };

            var response = await _chatService.GetChatMessageContentAsync(chatHistory, settings, cancellationToken: cancellationToken);
            return response.Content ?? "No response generated.";
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_isInitialized && _chatService != null);
        }
    }
}
