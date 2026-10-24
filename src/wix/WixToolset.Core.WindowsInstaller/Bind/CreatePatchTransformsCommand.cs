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
        public CreatePatchTransformsCommand(IMessaging messaging, IBackendHelper backendHelper, IFileSystem fileSystem, IPathResolver pathResolver, IFileResolver fileResolver, IReadOnlyCollection<IResolverExtension> resolverExtensions, IReadOnlyCollection<IWindowsInstallerBackendBinderExtension> backendExtensions, Intermediate intermediate, string intermediateFolder, IReadOnlyCollection<IBindPath> bindPaths)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.FileSystem = fileSystem;
            this.PathResolver = pathResolver;
            this.FileResolver = fileResolver;
            this.ResolverExtensions = resolverExtensions;
            this.BackendExtensions = backendExtensions;
            this.Intermediate = intermediate;
            this.IntermediateFolder = intermediateFolder;
            this.BindPaths = bindPaths;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IFileSystem FileSystem { get; }

        private IPathResolver PathResolver { get; }

        private IFileResolver FileResolver { get; }

        private IReadOnlyCollection<IResolverExtension> ResolverExtensions { get; }

        private IReadOnlyCollection<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private Intermediate Intermediate { get; }

        private string IntermediateFolder { get; }

        private IReadOnlyCollection<IBindPath> BindPaths { get; }

        public PatchFilterMap PatchFilterMap { get; private set; }

        public IEnumerable<PatchTransform> PatchTransforms { get; private set; }

        public IEnumerable<PatchTransform> Execute()
        {
            var patchFilterMap = new PatchFilterMap();
            var patchTransforms = new List<PatchTransform>();

            var symbols = this.Intermediate.Sections.SelectMany(s => s.Symbols);

            var patchBaselineSymbols = symbols.OfType<WixPatchBaselineSymbol>();

            var patchRefSymbols = symbols.OfType<WixPatchRefSymbol>().ToList();

            var patchMediaSymbols = symbols.OfType<MediaSymbol>().ToList();

            foreach (var symbol in patchBaselineSymbols)
            {
                var targetData = this.GetWindowsInstallerData(symbol.BaselineFile.Path, BindStage.Target);
                var updatedData = this.GetWindowsInstallerData(symbol.UpdateFile.Path, BindStage.Updated);

                this.ValidatePatchMediaDoesNotConflictWithTarget(symbol, patchMediaSymbols, targetData);

                if (patchRefSymbols.Count > 0)
                {
                    var targetCommand = new GeneratePatchFilterIdsCommand(this.BackendExtensions, targetData, "target:");
                    targetCommand.Execute();

                    patchFilterMap.AddTargetRowFilterIds(targetCommand.RowToFilterId);

                    var updatedCommand = new GeneratePatchFilterIdsCommand(this.BackendExtensions, updatedData, "updated:");
                    updatedCommand.Execute();

                    patchFilterMap.AddUpdatedRowFilterIds(updatedCommand.RowToFilterId);
                }

                var command = new GenerateTransformCommand(this.Messaging, targetData, updatedData, patchFilterMap, preserveUnchangedRows: true, showPedanticMessages: false);
                var transform = command.Execute();

                patchTransforms.Add(new PatchTransform(symbol.Id.Id, transform));
            }

            this.PatchFilterMap = patchFilterMap;
            this.PatchTransforms = patchTransforms;

            return this.PatchTransforms;
        }

        private void ValidatePatchMediaDoesNotConflictWithTarget(WixPatchBaselineSymbol baselineSymbol, IEnumerable<MediaSymbol> patchMediaSymbols, WindowsInstallerData targetData)
        {
            // The patch's Media row is added to the target when the patch is applied, so reusing a DiskId from the target
            // replaces the target's Media row and breaks source resolution for the target's files.
            if (targetData?.Tables.TryGetTable("Media", out var targetMediaTable) == true && targetMediaTable.Rows.Count > 0)
            {
                var targetDiskIds = targetMediaTable.Rows.Select(r => r.FieldAsInteger(0)).ToList();

                if (targetDiskIds.Contains(baselineSymbol.DiskId))
                {
                    var mediaSymbol = patchMediaSymbols.FirstOrDefault(m => m.DiskId == baselineSymbol.DiskId);

                    this.Messaging.Write(WindowsInstallerBackendErrors.PatchMediaDiskIdConflictsWithTarget(mediaSymbol?.SourceLineNumbers ?? baselineSymbol.SourceLineNumbers, baselineSymbol.DiskId, baselineSymbol.Id.Id, targetDiskIds.Max() + 1));
                }
            }
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

                var command = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, this.FileSystem, this.PathResolver, path, null, OutputType.Package, exportBasePath, extractFilesFolder, this.IntermediateFolder, enableDemodularization: false, skipSummaryInfo: false);
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
