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

using System.IO;
using System.Text;

namespace Redbox.Serilog.Stackdriver
{
    /// <summary>
    /// Custom minimal text writer that will pass-through to a provided text writer but whilst doing so will
    /// count the number of characters written.
    /// </summary>
    internal class CountingTextWriter : TextWriter
    {
        private readonly TextWriter originalOutput;

        public CountingTextWriter(TextWriter originalOutput)
        {
            this.originalOutput = originalOutput;
        }

        public long CharacterCount { get; private set; } = 0;

        //

        public override Encoding Encoding => originalOutput.Encoding;

        // NOTE: all other TextWriter overrides are written in terms of this, so we only have to
        // .. subclass this to count characters
        public override void Write(char value)
        {
            // increment count
            CharacterCount++;

            // defer actually write to the original writer
            originalOutput.Write(value);
        }
    }
}