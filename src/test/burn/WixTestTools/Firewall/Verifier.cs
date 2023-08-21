// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools.Firewall
{
    using System;
    using System.Collections.Generic;
    using NetFwTypeLib;
    using Xunit;

    public static class Verifier
    {
        static INetFwRules GetINetFwRules()
        {
            var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", true);
            var policyInstance = Activator.CreateInstance(policyType);
            var policy2 = policyInstance as INetFwPolicy2;
            return policy2.Rules;
        }

        static INetFwRule3 GetINetFwRule3(string name, UniqueCheck unique)
        {
            var rules = GetINetFwRules();
            INetFwRule3 rule3;

            if (unique != null)
            {
                var enumerator = rules.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    rule3 = enumerator.Current as INetFwRule3;
                    if (!unique.FirewallRuleIsUnique(rule3))
                    {
                        continue;
                    }

                    return rule3;
                }
            }

            var rule1 = rules.Item(name);
            rule3 = rule1 as INetFwRule3;
            return rule3;
        }

        public static RuleDetails GetFirewallRule(string name, UniqueCheck unique)
        {
            var rule = GetINetFwRule3(name, unique);
            var details = new RuleDetails(rule);
            return details;
        }

        public static bool FirewallRuleExists(string name, UniqueCheck unique = null)
        {
            try
            {
                GetINetFwRule3(name, unique);
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }
        }

        public static IEnumerable<RuleDetails> GetFirewallRules()
        {
            var rules = GetINetFwRules();
            var enumerator = rules.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var rule3 = enumerator.Current as INetFwRule3;
                yield return new RuleDetails(rule3);
            }
        }

        public static void AddFirewallRule(RuleDetails information)
        {
            var rules = GetINetFwRules();
            var rule1 = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            var rule3 = rule1 as INetFwRule3;

            rule3.Name = information.Name;

            if (!String.IsNullOrEmpty(information.Description))
            {
                rule3.Description = information.Description;
            }

            if (!String.IsNullOrEmpty(information.ApplicationName))
            {
                rule3.ApplicationName = information.ApplicationName;
            }

            if (!String.IsNullOrEmpty(information.ServiceName))
            {
                rule3.serviceName = information.ServiceName;
            }

            if (information.Protocol.HasValue)
            {
                rule3.Protocol = information.Protocol.Value;
            }

            if (!String.IsNullOrEmpty(information.LocalPorts))
            {
                rule3.LocalPorts = information.LocalPorts;
            }

            if (!String.IsNullOrEmpty(information.RemotePorts))
            {
                rule3.RemotePorts = information.RemotePorts;
            }

            if (!String.IsNullOrEmpty(information.LocalAddresses))
            {
                rule3.LocalAddresses = information.LocalAddresses;
            }

            if (!String.IsNullOrEmpty(information.RemoteAddresses))
            {
                rule3.RemoteAddresses = information.RemoteAddresses;
            }

            if (!String.IsNullOrEmpty(information.IcmpTypesAndCodes))
            {
                rule3.IcmpTypesAndCodes = information.IcmpTypesAndCodes;
            }

            if (information.Direction.HasValue)
            {
                rule3.Direction = information.Direction.Value;
            }

            if (information.Interfaces != null)
            {
                rule3.Interfaces = information.Interfaces;
            }

            if (!String.IsNullOrEmpty(information.InterfaceTypes))
            {
                rule3.InterfaceTypes = information.InterfaceTypes;
            }

            if (information.Enabled.HasValue)
            {
                rule3.Enabled = information.Enabled.Value;
            }

            if (!String.IsNullOrEmpty(information.Grouping))
            {
                rule3.Grouping = information.Grouping;
            }

            if (information.Profiles.HasValue)
            {
                rule3.Profiles = information.Profiles.Value;
            }

            if (information.EdgeTraversal.HasValue)
            {
                rule3.EdgeTraversal = information.EdgeTraversal.Value;
            }

            if (information.Action.HasValue)
            {
                rule3.Action = information.Action.Value;
            }

            if (information.EdgeTraversalOptions.HasValue)
            {
                rule3.EdgeTraversalOptions = information.EdgeTraversalOptions.Value;
            }

            if (!String.IsNullOrEmpty(information.LocalAppPackageId))
            {
                rule3.LocalAppPackageId = information.LocalAppPackageId;
            }

            if (!String.IsNullOrEmpty(information.LocalUserOwner))
            {
                rule3.LocalUserOwner = information.LocalUserOwner;
            }

            if (!String.IsNullOrEmpty(information.LocalUserAuthorizedList))
            {
                rule3.LocalUserAuthorizedList = information.LocalUserAuthorizedList;
            }

            if (!String.IsNullOrEmpty(information.RemoteUserAuthorizedList))
            {
                rule3.RemoteUserAuthorizedList = information.RemoteUserAuthorizedList;
            }

            if (!String.IsNullOrEmpty(information.RemoteMachineAuthorizedList))
            {
                rule3.RemoteMachineAuthorizedList = information.RemoteMachineAuthorizedList;
            }

            if (information.SecureFlags.HasValue)
            {
                rule3.SecureFlags = information.SecureFlags.Value;
            }

            rules.Add(rule3);
        }

        public static void UpdateFirewallRule(string name, RuleDetails information, UniqueCheck unique = null)
        {
            var rule = GetINetFwRule3(name, unique);

            // remove ports so the Protocol can be changed, if required
            if (information.Protocol.HasValue && rule.Protocol != information.Protocol.Value)
            {
                rule.LocalPorts = null;
                rule.RemotePorts = null;
            }

            rule.Name = information.Name;
            rule.Description = information.Description;
            rule.Direction = information.Direction ?? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.ApplicationName = information.ApplicationName;
            rule.serviceName = information.ServiceName;
            rule.Protocol = information.Protocol ?? 256;
            rule.LocalPorts = information.LocalPorts;
            rule.RemotePorts = information.RemotePorts;
            rule.LocalAddresses = information.LocalAddresses;
            rule.RemoteAddresses = information.RemoteAddresses;
            rule.IcmpTypesAndCodes = information.IcmpTypesAndCodes;
            rule.Interfaces = information.Interfaces;
            rule.InterfaceTypes = information.InterfaceTypes;
            rule.Enabled = information.Enabled ?? false;
            rule.Grouping = information.Grouping;
            rule.Profiles = information.Profiles ?? 0x7fffffff;
            rule.EdgeTraversal = information.EdgeTraversal ?? false;
            rule.Action = information.Action ?? NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            rule.EdgeTraversalOptions = information.EdgeTraversalOptions ?? 0x0;
            rule.LocalAppPackageId = information.LocalAppPackageId;
            rule.LocalUserOwner = information.LocalUserOwner;
            rule.LocalUserAuthorizedList = information.LocalUserAuthorizedList;
            rule.RemoteUserAuthorizedList = information.RemoteUserAuthorizedList;
            rule.RemoteMachineAuthorizedList = information.RemoteMachineAuthorizedList;
            rule.SecureFlags = information.SecureFlags ?? 0;
        }

        public static void EnableFirewallRule(string name, UniqueCheck unique = null)
        {
            var rule = GetINetFwRule3(name, unique);
            rule.Enabled = true;
        }

        public static void DisableFirewallRule(string name, UniqueCheck unique = null)
        {
            var rule = GetINetFwRule3(name, unique);
            rule.Enabled = false;
        }

        /// <summary>
        /// Removes a firewall rule by name. If multiple rules with the same name exist, only one of them is removed.<br/>
        /// This behavior is different from <b>netsh advfirewall firewall delete rule</b> where all matching rules are deleted if multiple matches are found.<br/>
        /// The firewall rule name cannot be null or an empty string.
        /// </summary>
        /// <param name="name">Name of the firewall rule to be removed.</param>
        public static void RemoveFirewallRuleByName(string name)
        {
            var rules = GetINetFwRules();
            rules.Remove(name);
        }

        static string FormatErrorMessage(string name, string property, object expected, object actual, UniqueCheck unique)
        {
            return $"Assert Failure: {property} differ on rule: {name}" +
                "\nExpected: " + expected +
                "\nActual: " + actual +
                "\n\nDirection: " + unique?.Direction +
                "\nProfile: " + unique?.Profile +
                "\nProtocol: " + unique?.Protocol +
                "\nApplicationName: " + unique?.ApplicationName +
                "\nLocalUserOwner: " + unique?.LocalUserOwner;
        }

        public static void VerifyFirewallRule(string name, RuleDetails expected, UniqueCheck unique = null)
        {
            var actual = GetFirewallRule(name, unique);
            Assert.True(expected.Name == actual.Name, String.Format("Assert Failure: Names differ on rule: \nExpected: {0}\nActual: {1}", expected.Name, actual.Name));
            Assert.True(expected.Description == actual.Description, FormatErrorMessage(name, "Descriptions", expected.Description, actual.Description, unique));
            Assert.True(expected.ApplicationName == actual.ApplicationName, FormatErrorMessage(name, "ApplicationNames", expected.ApplicationName, actual.ApplicationName, unique));
            Assert.True(expected.ServiceName == actual.ServiceName, FormatErrorMessage(name, "ServiceNames", expected.ServiceName, actual.ServiceName, unique));
            Assert.True(expected.Protocol == actual.Protocol, FormatErrorMessage(name, "Protocols", expected.Protocol, actual.Protocol, unique));
            Assert.True(expected.LocalPorts == actual.LocalPorts, FormatErrorMessage(name, "LocalPorts", expected.LocalPorts, actual.LocalPorts, unique));
            Assert.True(expected.LocalAddresses == actual.LocalAddresses, FormatErrorMessage(name, "LocalAddresses", expected.LocalAddresses, actual.LocalAddresses, unique));
            Assert.True(expected.RemotePorts == actual.RemotePorts, FormatErrorMessage(name, "RemotePorts", expected.RemotePorts, actual.RemotePorts, unique));
            Assert.True(expected.RemoteAddresses == actual.RemoteAddresses, FormatErrorMessage(name, "RemoteAddresses", expected.RemoteAddresses, actual.RemoteAddresses, unique));
            Assert.True(expected.IcmpTypesAndCodes == actual.IcmpTypesAndCodes, FormatErrorMessage(name, "IcmpTypesAndCodes", expected.IcmpTypesAndCodes, actual.Description, unique));
            Assert.True(expected.Direction == actual.Direction, FormatErrorMessage(name, "Directions", expected.Direction, actual.Direction, unique));
            Assert.Equal<object>(expected.Interfaces, actual.Interfaces);
            Assert.True(expected.InterfaceTypes == actual.InterfaceTypes, FormatErrorMessage(name, "InterfaceTypes", expected.InterfaceTypes, actual.InterfaceTypes, unique));
            Assert.True(expected.Enabled == actual.Enabled, FormatErrorMessage(name, "Enabled flags", expected.Enabled, actual.Enabled, unique));
            Assert.True(expected.Grouping == actual.Grouping, FormatErrorMessage(name, "Groupings", expected.Grouping, actual.Grouping, unique));
            Assert.True(expected.Profiles == actual.Profiles, FormatErrorMessage(name, "Profiles", expected.Profiles, actual.Profiles, unique));
            Assert.True(expected.EdgeTraversal == actual.EdgeTraversal, FormatErrorMessage(name, "EdgeTraversals", expected.EdgeTraversal, actual.EdgeTraversal, unique));
            Assert.True(expected.Action == actual.Action, FormatErrorMessage(name, "Actions", expected.Action, actual.Action, unique));
            Assert.True(expected.EdgeTraversalOptions == actual.EdgeTraversalOptions, FormatErrorMessage(name, "EdgeTraversalOptions", expected.EdgeTraversalOptions, actual.EdgeTraversalOptions, unique));
            Assert.True(expected.LocalAppPackageId == actual.LocalAppPackageId, FormatErrorMessage(name, "LocalAppPackageIds", expected.LocalAppPackageId, actual.LocalAppPackageId, unique));
            Assert.True(expected.LocalUserOwner == actual.LocalUserOwner, FormatErrorMessage(name, "LocalUserOwners", expected.LocalUserOwner, actual.LocalUserOwner, unique));
            Assert.True(expected.LocalUserAuthorizedList == actual.LocalUserAuthorizedList, FormatErrorMessage(name, "LocalUserAuthorizedLists", expected.LocalUserAuthorizedList, actual.LocalUserAuthorizedList, unique));
            Assert.True(expected.RemoteUserAuthorizedList == actual.RemoteUserAuthorizedList, FormatErrorMessage(name, "RemoteUserAuthorizedLists", expected.RemoteUserAuthorizedList, actual.RemoteUserAuthorizedList, unique));
            Assert.True(expected.RemoteMachineAuthorizedList == actual.RemoteMachineAuthorizedList, FormatErrorMessage(name, "RemoteMachineAuthorizedLists", expected.RemoteMachineAuthorizedList, actual.RemoteMachineAuthorizedList, unique));
            Assert.True(expected.SecureFlags == actual.SecureFlags, FormatErrorMessage(name, "SecureFlags", expected.SecureFlags, actual.SecureFlags, unique));
        }
    }
}
