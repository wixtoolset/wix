// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all layout extensions implement.
    /// </summary>
    public interface ILayoutExtension
    {
        /// <summary>
        /// Called before layout occurs.
        /// </summary>
        void PreLayout(ILayoutContext context);

        bool CopyFile(string source, string destination);

        bool MoveFile(string source, string destination);

        /// <summary>
        /// Called after all layout occurs.
        /// </summary>
        void PostLayout();
    }
}
