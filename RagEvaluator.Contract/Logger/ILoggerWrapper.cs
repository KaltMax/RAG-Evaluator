namespace RagEvaluator.Contract.Logger
{
    public interface ILoggerWrapper<TCategory>
    {
        void LogTrace(string messageTemplate, params object[] args);
        void LogTrace(Exception exception, string messageTemplate, params object[] args);
        void LogDebug(string messageTemplate, params object[] args);
        void LogDebug(Exception exception, string messageTemplate, params object[] args);
        void LogInformation(string messageTemplate, params object[] args);
        void LogInformation(Exception exception, string messageTemplate, params object[] args);
        void LogWarning(string messageTemplate, params object[] args);
        void LogWarning(Exception exception, string messageTemplate, params object[] args);
        void LogError(string messageTemplate, params object[] args);
        void LogError(Exception exception, string messageTemplate, params object[] args);
        void LogCritical(string messageTemplate, params object[] args);
        void LogCritical(Exception exception, string messageTemplate, params object[] args);
    }
}
