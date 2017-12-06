// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface ILinkContext
    {
        IServiceProvider ServiceProvider { get; }

        Messaging Messaging { get; set; }

        IEnumerable<ILinkerExtension> Extensions { get; set; }

        IEnumerable<IExtensionData> ExtensionData { get; set; }

        OutputType ExpectedOutputType { get; set; }

        IEnumerable<Intermediate> Intermediates { get; set; }

        ITupleDefinitionCreator TupleDefinitionCreator { get; set; }
    }
}
