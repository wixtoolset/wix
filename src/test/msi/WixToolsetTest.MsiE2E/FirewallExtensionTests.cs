// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using System.IO;
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
                EdgeTraversal = true,
                EdgeTraversalOptions = 1,
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
                EdgeTraversal = true,
                EdgeTraversalOptions = 1,
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

            Verifier.RemoveFirewallRulesByName("WiXToolset401 Test - 0002");
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

            Verifier.RemoveFirewallRulesByName("WiXToolset401 Test - 0001");
            Assert.False(Verifier.FirewallRuleExists("WiXToolset401 Test - 0001"));

            product.RepairProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var expected = new RuleDetails("WiXToolset401 Test - 0001")
            {
                Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
                ApplicationName = this.TestContext.GetTestInstallFolder(false, Path.Combine("FirewallRules", "product.wxs")),
                Description = "WiX Toolset firewall exception rule integration test - minimal app properties",
                Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
                EdgeTraversal = true,
                EdgeTraversalOptions = 1,
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
    }
}
