// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using Xunit.Abstractions;

    public abstract class WixTestBase
    {
        protected WixTestBase(ITestOutputHelper testOutputHelper)
        {
            this.TestContext = new WixTestContext(testOutputHelper);
        }

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public WixTestContext TestContext { get; }
    }
}
