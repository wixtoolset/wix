// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall.Tuples
{
    using WixToolset.Data;

    public static class FirewallTupleDefinitionNames
    {
        public static string WixFirewallException { get; } = "WixFirewallException";
    }

    public static partial class FirewallTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFirewallException = new IntermediateTupleDefinition(
            FirewallTupleDefinitionNames.WixFirewallException,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.RemoteAddresses), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Protocol), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Program), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Profile), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFirewallExceptionTupleFields.Description), IntermediateFieldType.String),
            },
            typeof(WixFirewallExceptionTuple));
    }
}
