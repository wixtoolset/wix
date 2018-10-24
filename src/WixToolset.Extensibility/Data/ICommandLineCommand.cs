// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Extensibility.Services;

    public interface ICommandLineCommand
    {
        bool ShowLogo { get; }

        bool StopParsing { get; }

        int Execute();

        bool TryParseArgument(ICommandLineParser parser, string argument); 
    }
}
