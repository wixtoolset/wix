// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IWindowsInstallerBackendDecompilerExtension
    {
        /// <summary>
        /// Called before decompiling occurs.
        /// </summary>
        void PreBackendDecompile(IDecompileContext context);

        // TODO: Redesign this interface to be useful.

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBackendDecompile(IDecompileResult result);
    }
}
