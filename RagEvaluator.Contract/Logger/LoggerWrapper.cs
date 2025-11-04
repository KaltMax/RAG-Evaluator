using Microsoft.Extensions.Logging;

namespace RagEvaluator.Contract.Logger
{
    public sealed class LoggerWrapper<TCategory> : ILoggerWrapper<TCategory>
    {
        private readonly ILogger<TCategory> _logger;
        public LoggerWrapper(ILogger<TCategory> logger) => _logger = logger;

        public void LogDebug(string m, params object[] a) => _logger.LogDebug(m, a);
        public void LogInformation(string m, params object[] a) => _logger.LogInformation(m, a);
        public void LogWarning(string m, params object[] a) => _logger.LogWarning(m, a);
        public void LogError(string m, params object[] a) => _logger.LogError(m, a);
        public void LogCritical(string m, params object[] a) => _logger.LogCritical(m, a);

        public void LogDebug(Exception ex, string m, params object[] a) => _logger.LogDebug(ex, m, a);
        public void LogInformation(Exception ex, string m, params object[] a) => _logger.LogInformation(ex, m, a);
        public void LogWarning(Exception ex, string m, params object[] a) => _logger.LogWarning(ex, m, a);
        public void LogError(Exception ex, string m, params object[] a) => _logger.LogError(ex, m, a);
        public void LogCritical(Exception ex, string m, params object[] a) => _logger.LogCritical(ex, m, a);
    }
}