// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public sealed class FirewallExtensionData : BaseExtensionData
    {
        public override string DefaultCulture => "en-US";

        public override bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = FirewallTupleDefinitions.ByName(name);
            return tupleDefinition != null;
        }

        public override Intermediate GetLibrary(ITupleDefinitionCreator tupleDefinitions)
        {
            return Intermediate.Load(typeof(FirewallExtensionData).Assembly, "WixToolset.Firewall.firewall.wixlib", tupleDefinitions);
        }
    }
}
