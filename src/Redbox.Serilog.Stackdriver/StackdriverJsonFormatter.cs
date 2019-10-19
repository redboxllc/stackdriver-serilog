// Copyright 2019 Redbox Automated Retail LLC
// Copyright 2018 Mehdi El Gueddari
// Copyright 2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Origin source code for class from: 
// https://github.com/mehdime/gcp-logging-playground

using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Formatting;
using System.Linq;

namespace Redbox.Serilog.Stackdriver
{
    /// <summary>
    /// Custom JSON formatter based on the built-in RenderedCompactJsonFormatter 
    /// but using property names that allow seemless integration with Stackdriver.
    /// </summary>
    public class StackdriverJsonFormatter : ITextFormatter
    {        
        readonly JsonValueFormatter _valueFormatter;

        public StackdriverJsonFormatter(JsonValueFormatter valueFormatter = null)
        {
            _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
        }

        /// <summary>
        /// Format the log event into the output. Subsequent events will be newline-delimited.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatEvent(logEvent, output, _valueFormatter);
            output.WriteLine();
        }

        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
        public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (valueFormatter == null) throw new ArgumentNullException(nameof(valueFormatter));

            /*
             * 'timestamp', 'message', 'severity' and 'exception' are well-known
             * properties that Stackdriver will use to display and analyse your 
             * logs correctly.
             */

            // TIMESTAMP
            output.Write("{\"timestamp\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));

            // MESSAGE
            output.Write("\",\"message\":");
            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            JsonValueFormatter.WriteQuotedJsonString(message, output);

            // FINGERPRINT
            output.Write(",\"fingerprint\":\"");
            var id = EventIdHash.Compute(logEvent.MessageTemplate.Text);
            output.Write(id.ToString("x8"));
            output.Write('"');
            
            // SEVERITY
            // https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry#LogSeverity
            output.Write(",\"severity\":\"");
            var severity = StackDriverLogLevel.GetSeverity(logEvent.Level);
            output.Write(severity);
            output.Write('\"');

            // EXCEPTION
            if (logEvent.Exception != null)
            {
                output.Write(",\"exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            // HTTPREQUEST
            TryOutputHttpRequest(logEvent, output, valueFormatter);

            // Custom Properties passed in by code logging
            foreach (var property in logEvent.Properties)
            {
                // Skip any properties used by Stackdriver to avoid double logging these values
                if(StackdriverLogKeys.All.Contains(property.Key)) continue;

                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }

                WriteKeyValue(output, valueFormatter, name, property.Value);
            }

            output.Write('}');
        }

        /// <summary>
        /// Outputs valid json key/value pair for the given key and LogEvenPropertyValue
        /// </summary>
        /// <param name="output"></param>
        /// <param name="formatter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void WriteKeyValue(TextWriter output, JsonValueFormatter formatter, 
            string key, LogEventPropertyValue value, bool prependComma = true)
        {
            if(prependComma) output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(':');
            formatter.Format(value, output);
        }

        /// <summary>
        /// Outputs a key/value pair as json if the given input key is present in the LogEvent
        /// </summary>
        /// <param name="output"></param>
        /// <param name="formatter"></param>
        /// <param name="logEvent"></param>
        /// <param name="outputKey"></param>
        /// <param name="inputKey"></param>
        private static bool WriteIfPresent(TextWriter output, JsonValueFormatter formatter, LogEvent logEvent, 
            string outputKey, string inputKey, bool prependComma = true)
        {
            if(logEvent.Properties.ContainsKey(inputKey) && logEvent.Properties[inputKey] != null)
            {
                var propertyValue = logEvent.Properties[inputKey];
                WriteKeyValue(output, formatter, outputKey, propertyValue, prependComma: prependComma);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Outputs Stackdriver's LogEvent.HttpRequest if the appropriate values are set in the LogEvent.Properties
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="output"></param>
        /// <param name="formatter"></param>
        private static void TryOutputHttpRequest(LogEvent logEvent, TextWriter output, JsonValueFormatter formatter)
        {
            output.Write(",\"httpRequest\":{");
            var hasFirstBeenOutput = false;
            // Map the middleware injected data
            foreach(var key in StackdriverLogKeys.HttpRequest.All)
            {
                hasFirstBeenOutput = WriteIfPresent(output, formatter, logEvent, key, key, prependComma: hasFirstBeenOutput);
            }
            // Map ASP logged data
            // ToDo: Map ASP logged data
            output.Write('}');
        }
    }
}