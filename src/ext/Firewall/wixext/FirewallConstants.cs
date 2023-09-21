// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System.Xml.Linq;

    static class FirewallConstants
    {
        internal static readonly XNamespace Namespace = "http://wixtoolset.org/schemas/v4/wxs/firewall";
        internal static readonly XName FirewallExceptionName = Namespace + "FirewallException";
        internal static readonly XName RemoteAddressName = Namespace + "RemoteAddress";
        internal static readonly XName InterfaceName = Namespace + "Interface";
        internal static readonly XName InterfaceTypeName = Namespace + "InterfaceType";
        internal static readonly XName LocalAddressName = Namespace + "LocalAddress";

        internal const string IntegerNotSetString = "-2147483648";

        // from icftypes.h
        public const int NET_FW_RULE_DIR_IN = 1;
        public const int NET_FW_RULE_DIR_OUT = 2;
        public const int NET_FW_IP_PROTOCOL_TCP = 6;
        public const int NET_FW_IP_PROTOCOL_UDP = 17;

        // from icftypes.h
        public const int NET_FW_PROFILE2_DOMAIN = 0x0001;
        public const int NET_FW_PROFILE2_PRIVATE = 0x0002;
        public const int NET_FW_PROFILE2_PUBLIC = 0x0004;
        public const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;

        // from icftypes.h
        public const int NET_FW_EDGE_TRAVERSAL_TYPE_DENY = 0;
        public const int NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW = 1;
        public const int NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP = 2;
        public const int NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER = 3;

        /// <summary>
        /// Firewall rules are stored in the registry.<br/>
        /// The pipe character is used to split firewall rule attributes, so is not permitted in any of them.
        /// </summary>
        public const char FORBIDDEN_FIREWALL_CHAR = '|';
    }
}
