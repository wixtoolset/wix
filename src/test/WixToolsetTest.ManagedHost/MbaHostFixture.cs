// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using WixBuildTools.TestSupport;
    using Xunit;

    public class MbaHostFixture
    {
        [Fact]
        public void CanLoadFullFramework2MBA()
        {
            var testEngine = new TestEngine();
            var baFile = TestData.Get(@"..\examples\Example.FullFramework2MBA\mbahost.dll");

            var result = testEngine.RunShutdownEngine(baFile);
            Assert.Equal(0, result.ExitCode);

            var logMessages = result.Output;
            Assert.Equal(2, logMessages.Count);
            Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
            Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[1]);
        }

        [Fact]
        public void CanLoadFullFramework4MBA()
        {
            var testEngine = new TestEngine();
            var baFile = TestData.Get(@"..\examples\Example.FullFramework4MBA\net48\mbahost.dll");

            var result = testEngine.RunShutdownEngine(baFile);
            Assert.Equal(0, result.ExitCode);

            var logMessages = result.Output;
            Assert.Equal(2, logMessages.Count);
            Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
            Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[1]);
        }
    }
}
