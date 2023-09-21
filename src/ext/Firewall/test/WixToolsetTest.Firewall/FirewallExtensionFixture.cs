// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Firewall
{
    using System.Data;
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

            var results = build.BuildAndQuery(Build, "Wix5FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix5ExecFirewallExceptionsInstall_X86\t3073\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5ExecFirewallExceptionsUninstall_X86\t3073\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsInstall_X86\t3329\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsUninstall_X86\t3329\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5SchedFirewallExceptionsInstall_X86\t1\tWix5FWCA_X86\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix5SchedFirewallExceptionsUninstall_X86\t1\tWix5FWCA_X86\tSchedFirewallExceptionsUninstall\t",
                "Wix5FirewallException:ExampleFirewall\tExampleApp\t*\t42\t6\t[#filNdJBJmq3UCUIwmXS8x21aAsvqzk]\t2\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tAn app-based firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex.BGtyMRGAhxb2hG.49JvWYz7fM0\tLocalScopeExample2\t*\t\t-2147483648\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with local scope property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t[LOCALSCOPE_PROP]\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex0HTxATWjpC2PCoY6DB7f2D1WaKU\tLocalScopeExample1\t*\t\t-2147483648\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with local scope\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\tLocalSubnet\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex4FeP470wYcFpw.g7fbIKiLnZPzg\tExampleDNSScope\tdns\t356\t17\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDNS scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex4zTcT0Iwu3dUtHIHXD5qfymvpcM\tdefertouser\t\t\t-2147483648\tfw.exe\t8\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDefer to user edge traversal\t1\t-2147483648\t3\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex8vMfBplrod4daEz3PqDTeX6olGE\tExampleDefaultGatewayScope\tDefaultGateway\t4432\t6\t\t0\t2\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tdefaultGateway scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexAMmHzFDyQmubTOnKS1Cn0Y3q_Ug\tINetFwRule3 properties\t*\t\t-2147483648\t\t16\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tINetFwRule3 passed via properties\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t[PROP1]\t[PROP2]\t[PROP3]\t[PROP4]\t[PROP5]\t[PROP6]",
                "Wix5FirewallException:fexArlOkFR7CAwVZ2wk8yNdiREydu0\tRemotePortExample2\t\t\t6\tfw.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with remote port property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t[REMOTEPORT_PROP]\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexaUTe2tRRcSYrPUTn44DAZhE.40Q\tExamplePort\tLocalSubnet\t42\t6\t\t4\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception\t2\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexD6w20c5HfNi4l1vHFj_eet4cC8I\tExampleWINSScope\twins\t6573\t6\t\t0\t1\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tWINS scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexeD3yox6fMflfRy7sDwSN2CMCS2s\tExampleService\t\t12000\t6\t%windir%\\system32\\svchost.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based service exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\tDHCP,WINS\t\tftpsrv\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexeok6aI2_AlclZggec4d8PBLFXLw\tinterface property\t\t54671\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tInterfaces with property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE_PROPERTY]\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexEPvcf4iexD1mVQdvxm7tD02nZEc\tICMPExample1\t\t\t2\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple ICMP rule\t1\t-2147483648\t-2147483648\t-2147483648\t\t4:*,9:*,12:*\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexfzjTQsWwZkHQpObtl0XaUosfcRk\tGroupingExample1\t\t\t-2147483648\tfw.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with grouping\t1\t-2147483648\t-2147483648\t-2147483648\t@yourresources.dll,-1005\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexHx2xbwZYzAi0oYp4YGWevJQs5eM\tRemotePortExample1\t*\t\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with remote port\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t34560\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexpWUzK53RVnaluW36gSmphPRY8VY\tExampleDHCPScope\tdhcp\t\t211\ttest.exe\t0\t4\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDHCP scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexuanTga5xaaFzr9JsAnUmpCNediw\tICMPExample2\t\t\t2\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with ICMP property\t1\t-2147483648\t-2147483648\t-2147483648\t\t[ICMP_PROP]\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexv60s7u2Dmd1imH5vEFYKPgEWhG4\tinterface nested\t127.0.0.1\t54671\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tInterfaces with nested elements\t1\t-2147483648\t-2147483648\t-2147483648\t\t\tWi-Fi|Local Area Connection\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexVr6uHcOCak5MHuTLwujjh_oKtbI\tGroupingExample2\t\t8732\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with grouping property\t1\t-2147483648\t-2147483648\t-2147483648\t[GROUPING_PROP]\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexwjf4OTFVE9SNiC4goVxBA6ENJBE\tINetFwRule3 values\t*\t\t-2147483648\t\t16\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple INetFwRule3 values\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\tS-1-15-2-1239072475-3687740317-1842961305-3395936705-4023953123-1525404051-2779347315\tO:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)\tS-1-5-21-1898747406-2352535518-1247798438-1914\t127.0.0.1\tO:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)\t3",
                "Wix5FirewallException:ServiceInstall.nested\tExampleNestedService\tLocalSubnet\t3546-7890\t6\t\t1\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception for a windows service\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\tLan,Wireless\t\t\tsvc1\t\t\t\t\t\t-2147483648",
            }, results);
        }

        [Fact]
        public void CanBuildUsingFirewallARM64()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Wix5FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix5ExecFirewallExceptionsInstall_A64\t3073\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5ExecFirewallExceptionsUninstall_A64\t3073\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsInstall_A64\t3329\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsUninstall_A64\t3329\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5SchedFirewallExceptionsInstall_A64\t1\tWix5FWCA_A64\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix5SchedFirewallExceptionsUninstall_A64\t1\tWix5FWCA_A64\tSchedFirewallExceptionsUninstall\t",
                "Wix5FirewallException:ExampleFirewall\tExampleApp\t*\t42\t6\t[#filNdJBJmq3UCUIwmXS8x21aAsvqzk]\t2\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tAn app-based firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex.BGtyMRGAhxb2hG.49JvWYz7fM0\tLocalScopeExample2\t*\t\t-2147483648\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with local scope property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t[LOCALSCOPE_PROP]\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex0HTxATWjpC2PCoY6DB7f2D1WaKU\tLocalScopeExample1\t*\t\t-2147483648\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with local scope\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\tLocalSubnet\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex4FeP470wYcFpw.g7fbIKiLnZPzg\tExampleDNSScope\tdns\t356\t17\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDNS scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex4zTcT0Iwu3dUtHIHXD5qfymvpcM\tdefertouser\t\t\t-2147483648\tfw.exe\t8\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDefer to user edge traversal\t1\t-2147483648\t3\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fex8vMfBplrod4daEz3PqDTeX6olGE\tExampleDefaultGatewayScope\tDefaultGateway\t4432\t6\t\t0\t2\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tdefaultGateway scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexAMmHzFDyQmubTOnKS1Cn0Y3q_Ug\tINetFwRule3 properties\t*\t\t-2147483648\t\t16\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tINetFwRule3 passed via properties\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t[PROP1]\t[PROP2]\t[PROP3]\t[PROP4]\t[PROP5]\t[PROP6]",
                "Wix5FirewallException:fexArlOkFR7CAwVZ2wk8yNdiREydu0\tRemotePortExample2\t\t\t6\tfw.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with remote port property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t[REMOTEPORT_PROP]\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexaUTe2tRRcSYrPUTn44DAZhE.40Q\tExamplePort\tLocalSubnet\t42\t6\t\t4\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception\t2\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexD6w20c5HfNi4l1vHFj_eet4cC8I\tExampleWINSScope\twins\t6573\t6\t\t0\t1\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tWINS scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexeD3yox6fMflfRy7sDwSN2CMCS2s\tExampleService\t\t12000\t6\t%windir%\\system32\\svchost.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based service exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\tDHCP,WINS\t\tftpsrv\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexeok6aI2_AlclZggec4d8PBLFXLw\tinterface property\t\t54671\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tInterfaces with property\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE_PROPERTY]\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexEPvcf4iexD1mVQdvxm7tD02nZEc\tICMPExample1\t\t\t2\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple ICMP rule\t1\t-2147483648\t-2147483648\t-2147483648\t\t4:*,9:*,12:*\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexfzjTQsWwZkHQpObtl0XaUosfcRk\tGroupingExample1\t\t\t-2147483648\tfw.exe\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with grouping\t1\t-2147483648\t-2147483648\t-2147483648\t@yourresources.dll,-1005\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexHx2xbwZYzAi0oYp4YGWevJQs5eM\tRemotePortExample1\t*\t\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple rule with remote port\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t34560\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexpWUzK53RVnaluW36gSmphPRY8VY\tExampleDHCPScope\tdhcp\t\t211\ttest.exe\t0\t4\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tDHCP scope firewall exception\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexuanTga5xaaFzr9JsAnUmpCNediw\tICMPExample2\t\t\t2\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with ICMP property\t1\t-2147483648\t-2147483648\t-2147483648\t\t[ICMP_PROP]\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexv60s7u2Dmd1imH5vEFYKPgEWhG4\tinterface nested\t127.0.0.1\t54671\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tInterfaces with nested elements\t1\t-2147483648\t-2147483648\t-2147483648\t\t\tWi-Fi|Local Area Connection\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexVr6uHcOCak5MHuTLwujjh_oKtbI\tGroupingExample2\t\t8732\t6\t\t0\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tRule with grouping property\t1\t-2147483648\t-2147483648\t-2147483648\t[GROUPING_PROP]\t\t\t\t\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexwjf4OTFVE9SNiC4goVxBA6ENJBE\tINetFwRule3 values\t*\t\t-2147483648\t\t16\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tSimple INetFwRule3 values\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\t\t\t\t\tS-1-15-2-1239072475-3687740317-1842961305-3395936705-4023953123-1525404051-2779347315\tO:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)\tS-1-5-21-1898747406-2352535518-1247798438-1914\t127.0.0.1\tO:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)\t3",
                "Wix5FirewallException:ServiceInstall.nested\tExampleNestedService\tLocalSubnet\t3546-7890\t6\t\t1\t-2147483648\tfilNdJBJmq3UCUIwmXS8x21aAsvqzk\tA port-based firewall exception for a windows service\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t\tLan,Wireless\t\t\tsvc1\t\t\t\t\t\t-2147483648",
            }, results);
        }

        [Fact]
        public void CanBuildWithProperties()
        {
            var folder = TestData.Get(@"TestData\UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix5FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix5ExecFirewallExceptionsInstall_X86\t3073\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5ExecFirewallExceptionsUninstall_X86\t3073\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsInstall_X86\t3329\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsUninstall_X86\t3329\tWix5FWCA_X86\tExecFirewallExceptions\t",
                "CustomAction:Wix5SchedFirewallExceptionsInstall_X86\t1\tWix5FWCA_X86\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix5SchedFirewallExceptionsUninstall_X86\t1\tWix5FWCA_X86\tSchedFirewallExceptionsUninstall\t",
                "Wix5FirewallException:fexRrE4bS.DwUJQMvzX0ALEsx7jrZs\tSingle Nested properties\t[REMOTEADDRESS]\t\t-2147483648\t\t0\t-2147483648\tFirewallComponent\t\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE]\t[INTERFACETYPE]\t[LOCALADDRESS]\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexvEy1GfdOjHlKcvsguyqK6mvYKyk\t[NAME]\t[REMOTESCOPE]\t[LOCALPORT]\t[PROTOCOL]\t[PROGRAM]\t16\t[PROFILE]\tFirewallComponent\t[DESCRIPTION]\t1\t[ACTION]\t[EDGETRAVERSAL]\t[ENABLED]\t[GROUPING]\t[ICMPTYPES]\t[INTERFACE]\t[INTERFACETYPE]\t[LOCALSCOPE]\t[REMOTEPORT]\t[SERVICE]\t[PACKAGEID]\t[LOCALUSERS]\t[LOCALOWNER]\t[REMOTEMACHINES]\t[REMOTEUSERS]\t[SECUREFLAGS]",
                "Wix5FirewallException:fexWywW3VGiEuG23FOv1YM6h7R6F5Q\tMultiple Nested properties\t[REMOTEADDRESS1],[REMOTEADDRESS2]\t\t-2147483648\t\t0\t-2147483648\tFirewallComponent\t\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE1]|[INTERFACE2]\t[INTERFACETYPE1],[INTERFACETYPE2]\t[LOCALADDRESS1],[LOCALADDRESS2]\t\t\t\t\t\t\t\t-2147483648",
            }, results);
        }

        [Fact]
        public void CanBuildWithPropertiesUsingFirewallARM64()
        {
            var folder = TestData.Get(@"TestData\UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Wix5FirewallException", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix5ExecFirewallExceptionsInstall_A64\t3073\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5ExecFirewallExceptionsUninstall_A64\t3073\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsInstall_A64\t3329\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5RollbackFirewallExceptionsUninstall_A64\t3329\tWix5FWCA_A64\tExecFirewallExceptions\t",
                "CustomAction:Wix5SchedFirewallExceptionsInstall_A64\t1\tWix5FWCA_A64\tSchedFirewallExceptionsInstall\t",
                "CustomAction:Wix5SchedFirewallExceptionsUninstall_A64\t1\tWix5FWCA_A64\tSchedFirewallExceptionsUninstall\t",
                "Wix5FirewallException:fexRrE4bS.DwUJQMvzX0ALEsx7jrZs\tSingle Nested properties\t[REMOTEADDRESS]\t\t-2147483648\t\t0\t-2147483648\tFirewallComponent\t\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE]\t[INTERFACETYPE]\t[LOCALADDRESS]\t\t\t\t\t\t\t\t-2147483648",
                "Wix5FirewallException:fexvEy1GfdOjHlKcvsguyqK6mvYKyk\t[NAME]\t[REMOTESCOPE]\t[LOCALPORT]\t[PROTOCOL]\t[PROGRAM]\t16\t[PROFILE]\tFirewallComponent\t[DESCRIPTION]\t1\t[ACTION]\t[EDGETRAVERSAL]\t[ENABLED]\t[GROUPING]\t[ICMPTYPES]\t[INTERFACE]\t[INTERFACETYPE]\t[LOCALSCOPE]\t[REMOTEPORT]\t[SERVICE]\t[PACKAGEID]\t[LOCALUSERS]\t[LOCALOWNER]\t[REMOTEMACHINES]\t[REMOTEUSERS]\t[SECUREFLAGS]",
                "Wix5FirewallException:fexWywW3VGiEuG23FOv1YM6h7R6F5Q\tMultiple Nested properties\t[REMOTEADDRESS1],[REMOTEADDRESS2]\t\t-2147483648\t\t0\t-2147483648\tFirewallComponent\t\t1\t-2147483648\t-2147483648\t-2147483648\t\t\t[INTERFACE1]|[INTERFACE2]\t[INTERFACETYPE1],[INTERFACETYPE2]\t[LOCALADDRESS1],[LOCALADDRESS2]\t\t\t\t\t\t\t\t-2147483648",
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
                "FirewallException",
                "FirewallException",
                "LocalAddress",
                "LocalAddress",
                "FirewallException",
                "RemoteAddress",
                "Interface",
                "Interface",
                "FirewallException",
                "FirewallException",
                "InterfaceType",
                "InterfaceType",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
                "FirewallException",
            }, actual.Select(a => a.Name).ToArray());
        }

        [Fact]
        public void CanRoundtripFirewallExceptionsWithProperties()
        {
            var folder = TestData.Get(@"TestData", "UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, "FirewallPropertiesDecompile.xml");

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
                "RemoteAddress",
                "RemoteAddress",
                "Interface",
                "Interface",
                "InterfaceType",
                "InterfaceType",
                "LocalAddress",
                "LocalAddress",
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
                "OnUpdate=DoNothing",
                "Description=An app-based firewall exception",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForPort()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExamplePort");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexaUTe2tRRcSYrPUTn44DAZhE.40Q",
                "Name=ExamplePort",
                "Scope=localSubnet",
                "Port=42",
                "Protocol=tcp",
                "OnUpdate=EnableOnly",
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
                "Id=fex4FeP470wYcFpw.g7fbIKiLnZPzg",
                "Name=ExampleDNSScope",
                "Scope=DNS",
                "Port=356",
                "Protocol=udp",
                "Description=DNS scope firewall exception",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForDHCPScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleDHCPScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexpWUzK53RVnaluW36gSmphPRY8VY",
                "Name=ExampleDHCPScope",
                "Scope=DHCP",
                "Protocol=211",
                "Program=test.exe",
                "Profile=public",
                "Description=DHCP scope firewall exception",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall"
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForWINSScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleWINSScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexD6w20c5HfNi4l1vHFj_eet4cC8I",
                "Name=ExampleWINSScope",
                "Scope=WINS",
                "Port=6573",
                "Protocol=tcp",
                "Profile=domain",
                "Description=WINS scope firewall exception",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForDefaultGatewayScope()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ExampleDefaultGatewayScope");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex8vMfBplrod4daEz3PqDTeX6olGE",
                "Name=ExampleDefaultGatewayScope",
                "Scope=defaultGateway",
                "Port=4432",
                "Protocol=tcp",
                "Profile=private",
                "Description=defaultGateway scope firewall exception",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForINetFwRule3Values()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "INetFwRule3 values");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexwjf4OTFVE9SNiC4goVxBA6ENJBE",
                "Name=INetFwRule3 values",
                "Scope=any",
                "Description=Simple INetFwRule3 values",
                "LocalAppPackageId=S-1-15-2-1239072475-3687740317-1842961305-3395936705-4023953123-1525404051-2779347315",
                "LocalUserAuthorizedList=O:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)",
                "LocalUserOwner=S-1-5-21-1898747406-2352535518-1247798438-1914",
                "RemoteMachineAuthorizedList=127.0.0.1",
                "RemoteUserAuthorizedList=O:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)",
                "IPSecSecureFlags=NegotiateEncryption",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForINetFwRule3Properties()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "INetFwRule3 properties");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexAMmHzFDyQmubTOnKS1Cn0Y3q_Ug",
                "Name=INetFwRule3 properties",
                "Scope=any",
                "Description=INetFwRule3 passed via properties",
                "LocalAppPackageId=[PROP1]",
                "LocalUserAuthorizedList=[PROP2]",
                "LocalUserOwner=[PROP3]",
                "RemoteMachineAuthorizedList=[PROP4]",
                "RemoteUserAuthorizedList=[PROP5]",
                "IPSecSecureFlags=[PROP6]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForGroupingValue()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "GroupingExample1");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexfzjTQsWwZkHQpObtl0XaUosfcRk",
                "Name=GroupingExample1",
                "Program=fw.exe",
                "Description=Simple rule with grouping",
                "Grouping=@yourresources.dll,-1005",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForGroupingProperty()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "GroupingExample2");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexVr6uHcOCak5MHuTLwujjh_oKtbI",
                "Name=GroupingExample2",
                "Port=8732",
                "Protocol=tcp",
                "Description=Rule with grouping property",
                "Grouping=[GROUPING_PROP]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForIcmpValue()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ICMPExample1");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexEPvcf4iexD1mVQdvxm7tD02nZEc",
                "Name=ICMPExample1",
                "Protocol=2",
                "Description=Simple ICMP rule",
                "IcmpTypesAndCodes=4:*,9:*,12:*",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForIcmpProperty()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "ICMPExample2");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexuanTga5xaaFzr9JsAnUmpCNediw",
                "Name=ICMPExample2",
                "Protocol=2",
                "Description=Rule with ICMP property",
                "IcmpTypesAndCodes=[ICMP_PROP]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForLocalScopeValue()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "LocalScopeExample1");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex0HTxATWjpC2PCoY6DB7f2D1WaKU",
                "Name=LocalScopeExample1",
                "Scope=any",
                "Description=Simple rule with local scope",
                "LocalScope=localSubnet",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForLocalScopeProperty()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "LocalScopeExample2");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fex.BGtyMRGAhxb2hG.49JvWYz7fM0",
                "Name=LocalScopeExample2",
                "Scope=any",
                "Description=Rule with local scope property",
                "LocalScope=[LOCALSCOPE_PROP]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForRemotePorts()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "RemotePortExample1");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexHx2xbwZYzAi0oYp4YGWevJQs5eM",
                "Name=RemotePortExample1",
                "Scope=any",
                "Protocol=tcp",
                "Description=Simple rule with remote port",
                "RemotePort=34560",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectForRemotePortsProperty()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "RemotePortExample2");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexArlOkFR7CAwVZ2wk8yNdiREydu0",
                "Name=RemotePortExample2",
                "Protocol=tcp",
                "Program=fw.exe",
                "Description=Rule with remote port property",
                "RemotePort=[REMOTEPORT_PROP]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall",
            }, actual.Attributes);
        }

        [Fact]
        public void RoundtripAttributesAreCorrectWhenPropertiesAreUsed()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "[NAME]", "UsingProperties");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexvEy1GfdOjHlKcvsguyqK6mvYKyk",
                "Name=[NAME]",
                "Scope=[REMOTESCOPE]",
                "Port=[LOCALPORT]",
                "Protocol=[PROTOCOL]",
                "Program=[PROGRAM]",
                "Profile=[PROFILE]",
                "Description=[DESCRIPTION]",
                "Action=[ACTION]",
                "EdgeTraversal=[EDGETRAVERSAL]",
                "Enabled=[ENABLED]",
                "Grouping=[GROUPING]",
                "IcmpTypesAndCodes=[ICMPTYPES]",
                "Interface=[INTERFACE]",
                "InterfaceType=[INTERFACETYPE]",
                "LocalScope=[LOCALSCOPE]",
                "RemotePort=[REMOTEPORT]",
                "Service=[SERVICE]",
                "LocalAppPackageId=[PACKAGEID]",
                "LocalUserAuthorizedList=[LOCALUSERS]",
                "LocalUserOwner=[LOCALOWNER]",
                "RemoteMachineAuthorizedList=[REMOTEMACHINES]",
                "RemoteUserAuthorizedList=[REMOTEUSERS]",
                "IPSecSecureFlags=[SECUREFLAGS]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall"
            }, actual.Attributes);

            var folder = TestData.Get(@"TestData", "UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, $"FirewallNothingNested.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var related = doc.Descendants()
                .Where(e =>
                {
                    return e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/firewall" &&
                    e.Parent.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "[NAME]");
                });

            var nested = related.Select(e => e.Attributes().Single(a => a.Name.LocalName == "Name").Value);
            Assert.False(nested.Any());
        }

        [Fact]
        public void RoundtripAttributesAreCorrectWhenNestedPropertiesAreUsed()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "Single Nested properties", "UsingProperties");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexRrE4bS.DwUJQMvzX0ALEsx7jrZs",
                "Name=Single Nested properties",
                "Scope=[REMOTEADDRESS]",
                "Interface=[INTERFACE]",
                "InterfaceType=[INTERFACETYPE]",
                "LocalScope=[LOCALADDRESS]",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall"
            }, actual.Attributes);

            var folder = TestData.Get(@"TestData", "UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, $"FirewallSingleNested.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var related = doc.Descendants()
                .Where(e =>
                {
                    return e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/firewall" &&
                    e.Parent.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "Single Nested properties");
                });

            var nested = related.Select(e => e.Attributes().Single(a => a.Name.LocalName == "Name").Value);
            Assert.False(nested.Any());
        }

        [Fact]
        public void RoundtripAttributesAreCorrectWhenMultipleNestedPropertiesAreUsed()
        {
            var actual = BuildAndDecompileAndBuild("http://wixtoolset.org/schemas/v4/wxs/firewall", "Multiple Nested properties", "UsingProperties");
            WixAssert.CompareLineByLine(new[]
            {
                "Id=fexWywW3VGiEuG23FOv1YM6h7R6F5Q",
                "Name=Multiple Nested properties",
                "xmlns=http://wixtoolset.org/schemas/v4/wxs/firewall"
            }, actual.Attributes);

            var folder = TestData.Get(@"TestData", "UsingProperties");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, $"FirewallMultipleNested.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var related = doc.Descendants()
                .Where(e =>
                {
                    return e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/firewall" &&
                    e.Parent.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "Multiple Nested properties");
                });

            var interfaces = related.Where(e => e.Name.LocalName == "Interface")
                .Select(e => e.Attributes().Single(a => a.Name.LocalName == "Name").Value);
            WixAssert.CompareLineByLine(new[]
            {
                "[INTERFACE1]",
                "[INTERFACE2]",
            }, interfaces.ToArray());

            var interfaceTypes = related.Where(e => e.Name.LocalName == "InterfaceType")
                .Select(e => e.Attributes().Single(a => a.Name.LocalName == "Value").Value);
            WixAssert.CompareLineByLine(new[]
            {
                "[INTERFACETYPE1]",
                "[INTERFACETYPE2]",
            }, interfaceTypes.ToArray());

            var remotes = related.Where(e => e.Name.LocalName == "RemoteAddress")
                .Select(e => e.Attributes().Single(a => a.Name.LocalName == "Value").Value);
            WixAssert.CompareLineByLine(new[]
            {
                "[REMOTEADDRESS1]",
                "[REMOTEADDRESS2]",
            }, remotes.ToArray());

            var locals = related.Where(e => e.Name.LocalName == "LocalAddress")
                .Select(e => e.Attributes().Single(a => a.Name.LocalName == "Value").Value);
            WixAssert.CompareLineByLine(new[]
            {
                "[LOCALADDRESS1]",
                "[LOCALADDRESS2]",
            }, locals.ToArray());
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

        private static AttributeVerifier BuildAndDecompileAndBuild(string nameSpace, string ruleName, string path = "UsingFirewall")
        {
            var folder = TestData.Get(@"TestData", path);
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, $"Firewall{ruleName}.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var actual = doc.Descendants()
                .Where(e =>
                {
                    return e.Name.Namespace == nameSpace && e.Name.LocalName == "FirewallException";
                })
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
