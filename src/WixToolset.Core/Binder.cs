// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Binder of the WiX toolset.
    /// </summary>
    public sealed class Binder
    {
        public Binder(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public int CabbingThreadCount { get; set; }

        public string CabCachePath { get; set; }

        public int Codepage { get; set; }

        public CompressionLevel? DefaultCompressionLevel { get; set; }

        public IEnumerable<IDelayedField> DelayedFields { get; set; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        public IEnumerable<string> Ices { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public string OutputPath { get; set; }

        public string OutputPdbPath { get; set; }

        public IEnumerable<string> SuppressIces { get; set; }

        public bool SuppressValidation { get; set; }

        public bool DeltaBinaryPatch { get; set; }

        public IServiceProvider ServiceProvider { get; }

        public BindResult Execute()
        {
            var context = this.ServiceProvider.GetService<IBindContext>();
            context.CabbingThreadCount = this.CabbingThreadCount;
            context.CabCachePath = this.CabCachePath;
            context.Codepage = this.Codepage;
            context.DefaultCompressionLevel = this.DefaultCompressionLevel;
            context.DelayedFields = this.DelayedFields;
            context.ExpectedEmbeddedFiles = this.ExpectedEmbeddedFiles;
            context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().Create<IBinderExtension>();
            context.Ices = this.Ices;
            context.IntermediateFolder = this.IntermediateFolder;
            context.IntermediateRepresentation = this.IntermediateRepresentation;
            context.OutputPath = this.OutputPath;
            context.OutputPdbPath = this.OutputPdbPath;
            context.SuppressIces = this.SuppressIces;
            context.SuppressValidation = this.SuppressValidation;

            // Prebind.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreBind(context);
            }

            // Bind.
            //
            this.WriteBuildInfoTable(context.IntermediateRepresentation, context.OutputPath, context.OutputPdbPath);

            var bindResult = this.BackendBind(context);

            if (bindResult != null)
            {
                // Postbind.
                //
                foreach (var extension in context.Extensions)
                {
                    extension.PostBind(bindResult);
                }
            }

            return bindResult;
        }

        private BindResult BackendBind(IBindContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendFactories = extensionManager.Create<IBackendFactory>();

            var entrySection = context.IntermediateRepresentation.Sections[0];

            foreach (var factory in backendFactories)
            {
                if (factory.TryCreateBackend(entrySection.Type.ToString(), context.OutputPath, null, out var backend))
                {
                    var result = backend.Bind(context);
                    return result;
                }
            }

            // TODO: messaging that a backend could not be found to bind the output type?

            return null;
        }
        
        private void WriteBuildInfoTable(Intermediate output, string outputFile, string outputPdbPath)
        {
            var entrySection = output.Sections.First(s => s.Type != SectionType.Fragment);

            var executingAssembly = Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);

            var buildInfoTuple = new WixBuildInfoTuple();
            buildInfoTuple.WixVersion = fileVersion.FileVersion;
            buildInfoTuple.WixOutputFile = outputFile;

            if (!String.IsNullOrEmpty(outputPdbPath))
            {
                buildInfoTuple.WixPdbFile = outputPdbPath;
            }

            entrySection.Tuples.Add(buildInfoTuple);
        }
    }
}
