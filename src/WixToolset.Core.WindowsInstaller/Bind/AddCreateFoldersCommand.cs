// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Add CreateFolder symbols, if not already present, for null-keypath components.
    /// </summary>
    internal class AddCreateFoldersCommand
    {
        internal AddCreateFoldersCommand(IntermediateSection section)
        {
            this.Section = section;
        }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var createFolderSymbolsByComponentRef = new HashSet<string>(this.Section.Symbols.OfType<CreateFolderSymbol>().Select(t => t.ComponentRef));
            foreach (var componentSymbol in this.Section.Symbols.OfType<ComponentSymbol>().Where(t => t.KeyPathType == ComponentKeyPathType.Directory).ToList())
            {
                if (!createFolderSymbolsByComponentRef.Contains(componentSymbol.Id.Id))
                {
                    this.Section.AddSymbol(new CreateFolderSymbol(componentSymbol.SourceLineNumbers)
                    {
                        DirectoryRef = componentSymbol.DirectoryRef,
                        ComponentRef = componentSymbol.Id.Id,
                    });
                }
            }
        }
    }
}