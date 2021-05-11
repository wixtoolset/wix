// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Initializes package state from the Exe contents.
    /// </summary>
    internal class ProcessExePackageCommand
    {
        public ProcessExePackageCommand(PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols)
        {
            this.AuthoredPayloads = payloadSymbols;
            this.Facade = facade;
        }

        public Dictionary<string, WixBundlePayloadSymbol> AuthoredPayloads { get; }

        public PackageFacade Facade { get; }

        /// <summary>
        /// Processes the Exe packages to add properties and payloads from the Exe packages.
        /// </summary>
        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageSymbol.PayloadRef];

            if (String.IsNullOrEmpty(this.Facade.PackageSymbol.CacheId))
            {
                this.Facade.PackageSymbol.CacheId = packagePayload.Hash;
            }

            this.Facade.PackageSymbol.Version = packagePayload.Version;
        }
    }
}
