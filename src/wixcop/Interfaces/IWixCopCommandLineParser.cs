// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.WixCop.Interfaces
{
    using WixToolset.Extensibility.Data;

    public interface IWixCopCommandLineParser
    {
        ICommandLineArguments Arguments { get; set; }

        ICommandLineCommand ParseWixCopCommandLine();
    }
}
