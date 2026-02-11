using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RagEvaluator.API.Middleware;

namespace RagEvaluator.Test.ApiTest
{
    public class ExceptionHandlerTests
    {
        private readonly ILogger<ExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ExceptionHandler _exceptionHandler;

        public ExceptionHandlerTests()
        {
            _logger = Substitute.For<ILogger<ExceptionHandler>>();
            _problemDetailsService = Substitute.For<IProblemDetailsService>();
            _problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>())
                .Returns(new ValueTask<bool>(true));
            _exceptionHandler = new ExceptionHandler(_logger, _problemDetailsService);
        }

        [Fact]
        public async Task TryHandleAsync_WithInvalidOperationException_Returns503()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new InvalidOperationException("Service is unavailable");

            // Act
            var result = await _exceptionHandler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result);
            Assert.Equal(503, httpContext.Response.StatusCode);
            await _problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
                ctx.ProblemDetails.Status == 503 &&
                ctx.ProblemDetails.Title == "Service not available" &&
                ctx.ProblemDetails.Detail == "Service is unavailable"));
        }

        [Fact]
        public async Task TryHandleAsync_WithArgumentException_Returns400()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new ArgumentException("Invalid argument");

            // Act
            var result = await _exceptionHandler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result);
            Assert.Equal(400, httpContext.Response.StatusCode);
            await _problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
                ctx.ProblemDetails.Status == 400 &&
                ctx.ProblemDetails.Title == "Bad request" &&
                ctx.ProblemDetails.Detail == "Invalid argument"));
        }

        [Fact]
        public async Task TryHandleAsync_WithUnhandledException_Returns500()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new Exception("Something went wrong");

            // Act
            var result = await _exceptionHandler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result);
            Assert.Equal(500, httpContext.Response.StatusCode);
            await _problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
                ctx.ProblemDetails.Status == 500 &&
                ctx.ProblemDetails.Title == "Internal server error" &&
                ctx.ProblemDetails.Detail == "Something went wrong"));
        }
    }
}
