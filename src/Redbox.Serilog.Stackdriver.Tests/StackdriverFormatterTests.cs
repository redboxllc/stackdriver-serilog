using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Redbox.Serilog.Stackdriver.Tests
{
    public class StackdriverFormatterTests
    {
        private static readonly DateTimeOffset DateTimeOffset = DateTimeOffset.Now;
        private readonly LogEvent _logEventStandard = new LogEvent(DateTimeOffset, LogEventLevel.Debug, 
            new Exception(), new MessageTemplate("{0}", new MessageTemplateToken[] { new TextToken("Hello") }), 
            new LogEventProperty[0]);

        [Fact]
        public void Test_StackdriverFormatter_Format()
        {
            using var writer = new StringWriter();
            new StackdriverJsonFormatter().Format(_logEventStandard, writer);
            var logDict = GetLogLineAsDictionary(writer.ToString());
            AssertValidLogLine(logDict);
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
            var ourLogLineDict = GetLogLineAsDictionary(lines[0]);
            AssertValidLogLine(ourLogLineDict);
            var errorLogLineDict = GetLogLineAsDictionary(lines[1]);
            AssertValidLogLine(errorLogLineDict, hasException: false);
        }

        private string[] SplitLogLogs(string logLines)
        {
            return logLines.Split("\n").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        /// <summary>
        /// Gets a log line in json format as a dictionary of string pairs
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetLogLineAsDictionary(string log)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(log);
        }

        /// <summary>
        /// Asserts required fields in log output are set and have valid values
        /// </summary>
        /// <param name="logDict"></param>
        /// <param name="hasException"></param>
        private void AssertValidLogLine(Dictionary<string, string> logDict, bool hasException = true)
        {
            Assert.True(logDict.ContainsKey("message"));
            Assert.NotEmpty(logDict["message"]);
            
            Assert.True(logDict.ContainsKey("timestamp"));
            var timestamp = DateTimeOffset.UtcDateTime.ToString("O");
            Assert.Equal(logDict["timestamp"], timestamp);
            
            Assert.True(logDict.ContainsKey("fingerprint"));
            Assert.NotEmpty(logDict["fingerprint"]);
            
            Assert.True(logDict.ContainsKey("severity"));
            Assert.NotEmpty(logDict["severity"]);

            if (hasException)
            {
                Assert.True(logDict.ContainsKey("exception"));
                Assert.NotEmpty(logDict["exception"]);
            }
        }
    }
}
