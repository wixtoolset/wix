// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data.Tuples;

    /// <summary>
    /// Initializes package state from the Exe contents.
    /// </summary>
    internal class ProcessExePackageCommand
    {
        public ProcessExePackageCommand(PackageFacade facade, Dictionary<string, WixBundlePayloadTuple> payloadTuples)
        {
            this.AuthoredPayloads = payloadTuples;
            this.Facade = facade;
        }

        public Dictionary<string, WixBundlePayloadTuple> AuthoredPayloads { get; }

        public PackageFacade Facade { get; }

        /// <summary>
        /// Processes the Exe packages to add properties and payloads from the Exe packages.
        /// </summary>
        public void Execute()
        {
            var packagePayload = this.AuthoredPayloads[this.Facade.PackageTuple.PayloadRef];

            if (String.IsNullOrEmpty(this.Facade.PackageTuple.CacheId))
            {
                this.Facade.PackageTuple.CacheId = packagePayload.Hash;
            }

            this.Facade.PackageTuple.Version = packagePayload.Version;
        }
    }
}
