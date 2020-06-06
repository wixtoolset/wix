// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    public abstract class BaseExtensionCommandLine : IExtensionCommandLine
    {
        public virtual IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches => Enumerable.Empty<ExtensionCommandLineSwitch>();

        public virtual void PostParse()
        {
        }

        public virtual void PreParse(ICommandLineContext context)
        {
        }

        public virtual bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            return false;
        }

        public virtual bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;
            return false;
        }
    }
}
