// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class GetFileFacadesCommand
    {
        public GetFileFacadesCommand(IntermediateSection section, IWindowsInstallerBackendHelper backendHelper)
        {
            this.Section = section;
            this.BackendHelper = backendHelper;
        }

        private IntermediateSection Section { get; }

        private IWindowsInstallerBackendHelper BackendHelper { get; }

        public List<IFileFacade> FileFacades { get; private set; }

        public void Execute()
        {
            var facades = new List<IFileFacade>();

            var assemblyFile = this.Section.Symbols.OfType<AssemblySymbol>().ToDictionary(t => t.Id.Id);
#if TODO_PATCHING_DELTA
            //var deltaPatchFiles = this.Section.Symbols.OfType<WixDeltaPatchFileSymbol>().ToDictionary(t => t.Id.Id);
#endif

            foreach (var file in this.Section.Symbols.OfType<FileSymbol>())
            {
                assemblyFile.TryGetValue(file.Id.Id, out var assembly);

#if TODO_PATCHING_DELTA
                //deltaPatchFiles.TryGetValue(file.Id.Id, out var deltaPatchFile);
                // TODO: should we be passing along delta information to the file facade? Probably, right?
#endif
                var fileFacade = this.BackendHelper.CreateFileFacade(file, assembly);

                facades.Add(fileFacade);
            }

#if TODO_PATCHING_DELTA
            this.ResolveDeltaPatchSymbolPaths(deltaPatchFiles, facades);
#endif

            this.FileFacades = facades;
        }

#if TODO_PATCHING_DELTA
        /// <summary>
        /// Merge data from the WixPatchSymbolPaths rows into the WixDeltaPatchFile rows.
        /// </summary>
        public void ResolveDeltaPatchSymbolPaths(Dictionary<string, WixDeltaPatchFileSymbol> deltaPatchFiles, IEnumerable<FileFacade> facades)
        {
            ILookup<string, FileFacade> filesByComponent = null;
            ILookup<string, FileFacade> filesByDirectory = null;
            ILookup<string, FileFacade> filesByDiskId = null;

            foreach (var row in this.Section.Symbols.OfType<WixDeltaPatchSymbolPathsSymbol>().OrderBy(r => r.SymbolType))
            {
                switch (row.SymbolType)
                {
                    case SymbolPathType.File:
                        this.MergeSymbolPaths(row, deltaPatchFiles[row.SymbolId]);
                        break;

                    case SymbolPathType.Component:
                        if (null == filesByComponent)
                        {
                            filesByComponent = facades.ToLookup(f => f.File.ComponentRef);
                        }

                        foreach (var facade in filesByComponent[row.SymbolId])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.Id.Id]);
                        }
                        break;

                    case SymbolPathType.Directory:
                        if (null == filesByDirectory)
                        {
                            filesByDirectory = facades.ToLookup(f => f.File.DirectoryRef);
                        }

                        foreach (var facade in filesByDirectory[row.SymbolId])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.Id.Id]);
                        }
                        break;

                    case SymbolPathType.Media:
                        if (null == filesByDiskId)
                        {
                            filesByDiskId = facades.ToLookup(f => f.File.DiskId.ToString(CultureInfo.InvariantCulture));
                        }

                        foreach (var facade in filesByDiskId[row.SymbolId])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.Id.Id]);
                        }
                        break;

                    case SymbolPathType.Product:
                        foreach (var fileRow in deltaPatchFiles.Values)
                        {
                            this.MergeSymbolPaths(row, fileRow);
                        }
                        break;

                    default:
                        // error
                        break;
                }
            }
        }

        /// <summary>
        /// Merge data from a row in the WixPatchSymbolsPaths table into an associated WixDeltaPatchFile row.
        /// </summary>
        /// <param name="row">Row from the WixPatchSymbolsPaths table.</param>
        /// <param name="file">FileRow into which to set symbol information.</param>
        /// <comment>This includes PreviousData as well.</comment>
        private void MergeSymbolPaths(WixDeltaPatchSymbolPathsSymbol row, WixDeltaPatchFileSymbol file)
        {
            if (file.SymbolPaths is null)
            {
                file.SymbolPaths = row.SymbolPaths;
            }
            else
            {
                file.SymbolPaths = String.Concat(file.SymbolPaths, ";", row.SymbolPaths);
            }

            Field field = row.Fields[2];
            if (null != field.PreviousData)
            {
                if (null == file.PreviousSymbols)
                {
                    file.PreviousSymbols = field.PreviousData;
                }
                else
                {
                    file.PreviousSymbols = String.Concat(file.PreviousSymbols, ";", field.PreviousData);
                }
            }
        }
#endif
    }
}
