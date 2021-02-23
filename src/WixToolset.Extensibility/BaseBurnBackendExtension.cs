// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a Burn backend extension.
    /// </summary>
    public abstract class BaseBurnBackendExtension : IBurnBackendExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IBindContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Backend helper for use by the extension.
        /// </summary>
        protected IBurnBackendHelper BackendHelper { get; private set; }

        /// <summary>
        /// Optional symbol definitions.
        /// </summary>
        protected virtual IEnumerable<IntermediateSymbolDefinition> SymbolDefinitions => Enumerable.Empty<IntermediateSymbolDefinition>();

        /// <summary>
        /// See <see cref="IBurnBackendExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.BackendHelper = context.ServiceProvider.GetService<IBurnBackendHelper>();
        }

        /// <summary>
        /// See <see cref="IBurnBackendExtension.ResolveRelatedFile(String, String, String, SourceLineNumber)"/>
        /// </summary>
        public virtual IResolveFileResult ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers)
        {
            return null;
        }

        /// <summary>
        /// See <see cref="IBurnBackendExtension.SymbolsFinalized(IntermediateSection)"/>
        /// </summary>
        public virtual void SymbolsFinalized(IntermediateSection section)
        {
        }

        /// <summary>
        /// See <see cref="IBurnBackendExtension.ResolveUrl(String, String, String, String, String)"/>
        /// </summary>
        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            return null;
        }

        /// <summary>
        /// See <see cref="IBurnBackendExtension.TryProcessSymbol(IntermediateSection, IntermediateSymbol)"/>
        /// </summary>
        public virtual bool TryProcessSymbol(IntermediateSection section, IntermediateSymbol symbol)
        {
            if (this.SymbolDefinitions.Any(t => t == symbol.Definition) &&
                symbol.Definition.HasTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag))
            {
                this.BackendHelper.AddBootstrapperApplicationData(symbol);
                return true;
            }

            return false;
        }

        /// <summary>
        /// See <see cref="IBurnBackendExtension.PostBackendBind(IBindResult)"/>
        /// </summary>
        /// <param name="result"></param>
        public virtual void PostBackendBind(IBindResult result)
        {
        }
    }
}
