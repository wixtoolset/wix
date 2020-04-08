// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using WixToolset.Data;

    public enum FirewallTupleDefinitionType
    {
        WixFirewallException,
    }

    public static partial class FirewallTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out FirewallTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(FirewallTupleDefinitionType type)
        {
            switch (type)
            {
                case FirewallTupleDefinitionType.WixFirewallException:
                    return FirewallTupleDefinitions.WixFirewallException;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
