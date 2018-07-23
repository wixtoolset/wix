// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    public class LayoutContext : ILayoutContext
    {
        internal LayoutContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IEnumerable<ILayoutExtension> Extensions { get; set; }

        public IEnumerable<IFileSystemExtension> FileSystemExtensions { get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; set; }

        public IEnumerable<string> ContentFilePaths { get; set; }

        public string OutputPdbPath { get; set; }

        public string ContentsFile { get; set; }

        public string OutputsFile { get; set; }

        public string BuiltOutputsFile { get; set; }

        public bool SuppressAclReset { get; set; }
    }
}
