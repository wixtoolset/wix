// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public interface IFileSystemContext
    {
        IServiceProvider ServiceProvider { get; }

        IMessaging Messaging { get; set; }

        string CabCachePath { get; set; }

        string IntermediateFolder { get; set; }

        Intermediate IntermediateRepresentation { get; set; }

        string OutputPath { get; set; }

        string OutputPdbPath { get; set; }
    }
}
