// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for a command-line command.
    /// </summary>
    public abstract class BaseCommandLineCommand : ICommandLineCommand
    {
        /// <summary>
        /// See <see cref="ICommandLineCommand.ShowLogo" />
        /// </summary>
        public virtual bool ShowLogo => false;

        /// <summary>
        /// See <see cref="ICommandLineCommand.StopParsing" />
        /// </summary>
        public bool StopParsing { get; protected set; }

        /// <summary>
        /// See <see cref="ICommandLineCommand.ExecuteAsync" />
        /// </summary>
        public abstract Task<int> ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// See <see cref="ICommandLineCommand.GetCommandLineHelp" />
        /// </summary>
        public abstract CommandLineHelp GetCommandLineHelp();

        /// <summary>
        /// See <see cref="ICommandLineCommand.TryParseArgument" />
        /// </summary>
        public abstract bool TryParseArgument(ICommandLineParser parser, string argument);
    }
}
