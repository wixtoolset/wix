// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface IBindContext
    {
        IServiceProvider ServiceProvider { get; }

        Messaging Messaging { get; set; }

        IEnumerable<BindPath> BindPaths { get; set; }

        int CabbingThreadCount { get; set; }

        string CabCachePath { get; set; }

        int Codepage { get; set; }

        CompressionLevel DefaultCompressionLevel { get; set; }

        IEnumerable<IDelayedField> DelayedFields { get; set; }

        IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        IExtensionManager ExtensionManager { get; set; }

        IEnumerable<IBinderExtension> Extensions { get; set; }

        IEnumerable<string> Ices { get; set; }

        string IntermediateFolder { get; set; }

        Output IntermediateRepresentation { get; set; }

        string OutputPath { get; set; }

        string OutputPdbPath { get; set; }

        bool SuppressAclReset { get; set; }

        IEnumerable<string> SuppressIces { get; set; }

        bool SuppressValidation { get; set; }

        IBindVariableResolver WixVariableResolver { get; set; }

        string ContentsFile { get; set; }

        string OutputsFile { get; set; }

        string BuiltOutputsFile { get; set; }

        string WixprojectFile { get; set; }
    }
}
