using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CinemaBooking.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Correlation ID resolution
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) || string.IsNullOrEmpty(correlationId))
            {
                correlationId = System.Guid.NewGuid().ToString();
            }

            context.Items[CorrelationIdHeader] = correlationId.ToString();
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            var request = context.Request;
            _logger.LogInformation("HTTP Request Started. Method: {Method}, Path: {Path}, QueryString: {Query}, CorrelationId: {CorrelationId}",
                request.Method, request.Path, request.QueryString, correlationId);

            await _next(context);

            stopwatch.Stop();
            var response = context.Response;
            _logger.LogInformation("HTTP Request Finished. Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId, context.TraceIdentifier);
        }
    }
}
