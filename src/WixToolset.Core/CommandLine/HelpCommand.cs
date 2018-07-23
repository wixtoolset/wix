// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using WixToolset.Extensibility.Data;

    internal class HelpCommand : ICommandLineCommand
    {
        public HelpCommand(Commands command)
        {
            this.Command = command;
        }

        public Commands Command { get; }

        public int Execute()
        {
            if (this.Command == Commands.Unknown)
            {
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine();
            }

            return -1;
        }
    }
}
