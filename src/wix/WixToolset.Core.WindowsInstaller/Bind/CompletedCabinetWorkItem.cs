// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using WixToolset.Core.Native;

    internal class CompletedCabinetWorkItem
    {
        public CompletedCabinetWorkItem(int diskId, IReadOnlyCollection<CabinetCreated> created)
        {
            this.DiskId = diskId;
            this.CreatedCabinets = created;
        }

        public int DiskId { get; }

        public IReadOnlyCollection<CabinetCreated> CreatedCabinets { get; }
    }
}
