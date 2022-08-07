// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Command-line context.
    /// </summary>
    public interface ICommandLineContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Extension manager.
        /// </summary>
        IExtensionManager ExtensionManager { get; set; }

        /// <summary>
        /// Command-line arguments.
        /// </summary>
        ICommandLineArguments Arguments { get; set; }
    }
}
