// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Extensibility.Data;

    public interface ICommandLine
    {
        IExtensionManager ExtensionManager { get; set; }

        ICommandLineArguments Arguments { get; set; }

        ICommandLineCommand ParseStandardCommandLine();
    }
}
