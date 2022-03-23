// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Supports converting Windows Installer databases to source code.
    /// </summary>
    public interface IWindowsInstallerDecompiler
    {
        /// <summary>
        /// Converts Windows Installer database back to source code.
        /// </summary>
        /// <param name="context">Context for decompiling.</param>
        /// <returns>Result of decompilation.</returns>
        IWindowsInstallerDecompileResult Decompile(IWindowsInstallerDecompileContext context);
    }
}
