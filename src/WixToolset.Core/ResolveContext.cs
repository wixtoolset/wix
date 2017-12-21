// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public class ResolveContext : IResolveContext
    {
        internal ResolveContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; set; }

        public IEnumerable<BindPath> BindPaths { get; set; }

        public IEnumerable<IResolverExtension> Extensions { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public IBindVariableResolver WixVariableResolver { get; set; }
    }
}
