// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IBinderExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBind(IBindContext context);

        /// <summary>
        /// Called after all binding occurs.
        /// </summary>
        void PostBind(IBindResult result);
    }
}
