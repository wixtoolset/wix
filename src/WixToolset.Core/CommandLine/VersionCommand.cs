// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;

namespace WixToolset.Core
{
    internal class VersionCommand : ICommandLineCommand
    {
        public int Execute()
        {
            Console.WriteLine("wix version {0}", ThisAssembly.AssemblyInformationalVersion);
            Console.WriteLine();

            return 0;
        }
    }
}
