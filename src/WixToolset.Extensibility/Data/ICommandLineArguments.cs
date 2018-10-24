// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Extensibility.Services;

    public interface ICommandLineArguments
    {
        string[] OriginalArguments { get; set; }

        string[] Arguments { get; set; }

        string[] Extensions { get; set; }

        string ErrorArgument { get; set; }

        void Populate(string commandLine);

        void Populate(string[] args);

        ICommandLineParser Parse();
    }
}
