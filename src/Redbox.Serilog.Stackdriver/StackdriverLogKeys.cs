namespace Redbox.Serilog.Stackdriver
{
    internal static class StackdriverLogKeys
    {
        /// <summary>
        /// Contains all keys used by the Stackdriver Log Formatter
        /// </summary>
        public static string[] All = HttpRequest.All;

        /// <summary>
        /// Contains any keys used in the Stackdriver LogEvent.HttpRequest object
        /// </summary>
        internal static class HttpRequest
        {
            internal static string RemoteIp = "remoteIp";
            internal static string ServerIp = "serverIp";
            internal static string UserAgent = "userAgent";
            internal static string Referer = "referer";

            internal static string[] All = new[]{
                Referer,
                RemoteIp,
                ServerIp,
                UserAgent
            };
        }
    }
}