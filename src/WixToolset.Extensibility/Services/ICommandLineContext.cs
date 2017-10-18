// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using WixToolset.Data;

    public interface ICommandLineContext
    {
        IServiceProvider ServiceProvider { get; }

        Messaging Messaging { get; set; }

        IExtensionManager ExtensionManager { get; set; }

        string Arguments { get; set; }

        string[] ParsedArguments { get; set; }
    }
}
