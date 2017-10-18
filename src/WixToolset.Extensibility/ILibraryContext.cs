// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface ILibraryContext
    {
        bool BindFiles { get; set; }

        IEnumerable<ILibrarianExtension> Extensions { get; set; }

        IEnumerable<Localization> Localizations { get; set; }

        IEnumerable<Section> Sections { get; set; }

        IBindVariableResolver WixVariableResolver { get; set; }
    }
}
