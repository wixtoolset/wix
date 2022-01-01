// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Xunit;

    public class WixAssert : Assert
    {
        public static void CompareLineByLine(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < expectedLines.Length; ++i)
            {
                Assert.True(actualLines.Length > i, $"{i}: expectedLines longer than actualLines");
                WixAssert.StringEqual($"{i}: {expectedLines[i]}", $"{i}: {actualLines[i]}");
            }

            Assert.True(expectedLines.Length == actualLines.Length, $"actualLines ({actualLines.Length}) longer than expectedLines ({expectedLines.Length})");
        }

        public static void CompareXml(XContainer xExpected, XContainer xActual)
        {
            var expecteds = xExpected.Descendants().Select(x => $"{x.Name.LocalName}:{String.Join(",", x.Attributes().OrderBy(a => a.Name.LocalName).Select(a => $"{a.Name.LocalName}={a.Value}"))}");
            var actuals = xActual.Descendants().Select(x => $"{x.Name.LocalName}:{String.Join(",", x.Attributes().OrderBy(a => a.Name.LocalName).Select(a => $"{a.Name.LocalName}={a.Value}"))}");

            CompareLineByLine(expecteds.OrderBy(s => s).ToArray(), actuals.OrderBy(s => s).ToArray());
        }

        public static void CompareXml(string expectedPath, string actualPath)
        {
            var expectedDoc = XDocument.Load(expectedPath, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            var actualDoc = XDocument.Load(actualPath, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);

            CompareXml(expectedDoc, actualDoc);
        }

        public static void Succeeded(int hr, string format, params object[] formatArgs)
        {
            if (0 > hr)
            {
                throw new SucceededException(hr, String.Format(format, formatArgs));
            }
        }

        public static void StringCollectionEmpty(IList<string> collection)
        {
            if (collection.Count > 0)
            {
                Assert.True(false, $"The collection was expected to be empty, but instead was [{Environment.NewLine}\"{String.Join($"\", {Environment.NewLine}\"", collection)}\"{Environment.NewLine}]");
            }
        }

        public static void StringEqual(string expected, string actual, bool ignoreCase = false)
        {
            var comparer = ignoreCase ? StringObjectEqualityComparer.InvariantCultureIgnoreCase : StringObjectEqualityComparer.InvariantCulture;
            Assert.Equal<object>(expected, actual, comparer);
        }

        public static void NotStringEqual(string expected, string actual, bool ignoreCase = false)
        {
            var comparer = ignoreCase ? StringObjectEqualityComparer.InvariantCultureIgnoreCase : StringObjectEqualityComparer.InvariantCulture;
            Assert.NotEqual<object>(expected, actual, comparer);
        }

        private class StringObjectEqualityComparer : IEqualityComparer<object>
        {
            public static readonly StringObjectEqualityComparer InvariantCultureIgnoreCase = new StringObjectEqualityComparer(true);
            public static readonly StringObjectEqualityComparer InvariantCulture = new StringObjectEqualityComparer(false);

            private readonly StringComparer _stringComparer;

            public StringObjectEqualityComparer(bool ignoreCase)
            {
                this._stringComparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
            }

            public new bool Equals(object x, object y)
            {
                return this._stringComparer.Equals((string)x,(string)y);
            }

            public int GetHashCode(object obj)
            {
                return this._stringComparer.GetHashCode((string)obj);
            }
        }
    }
}
