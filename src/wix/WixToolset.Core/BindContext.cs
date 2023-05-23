// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class BindContext : IBindContext
    {
        internal BindContext(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        public int CabbingThreadCount { get; set; }

        public string CabCachePath { get; set; }

        public CompressionLevel? DefaultCompressionLevel { get; set; }

        public IReadOnlyCollection<IDelayedField> DelayedFields { get; set; }

        public IReadOnlyCollection<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        public IReadOnlyCollection<IBinderExtension> Extensions { get; set; }

        public IReadOnlyCollection<IFileSystemExtension> FileSystemExtensions { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public string OutputPath { get; set; }

        public PdbType PdbType { get; set; }

        public string PdbPath { get; set; }

        public int? ResolvedCodepage { get; set; }

        public int? ResolvedSummaryInformationCodepage { get; set; }

        public int? ResolvedLcid { get; set; }

        public bool SuppressLayout { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
