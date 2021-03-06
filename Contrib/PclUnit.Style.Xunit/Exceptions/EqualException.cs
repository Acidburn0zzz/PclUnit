﻿/*
 * Copyright 2013 Outercurve Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace PclUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when two values are unexpectedly not equal.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class EqualException : AssertActualExpectedException
    {
        static Dictionary<char, string> _encodings = new Dictionary<char, string> {
            { '\r', "\\r" },
            { '\n', "\\n" },
            { '\t', "\\t" },
            { '\0', "\\0" }
        };

        string _message;

        /// <summary>
        /// Creates a new instance of the <see cref="EqualException"/> class.
        /// </summary>
        /// <param name="expected">The expected object value</param>
        /// <param name="actual">The actual object value</param>
        public EqualException(object expected, object actual)
            : base(expected, actual, "Assert.Equal() Failure")
        {
            ActualIndex = -1;
            ExpectedIndex = -1;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="EqualException"/> class for string comparisons.
        /// </summary>
        /// <param name="expected">The expected string value</param>
        /// <param name="actual">The actual string value</param>
        /// <param name="expectedIndex">The first index in the expected string where the strings differ</param>
        /// <param name="actualIndex">The first index in the actual string where the strings differ</param>
        public EqualException(string expected, string actual, int expectedIndex, int actualIndex)
            : base(expected, actual, "Assert.Equal() Failure")
        {
            ActualIndex = actualIndex;
            ExpectedIndex = expectedIndex;
        }

        /// <summary>
        /// Gets the index into the actual value where the values first differed.
        /// Returns -1 if the difference index points were not provided.
        /// </summary>
        public int ActualIndex { get; private set; }

        /// <summary>
        /// Gets the index into the expected value where the values first differed.
        /// Returns -1 if the difference index points were not provided.
        /// </summary>
        public int ExpectedIndex { get; private set; }

        private class ShortenEncoded
        {
            public ShortenEncoded(string item1, string item2)
            {
                PrintedValue = item1;
                PrintedPointer = item2;
            }

            public string PrintedValue { get; private set; }
            public string PrintedPointer { get; private set; }
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (_message == null)
                    _message = CreateMessage();

                return _message;
            }
        }

        string CreateMessage()
        {
            if (ExpectedIndex == -1)
                return base.Message;

            var printedExpected = ShortenAndEncode(Expected, ExpectedIndex, '↓');
            var printedActual = ShortenAndEncode(Actual, ActualIndex, '↑');

            return String.Format(
                CultureInfo.CurrentCulture,
                "{1}{0}          {2}{0}Expected: {3}{0}Actual:   {4}{0}          {5}",
                Environment.NewLine,
                UserMessage,
                printedExpected.PrintedPointer,
                printedExpected.PrintedValue ?? "(null)",
                printedActual.PrintedValue ?? "(null)",
                printedActual.PrintedPointer
            );
        }

        static ShortenEncoded ShortenAndEncode(string value, int position, char pointer)
        {
            int start = Math.Max(position - 20, 0);
            int end = Math.Min(position + 41, value.Length);
            StringBuilder printedValue = new StringBuilder(100);
            StringBuilder printedPointer = new StringBuilder(100);

            if (start > 0)
            {
                printedValue.Append("···");
                printedPointer.Append("   ");
            }

            for (int idx = start; idx < end; ++idx)
            {
                char c = value[idx];
                string encoding;
                int paddingLength = 1;

                if (_encodings.TryGetValue(c, out encoding))
                {
                    printedValue.Append(encoding);
                    paddingLength = encoding.Length;
                }
                else
                    printedValue.Append(c);

                if (idx < position)
                    printedPointer.Append(' ', paddingLength);
                else if (idx == position)
                    printedPointer.AppendFormat("{0} (pos {1})", pointer, position);
            }

            if (value.Length == position)
                printedPointer.AppendFormat("{0} (pos {1})", pointer, position);

            if (end < value.Length)
                printedValue.Append("···");

            return new ShortenEncoded(printedValue.ToString(), printedPointer.ToString());
        }
    }
}