// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using WixToolset.Extensibility.Services;

#pragma warning disable 1591 // TODO: add documentation
    public interface IInscribeContext
    {
        IServiceProvider ServiceProvider { get; }

        string InputFilePath { get; set; }

        string IntermediateFolder { get; set; }

        string OutputFile { get; set; }

        string SignedEngineFile { get; set; }
    }
}
