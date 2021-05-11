// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class LayoutContext : ILayoutContext
    {
        internal LayoutContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyCollection<ILayoutExtension> Extensions { get; set; }

        public IReadOnlyCollection<IFileSystemExtension> FileSystemExtensions { get; set; }

        public IReadOnlyCollection<IFileTransfer> FileTransfers { get; set; }

        public IReadOnlyCollection<ITrackedFile> TrackedFiles { get; set; }

        public string IntermediateFolder { get; set; }

        public string ContentsFile { get; set; }

        public string OutputsFile { get; set; }

        public string BuiltOutputsFile { get; set; }

        public bool ResetAcls { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
