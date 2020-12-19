// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
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
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public virtual void BundleFinalize()
        {
        }

        /// <summary>
        /// Called after output is bound into its final format.
        /// </summary>
        /// <param name="result"></param>
        public virtual void PostBackendBind(IBindResult result)
        {
        }

        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        /// <param name="context"></param>
        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.BackendHelper = context.ServiceProvider.GetService<IBurnBackendHelper>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="relatedSource"></param>
        /// <param name="type"></param>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="bindStage"></param>
        /// <returns></returns>
        public virtual IResolveFileResult ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fallbackUrl"></param>
        /// <param name="packageId"></param>
        /// <param name="payloadId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            return null;
        }

        /// <summary>
        /// Called for each extension symbol that hasn't been handled yet.
        /// Use IBurnBackendHelper to add data to the appropriate data manifest.
        /// </summary>
        /// <param name="section">The linked section.</param>
        /// <param name="symbol">The current symbol.</param>
        /// <returns>
        /// True if the extension handled the symbol, false otherwise.
        /// The Burn backend will warn on all unhandled symbols.
        /// </returns>
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
