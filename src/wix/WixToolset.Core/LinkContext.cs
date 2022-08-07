// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class LinkContext : ILinkContext
    {
        internal LinkContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyCollection<ILinkerExtension> Extensions { get; set; }

        public IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        public OutputType ExpectedOutputType { get; set; }

        public string IntermediateFolder { get; set; }

        public IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        public string OutputPath { get; set; }

        public ISymbolDefinitionCreator SymbolDefinitionCreator { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
