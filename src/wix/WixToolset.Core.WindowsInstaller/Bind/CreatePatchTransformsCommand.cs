// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CreatePatchTransformsCommand
    {
        public CreatePatchTransformsCommand(IMessaging messaging, IBackendHelper backendHelper, IPathResolver pathResolver, IFileResolver fileResolver, IReadOnlyCollection<IResolverExtension> resolverExtensions, Intermediate intermediate, string intermediateFolder, IReadOnlyCollection<IBindPath> bindPaths)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.PathResolver = pathResolver;
            this.FileResolver = fileResolver;
            this.ResolverExtensions = resolverExtensions;
            this.Intermediate = intermediate;
            this.IntermediateFolder = intermediateFolder;
            this.BindPaths = bindPaths;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IPathResolver PathResolver { get; }

        private IFileResolver FileResolver { get; }

        private IReadOnlyCollection<IResolverExtension> ResolverExtensions { get; }

        private Intermediate Intermediate { get; }

        private string IntermediateFolder { get; }

        private IReadOnlyCollection<IBindPath> BindPaths { get; }

        public IEnumerable<PatchTransform> PatchTransforms { get; private set; }

        public IEnumerable<PatchTransform> Execute()
        {
            var patchTransforms = new List<PatchTransform>();

            var symbols = this.Intermediate.Sections.SelectMany(s => s.Symbols);

            var patchBaselineSymbols = symbols.OfType<WixPatchBaselineSymbol>();

            var patchRefSymbols = symbols.OfType<WixPatchRefSymbol>().ToList();

            foreach (var symbol in patchBaselineSymbols)
            {
                var targetData = this.GetWindowsInstallerData(symbol.BaselineFile.Path, BindStage.Target);
                var updatedData = this.GetWindowsInstallerData(symbol.UpdateFile.Path, BindStage.Updated);

                if (patchRefSymbols.Count > 0)
                {
                    var targetCommand = new GenerateSectionIdsCommand(targetData);
                    targetCommand.Execute();

                    var updatedCommand = new GenerateSectionIdsCommand(updatedData);
                    updatedCommand.Execute();
                }

                var command = new GenerateTransformCommand(this.Messaging, targetData, updatedData, preserveUnchangedRows: true, showPedanticMessages: false);
                var transform = command.Execute();

                patchTransforms.Add(new PatchTransform(symbol.Id.Id, transform));
            }

            this.PatchTransforms = patchTransforms;

            return this.PatchTransforms;
        }

        private WindowsInstallerData GetWindowsInstallerData(string path, BindStage stage)
        {
            if (DataLoader.TryLoadWindowsInstallerData(path, true, out var data))
            {
                // Re-resolve file paths only when loading from .wixpdb.
                this.ReResolveWindowsInstallerData(data, stage);
            }
            else
            {
                var stageFolder = $"_{stage.ToString().ToLowerInvariant()}_msi";
                var exportBasePath = Path.Combine(this.IntermediateFolder, stageFolder);
                var extractFilesFolder = Path.Combine(exportBasePath, "File");

                var command = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, this.PathResolver, path, null, OutputType.Product, exportBasePath, extractFilesFolder, this.IntermediateFolder, enableDemodularization: false, skipSummaryInfo: false);
                data = command.Execute();
            }

            return data;
        }

        private void ReResolveWindowsInstallerData(WindowsInstallerData data, BindStage stage)
        {
            var bindPaths = this.BindPaths.Where(b => b.Stage == stage).ToList();

            if (bindPaths.Count == 0)
            {
                return;
            }

            foreach (var table in data.Tables)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var field in row.Fields.Where(f => f.Column.Type == ColumnType.Object))
                    {
                        if (field.PreviousData != null)
                        {
                            try
                            {
                                var originalPath = field.AsString();

                                var resolvedPath = this.FileResolver.ResolveFile(field.PreviousData, this.ResolverExtensions, bindPaths, stage, row.SourceLineNumbers, null);

                                if (!String.Equals(originalPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    field.Data = resolvedPath;
                                }
                            }
                            catch (WixException e)
                            {
                                this.Messaging.Write(e.Error);
                            }
                        }
                    }
                }
            }
        }
    }
}
