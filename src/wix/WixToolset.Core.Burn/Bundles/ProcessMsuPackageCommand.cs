// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Processes the Msu packages to add properties and payloads from the Msu packages.
    /// </summary>
    internal class ProcessMsuPackageCommand
    {
        public ProcessMsuPackageCommand(IMessaging messaging, PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols)
        {
            this.Messaging = messaging;
            this.Facade = facade;
            this.AuthoredPayloads = payloadSymbols;
        }

        public IMessaging Messaging { get; }

        public Dictionary<string, WixBundlePayloadSymbol> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageSymbol.PayloadRef];

            if (String.IsNullOrEmpty(this.Facade.PackageSymbol.CacheId))
            {
                this.Facade.PackageSymbol.CacheId = CacheIdGenerator.GenerateLocalCacheId(this.Messaging, null, packagePayload, this.Facade.PackageSymbol.SourceLineNumbers, "MsuPackage");
            }

            this.Facade.PackageSymbol.Scope = WixBundleScopeType.PerMachine; // MSUs are always per-machine.
        }
    }
}
