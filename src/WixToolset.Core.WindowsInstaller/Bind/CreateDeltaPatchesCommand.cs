// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Creates delta patches and updates the appropriate rows to point to the newly generated patches.
    /// </summary>
    internal class CreateDeltaPatchesCommand
    {
        public CreateDeltaPatchesCommand(List<IFileFacade> fileFacades, string intermediateFolder, WixPatchSymbol wixPatchId)
        {
            this.FileFacades = fileFacades;
            this.IntermediateFolder = intermediateFolder;
            this.WixPatchId = wixPatchId;
        }

        private IEnumerable<IFileFacade> FileFacades { get; }

        private WixPatchSymbol WixPatchId { get; }

        private string IntermediateFolder { get; }

        public void Execute()
        {
            var optimizePatchSizeForLargeFiles = this.WixPatchId?.OptimizePatchSizeForLargeFiles ?? false;
            var apiPatchingSymbolFlags = (PatchSymbolFlags)(this.WixPatchId?.ApiPatchingSymbolFlags ?? 0);

#if TODO_PATCHING_DELTA
            foreach (FileFacade facade in this.FileFacades)
            {
                if (RowOperation.Modify == facade.File.Operation &&
                    0 != (facade.WixFile.PatchAttributes & PatchAttributeType.IncludeWholeFile))
                {
                    string deltaBase = String.Concat("delta_", facade.File.File);
                    string deltaFile = Path.Combine(this.IntermediateFolder, String.Concat(deltaBase, ".dpf"));
                    string headerFile = Path.Combine(this.IntermediateFolder, String.Concat(deltaBase, ".phd"));

                    bool retainRangeWarning = false;

                    if (PatchAPI.PatchInterop.CreateDelta(
                            deltaFile,
                            facade.WixFile.Source,
                            facade.DeltaPatchFile.Symbols,
                            facade.DeltaPatchFile.RetainOffsets,
                            new[] { facade.WixFile.PreviousSource },
                            facade.DeltaPatchFile.PreviousSymbols.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousIgnoreLengths.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousIgnoreOffsets.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousRetainLengths.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousRetainOffsets.Split(new[] { ';' }),
                            apiPatchingSymbolFlags,
                            optimizePatchSizeForLargeFiles,
                            out retainRangeWarning))
                    {
                        PatchAPI.PatchInterop.ExtractDeltaHeader(deltaFile, headerFile);

                        facade.WixFile.Source = deltaFile;
                        facade.WixFile.DeltaPatchHeaderSource = headerFile;
                    }

                    if (retainRangeWarning)
                    {
                        // TODO: get patch family to add to warning message for PatchWiz parity.
                        Messaging.Instance.OnMessage(WixWarnings.RetainRangeMismatch(facade.File.SourceLineNumbers, facade.File.File));
                    }
                }
            }
#endif

            throw new NotImplementedException();
        }
    }
}
