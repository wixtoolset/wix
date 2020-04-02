// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CompileContext : ICompileContext
    {
        internal CompileContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public string CompilationId { get; set; }

        public IEnumerable<ICompilerExtension> Extensions { get; set; }

        public string OutputPath { get; set; }

        public Platform Platform { get; set; }

        public XDocument Source { get; set; }
    }
}
