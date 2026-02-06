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
            [PromptTemplate.BasicEn, PromptTemplate.InstructedEn, PromptTemplate.NativeLanguage];

        public static string Resolve(PromptTemplate template, string language, RagConfiguration config)
        {
            return template switch
            {
                PromptTemplate.BasicEn => config.PromptBasic,

                PromptTemplate.InstructedEn => config.PromptInstructed,

                PromptTemplate.NativeLanguage => language switch
                {
                    "de" => config.PromptNativeDe,
                    _ => config.PromptNativeEn,
                },

                _ => throw new ArgumentException($"Unknown prompt template: {template}", nameof(template))
            };
        }
    }
}
