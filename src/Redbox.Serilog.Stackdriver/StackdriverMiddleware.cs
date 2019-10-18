using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Redbox.Serilog.Stackdriver
{
    public class StackdriverLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticContext _diagnosticContext;

        public StackdriverLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
        {
            _next = next;
            _diagnosticContext = diagnosticContext;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // Request 
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            if(!string.IsNullOrWhiteSpace(userAgent))
            {
                _diagnosticContext.Set("UserAgent", userAgent);
            }
            
            // Trigger response/other middlewares
            await _next.Invoke(httpContext);
            
            // Response
            
        }
    }
}