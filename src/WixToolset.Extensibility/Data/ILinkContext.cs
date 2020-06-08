// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public interface ILinkContext
    {
        IWixToolsetServiceProvider ServiceProvider { get; }

        IEnumerable<ILinkerExtension> Extensions { get; set; }

        IEnumerable<IExtensionData> ExtensionData { get; set; }

        OutputType ExpectedOutputType { get; set; }

        IEnumerable<Intermediate> Intermediates { get; set; }

        ITupleDefinitionCreator TupleDefinitionCreator { get; set; }

        CancellationToken CancellationToken { get; set; }
    }
}
