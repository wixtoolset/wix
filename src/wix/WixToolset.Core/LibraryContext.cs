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

    internal class LibraryContext : ILibraryContext
    {
        internal LibraryContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; set; }

        public bool BindFiles { get; set; }

        public IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        public IReadOnlyCollection<ILibrarianExtension> Extensions { get; set; }

        public string LibraryId { get; set; }

        public IReadOnlyCollection<Localization> Localizations { get; set; }

        public IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
