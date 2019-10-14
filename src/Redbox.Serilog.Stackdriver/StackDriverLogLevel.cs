// Copyright 2019 Redbox Automated Retail LLC
// Copyright 2018 Mehdi El Gueddari
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

using Serilog.Events;

namespace Redbox.Serilog.Stackdriver
{
    /// <summary>
    /// Mappings of Microsoft/Serilog Log Levels to Stackdriver
    /// </summary>
    public static class StackDriverLogLevel
    {
        public const string DEBUG = "DEBUG";
        public const string WARNING = "WARNING";
        public const string ERROR = "ERROR";
        public const string FATAL = "CRITICAL";
        public const string INFO = "INFO";

        /// <summary>
        /// Returns the appropriate StackDriver LogLevel for the given Serilog LogEventLevel
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string GetSeverity(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Debug:
                case LogEventLevel.Verbose: // Stackdriver doesn't have a Verbose level
                    return StackDriverLogLevel.DEBUG;
                case LogEventLevel.Warning:
                    return StackDriverLogLevel.WARNING;
                case LogEventLevel.Error:
                    return StackDriverLogLevel.ERROR;
                case LogEventLevel.Fatal:
                    return StackDriverLogLevel.FATAL;
                case LogEventLevel.Information:
                    return StackDriverLogLevel.INFO;
                default:
                    return "DEFAULT";
            }
        }
    }
}