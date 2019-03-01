// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface ILibraryContext
    {
        IServiceProvider ServiceProvider { get; }

        bool BindFiles { get; set; }

        IEnumerable<IBindPath> BindPaths { get; set; }

        IEnumerable<ILibrarianExtension> Extensions { get; set; }

        string LibraryId { get; set; }

        IEnumerable<Localization> Localizations { get; set; }

        IEnumerable<Intermediate> Intermediates { get; set; }
    }
}
