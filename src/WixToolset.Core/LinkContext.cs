// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class LinkContext : ILinkContext
    {
        internal LinkContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public IEnumerable<ILinkerExtension> Extensions { get; set; }

        public IEnumerable<IExtensionData> ExtensionData { get; set; }

        public OutputType ExpectedOutputType { get; set; }

        public IEnumerable<Intermediate> Intermediates { get; set; }

        public ISymbolDefinitionCreator SymbolDefinitionCreator { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
