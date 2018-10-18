// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class DecompileContext : IDecompileContext
    {
        internal DecompileContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public OutputType DecompileType { get; set; }

        public IEnumerable<IDecompilerExtension> Extensions { get; set; }

        public string IntermediateFolder { get; set; }

        public string OutputPath { get; set; }
    }
}
