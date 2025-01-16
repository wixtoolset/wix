// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Msmq
{
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Msmq;
    using WixToolset.Util;
    using Xunit;

    public class MsmqExtensionFixture
    {
        [Fact]
        public void CanBuildUsingMessageQueue()
        {
            var folder = TestData.Get(@"TestData\UsingMessageQueue");
            var build = new Builder(folder, new[] { typeof(MsmqExtensionFactory), typeof(UtilExtensionFactory) }, new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4MessageQueue", "CustomAction", "Wix4MessageQueueUserPermission", "Wix4MessageQueueGroupPermission", "Wix4Group", "Wix4User");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4MessageQueuingExecuteInstall_A64\t3073\tWix4MsmqCA_A64\tMessageQueuingExecuteInstall\t",
                "CustomAction:Wix4MessageQueuingExecuteUninstall_A64\t3073\tWix4MsmqCA_A64\tMessageQueuingExecuteUninstall\t",
                "CustomAction:Wix4MessageQueuingInstall_A64\t1\tWix4MsmqCA_A64\tMessageQueuingInstall\t",
                "CustomAction:Wix4MessageQueuingRollbackInstall_A64\t3329\tWix4MsmqCA_A64\tMessageQueuingRollbackInstall\t",
                "CustomAction:Wix4MessageQueuingRollbackUninstall_A64\t3329\tWix4MsmqCA_A64\tMessageQueuingRollbackUninstall\t",
                "CustomAction:Wix4MessageQueuingUninstall_A64\t1\tWix4MsmqCA_A64\tMessageQueuingUninstall\t",
                "Wix4Group:TestGroup\t\tTestGroup\t",
                "Wix4MessageQueue:TestMQ\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\t\t\tMQLabel\t\tMQPath\t\t\t\t0",
                "Wix4MessageQueueGroupPermission:TestMQ_TestGroup\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTestMQ\tTestGroup\t160",
                "Wix4MessageQueueUserPermission:TestMQ_TestUser\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTestMQ\tTestUser\t160",
                "Wix4User:TestUser\t\tTestUser\t\t\t\t0",
            }, results);
        }

        private static void Build(string[] args)
        {
            args = args.Concat(new[] { "-arch", "arm64" }).ToArray();

            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
