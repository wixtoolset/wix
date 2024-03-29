// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;

    /// <summary>
    /// Command information passed from the engine for the BA to perform.
    /// </summary>
    public interface IBootstrapperCommand
    {
        /// <summary>
        /// Gets the action for the BA to perform.
        /// </summary>
        LaunchAction Action { get; }

        /// <summary>
        /// Gets the display level for the BA.
        /// </summary>
        Display Display { get; }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <returns>
        /// Command line arguments not handled by the engine.
        /// </returns>
        string CommandLine { get; }

        /// <summary>
        /// Hint for the initial visibility of the window.
        /// </summary>
        int CmdShow { get; }

        /// <summary>
        /// Gets the method of how the engine was resumed from a previous installation step.
        /// </summary>
        ResumeType Resume { get; }

        /// <summary>
        /// Gets the handle to the splash screen window. If no splash screen was displayed this value will be IntPtr.Zero.
        /// </summary>
        IntPtr SplashScreen { get; }

        /// <summary>
        /// If this was run from a related bundle, specifies the relation type.
        /// </summary>
        RelationType Relation { get; }

        /// <summary>
        /// If this was run from a backward compatible bundle.
        /// </summary>
        bool Passthrough { get; }

        /// <summary>
        /// Gets layout directory.
        /// </summary>
        string LayoutDirectory { get; }

        /// <summary>
        /// Gets bootstrapper working folder.
        /// </summary>
        string BootstrapperWorkingFolder { get; }

        /// <summary>
        /// Gets path to BootstrapperApplicationData.xml.
        /// </summary>
        string BootstrapperApplicationDataPath { get; }

        /// <summary>
        /// Parses the command line arguments into an <see cref="IMbaCommand"/>.
        /// </summary>
        /// <returns>
        /// The parsed information.
        /// </returns>
        /// <exception type="Win32Exception">The command line could not be parsed.</exception>
        IMbaCommand ParseCommandLine();
    }
}
