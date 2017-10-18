// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;

    public interface IInscribeContext
    {
        IServiceProvider ServiceProvider { get; }

        string InputFilePath { get; set; }

        string IntermediateFolder { get; set; }

        Messaging Messaging { get; }

        string OutputFile { get; set; }

        string SignedEngineFile { get; set; }
    }
}
