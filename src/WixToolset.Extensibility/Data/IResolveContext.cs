// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

#pragma warning disable 1591 // TODO: add documentation
    public interface IResolveContext
    {
        IServiceProvider ServiceProvider { get; }

        IEnumerable<IBindPath> BindPaths { get; set; }

        IEnumerable<IResolverExtension> Extensions { get; set; }

        IEnumerable<IExtensionData> ExtensionData { get; set; }

        IEnumerable<string> FilterCultures { get; set; }

        string IntermediateFolder { get; set; }

        Intermediate IntermediateRepresentation { get; set; }

        IEnumerable<Localization> Localizations { get; set; }

        bool AllowUnresolvedVariables { get; set; }

        CancellationToken CancellationToken { get; set; }
    }
}
