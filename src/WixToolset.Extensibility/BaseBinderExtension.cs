// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data.Bind;

    /// <summary>
    /// Base class for creating a resolver extension.
    /// </summary>
    public abstract class BaseBinderExtension : IBinderExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IBindContext Context { get; private set; }

        /// <summary>
        /// Called at the beginning of bind.
        /// </summary>
        public virtual void PreBind(IBindContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Called at the end of bind.
        /// </summary>
        public virtual void PostBind(BindResult result)
        {
        }
    }
}
