// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ExamplePreprocessorExtensionAndCommandLine : BasePreprocessorExtension, IExtensionCommandLine
    {
        private string exampleValueFromCommandLine;

        public IReadOnlyCollection<ExtensionCommandLineSwitch> CommandLineSwitches => throw new NotImplementedException();

        public ExamplePreprocessorExtensionAndCommandLine()
        {
            this.Prefixes = new[] { "ex" };
        }

        public void PreParse(ICommandLineContext context)
        {
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument) && argument.Substring(1).Equals("example", StringComparison.OrdinalIgnoreCase))
            {
                this.exampleValueFromCommandLine = parser.GetNextArgumentOrError(argument);
                return true;
            }

            return false;
        }

        public bool TryParseCommand(ICommandLineParser parser, string argument, out ICommandLineCommand command)
        {
            command = null;
            return false;
        }

        public void PostParse()
        {
        }

        public override string GetVariableValue(string prefix, string name)
        {
            if (prefix == "ex" && "test".Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return String.IsNullOrWhiteSpace(this.exampleValueFromCommandLine) ? "(null)" : this.exampleValueFromCommandLine;
            }

            return null;
        }
    }
}
