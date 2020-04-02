// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class LibraryContext : ILibraryContext
    {
        internal LibraryContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; set; }

        public bool BindFiles { get; set; }

        public IEnumerable<IBindPath> BindPaths { get; set; }

        public IEnumerable<ILibrarianExtension> Extensions { get; set; }

        public string LibraryId { get; set; }

        public IEnumerable<Localization> Localizations { get; set; }

        public IEnumerable<Intermediate> Intermediates { get; set; }
    }
}
