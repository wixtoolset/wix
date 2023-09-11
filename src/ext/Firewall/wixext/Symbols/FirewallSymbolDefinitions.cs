// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using WixToolset.Data;

    public enum FirewallSymbolDefinitionType
    {
        WixFirewallException,
    }

    public static partial class FirewallSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out FirewallSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(FirewallSymbolDefinitionType type)
        {
            switch (type)
            {
                case FirewallSymbolDefinitionType.WixFirewallException:
                    return FirewallSymbolDefinitions.WixFirewallException;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
