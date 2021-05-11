// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Processes the Msu packages to add properties and payloads from the Msu packages.
    /// </summary>
    internal class ProcessMsuPackageCommand
    {
        public ProcessMsuPackageCommand(PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols)
        {
            this.AuthoredPayloads = payloadSymbols;
            this.Facade = facade;
        }

        public Dictionary<string, WixBundlePayloadSymbol> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageSymbol.PayloadRef];

            if (String.IsNullOrEmpty(this.Facade.PackageSymbol.CacheId))
            {
                this.Facade.PackageSymbol.CacheId = packagePayload.Hash;
            }

            this.Facade.PackageSymbol.PerMachine = YesNoDefaultType.Yes; // MSUs are always per-machine.
        }
    }
}
