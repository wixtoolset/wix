// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Diagnostics;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    internal class PackageFacade
    {
        public PackageFacade(WixBundlePackageTuple packageTuple, IntermediateTuple specificPackageTuple)
        {
            Debug.Assert(packageTuple.Id.Id == specificPackageTuple.Id.Id);

            this.PackageTuple = packageTuple;
            this.SpecificPackageTuple = specificPackageTuple;
        }

        public string PackageId => this.PackageTuple.Id.Id;

        public WixBundlePackageTuple PackageTuple { get; }

        public IntermediateTuple SpecificPackageTuple { get; }
    }
}
