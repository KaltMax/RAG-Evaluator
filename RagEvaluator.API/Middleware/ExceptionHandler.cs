using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RagEvaluator.API.Middleware
{
    /// <summary>
    /// Global exception handler middleware that maps exceptions to appropriate HTTP status codes and returns standardized problem details responses.
    /// </summary>
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public ExceptionHandler(ILogger<ExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                InvalidOperationException => (HttpStatusCode.ServiceUnavailable, "Service not available"),
                ArgumentException => (HttpStatusCode.BadRequest, "Bad request"),
                _ => (HttpStatusCode.InternalServerError, "Internal server error")
            };

            _logger.LogError(exception, "{Error}", title);

            httpContext.Response.StatusCode = (int)statusCode;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = title,
                    Detail = exception.Message
                }
            });
        }
    }
}
