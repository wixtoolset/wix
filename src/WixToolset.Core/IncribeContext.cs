// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class InscribeContext : IInscribeContext
    {
        public InscribeContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public string IntermediateFolder { get; set; }

        public string InputFilePath { get; set; }

        public string SignedEngineFile { get; set; }

        public string OutputFile { get; set; }
    }
}
