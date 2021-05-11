// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Diagnostics;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class PackageFacade
    {
        public PackageFacade(WixBundlePackageSymbol packageSymbol, IntermediateSymbol specificPackageSymbol)
        {
            Debug.Assert(packageSymbol.Id.Id == specificPackageSymbol.Id.Id);

            this.PackageSymbol = packageSymbol;
            this.SpecificPackageSymbol = specificPackageSymbol;
        }

        public string PackageId => this.PackageSymbol.Id.Id;

        public WixBundlePackageSymbol PackageSymbol { get; }

        public IntermediateSymbol SpecificPackageSymbol { get; }
    }
}
