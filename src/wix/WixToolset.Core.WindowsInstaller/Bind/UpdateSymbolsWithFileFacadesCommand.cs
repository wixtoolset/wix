// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class UpdateSymbolsWithFileFacadesCommand
    {
        private readonly IntermediateSection section;
        private readonly List<IFileFacade> allFileFacades;

        public UpdateSymbolsWithFileFacadesCommand(IntermediateSection section, List<IFileFacade> allFileFacades)
        {
            this.section = section;
            this.allFileFacades = allFileFacades;
        }

        public void Execute()
        {
            var fileSymbolsById = this.section.Symbols.OfType<FileSymbol>().ToDictionary(f => f.Id.Id);

            foreach (var facade in this.allFileFacades)
            {
                if (fileSymbolsById.TryGetValue(facade.Id, out var fileSymbol))
                {
                    // Only update the file symbol if the facade value changed
                    if (fileSymbol.DiskId != facade.DiskId)
                    {
                        fileSymbol.DiskId = facade.DiskId;
                    }

                    if (fileSymbol.FileSize != facade.FileSize)
                    {
                        fileSymbol.FileSize = facade.FileSize;
                    }

                    if (fileSymbol.Language != facade.Language)
                    {
                        fileSymbol.Language = facade.Language;
                    }

                    if (fileSymbol.Sequence != facade.Sequence)
                    {
                        fileSymbol.Sequence = facade.Sequence;
                    }

                    if (fileSymbol.Version != facade.Version)
                    {
                        fileSymbol.Version = facade.Version;
                    }
                }

                if (facade.MsiFileHashSymbol != null)
                {
                    this.section.AddSymbol(facade.MsiFileHashSymbol);
                }

                if (facade.AssemblyNameSymbols != null)
                {
                    foreach (var assemblyNameSymbol in facade.AssemblyNameSymbols)
                    {
                        this.section.AddSymbol(assemblyNameSymbol);
                    }
                }
            }
        }
    }
}
