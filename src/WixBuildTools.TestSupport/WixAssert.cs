// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using Xunit;

    public class WixAssert : Assert
    {
        public static void AssertEqualLineByLine(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < expectedLines.Length; ++i)
            {
                Assert.True(actualLines.Length > i, $"{i}: expectedLines longer than actualLines");
                Assert.Equal($"{i}: {expectedLines[i]}", $"{i}: {actualLines[i]}");
            }
            Assert.True(expectedLines.Length == actualLines.Length, "actualLines longer than expectedLines");
        }

        public static void Succeeded(int hr, string format, params object[] formatArgs)
        {
            if (0 > hr)
            {
                throw new SucceededException(hr, String.Format(format, formatArgs));
            }
        }
    }
}
