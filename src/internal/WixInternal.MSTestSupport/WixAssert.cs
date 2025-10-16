// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.MSTestSupport
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    public class WixAssert
    {
        public static void CompareLineByLine(string[] expectedLines, string[] actualLines)
        {
            var lineNumber = 0;

            for (; lineNumber < expectedLines.Length && lineNumber < actualLines.Length; ++lineNumber)
            {
                StringEqual($"{lineNumber}: {expectedLines[lineNumber]}", $"{lineNumber}: {actualLines[lineNumber]}");
            }

            var additionalExpectedLines = expectedLines.Length > lineNumber ? String.Join(Environment.NewLine, expectedLines.Skip(lineNumber).Select((s, i) => $"{lineNumber + i}: {s}")) : $"Missing {actualLines.Length - lineNumber} lines";
            var additionalActualLines = actualLines.Length > lineNumber ? String.Join(Environment.NewLine, actualLines.Skip(lineNumber).Select((s, i) => $"{lineNumber + i}: {s}")) : $"Missing {expectedLines.Length - lineNumber} lines";

            Assert.AreEqual<object>(additionalExpectedLines, additionalActualLines, StringObjectEqualityComparer.InvariantCulture);
        }

        public static void CompareXml(XContainer xExpected, XContainer xActual)
        {
            var expecteds = ComparableElements(xExpected);
            var actuals = ComparableElements(xActual);

            CompareLineByLine(expecteds.OrderBy(s => s).ToArray(), actuals.OrderBy(s => s).ToArray());
        }

        public static void CompareXml(string expectedPath, string actualPath)
        {
            var expectedDoc = XDocument.Load(expectedPath, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            var actualDoc = XDocument.Load(actualPath, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);

            CompareXml(expectedDoc, actualDoc);
        }

        private static IEnumerable<string> ComparableElements(XContainer container)
        {
            return container.Descendants().Select(x => $"{x.Name.LocalName}:{String.Join(",", x.Attributes().OrderBy(a => a.Name.LocalName).Select(a => $"{a.Name.LocalName}={ComparableAttribute(a)}"))}");
        }

        private static string ComparableAttribute(XAttribute attribute)
        {
            switch (attribute.Name.LocalName)
            {
                case "SourceFile":
                    return "<SourceFile>";
                default:
                    return attribute.Value;
            }
        }

        public static void StringCollectionEmpty(IList<string> collection)
        {
            if (collection.Count > 0)
            {
                Assert.Fail($"The collection was expected to be empty, but instead was [{Environment.NewLine}\"{String.Join($"\", {Environment.NewLine}\"", collection)}\"{Environment.NewLine}]");
            }
        }

        public static void StringEqual(string expected, string actual, bool ignoreCase = false)
        {
            WixStringEqualException.ThrowIfNotEqual(expected, actual, ignoreCase);
        }

        public static void NotStringEqual(string expected, string actual, bool ignoreCase = false)
        {
            var comparer = ignoreCase ? StringObjectEqualityComparer.InvariantCultureIgnoreCase : StringObjectEqualityComparer.InvariantCulture;
            Assert.AreNotEqual(expected, actual, comparer);
        }

        public static T Single<T>(IEnumerable<T> collection)
        {
            return collection.Single();
        }

        public static T Single<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            return collection.Where(predicate).Single();
        }

        public static void All<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            Assert.IsTrue(collection.All(predicate));
        }

        public static void Any<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            Assert.IsTrue(collection.Any(predicate));
        }

        public static void NotEmpty<T>(IEnumerable<T> collection)
        {
            Assert.IsGreaterThan(0, collection.Count());
        }

        public static void Empty<T>(IEnumerable<T> collection)
        {
            Assert.AreEqual(0, collection.Count());
        }

        public static void Empty<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            var results = collection.Where(predicate);
            Assert.AreEqual(0, results.Count());
        }
    }

    internal class StringObjectEqualityComparer : IEqualityComparer<object>
    {
        public static readonly StringObjectEqualityComparer InvariantCultureIgnoreCase = new StringObjectEqualityComparer(true);
        public static readonly StringObjectEqualityComparer InvariantCulture = new StringObjectEqualityComparer(false);

        private readonly StringComparer stringComparer;

        public StringObjectEqualityComparer(bool ignoreCase)
        {
            this.stringComparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
        }

        public new bool Equals(object x, object y)
        {
            return this.stringComparer.Equals((string)x, (string)y);
        }

        public int GetHashCode(object obj)
        {
            return this.stringComparer.GetHashCode((string)obj);
        }
    }

    public class WixStringEqualException : AssertFailedException
    {
        public WixStringEqualException(string userMessage) : base(userMessage) { }

        public static void ThrowIfNotEqual(string expected, string actual, bool ignoreCase)
        {
            var comparer = ignoreCase ? StringObjectEqualityComparer.InvariantCultureIgnoreCase : StringObjectEqualityComparer.InvariantCulture;
            if (comparer.Equals(expected, actual))
            {
                return;
            }

            var sbMessage = new StringBuilder();

            try
            {
                Assert.AreEqual(expected, actual, ignoreCase);
            }
            catch (AssertFailedException xe)
            {
                // If either string is not completely in the message, then make sure it gets in there.
                if (!xe.Message.Contains(expected) || !xe.Message.Contains(actual))
                {
                    sbMessage.AppendLine(xe.Message);
                    sbMessage.AppendLine();
                    sbMessage.AppendFormat("Expected: {0}", expected);
                    sbMessage.AppendLine();
                    sbMessage.AppendFormat("Actual:   {0}", actual);
                }
                else
                {
                    throw;
                }
            }

            throw new WixStringEqualException(sbMessage.ToString());
        }
    }
}
