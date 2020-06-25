// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class GetPackageFacadesCommand
    {
        public GetPackageFacadesCommand(IEnumerable<WixBundlePackageSymbol> chainPackageSymbols, IntermediateSection section)
        {
            this.ChainPackageSymbols = chainPackageSymbols;
            this.Section = section;
        }

        private IEnumerable<WixBundlePackageSymbol> ChainPackageSymbols { get; }

        private IntermediateSection Section { get; }

        public IDictionary<string, PackageFacade> PackageFacades { get; private set; }

        public void Execute()
        {
            var exePackages = this.Section.Symbols.OfType<WixBundleExePackageSymbol>().ToDictionary(t => t.Id.Id);
            var msiPackages = this.Section.Symbols.OfType<WixBundleMsiPackageSymbol>().ToDictionary(t => t.Id.Id);
            var mspPackages = this.Section.Symbols.OfType<WixBundleMspPackageSymbol>().ToDictionary(t => t.Id.Id);
            var msuPackages = this.Section.Symbols.OfType<WixBundleMsuPackageSymbol>().ToDictionary(t => t.Id.Id);

            var facades = new Dictionary<string, PackageFacade>();

            foreach (var package in this.ChainPackageSymbols)
            {
                var id = package.Id.Id;
                switch (package.Type)
                {
                case WixBundlePackageType.Exe:
                    if (exePackages.TryGetValue(id, out var exePackage))
                    {
                        facades.Add(id, new PackageFacade(package, exePackage));
                    }
                    break;

                case WixBundlePackageType.Msi:
                    if (msiPackages.TryGetValue(id, out var msiPackage))
                    {
                        facades.Add(id, new PackageFacade(package, msiPackage));
                    }
                    break;

                case WixBundlePackageType.Msp:
                    if (mspPackages.TryGetValue(id, out var mspPackage))
                    {
                        facades.Add(id, new PackageFacade(package, mspPackage));
                    }
                    break;

                case WixBundlePackageType.Msu:
                    if (msuPackages.TryGetValue(id, out var msuPackage))
                    {
                        facades.Add(id, new PackageFacade(package, msuPackage));
                    }
                    break;
                }
            }

            this.PackageFacades = facades;
        }
    }
}
