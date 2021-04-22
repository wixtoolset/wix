// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating a resolver extension.
    /// </summary>
    public abstract class BaseExtensionData : IExtensionData
    {
        /// <summary>
        /// See <see cref="IExtensionData.DefaultCulture"/>
        /// </summary>
        public virtual string DefaultCulture => null;

        /// <summary>
        /// See <see cref="IExtensionData.GetLibrary"/>
        /// </summary>
        public virtual Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return null;
        }

        /// <summary>
        /// See <see cref="IExtensionData.TryGetSymbolDefinitionByName"/>
        /// </summary>
        public virtual bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = null;
            return false;
        }
    }
}
