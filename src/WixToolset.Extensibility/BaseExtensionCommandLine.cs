// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for extensions to be able to parse the command-line.
    /// </summary>
    public abstract class BaseExtensionCommandLine : IExtensionCommandLine
    {
        /// <summary>
        /// See <see cref="IExtensionCommandLine.CommandLineSwitches" />
        /// </summary>
        public virtual IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches => Enumerable.Empty<ExtensionCommandLineSwitch>();

        /// <summary>
        /// See <see cref="IExtensionCommandLine.PostParse" />
        /// </summary>
        public virtual void PostParse()
        {
        }

        /// <summary>
        /// See <see cref="IExtensionCommandLine.PreParse" />
        /// </summary>
        public virtual void PreParse(ICommandLineContext context)
        {
        }

        /// <summary>
        /// See <see cref="IExtensionCommandLine.TryParseArgument" />
        /// </summary>
        public virtual bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            return false;
        }

        /// <summary>
        /// See <see cref="IExtensionCommandLine.TryParseCommand" />
        /// </summary>
        public virtual bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;
            return false;
        }
    }
}
