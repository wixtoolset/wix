// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Interface extensions implement to be able to parse command-line options.
    /// </summary>
    public interface IExtensionCommandLine
    {
        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches { get; }

        void PreParse(ICommandLineContext context);

        bool TryParseArgument(ICommandLineParser parser, string argument);

        bool TryParseCommand(ICommandLineParser parser, out ICommandLineCommand command);

        void PostParse();
    }
}
