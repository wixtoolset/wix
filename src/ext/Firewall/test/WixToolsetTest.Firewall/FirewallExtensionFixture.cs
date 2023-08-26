// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Firewall
{
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Firewall;
    using Xunit;

    public class FirewallExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFirewall()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4ExecFirewallExceptionsInstall_X86\t3073\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4ExecFirewallExceptionsUninstall_X86\t3073\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsInstall_X86\t3329\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsUninstall_X86\t3329\tWix4FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix4SchedFirewallExceptionsInstall_X86\t1\tWix4FWCA_X86\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix4SchedFirewallExceptionsUninstall_X86\t1\tWix4FWCA_X86\tSchedFirewallExceptionsUninstall\t",
                "Wix4FirewallException:ExampleFirewall\tExampleApp\t*\t42\t6\t[#filNdJBJmq3UCUIwmXS8x21aAsvqzk]\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tAn app-based firewall exception\t1",
                "Wix4FirewallException:fex_ZpDsnKyHlYiA24JHzvFxm3uLZ8\tExampleDefaultGatewayScope\tDefaultGateway\t4432\t6\t\t0\t2\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tdefaultGateway scope firewall exception\t1",
                "Wix4FirewallException:fex6bkfWwpiRGI.wVFx0T7W4LXIHxU\tExampleDHCPScope\tdhcp\t\t211\ttest.exe\t0\t4\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDHCP scope firewall exception\t1",
                "Wix4FirewallException:fex70IVsYNnbwiHQrEepmdTPKH8XYs\tExamplePort\tLocalSubnet\t42\t6\t\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception\t2",
                "Wix4FirewallException:fexXxaXCXXFh.UxO_BjmZxi1B1du_Q\tExampleWINSScope\twins\t6573\t6\t\t0\t1\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tWINS scope firewall exception\t1",
                "Wix4FirewallException:fexxY71H2ZBkPalv7uid1Yy4qaA_lA\tExampleDNSScope\tdns\t356\t17\t\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDNS scope firewall exception\t1",
            }, results);
        }

        [Fact]
        public void CanBuildUsingFirewallARM64()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Wix4FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4ExecFirewallExceptionsInstall_A64\t3073\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4ExecFirewallExceptionsUninstall_A64\t3073\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsInstall_A64\t3329\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4RollbackFirewallExceptionsUninstall_A64\t3329\tWix4FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix4SchedFirewallExceptionsInstall_A64\t1\tWix4FWCA_A64\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix4SchedFirewallExceptionsUninstall_A64\t1\tWix4FWCA_A64\tSchedFirewallExceptionsUninstall\t",
                "Wix4FirewallException:ExampleFirewall\tExampleApp\t*\t42\t6\t[#filNdJBJmq3UCUIwmXS8x21aAsvqzk]\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tAn app-based firewall exception\t1",
                "Wix4FirewallException:fex_ZpDsnKyHlYiA24JHzvFxm3uLZ8\tExampleDefaultGatewayScope\tDefaultGateway\t4432\t6\t\t0\t2\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tdefaultGateway scope firewall exception\t1",
                "Wix4FirewallException:fex6bkfWwpiRGI.wVFx0T7W4LXIHxU\tExampleDHCPScope\tdhcp\t\t211\ttest.exe\t0\t4\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDHCP scope firewall exception\t1",
                "Wix4FirewallException:fex70IVsYNnbwiHQrEepmdTPKH8XYs\tExamplePort\tLocalSubnet\t42\t6\t\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception\t2",
                "Wix4FirewallException:fexXxaXCXXFh.UxO_BjmZxi1B1du_Q\tExampleWINSScope\twins\t6573\t6\t\t0\t1\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tWINS scope firewall exception\t1",
                "Wix4FirewallException:fexxY71H2ZBkPalv7uid1Yy4qaA_lA\tExampleDNSScope\tdns\t356\t17\t\t0\t2147483647\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDNS scope firewall exception\t1",
            }, results);
        }

        [Fact]
        public void CanRoundtripFirewallExceptions()
        {
            var folder = TestData.Get(@"TestData", "UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, "FirewallExceptionDecompile.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var actual = doc.Descendants()
                .Where(e => e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/firewall")
                .Select(fe => new { Name = fe.Name.LocalName, Attributes = fe.Attributes().Select(a => $"{a.Name.LocalName}={a.Value}").ToArray() })
                .ToArray();

            WixAssert.CompareLineByLine(new[]
            {
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
            }, actual.Select(a => a.Name).ToArray());
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForApp()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleApp");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=ExampleFirewall",
                "Name=ExampleApp",
                "Scope=any",
                "Port=42",
                "Protocol=tcp",
                "Program=[#filNdJBJmq3UCUIwmXS8x21aAsvqzk]",
                "Profile=all",
                "Description=An app-based firewall exception",
                "Outbound=no",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForPort()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExamplePort");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex70IVsYNnbwiHQrEepmdTPKH8XYs",
                "Name=ExamplePort",
                "Scope=localSubnet",
                "Port=42",
                "Protocol=tcp",
                "Profile=all",
                "Description=A port-based firewall exception",
                "Outbound=yes",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForDNSScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleDNSScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexxY71H2ZBkPalv7uid1Yy4qaA_lA",
                "Name=ExampleDNSScope",
                "Scope=DNS",
                "Port=356",
                "Protocol=udp",
                "Profile=all",
                "Description=DNS scope firewall exception",
                "Outbound=no",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForDHCPScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleDHCPScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex6bkfWwpiRGI.wVFx0T7W4LXIHxU",
                "Name=ExampleDHCPScope",
                "Scope=DHCP",
                "Protocol=211",
                "Program=test.exe",
                "Profile=public",
                "Description=DHCP scope firewall exception",
                "Outbound=no",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall"
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForWINSScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleWINSScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexXxaXCXXFh.UxO_BjmZxi1B1du_Q",
                "Name=ExampleWINSScope",
                "Scope=WINS",
                "Port=6573",
                "Protocol=tcp",
                "Profile=domain",
                "Description=WINS scope firewall exception",
                "Outbound=no",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForDefaultGatewayScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleDefaultGatewayScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex_ZpDsnKyHlYiA24JHzvFxm3uLZ8",
                "Name=ExampleDefaultGatewayScope",
                "Scope=defaultGateway",
                "Port=4432",
                "Protocol=tcp",
                "Profile=private",
                "Description=defaultGateway scope firewall exception",
                "Outbound=no",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
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

        private static void Decompile(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
        class AttributeVerifier
        {
            public string Name { get; set; }
            public string[] Attributes { get; set; }
        }

        private static AttributeVerifier BuildAndDecompileAndBuild(string nameSpace, string ruleName)
        {
            var folder = TestData.Get(@"TestData", "UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, $"Firewall{ruleName}.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var actual = doc.Descendants()
                .Where(e => e.Name.Namespace == nameSpace)
                .Select(fe => new AttributeVerifier
                {
                    Name = fe.Attributes().Single(a => a.Name.LocalName == "Name").Value,
                    Attributes = fe.Attributes().Select(a => $"{a.Name.LocalName}={a.Value}").ToArray()
                })
                .Single(av => av.Name == ruleName);

            return actual;
        }
    }
}
