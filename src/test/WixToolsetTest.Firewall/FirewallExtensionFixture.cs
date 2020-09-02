// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Firewall
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Firewall;
    using Xunit;

    public class FirewallExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFirewall()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "WixFirewallException", "CustomAction");
            Assert.Equal(new[]
            {
                "CustomAction:Wix4ExecFirewallExceptionsInstall_X86\t3073\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4ExecFirewallExceptionsUninstall_X86\t3073\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsInstall_X86\t3329\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsUninstall_X86\t3329\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4SchedFirewallExceptionsInstall_X86\t1\tWix4FWCA_X86\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix4SchedFirewallExceptionsUninstall_X86\t1\tWix4FWCA_X86\tSchedFirewallExceptionsUninstall\t",
                "WixFirewallException:ExampleFirewall\texample\t*\t42\t6\t\t0\t2147483647\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example firewall\t1",
            }, results);
        }

        [Fact]
        public void CanBuildUsingFirewallARM64()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "WixFirewallException", "CustomAction");
            Assert.Equal(new[]
            {
                "CustomAction:Wix4ExecFirewallExceptionsInstall_A64\t3073\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4ExecFirewallExceptionsUninstall_A64\t3073\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsInstall_A64\t3329\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsUninstall_A64\t3329\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4SchedFirewallExceptionsInstall_A64\t1\tWix4FWCA_A64\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix4SchedFirewallExceptionsUninstall_A64\t1\tWix4FWCA_A64\tSchedFirewallExceptionsUninstall\t",
                "WixFirewallException:ExampleFirewall\texample\t*\t42\t6\t\t0\t2147483647\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example firewall\t1",
            }, results);
        }

        [Fact]
        public void CanBuildUsingOutboundFirewall()
        {
            var folder = TestData.Get(@"TestData\UsingOutboundFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "WixFirewallException");
            Assert.Equal(new[]
            {
                "WixFirewallException:fex.5c8b_4C0THcQTvn8tpwhoRrgck\texample\t*\t42\t6\t\t0\t2147483647\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example outbound firewall\t2",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("arm64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }
    }
}
