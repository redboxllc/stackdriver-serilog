using System.Collections.Generic;

namespace Redbox.Serilog.Stackdriver
{
    internal static class StackdriverLogKeys
    {
        public static string Timestamp = "timestamp";
        public static string Message = "message";
        public static string Fingerprint = "fingerprint";
        public static string Severity = "severity";
        public static string Exception = "exception";

        /// <summary>
        /// Contains all keys used by the Stackdriver Log Formatter
        /// </summary>
        public static string[] GetAll() {
            // Add base keys
            var all = new List<string> {
                Timestamp,
                Message,
                Fingerprint,
                Severity,
                Exception
            };
            
            // Add subclass keys
            all.AddRange(HttpRequest.All);

            return all.ToArray();
        }

        /// <summary>
        /// Contains any keys used in the Stackdriver LogEvent.HttpRequest object
        /// </summary>
        internal static class HttpRequest
        {
            internal static string RemoteIp = "remoteIp";
            internal static string ServerIp = "serverIp";
            internal static string UserAgent = "userAgent";
            internal static string Referer = "referer";

            internal static string Protocol = "protocol";
            internal static string RequestMethod = "requestMethod";
            internal static string Status = "status";
            

            /// <summary>
            /// Array of all static strings used as Log Keys
            /// </summary>
            /// <value></value>
            internal static string[] All = new[]{
                Referer,
                RemoteIp,
                ServerIp,
                UserAgent,
                Protocol
            };

            /// <summary>
            /// Array of all Log Keys set by the StackdriverMiddleware class
            /// </summary>
            /// <value></value>
            internal static string[] AllMiddlewareKeys = new[]{
                Referer,
                RemoteIp,
                ServerIp,
                UserAgent,
                Protocol
            };
        }
    }
}