// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Interface all resolver extensions implement.
    /// </summary>
    public interface ILayoutExtension
    {
        /// <summary>
        /// Called before resolving occurs.
        /// </summary>
        void PreLayout(ILayoutContext context);

        /// <summary>
        /// Called after all resolving occurs.
        /// </summary>
        void PostLayout();
    }
}
