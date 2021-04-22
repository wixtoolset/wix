// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface ILinkerExtension
    {
        /// <summary>
        /// Called before linking occurs.
        /// </summary>
        void PreLink(ILinkContext context);

        /// <summary>
        /// Called after all linking occurs.
        /// </summary>
        void PostLink(Intermediate intermediate);
    }
}
