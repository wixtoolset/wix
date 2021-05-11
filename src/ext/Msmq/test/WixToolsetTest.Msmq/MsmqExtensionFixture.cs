// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Msmq
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Msmq;
    using Xunit;

    public class MsmqExtensionFixture
    {
        [Fact]
        public void CanBuildUsingMessageQueue()
        {
            var folder = TestData.Get(@"TestData\UsingMessageQueue");
            var build = new Builder(folder, typeof(MsmqExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "MessageQueue");
            Assert.Equal(new[]
            {
                "MessageQueue:TestMQ\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\t\t\tMQLabel\t\tMQPath\t\t\t\t0",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
