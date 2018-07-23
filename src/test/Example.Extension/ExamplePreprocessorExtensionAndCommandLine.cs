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

        public IEnumerable<ExtensionCommandLineSwitch> CommandLineSwitches => throw new NotImplementedException();

        public ExamplePreprocessorExtensionAndCommandLine()
        {
            this.Prefixes = new[] { "ex" };
        }

        public void PreParse(ICommandLineContext context)
        {
        }

        public bool TryParseArgument(IParseCommandLine parseCommandLine, string arg)
        {
            if (parseCommandLine.IsSwitch(arg) && arg.Substring(1).Equals("example", StringComparison.OrdinalIgnoreCase))
            {
                this.exampleValueFromCommandLine = parseCommandLine.GetNextArgumentOrError(arg);
                return true;
            }

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