// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Msmq
{
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixInternal.MSTestSupport;
    using WixInternal.Core.MSTestPackage;
    using WixToolset.Msmq;

    [TestClass]
    public class MsmqExtensionFixture
    {
        [TestMethod]
        public void CanBuildUsingMessageQueue()
        {
            var folder = TestData.Get(@"TestData\UsingMessageQueue");
            var build = new Builder(folder, new[] { typeof(MsmqExtensionFactory) }, new[] { folder });

            var results = build.BuildAndQuery(BuildWithUtil, "Wix4MessageQueue", "CustomAction", "Wix4MessageQueueUserPermission", "Wix4MessageQueueGroupPermission", "Wix4Group", "Wix4User");
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

        [TestMethod]
        [Ignore("Util:Wix4Group and Util:Wix6Group decompilation issues prevent this usage currently")]
        public void CanRoundtripMessageQueue()
        {
            var folder = TestData.Get(@"TestData\UsingMessageQueue");
            var build = new Builder(folder, new[] { typeof(MsmqExtensionFactory) }, new[] { folder });
            var output = Path.Combine(folder, "MessageQueueDecompile.xml");

            build.BuildAndDecompileAndBuild(BuildWithUtil, DecompileWithUtil, output);

            var doc = XDocument.Load(output);
            var actual = doc.Descendants()
                .Where(e => e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/msmq")
                .Select(fe => new { Name = fe.Name.LocalName, Id = fe.Attributes().Where(a => a.Name == "Id").Select(a => a.Value).FirstOrDefault() })
                .ToArray();

            WixAssert.CompareLineByLine(new[]
            {
                "MessageQueue:TestMQ",
                "MessageQueuePermission:TestMQ_TestUser",
                "MessageQueuePermission:TestMQ_TestGroup",
            }, actual.Select(a => $"{a.Name}:{a.Id}").ToArray());
        }

        private static void BuildWithUtil(string[] args)
        {
            var extensionResult = WixRunner.Execute(warningsAsErrors: true, new[]
                {
                    "extension", "add",
                    "WixToolset.Util.wixext",
                });

            args = args.Concat(new[]
            {
                "-ext", "WixToolset.Util.wixext",
                "-arch", "arm64"
            }).ToArray();

            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void DecompileWithUtil(string[] args)
        {
            args = args.Concat(new[]
            {
                "-ext", "WixToolset.Util.wixext",
            }).ToArray();

            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
