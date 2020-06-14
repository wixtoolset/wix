// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
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

        public virtual void BundleFinalize()
        {
        }

        public virtual bool IsTupleForExtension(IntermediateTuple tuple)
        {
            return false;
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

        public virtual bool TryAddTupleToDataManifest(IntermediateSection section, IntermediateTuple tuple)
        {
            if (this.IsTupleForExtension(tuple) && tuple.HasTag(WixToolset.Data.Burn.BurnConstants.BootstrapperApplicationDataTupleDefinitionTag))
            {
                this.BackendHelper.AddBootstrapperApplicationData(tuple);
                return true;
            }

            return false;
        }
    }
}
