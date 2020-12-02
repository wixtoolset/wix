// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using Xunit.Abstractions;

    public abstract class WixTestBase
    {
        protected WixTestBase(ITestOutputHelper testOutputHelper, string testGroupName)
        {
            this.TestContext = new WixTestContext(testOutputHelper, testGroupName);
        }

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public WixTestContext TestContext { get; }
    }
}
