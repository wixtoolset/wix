// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating a resolver extension.
    /// </summary>
    public abstract class BaseExtensionData : IExtensionData
    {
        public virtual string DefaultCulture => null;

        public virtual Intermediate GetLibrary(ITupleDefinitionCreator tupleDefinitions)
        {
            return null;
        }

        public virtual bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = null;
            return false;
        }
    }
}
