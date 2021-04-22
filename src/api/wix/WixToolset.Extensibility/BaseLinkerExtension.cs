// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a linker extension.
    /// </summary>
    public abstract class BaseLinkerExtension : ILinkerExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected ILinkContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Called at the beginning of the linking.
        /// </summary>
        public virtual void PreLink(ILinkContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        /// <summary>
        /// Called at the end of the linking.
        /// </summary>
        public virtual void PostLink(Intermediate intermediate)
        {
        }
    }
}
