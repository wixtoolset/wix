// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class GetFileFacadesCommand : ICommand
    {
        public Table FileTable { private get; set; }

        public Table WixFileTable { private get; set; }

        public Table WixDeltaPatchFileTable { private get; set; }

        public Table WixDeltaPatchSymbolPathsTable { private get; set; }

        public List<FileFacade> FileFacades { get; private set; }

        public void Execute()
        {
            List<FileFacade> facades = new List<FileFacade>(this.FileTable.Rows.Count);

            RowDictionary<WixFileRow> wixFiles = new RowDictionary<WixFileRow>(this.WixFileTable);
            RowDictionary<WixDeltaPatchFileRow> deltaPatchFiles = new RowDictionary<WixDeltaPatchFileRow>(this.WixDeltaPatchFileTable);

            foreach (FileRow file in this.FileTable.Rows)
            {
                WixDeltaPatchFileRow deltaPatchFile = null;

                deltaPatchFiles.TryGetValue(file.File, out deltaPatchFile);

                facades.Add(new FileFacade(file, wixFiles[file.File], deltaPatchFile));
            }

            if (null != this.WixDeltaPatchSymbolPathsTable)
            {
                this.ResolveDeltaPatchSymbolPaths(deltaPatchFiles, facades);
            }

            this.FileFacades = facades;
        }

        /// <summary>
        /// Merge data from the WixPatchSymbolPaths rows into the WixDeltaPatchFile rows.
        /// </summary>
        public RowDictionary<WixDeltaPatchFileRow> ResolveDeltaPatchSymbolPaths(RowDictionary<WixDeltaPatchFileRow> deltaPatchFiles, IEnumerable<FileFacade> facades)
        {
            ILookup<string, FileFacade> filesByComponent = null;
            ILookup<string, FileFacade> filesByDirectory = null;
            ILookup<string, FileFacade> filesByDiskId = null;

            foreach (WixDeltaPatchSymbolPathsRow row in this.WixDeltaPatchSymbolPathsTable.RowsAs<WixDeltaPatchSymbolPathsRow>().OrderBy(r => r.Type))
            {
                switch (row.Type)
                {
                    case SymbolPathType.File:
                        this.MergeSymbolPaths(row, deltaPatchFiles[row.Id]);
                        break;

                    case SymbolPathType.Component:
                        if (null == filesByComponent)
                        {
                            filesByComponent = facades.ToLookup(f => f.File.Component);
                        }

                        foreach (FileFacade facade in filesByComponent[row.Id])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.File]);
                        }
                        break;

                    case SymbolPathType.Directory:
                        if (null == filesByDirectory)
                        {
                            filesByDirectory = facades.ToLookup(f => f.WixFile.Directory);
                        }

                        foreach (FileFacade facade in filesByDirectory[row.Id])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.File]);
                        }
                        break;

                    case SymbolPathType.Media:
                        if (null == filesByDiskId)
                        {
                            filesByDiskId = facades.ToLookup(f => f.WixFile.DiskId.ToString(CultureInfo.InvariantCulture));
                        }

                        foreach (FileFacade facade in filesByDiskId[row.Id])
                        {
                            this.MergeSymbolPaths(row, deltaPatchFiles[facade.File.File]);
                        }
                        break;

                    case SymbolPathType.Product:
                        foreach (WixDeltaPatchFileRow fileRow in deltaPatchFiles.Values)
                        {
                            this.MergeSymbolPaths(row, fileRow);
                        }
                        break;

                    default:
                        // error
                        break;
                }
            }

            return deltaPatchFiles;
        }

        /// <summary>
        /// Merge data from a row in the WixPatchSymbolsPaths table into an associated WixDeltaPatchFile row.
        /// </summary>
        /// <param name="row">Row from the WixPatchSymbolsPaths table.</param>
        /// <param name="file">FileRow into which to set symbol information.</param>
        /// <comment>This includes PreviousData as well.</comment>
        private void MergeSymbolPaths(WixDeltaPatchSymbolPathsRow row, WixDeltaPatchFileRow file)
        {
            if (null == file.Symbols)
            {
                file.Symbols = row.SymbolPaths;
            }
            else
            {
                file.Symbols = String.Concat(file.Symbols, ";", row.SymbolPaths);
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
    }
}
