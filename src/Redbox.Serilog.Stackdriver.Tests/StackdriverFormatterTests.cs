using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Redbox.Serilog.Stackdriver.Tests
{
    public class StackdriverFormatterTests
    {
        private static readonly DateTimeOffset DateTimeOffset = DateTimeOffset.Now;

        [Fact]
        public void Test_StackdriverFormatter_Format()
        {
            var propertyName = "greeting";
            var propertyValue = "hello";
            var logEvent = new LogEvent(DateTimeOffset, LogEventLevel.Debug, new Exception(),
                new MessageTemplate("{greeting}",
                    new MessageTemplateToken[] { new PropertyToken(propertyName, propertyValue, "l") }),
                new LogEventProperty[0]);

            using var writer = new StringWriter();
            new StackdriverJsonFormatter().Format(logEvent, writer);
            var log = JObject.Parse(writer.ToString());

            AssertValidLogLine(log);
            Assert.True(log.Value<string>("message") == propertyValue);
        }

        [Fact]
        public void Test_StackdrvierFormatter_FormatLong()
        {
            // Creates a large string > 200kb
            var token = new TextToken(new string('*', 51200));
            var logEvent = new LogEvent(DateTimeOffset, LogEventLevel.Debug,
                new Exception(), new MessageTemplate("{0}", new MessageTemplateToken[] { token }),
                new LogEventProperty[0]);

            using var writer = new StringWriter();
            new StackdriverJsonFormatter().Format(logEvent, writer);
            var lines = SplitLogLogs(writer.ToString());

            // The log created was longer than Stackdriver's soft limit of 256 bytes
            // This means the json will be spread out onto two lines, breaking search
            // In this scenario the library should add an additional log event informing
            // the user of this issue
            Assert.True(lines.Length == 2);
            // Validate each line is valid json
            var ourLogLine = JObject.Parse(lines[0]);
            AssertValidLogLine(ourLogLine);
            var errorLogLine = JObject.Parse(lines[1]);
            AssertValidLogLine(errorLogLine, hasException: false);
        }

        [Fact]
        public void Test_StackdriverFormatter_MarkErrorsForErrorReporting()
        {
            // Creates a large string > 200kb
            var token = new TextToken("test error");
            var logEvent = new LogEvent(DateTimeOffset, LogEventLevel.Error,
                null, new MessageTemplate("{0}", new MessageTemplateToken[] { token }),
                new[] { new LogEventProperty("SourceContext", new ScalarValue("the source context")) });

            using var writer = new StringWriter();
            new StackdriverJsonFormatter(markErrorsForErrorReporting: true).Format(logEvent, writer);
            var log = JObject.Parse(writer.ToString());

            AssertValidLogLine(log, false);
            
            // @type
            Assert.Equal(
                "type.googleapis.com/google.devtools.clouderrorreporting.v1beta1.ReportedErrorEvent",
                log.Value<string>("@type")
            );
            
            // Report location
            Assert.Equal("the source context", log.SelectToken("context.reportLocation.filePath")?.Value<string>());
            
            // Service context
            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            Assert.Equal(assemblyName?.Name, log.SelectToken("serviceContext.service")?.Value<string>());
            Assert.Equal(assemblyName?.Version?.ToString(), log.SelectToken("serviceContext.version")?.Value<string>());
        }

        private string[] SplitLogLogs(string logLines)
        {
            return logLines.Split("\n").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        /// <summary>
        /// Asserts required fields in log output are set and have valid values
        /// </summary>
        /// <param name="log"></param>
        /// <param name="hasException"></param>
        private void AssertValidLogLine(JObject log,
            bool hasException = true)
        {
            Assert.True(log.ContainsKey("message"));
            Assert.NotEmpty(log.Value<string>("message"));

            Assert.True(log.ContainsKey("timestamp"));
            var timestamp = DateTimeOffset.UtcDateTime.ToString("O");
            Assert.Equal(log.Value<DateTime>("timestamp").ToString("O"), timestamp);

            Assert.True(log.ContainsKey("fingerprint"));
            Assert.NotEmpty(log.Value<string>("fingerprint"));

            Assert.True(log.ContainsKey("severity"));
            Assert.NotEmpty(log.Value<string>("severity"));

            Assert.True(log.ContainsKey(("MessageTemplate")));
            Assert.NotEmpty(log.Value<string>("MessageTemplate"));

            if (hasException)
            {
                Assert.True(log.ContainsKey("exception"));
                Assert.NotEmpty(log.Value<string>("exception"));
            }
        }
    }
}