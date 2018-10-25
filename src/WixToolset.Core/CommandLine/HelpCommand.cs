// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class HelpCommand : ICommandLineCommand
    {
        public bool ShowLogo => true;

        public bool StopParsing => true;

        public int Execute()
        {
            Console.WriteLine("TODO: Show list of available commands");

            return -1;
        }

        public bool TryParseArgument(ICommandLineParser parseHelper, string argument)
        {
            return true; // eat any arguments
        }
    }
}
