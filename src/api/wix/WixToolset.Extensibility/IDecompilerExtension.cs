// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Base class for creating a decompiler extension.
    /// </summary>
    public interface IDecompilerExtension
    {
        /// <summary>
        /// Called before decompiling occurs.
        /// </summary>
        void PreDecompile(IDecompileContext context);

        /// <summary>
        /// Called after all decompiling occurs.
        /// </summary>
        void PostDecompile(IDecompileResult result);
    }
}
