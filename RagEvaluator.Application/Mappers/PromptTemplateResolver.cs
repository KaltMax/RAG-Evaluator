using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Mappers
{
    /// <summary>
    /// Resolves a system prompt from a template key, query language, and configuration.
    /// </summary>
    public static class PromptTemplateResolver
    {
        public static readonly List<PromptTemplate> AvailableTemplates =
            [PromptTemplate.Basic, PromptTemplate.Instructed, PromptTemplate.LanguageAware];

        public static string Resolve(PromptTemplate template, string language, RagConfiguration config)
        {
            return template switch
            {
                PromptTemplate.Basic => config.PromptBasic,

                PromptTemplate.Instructed => config.PromptInstructed,

                PromptTemplate.LanguageAware => language switch
                {
                    "de" => config.PromptLanguageAwareDe,
                    _ => config.PromptLanguageAwareEn,
                },

                _ => throw new ArgumentException($"Unknown prompt template: {template}", nameof(template))
            };
        }
    }
}
