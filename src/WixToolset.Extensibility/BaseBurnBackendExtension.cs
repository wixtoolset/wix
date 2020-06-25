// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

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

        public virtual void BundleFinalize()
        {
        }

        public virtual void PostBackendBind(IBindResult result)
        {
        }

        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.BackendHelper = context.ServiceProvider.GetService<IBurnBackendHelper>();
        }

        public virtual IResolveFileResult ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            return null;
        }

        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            return null;
        }

        public virtual bool TryAddSymbolToDataManifest(IntermediateSection section, IntermediateSymbol symbol)
        {
            if (this.SymbolDefinitions.Any(t => t == symbol.Definition) &&
                symbol.Definition.HasTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag))
            {
                this.BackendHelper.AddBootstrapperApplicationData(symbol);
                return true;
            }

            return false;
        }
    }
}
