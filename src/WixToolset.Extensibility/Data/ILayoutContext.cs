// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;

    public interface ILayoutContext
    {
        IServiceProvider ServiceProvider { get; }

        IEnumerable<ILayoutExtension> Extensions { get; set; }

        IEnumerable<ITrackedFile> TrackedFiles { get; set; }

        IEnumerable<IFileTransfer> FileTransfers { get; set; }

        string ContentsFile { get; set; }

        string OutputsFile { get; set; }

        string IntermediateFolder { get; set; }

        string BuiltOutputsFile { get; set; }

        bool SuppressAclReset { get; set; }
    }
}
