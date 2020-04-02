// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    public interface IBindContext
    {
        IWixToolsetServiceProvider ServiceProvider { get; }

        string BurnStubPath { get; set; }

        int CabbingThreadCount { get; set; }

        string CabCachePath { get; set; }

        int Codepage { get; set; }

        CompressionLevel? DefaultCompressionLevel { get; set; }

        IEnumerable<IDelayedField> DelayedFields { get; set; }

        IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        IEnumerable<IBinderExtension> Extensions { get; set; }

        IEnumerable<IFileSystemExtension> FileSystemExtensions { get; set; }

        IEnumerable<string> Ices { get; set; }

        string IntermediateFolder { get; set; }

        Intermediate IntermediateRepresentation { get; set; }

        string OutputPath { get; set; }

        PdbType PdbType { get; set; }

        string PdbPath { get; set; }

        IEnumerable<string> SuppressIces { get; set; }

        bool SuppressValidation { get; set; }
    }
}
