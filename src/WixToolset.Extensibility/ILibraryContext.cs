// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public interface ILibraryContext
    {
        IServiceProvider ServiceProvider { get; }

        IMessaging Messaging { get; set; }

        bool BindFiles { get; set; }

        IEnumerable<BindPath> BindPaths { get; set; }

        IEnumerable<ILibrarianExtension> Extensions { get; set; }

        string LibraryId { get; set; }

        IEnumerable<Localization> Localizations { get; set; }

        IEnumerable<Intermediate> Intermediates { get; set; }

        IBindVariableResolver WixVariableResolver { get; set; }
    }
}
