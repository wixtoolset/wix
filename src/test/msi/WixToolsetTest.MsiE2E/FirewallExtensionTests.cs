// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using NetFwTypeLib;
    using WixTestTools;
    using WixTestTools.Firewall;
    using Xunit;
    using Xunit.Abstractions;

    public class FirewallExtensionTests : MsiE2ETests
    {
        public FirewallExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void VerifierSelfTest()
        {
            foreach (var expected in Verifier.GetFirewallRules())
            {
                var check = new UniqueCheck(expected);
                Verifier.VerifyFirewallRule(expected.Name, expected, check);
            }
        }

        [RuntimeFact]
        public void CanInstallAndUninstallFirewallRulesWithMinimalProperties()
        {
            var product = this.CreatePackageInstaller("FirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate new firewall exception details.
            var expected1 = new RuleDetails("WiXToolset401 Test - 0001")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRules", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - minimal app properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 256,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0001", expected1);

            var expected2 = new RuleDetails("WiXToolset401 Test - 0002")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - minimal port properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "23456",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0002", expected2);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0001"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0002"));
        }

        [RuntimeFact]
        public void DisabledPortFirewallRuleIsEnabledAfterRepair()
        {
            var product = this.CreatePackageInstaller("FirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Verifier.DisableFirewallRule("WiXToolset401 Test - 0002");

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected = new RuleDetails("WiXToolset401 Test - 0002")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - minimal port properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "23456",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0002", expected);
        }

        [RuntimeFact]
        public void DisabledApplicationFirewallRuleIsEnabledAfterRepair()
        {
            var product = this.CreatePackageInstaller("FirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Verifier.DisableFirewallRule("WiXToolset401 Test - 0001");

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected = new RuleDetails("WiXToolset401 Test - 0001")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRules", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - minimal app properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 256,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0001", expected);
        }

        [RuntimeFact]
        public void MissingPortFirewallRuleIsAddedAfterRepair()
        {
            var product = this.CreatePackageInstaller("FirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Verifier.RemoveFirewallRuleByName("WiXToolset401 Test - 0002");
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0002"));

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected = new RuleDetails("WiXToolset401 Test - 0002")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - minimal port properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "23456",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0002", expected);
        }

        [RuntimeFact]
        public void MissingApplicationFirewallRuleIsAddedAfterRepair()
        {
            var product = this.CreatePackageInstaller("FirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Verifier.RemoveFirewallRuleByName("WiXToolset401 Test - 0001");
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0001"));

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected = new RuleDetails("WiXToolset401 Test - 0001")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRules", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - minimal app properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 256,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0001", expected);
        }

        [RuntimeFact]
        public void FirewallRulesUseFormattedStringProperties()
        {
            var product = this.CreatePackageInstaller("DynamicFirewallRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset401 Test - 0003")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("DynamicFirewallRules", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - dynamic app description 9999",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "LocalSubnet",
                SecureFlags = 0,
                LocalPorts = "9999",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0003", expected1);

            var expected2 = new RuleDetails("WiXToolset401 Test - 0004")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - dynamic port description 9999",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                SecureFlags = 0,
                LocalPorts = "9999",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0004", expected2);


            var expected3 = new RuleDetails("WiXToolset401 Test - 0005 - 9999")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("windir"), "system32", "9999.exe"),
                Description = "WiX Toolset firewall exception rule integration test - dynamic Name 9999",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = 2,
                Protocol = 17,
                RemoteAddresses = "127.0.0.1/255.255.255.255,192.168.1.1/255.255.255.255",
                SecureFlags = 0,
                LocalPorts = "9999",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0005 - 9999", expected3);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0003"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0004"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0005 - 9999"));
        }

        [RuntimeFact]
        public void SucceedWhenIgnoreOnFailureIsSet()
        {
            var product = this.CreatePackageInstaller("IgnoreFailedFirewallRules");
            var log1 = product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0006 pipe"));
            Assert.True(LogVerifier.MessageInLogFile(log1, "failed to add firewall exception 'WiXToolset401 Test - 0006 pipe' to the list"));

            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0007 pipe"));
            Assert.True(LogVerifier.MessageInLogFile(log1, "failed to add firewall exception 'WiXToolset401 Test - 0007 pipe' to the list"));

            var expected = new RuleDetails("WiXToolset401 Test - 0008 removal")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = "test.exe",
                Description = "WiX Toolset firewall exception rule integration test - removal test",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalPorts = "52390",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0008 removal", expected);
            Verifier.RemoveFirewallRuleByName("WiXToolset401 Test - 0008 removal");

            var log2 = product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, "NORULENAME=1");
            Assert.True(LogVerifier.MessageInLogFile(log2, "failed to remove firewall exception for name"));
        }

        [RuntimeFact]
        public void VarietyOfProtocolValuesCanBeUsed()
        {
            var product = this.CreatePackageInstaller("ProtocolRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset401 Test - 0009")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - protocol TCP",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                SecureFlags = 0,
                LocalPorts = "900",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0009", expected1);


            var expected2 = new RuleDetails("WiXToolset401 Test - 0010")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - protocol UDP",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 17,
                RemoteAddresses = "*",
                SecureFlags = 0,
                LocalPorts = "1000",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0010", expected2);


            var expected3 = new RuleDetails("WiXToolset401 Test - 0011")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = "test.exe",
                Description = "WiX Toolset firewall exception rule integration test - ports can only be specified if protocol is TCP or UDP",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 134,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0011", expected3);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0009"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0010"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0011"));
        }

        [RuntimeFact]
        public void FullSetOfScopeValuesCanBeUsed()
        {
            var product = this.CreatePackageInstaller("ScopeRules");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset401 Test - 0012")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope any",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                SecureFlags = 0,
                LocalPorts = "1200",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0012", expected1);


            var expected2 = new RuleDetails("WiXToolset401 Test - 0013")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope local subnet",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "LocalSubnet",
                SecureFlags = 0,
                LocalPorts = "1300",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0013", expected2);


            var expected3 = new RuleDetails("WiXToolset401 Test - 0014")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope DNS",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "DNS",
                SecureFlags = 0,
                LocalPorts = "1400",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0014", expected3);


            var expected4 = new RuleDetails("WiXToolset401 Test - 0015")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope DHCP",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "DHCP",
                SecureFlags = 0,
                LocalPorts = "1500",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0015", expected4);


            var expected5 = new RuleDetails("WiXToolset401 Test - 0016")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope WINS",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "WINS",
                SecureFlags = 0,
                LocalPorts = "1600",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0016", expected5);


            var expected6 = new RuleDetails("WiXToolset401 Test - 0017")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - scope default gateway",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "DefaultGateway",
                SecureFlags = 0,
                LocalPorts = "1700",
                RemotePorts = "*",
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0017", expected6);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0012"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0013"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0014"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0015"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0016"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0017"));
        }

        [RuntimeFact]
        public void CanInstallAndUninstallFirewallRulesWithInterfaces()
        {
            var names = NetworkInterface.GetAllNetworkInterfaces()
                .Take(3)
                .Select(ni => ni.Name);

            var props = names.Select((name, idx) => $"INTERFACE{idx + 1}=\"{name}\"")
                .Concat(new[] { "INTERFACETYPE=Lan" }).ToArray();

            var product = this.CreatePackageInstaller("FirewallRulesInterfaces");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS, props);

            var expected1 = new RuleDetails("WiXToolset500 Test - 0028")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRulesInterfaces", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - three interfaces",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "Lan,Wireless,RemoteAccess",
                Interfaces = names.ToArray<object>(),
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 256,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset500 Test - 0028", expected1);

            var expected2 = new RuleDetails("WiXToolset500 Test - 0029")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - one interface",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "Lan",
                Interfaces = names.Take(1).ToArray<object>(),
                LocalAddresses = "*",
                LocalPorts = "29292",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset500 Test - 0029", expected2);

            props = names.Take(1).Select((name, idx) => $"INTERFACE{idx + 2}=\"{name}\"").ToArray();

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, props);

            var expected3 = new RuleDetails("WiXToolset500 Test - 0028")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRulesInterfaces", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - three interfaces",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "Lan,Wireless,RemoteAccess",
                Interfaces = names.Take(1).ToArray<object>(),
                LocalAddresses = "*",
                Profiles = Int32.MaxValue,
                Protocol = 256,
                RemoteAddresses = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset500 Test - 0028", expected3);

            var expected4 = new RuleDetails("WiXToolset500 Test - 0029")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - one interface",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "29292",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset500 Test - 0029", expected4);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset500 Test - 0028"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset500 Test - 0029"));
        }

        [RuntimeFact]
        public void CanInstallAndUninstallFirewallRulesPackagedByDifferentModules()
        {
            var product = this.CreatePackageInstaller("CrossVersionMerge");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate new firewall exception details.
            var expected1 = new RuleDetails("WiXToolset401 Test - 0018")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MsiPackage", "file1.txt"),
                Description = "WiX Toolset firewall exception rule integration test - module 401 MergeRedirectFolder - app",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = true,
                EdgeTraversalOptions = 1,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "40101",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0018", expected1);

            var expected2 = new RuleDetails("WiXToolset401 Test - 0019")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - module 401 MergeRedirectFolder - port",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "40102",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0019", expected2);

            var expected3 = new RuleDetails("WiXToolset401 Test - 0020")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MsiPackage", "file2.txt"),
                Description = "WiX Toolset firewall exception rule integration test - module 401 NotTheMergeRedirectFolder - app",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = true,
                EdgeTraversalOptions = 1,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "40103",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0020", expected3);

            var expected4 = new RuleDetails("WiXToolset401 Test - 0021")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - module 401 NotTheMergeRedirectFolder - port",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "40104",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset401 Test - 0021", expected4);

            var expected5 = new RuleDetails("WiXToolset Test - 0022")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MsiPackage", "file1.txt"),
                Description = "WiX Toolset firewall exception rule integration test - module MergeRedirectFolder - app",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "50001",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0022", expected5);

            var expected6 = new RuleDetails("WiXToolset Test - 0023")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - module MergeRedirectFolder - port",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "50002",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0023", expected6);

            var expected7 = new RuleDetails("WiXToolset Test - 0024")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MsiPackage", "file2.txt"),
                Description = "WiX Toolset firewall exception rule integration test - module NotTheMergeRedirectFolder - app",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "50003",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0024", expected7);

            var expected8 = new RuleDetails("WiXToolset Test - 0025")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - module NotTheMergeRedirectFolder - port",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "50004",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0025", expected8);

            var expected9 = new RuleDetails("WiXToolset Test - 0026")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "MsiPackage", "package.wxs"),
                Description = "WiX Toolset firewall exception rule integration test - package app",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "20001",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0026", expected9);

            var expected10 = new RuleDetails("WiXToolset Test - 0027")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Description = "WiX Toolset firewall exception rule integration test - package port",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                LocalAddresses = "*",
                LocalPorts = "20002",
                Profiles = Int32.MaxValue,
                Protocol = 6,
                RemoteAddresses = "*",
                RemotePorts = "*",
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0027", expected10);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0018"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0019"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0020"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0021"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0022"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0023"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0024"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0025"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0026"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0027"));
        }

        [RuntimeFact]
        public void ServiceNameIsPassedIntoNestedRules()
        {
            var product = this.CreatePackageInstaller("NestedService");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset Test - 0031")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                Description = "WiX Toolset firewall exception rule integration test - service property",
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
                ServiceName = "Spooler",
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0031", expected1);

            var expected2 = new RuleDetails("WiXToolset Test - 0032")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                Description = "WiX Toolset firewall exception rule integration test - ServiceConfig",
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
                ServiceName = "Spooler",
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0032", expected2);

            var expected3 = new RuleDetails("WiXToolset Test - 0033")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                Description = "WiX Toolset firewall exception rule integration test - ServiceInstall",
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
                ServiceName = "WixTestFirewallSrv",
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0033", expected3);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0031"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0032"));
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0033"));
        }

        [RuntimeFact]
        public void SucceedWhenEnableOnlyFlagIsSet()
        {
            var product = this.CreatePackageInstaller("FirewallRulesProperties");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset Test - 0028")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0028", expected1);

            Verifier.DisableFirewallRule("WiXToolset Test - 0028");

            var args = new[]
            {
                "LOCALPORT=3456",
                "PROTOCOL=6",
                "PROGRAM=ShouldBeUnchanged",
                "PROFILE=2",
                "DESCRIPTION=ShouldBeUnchanged",
                "REMOTESCOPE=ShouldBeUnchanged",
                "EDGETRAVERSAL=3",
                "ENABLED=1",
                "GROUPING=ShouldBeUnchanged",
                "ICMPTYPES=ShouldBeUnchanged",
                "INTERFACE=ShouldBeUnchanged",
                "INTERFACETYPE=ShouldBeUnchanged",
                "LOCALSCOPE=ShouldBeUnchanged",
                "REMOTEPORT=60000",
                "SERVICE=ShouldBeUnchanged",
                "PACKAGEID=ShouldBeUnchanged",
                "LOCALUSERS=ShouldBeUnchanged",
                "LOCALOWNER=ShouldBeUnchanged",
                "REMOTEMACHINES=ShouldBeUnchanged",
                "REMOTEUSERS=ShouldBeUnchanged",
                "SECUREFLAGS=15",
                "REMOTEADDRESS=ShouldBeUnchanged",
                "LOCALADDRESS=ShouldBeUnchanged",
            };

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, args);

            var expected2 = new RuleDetails("WiXToolset Test - 0028")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0028", expected2);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0028"));
        }

        [RuntimeFact]
        public void SucceedWhenDoNothingFlagIsSet()
        {
            var product = this.CreatePackageInstaller("FirewallRulesProperties");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset Test - 0029")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0029", expected1);
            Verifier.DisableFirewallRule("WiXToolset Test - 0029");

            var args = new[]
            {
                "INTERFACE=ShouldBeUnchanged",
                "INTERFACETYPE=ShouldBeUnchanged",
                "REMOTEADDRESS=ShouldBeUnchanged",
                "LOCALADDRESS=ShouldBeUnchanged",
            };

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, args);

            var expected2 = new RuleDetails("WiXToolset Test - 0029")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = false, // remains as disabled after the repair
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0029", expected2);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0029"));
        }

        [RuntimeFact]
        public void SucceedWhenNoFlagIsSet()
        {
            var product = this.CreatePackageInstaller("FirewallRulesProperties");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected1 = new RuleDetails("WiXToolset Test - 0030")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "*",
                RemoteAddresses = "*",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                InterfaceTypes = "All",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0030", expected1);
            Verifier.DisableFirewallRule("WiXToolset Test - 0030");

            var names = NetworkInterface.GetAllNetworkInterfaces()
                .Take(2)
                .Select(ni => ni.Name);

            var args = names.Select((name, idx) => $"INTERFACE{idx + 1}=\"{name}\"")
            .Concat(new[]
            {
                "INTERFACETYPE1=Wireless",
                "INTERFACETYPE2=Lan",
                "REMOTEADDRESS1=DHCP",
                "REMOTEADDRESS2=LocalSubnet",
                "LOCALADDRESS1=127.0.0.1",
                "LOCALADDRESS2=192.168.1.1",
            })
            .ToArray();

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS, args);

            var expected2 = new RuleDetails("WiXToolset Test - 0030")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                Protocol = 256,
                LocalAddresses = "127.0.0.1/255.255.255.255,192.168.1.1/255.255.255.255",
                RemoteAddresses = "LocalSubnet,DHCP",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                Description = "",
                EdgeTraversal = false,
                EdgeTraversalOptions = 0,
                Enabled = true,
                Interfaces = names.ToArray(),
                InterfaceTypes = "Lan,Wireless",
                Profiles = Int32.MaxValue,
                SecureFlags = 0,
            };

            Verifier.VerifyFirewallRule("WiXToolset Test - 0030", expected2);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the firewall exceptions have been removed.
            Assert.False(Verifier.FirewallRuleExists("WiXToolset Test - 0030"));
        }
    }
}
