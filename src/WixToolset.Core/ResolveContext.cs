// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ResolveContext : IResolveContext
    {
        internal ResolveContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        public IReadOnlyCollection<IResolverExtension> Extensions { get; set; }

        public IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        public IReadOnlyCollection<string> FilterCultures { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public IReadOnlyCollection<Localization> Localizations { get; set; }

        public IVariableResolver VariableResolver { get; set; }

        public bool AllowUnresolvedVariables { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
