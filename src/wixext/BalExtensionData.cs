// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset Bal Extension.
    /// </summary>
    public sealed class BalExtensionData : BaseExtensionData
    {
        /// <summary>
        /// Gets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        public override string DefaultCulture => "en-US";

        public override bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = BalTupleDefinitions.ByName(name);
            return tupleDefinition != null;
        }

        public override Intermediate GetLibrary(ITupleDefinitionCreator tupleDefinitions)
        {
            return Intermediate.Load(typeof(BalExtensionData).Assembly, "WixToolset.Bal.bal.wixlib", tupleDefinitions);
        }
    }
}
