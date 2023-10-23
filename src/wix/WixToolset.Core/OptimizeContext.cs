// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class OptimizeContext : IOptimizeContext
    {
        internal OptimizeContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyCollection<IOptimizerExtension> Extensions { get; set; }

        public string IntermediateFolder { get; set; }

        public IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        public IDictionary<string, string> BindVariables { get; set; }

        public Platform Platform { get; set; }

        public bool IsCurrentPlatform64Bit => this.Platform == Platform.ARM64 || this.Platform == Platform.X64;

        public IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        public IReadOnlyCollection<Localization> Localizations { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
