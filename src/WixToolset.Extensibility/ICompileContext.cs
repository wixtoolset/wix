// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;

    public interface ICompileContext
    {
        IServiceProvider ServiceProvider { get; }

        Messaging Messaging { get; set; }

        string CompilationId { get; set; }

        IEnumerable<ICompilerExtension> Extensions { get; set; }

        string OutputPath { get; set; }

        Platform Platform { get; set; }

        XDocument Source { get; set; }
    }
}
