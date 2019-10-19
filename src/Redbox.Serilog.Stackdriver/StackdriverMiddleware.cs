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

        private void HandleUserAgent(HttpRequest request)
        {
            var userAgent = request?.Headers["User-Agent"].ToString();
            if(!string.IsNullOrWhiteSpace(userAgent))
            {
                _diagnosticContext.Set(StackdriverLogKeys.HttpRequest.UserAgent, userAgent);
            }
        }

        private void HandleReferer(HttpRequest request)
        {
            var referer = request?.Headers["Referer"].ToString();
            if(!string.IsNullOrWhiteSpace(referer))
            {
                _diagnosticContext.Set(StackdriverLogKeys.HttpRequest.Referer, referer);
            }
        }

        private void HandleServerIp(IHttpConnectionFeature httpConnection)
        {
            if(httpConnection == null) return;
            var localIpAddress = httpConnection.LocalIpAddress?.ToString();
            if(!string.IsNullOrWhiteSpace(localIpAddress))
            {
                _diagnosticContext.Set(StackdriverLogKeys.HttpRequest.ServerIp, localIpAddress);
            }
        }

        private void HandleRemoteIp(HttpRequest request, IHttpConnectionFeature httpConnection)
        {
            if(httpConnection == null) return;

            // Check for LB/proxy forwarded header first
            // If this is set Remote IP is likely incorrect
            var forwardedIp = request?.Headers["X-Forwarded-For"].ToString();
            if(!string.IsNullOrWhiteSpace(forwardedIp))
            {
                _diagnosticContext.Set(StackdriverLogKeys.HttpRequest.RemoteIp, forwardedIp);
                return;
            }

            var remoteIp = httpConnection?.RemoteIpAddress?.ToString();
            if(!string.IsNullOrWhiteSpace(remoteIp))
            {
                _diagnosticContext.Set(StackdriverLogKeys.HttpRequest.RemoteIp, remoteIp);
            }
        }
    }
}