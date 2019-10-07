// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    /// <summary>
    /// Processes the Msu packages to add properties and payloads from the Msu packages.
    /// </summary>
    internal class ProcessMsuPackageCommand
    {
        public ProcessMsuPackageCommand(PackageFacade facade, Dictionary<string, WixBundlePayloadTuple> payloadTuples)
        {
            this.AuthoredPayloads = payloadTuples;
            this.Facade = facade;
        }

        public Dictionary<string, WixBundlePayloadTuple> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageTuple.PayloadRef];

            if (String.IsNullOrEmpty(this.Facade.PackageTuple.CacheId))
            {
                this.Facade.PackageTuple.CacheId = packagePayload.Hash;
            }

            this.Facade.PackageTuple.PerMachine = YesNoDefaultType.Yes; // MSUs are always per-machine.
        }
    }
}
