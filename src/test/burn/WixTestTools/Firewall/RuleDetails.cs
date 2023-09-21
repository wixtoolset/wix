// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools.Firewall
{
    using NetFwTypeLib;

    /// <summary>
    /// The RuleDetails class provides access to the properties of a firewall rule.<br/>
    /// Details are retrieved via the <b>NetFwTypeLib.INetFwRule3</b> interface and originally stored at<br/>
    /// <b>HKLM\SYSTEM\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules</b>.
    /// </summary>
    public class RuleDetails
    {
        public RuleDetails(string name)
        {
            this.Name = name;
        }

        public RuleDetails(INetFwRule3 rule)
        {
            this.Name = rule.Name;
            this.Description = rule.Description;
            this.ApplicationName = rule.ApplicationName;
            this.ServiceName = rule.serviceName;
            this.Protocol = rule.Protocol;
            this.LocalPorts = rule.LocalPorts;
            this.RemotePorts = rule.RemotePorts;
            this.LocalAddresses = rule.LocalAddresses;
            this.RemoteAddresses = rule.RemoteAddresses;
            this.IcmpTypesAndCodes = rule.IcmpTypesAndCodes;
            this.Direction = rule.Direction;
            this.Interfaces = rule.Interfaces as object[];
            this.InterfaceTypes = rule.InterfaceTypes;
            this.Enabled = rule.Enabled;
            this.Grouping = rule.Grouping;
            this.Profiles = rule.Profiles;
            this.EdgeTraversal = rule.EdgeTraversal;
            this.Action = rule.Action;
            this.EdgeTraversalOptions = rule.EdgeTraversalOptions;
            this.LocalAppPackageId = rule.LocalAppPackageId;
            this.LocalUserOwner = rule.LocalUserOwner;
            this.LocalUserAuthorizedList = rule.LocalUserAuthorizedList;
            this.RemoteUserAuthorizedList = rule.RemoteUserAuthorizedList;
            this.RemoteMachineAuthorizedList = rule.RemoteMachineAuthorizedList;
            this.SecureFlags = rule.SecureFlags;
        }

        /// <summary>
        /// Specifies a friendly name of this rule. Rule name should be unique, must not contain a "|" (pipe) character and cannot be "all".<br/>
        /// Use <b>netsh advfirewall firewall add rule help</b> for more information.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// This property is optional. It specifies the description of this rule.<br/>
        /// The string must not contain the "|" character, as the pipe character is used to separate firewall rule properties when stored in the registry.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// This property is optional. It specifies the path and name (an image path) of the application to which this rule applies.<br/>
        /// Environment variables can be used in the path.<br/>
        /// Example: "%ProgramFiles%\Windows Defender\MsMpEng.exe"<br/>
        /// Example: "%SystemRoot%\system32\svchost.exe"
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// This property is optional. A service name value of "*" indicates that a service, not an application, must be sending or receiving traffic.<br/>
        /// Example: "Spooler"<br/>
        /// Example: "FTPSVC"
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// This property is optional. The Protocol property must be set before the LocalPorts or RemotePorts properties, or an error will be returned.<br/>
        /// A list of protocol numbers is available at the <a href="https://www.iana.org/assignments/protocol-numbers/">IANA website</a>.
        /// The default value is 256, which means any protocol.<br/>
        /// Example: 6 , which specifies the TCP protocol<br/>
        /// Example: 17 , which specifies the UDP protocol
        /// </summary>
        public int? Protocol { get; set; }

        /// <summary>
        /// This property is optional. The Protocol property must be set before the LocalPorts property or an error will be returned.<br/>
        /// A clear text string containing a single port, a comma separated list of port numbers, or a port range can be provided. "RPC" is an acceptable value.<br/>
        /// Example: "23456"<br/>
        /// Example: "10234-10300"<br/>
        /// Example: "5026,7239"
        /// </summary>
        public string LocalPorts { get; set; }

        /// <summary>
        /// This property is optional. The Protocol property must be set before the RemotePorts property or an error will be returned.<br/>
        /// A clear text string containing a single port, a comma separated list of port numbers, or a port range can be provided.<br/>
        /// Example: "23456"<br/>
        /// Example: "10234-10300"<br/>
        /// Example: "5026,7239"
        /// </summary>
        public string RemotePorts { get; set; }

        /// <summary>
        /// This property is optional. It consists of one or more comma-delimited tokens specifying the local addresses from which the application can listen for traffic.<br/>
        /// "<b>*</b>" indicates any local address and is the default value. If present, this must be the only token included.<br/>
        /// Other tokens:<br/>
        /// <b>o</b> "Defaultgateway"<br/>
        /// <b>o</b> "DHCP"<br/>
        /// <b>o</b> "WINS"<br/>
        /// <b>o</b> "LocalSubnet" indicates any local address on the local subnet. This token is not case-sensitive.<br/>
        /// <b>o</b> A subnet can be specified using either the subnet mask or network prefix notation. If neither a subnet mask not a network prefix is specified, the subnet mask defaults to 255.255.255.255.<br/>
        /// <b>o</b> A valid IPv6 address.<br/>
        /// <b>o</b> An IPv4 address range in the format of "start address - end address" with no spaces included.<br/>
        /// <b>o</b> An IPv6 address range in the format of "start address - end address" with no spaces included.<br/>
        /// Example: "LocalSubnet"
        /// </summary>
        public string LocalAddresses { get; set; }

        /// <summary>
        /// This property is optional. It consists of one or more comma-delimited tokens specifying the local addresses from which the application can listen for traffic.<br/>
        /// "<b>*</b>" indicates any remote address and is the default value. If present, this must be the only token included.<br/>
        /// Other tokens:<br/>
        /// <b>o</b> "Defaultgateway"<br/>
        /// <b>o</b> "DHCP"<br/>
        /// <b>o</b> "WINS"<br/>
        /// <b>o</b> "LocalSubnet" indicates any local address on the local subnet. This token is not case-sensitive.<br/>
        /// <b>o</b> A subnet can be specified using either the subnet mask or network prefix notation. If neither a subnet mask not a network prefix is specified, the subnet mask defaults to 255.255.255.255.<br/>
        /// <b>o</b> A valid IPv6 address.<br/>
        /// <b>o</b> An IPv4 address range in the format of "start address - end address" with no spaces included.<br/>
        /// <b>o</b> An IPv6 address range in the format of "start address - end address" with no spaces included.<br/>
        /// Example: "LocalSubnet"
        /// </summary>
        public string RemoteAddresses { get; set; }

        /// <summary>
        /// This property is optional. A list of ICMP types and codes separated by semicolon. "*" indicates all ICMP types and codes.<br/>
        /// Example: "4:*,9:*,12:*"
        /// </summary>
        public string IcmpTypesAndCodes { get; set; }

        /// <summary>
        /// This property is optional. If this property is not specified, the default value is NET_FW_RULE_DIR_IN.
        /// </summary>
        public NET_FW_RULE_DIRECTION_? Direction { get; set; }

        /// <summary>
        /// This parameter allows the specification of an array of interface LUIDs (locally unique identifiers) supplied as strings.<br/>
        /// This is commonly used by USB RNDIS (Remote Network Driver Interface Specification) devices to restrict traffic to a specific non-routable interface.<br/>
        /// Use <b>netsh trace show interfaces</b> to show a list of local interfaces and their LUIDs.<br/>
        /// The interfaces are stored in the registry as GUIDs, but need to be passed to the API as text. eg from the registry<br/>
        /// v2.30|Action=Allow|Active=TRUE|Dir=In|Protocol=6|LPort=23456|IF={423411CD-E627-4A1A-9E1F-C5BE6CD2CC99}|IF={49A98AD0-8379-4079-A445-77066C52E338}|Name=WiXToolset401 Test - 0002|Desc=WiX Toolset firewall exception rule integration test - minimal port properties|<br/>
        /// Example API value: new object[] { "Wi-Fi", "Local Area Connection* 14" }
        /// </summary>
        public object[] Interfaces { get; set; }

        /// <summary>
        /// This property is optional. It specifies the list of interface types for which the rule applies.<br/>
        /// Acceptable values for this property are "RemoteAccess", "Wireless", "Lan", and "All".<br/>
        /// If more than one interface type is specified, the strings must be separated by a comma.<br/>
        /// Example: "Lan,Wireless"
        /// </summary>
        public string InterfaceTypes { get; set; }

        /// <summary>
        /// This property is optional. It enables or disables a rule. A new rule is disabled by default.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// This property is optional. It specifies the group to which an individual rule belongs and groups multiple rules into a single line in the Windows Firewall control panel.<br/>
        /// This allows the users to enable or disable multiple rules with a single click.<br/>
        /// The Grouping property can also be specified using indirect strings.<br/>
        /// Example: "Simple Group Name"<br/>
        /// Example: "@yourresources.dll,-1005"
        /// </summary>
        public string Grouping { get; set; }

        /// <summary>
        /// This property is optional. The NET_FW_PROFILE_TYPE2 enumerated type specifies the type of profile.<br/>
        /// NET_FW_PROFILE2_ALL is the default value. Profiles can be combined from the following values:<br/>
        /// <b>o</b> NET_FW_PROFILE2_DOMAIN = 0x1<br/>
        /// <b>o</b> NET_FW_PROFILE2_PRIVATE = 0x2<br/>
        /// <b>o</b> NET_FW_PROFILE2_PUBLIC = 0x4<br/>
        /// <b>o</b> NET_FW_PROFILE2_ALL = 0x7fffffff<br/>
        /// Example: 0x5
        /// </summary>
        public int? Profiles { get; set; }

        /// <summary>
        /// New rules have the EdgeTraversal property disabled by default.<br/>
        /// The EdgeTraversal property indicates that specific inbound traffic is allowed to tunnel through NATs and other edge devices using the Teredo tunneling technology.<br/>
        /// The application or service with the inbound firewall rule needs to support IPv6. The primary application of this setting allows listeners on the host to be globally addressable through a Teredo IPv6 address.<br/>
        /// See EdgeTraversalOptions property for additional information.
        /// </summary>
        public bool? EdgeTraversal { get; set; }

        /// <summary>
        /// This property is optional. The NET_FW_ACTION enumerated type specifies the action for this rule.<br/>
        /// NET_FW_ACTION_ALLOW is the default value. The Action must be specified from the following list of values:<br/>
        /// <b>o</b> NET_FW_ACTION_BLOCK = 0x0<br/>
        /// <b>o</b> NET_FW_ACTION_ALLOW = 0x1<br/>
        /// </summary>
        public NET_FW_ACTION_? Action { get; set; }

        /// <summary>
        /// This property is optional and can be used to access the edge traversal properties of a firewall rule defined by NET_FW_EDGE_TRAVERSAL_TYPE enumerated type.<br/>
        /// NET_FW_EDGE_TRAVERSAL_TYPE_DENY is the default value. Enumerated types cannot be combined and must be selected from the following list of values:<br/>
        /// <b>o</b> NET_FW_EDGE_TRAVERSAL_TYPE_DENY = 0x0 - Edge traversal traffic is always blocked.<br/>
        /// <b>o</b> NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW = 0x1 - Edge traversal traffic is always allowed.<br/>
        /// These two options above are equivalent to setting the EdgeTraversal property to true or false.<br/>
        /// <b>o</b> NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP = 0x2 - Edge traversal traffic is allowed when the application sets the IPV6_PROTECTION_LEVEL socket option to PROTECTION_LEVEL_UNRESTRICTED. Otherwise, it is blocked.<br/>
        /// <b>o</b> NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER = 0x3 - The user is prompted whether to allow edge traversal traffic when the application sets the IPV6_PROTECTION_LEVEL socket option to PROTECTION_LEVEL_UNRESTRICTED.<br/>
		/// If the user chooses to allow edge traversal traffic, the rule is modified to defer to the application's settings. If the application has not set the IPV6_PROTECTION_LEVEL socket option to PROTECTION_LEVEL_UNRESTRICTED,<br/>
		/// edge traversal traffic is blocked. In order to use this option, the firewall rule must have both the application path and protocol scopes specified. This option cannot be used if port(s) are defined.
        /// </summary>
        public int? EdgeTraversalOptions { get; set; }

        /// <summary>
        /// This property is optional. It specifies the package identifier or the app container identifier of a process, whether from a Windows Store app or a desktop app.<br/>
        /// For more details and examples look at <b>HKLM\SYSTEM\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\RestrictedServices\AppIso\FirewallRules</b>.<br/>
        /// Example: "S-1-15-2-1239072475-3687740317-1842961305-3395936705-4023953123-1525404051-2779347315" Microsoft.WindowsMaps
        /// </summary>
        public string LocalAppPackageId { get; set; }

        /// <summary>
        /// This property is optional. It specifies the user security identifier (SID) of the user who is the owner of the rule.<br/>
        /// If this rule does not specify LocalUserAuthorizedList, all the traffic that this rule matches must be destined to or originated from this user.<br/>
        /// Example: "S-1-5-21-1898747406-2352535518-1247798438-1914"
        /// </summary>
        public string LocalUserOwner { get; set; }

        /// <summary>
        /// This property is optional. It specifies a list of authorized local users for an app container (using SDDL).<br/>
        /// Example: "O:LSD:(A;;CC;;;S-1-5-84-0-0-0-0-0)"
        /// </summary>
        public string LocalUserAuthorizedList { get; set; }

        /// <summary>
        /// This property is optional. It specifies a list of remote users who are authorized to access an app container (using SDDL).<br/>
        /// </summary>
        public string RemoteUserAuthorizedList { get; set; }

        /// <summary>
        /// This property is optional. It specifies a list of remote computers which are authorized to access an app container.<br/>
        /// </summary>
        public string RemoteMachineAuthorizedList { get; set; }

        /// <summary>
        /// This property is optional. It specifies which firewall verifications of security levels provided by IPsec must be guaranteed to allow the connection.<br/>
        /// The allowed values must correspond to one of those of the <b>NET_FW_AUTHENTICATE_TYPE</b> enumeration:<br/>
        /// <b>o</b> NET_FW_AUTHENTICATE_NONE - 0x0 - No security check is performed.<br/>
        /// <b>o</b> NET_FW_AUTHENTICATE_NO_ENCAPSULATION - 0x1 - The traffic is allowed if it is IPsec-protected with authentication and no encapsulation protection. This means that the peer is authenticated, but there is no integrity protection on the data.<br/>
        /// <b>o</b> NET_FW_AUTHENTICATE_WITH_INTEGRITY - 0x2 - The traffic is allowed if it is IPsec-protected with authentication and integrity protection.<br/>
        /// <b>o</b> NET_FW_AUTHENTICATE_AND_NEGOTIATE_ENCRYPTION - 0x3 - The traffic is allowed if its is IPsec-protected with authentication and integrity protection. In addition, negotiation of encryption protections on subsequent packets is requested.<br/>
        /// <b>o</b> NET_FW_AUTHENTICATE_AND_ENCRYPT - 0x4 - The traffic is allowed if it is IPsec-protected with authentication, integrity and encryption protection since the very first packet.
        /// </summary>
        public int? SecureFlags { get; set; }
    }
}
