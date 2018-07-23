// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a resolver extension.
    /// </summary>
    public abstract class BaseLayoutExtension : ILayoutExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected ILayoutContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Called at the beginning of layout.
        /// </summary>
        public virtual void PreLayout(ILayoutContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        public virtual bool CopyFile(string source, string destination)
        {
            return false;
        }

        public virtual bool MoveFile(string source, string destination)
        {
            return false;
        }

        /// <summary>
        /// Called at the end of ayout.
        /// </summary>
        public virtual void PostLayout()
        {
        }
    }
}
