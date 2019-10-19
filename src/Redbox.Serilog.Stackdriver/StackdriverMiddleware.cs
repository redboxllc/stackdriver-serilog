using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Serilog;

namespace Redbox.Serilog.Stackdriver
{
    /// <summary>
    /// Middleware that adds additional Stackdriver LogEvent properties not found in ASP's default logging
    /// </summary>
    public class StackdriverLoggingMiddleware
    {
        /// <summary>
        /// Prefix used for the key in log properties to avoid collisions with other logging frameworks or ASP
        /// </summary>
        /// <returns></returns>
        public static string StackdriverKeyPrefix = $"{nameof(StackdriverLoggingMiddleware)}_";
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
            HandleUserAgent(httpContext?.Request);
            HandleReferer(httpContext?.Request);

            var httpConnection = httpContext?.Features?.Get<IHttpConnectionFeature>();
            HandleServerIp(httpConnection);
            HandleRemoteIp(httpContext?.Request, httpConnection);

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

        private void HandleReferer(HttpRequest request)
        {
            var referer = request?.Headers["Referer"].ToString();
            if(!string.IsNullOrWhiteSpace(referer))
            {
                AddToLog("referer", referer);
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

        private void HandleRemoteIp(HttpRequest request, IHttpConnectionFeature httpConnection)
        {
            if(httpConnection == null) return;

            // Check for LB/proxy forwarded header first
            // If this is set Remote IP is likely incorrect
            var forwardedIp = request?.Headers["X-Forwarded-For"].ToString();
            var key = "remoteIp";
            if(!string.IsNullOrWhiteSpace(forwardedIp))
            {
                AddToLog(key, forwardedIp);
                return;
            }

            var remoteIp = httpConnection?.RemoteIpAddress?.ToString();
            if(!string.IsNullOrWhiteSpace(remoteIp))
            {
                AddToLog(key, remoteIp);
            }
        }
    }
}