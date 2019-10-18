using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Serilog;

namespace Redbox.Serilog.Stackdriver
{
    public class StackdriverLoggingMiddleware
    {
        public static string StackdriverKeyPrefix = "Stackdriver_";
        private readonly RequestDelegate _next;
        private readonly IDiagnosticContext _diagnosticContext;

        public StackdriverLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
        {
            _next = next;
            _diagnosticContext = diagnosticContext;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpConnection = httpContext?.Features?.Get<IHttpConnectionFeature>();
            // Request 
            HandleUserAgent(httpContext?.Request);
            HandleServerIp(httpConnection);
            
            // Trigger response/other middlewares
            await _next.Invoke(httpContext);
            
            // Response
            
        }

        /// <summary>
        /// Adds the key and value to the log.  The key is prefixed with StackdriverKeyPrefix to avoid 
        /// collisions with other logging middlewares/components.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void AddToLog(string key, string value)
        {
            _diagnosticContext.Set($"{StackdriverKeyPrefix}{key}", value);
        }

        private void HandleUserAgent(HttpRequest request)
        {
            var userAgent = request?.Headers["User-Agent"].ToString();
            if(!string.IsNullOrWhiteSpace(userAgent))
            {
                AddToLog("userAgent", userAgent);
            }
        }

        private void HandleServerIp(IHttpConnectionFeature httpConnection)
        {
            if(httpConnection == null) return;
            var localIpAddress = httpConnection.LocalIpAddress?.ToString();
            if(!string.IsNullOrWhiteSpace(localIpAddress))
            {
                AddToLog("serverIp", localIpAddress);
            }
        }
    }
}