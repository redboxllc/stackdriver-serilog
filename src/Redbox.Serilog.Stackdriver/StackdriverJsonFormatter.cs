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
using Serilog.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Redbox.Serilog.Stackdriver
{
    /// <summary>
    /// Custom JSON formatter based on the built-in RenderedCompactJsonFormatter 
    /// but using property names that allow seemless integration with Stackdriver.
    /// </summary>
    public class StackdriverJsonFormatter : ITextFormatter
    {
        // 256kb apparently, but we're conservative (https://cloud.google.com/logging/quotas)
        private static readonly int STACKDRIVER_ENTRY_LIMIT_BYTES = 200 * 1024; // 258kb, reduced to 200kb

        private readonly bool _checkForPayloadLimit;
        private readonly bool _includeMessageTemplate;
        private readonly JsonValueFormatter _valueFormatter;

        public StackdriverJsonFormatter(bool checkForPayloadLimit = true, 
            bool includeMessageTemplate = true,
            JsonValueFormatter valueFormatter = null)
        {
            _checkForPayloadLimit = checkForPayloadLimit;
            _includeMessageTemplate = includeMessageTemplate;
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
        }

        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
        public void FormatEvent(LogEvent logEvent, TextWriter originalOutput, JsonValueFormatter valueFormatter)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (originalOutput == null) throw new ArgumentNullException(nameof(originalOutput));
            if (valueFormatter == null) throw new ArgumentNullException(nameof(valueFormatter));

            // wrap the originally text writer in one that can count the number of characters written
            var output = new CountingTextWriter(originalOutput);

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

            // Serilog Message Template
            if (_includeMessageTemplate)
            {
                output.Write(",\"messageTemplate\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);
            }

            // Custom Properties passed in by code logging
            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }

                WriteKeyValue(output, valueFormatter, name, property.Value);
            }

            output.Write('}');
			output.WriteLine(); // finish the log line

			// if we have blown the limit of a single stackdriver line (which means that error reporting won't parse 
			// it correctly for instance) - then log that fact out too so we can adjust the logging and fix the problem
            if (_checkForPayloadLimit && (output.CharacterCount * 4) >= STACKDRIVER_ENTRY_LIMIT_BYTES)
			{
				string text = "An attempt was made to write a log event to stackdriver that exceeds StackDriver Entry length limit - check logs for partially parsed entry just prior to this and fix at source";
				var tooLongLogEvent = new LogEvent(
					logEvent.Timestamp, LogEventLevel.Fatal, null,
					new MessageTemplate(text, new MessageTemplateToken[] { new TextToken(text) }), // this is actually what gets rendered
					new List<LogEventProperty>()
				);
				FormatEvent(tooLongLogEvent, output, valueFormatter);
			}
        }

        /// <summary>
        /// Outputs valid json key/value pair for the given key and LogEvenPropertyValue
        /// </summary>
        /// <param name="output"></param>
        /// <param name="formatter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void WriteKeyValue(TextWriter output, JsonValueFormatter formatter,
            string key, LogEventPropertyValue value, bool prependComma = true)
        {
            if (prependComma) output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(':');
            formatter.Format(value, output);
        }
    }
}