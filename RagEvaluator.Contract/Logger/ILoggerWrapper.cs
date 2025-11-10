namespace RagEvaluator.Contract.Logger
{
    /// <summary>
    /// Defines a generic interface for logging messages at various severity levels for a specified category type.
    /// </summary>
    /// <typeparam name="TCategory">The type representing the category for log messages. Typically used to group or identify the source of log
    /// entries.</typeparam>
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
