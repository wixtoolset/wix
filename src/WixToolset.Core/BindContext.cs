// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BindContext : IBindContext
    {
        internal BindContext(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IWixToolsetServiceProvider ServiceProvider { get; }

        public IEnumerable<BindPath> BindPaths { get; set; }

        public string BurnStubPath { get; set; }

        public int CabbingThreadCount { get; set; }

        public string CabCachePath { get; set; }

        public int Codepage { get; set; }

        public CompressionLevel? DefaultCompressionLevel { get; set; }

        public IEnumerable<IDelayedField> DelayedFields { get; set; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        public IEnumerable<IBinderExtension> Extensions { get; set; }

        public IEnumerable<IFileSystemExtension> FileSystemExtensions { get; set; }

        public IEnumerable<string> Ices { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public string OutputPath { get; set; }

        public PdbType PdbType { get; set; }

        public string PdbPath { get; set; }

        public IEnumerable<string> SuppressIces { get; set; }

        public bool SuppressValidation { get; set; }

        public bool SuppressLayout { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
