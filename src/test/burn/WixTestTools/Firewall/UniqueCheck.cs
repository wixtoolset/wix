// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools.Firewall
{
    using NetFwTypeLib;

    /// <summary>
    /// A lot of firewall rules don't follow the Microsoft recommendation of using unique names.<br/>
    /// This class helps to disambiguate the rules based on Name, Direction, Profile, Protocol, ApplicationName, LocalUserOwner and RemoteAddresses.
    /// </summary>
    public class UniqueCheck
    {
        public UniqueCheck()
        {
        }

        public UniqueCheck(RuleDetails details)
        {
            this.Name = details.Name;
            this.Direction = details.Direction;
            this.Profile = details.Profiles;
            this.Protocol = details.Protocol;
            this.ApplicationName = details.ApplicationName;
            this.LocalUserOwner = details.LocalUserOwner;
            this.RemoteAddresses = details.RemoteAddresses;
        }


        public string Name { get; set; }

        public NET_FW_RULE_DIRECTION_? Direction { get; set; }

        public int? Profile { get; set; }

        public int? Protocol { get; set; }

        public string ApplicationName { get; set; }

        public string LocalUserOwner { get; set; }

        public string RemoteAddresses { get; set; }

        public bool FirewallRuleIsUnique(INetFwRule3 rule)
        {
            if (this.Name != null && rule.Name != this.Name)
            {
                return false;
            }

            if (this.Direction.HasValue && rule.Direction != this.Direction.Value)
            {
                return false;
            }

            if (this.Profile.HasValue && rule.Profiles != this.Profile.Value)
            {
                return false;
            }

            if (this.Protocol.HasValue && rule.Protocol != this.Protocol.Value)
            {
                return false;
            }

            if (this.ApplicationName != null && rule.ApplicationName != this.ApplicationName)
            {
                return false;
            }

            if (this.LocalUserOwner != null && rule.LocalUserOwner != this.LocalUserOwner)
            {
                return false;
            }

            if (this.RemoteAddresses != null && rule.RemoteAddresses != this.RemoteAddresses)
            {
                return false;
            }

            return true;
        }
    }
}
