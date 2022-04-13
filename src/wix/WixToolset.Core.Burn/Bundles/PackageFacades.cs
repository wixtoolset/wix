// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    internal class PackageFacades
    {
        private Dictionary<string, PackageFacade> FacadesByPackageId { get; } = new Dictionary<string, PackageFacade>();

        private Dictionary<string, List<PackageFacade>> FacadesByPackagePayloadId { get; } = new Dictionary<string, List<PackageFacade>>();

        private List<PackageFacade> OrderedFacades { get; } = new List<PackageFacade>();

        public void Add(PackageFacade item)
        {
            this.FacadesByPackageId.Add(item.PackageId, item);

            if (!this.FacadesByPackagePayloadId.TryGetValue(item.PackageSymbol.PayloadRef, out var facades))
            {
                facades = new List<PackageFacade>();
                this.FacadesByPackagePayloadId.Add(item.PackageSymbol.PayloadRef, facades);
            }

            facades.Add(item);
        }

        public void AddOrdered(PackageFacade item)
        {
            if (!this.FacadesByPackageId.ContainsKey(item.PackageId))
            {
                throw new WixException("Ordered PackageFacade must already exist");
            }

            this.OrderedFacades.Add(item);
        }

        public IReadOnlyCollection<PackageFacade> OrderedValues => this.OrderedFacades.AsReadOnly();

        public IEnumerable<PackageFacade> Values => this.FacadesByPackageId.Values;

        public bool TryGetFacadeByPackageId(string packageId, out PackageFacade facade)
        {
            return this.FacadesByPackageId.TryGetValue(packageId, out facade);
        }

        public bool TryGetFacadesByPackagePayloadId(string packagePayloadId, out List<PackageFacade> facades)
        {
            return this.FacadesByPackagePayloadId.TryGetValue(packagePayloadId, out facades);
        }
    }
}
