// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Bundles
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Initializes package state from the Exe contents.
    /// </summary>
    internal class ProcessExePackageCommand : ICommand
    {
        public RowDictionary<WixBundlePayloadRow> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        /// <summary>
        /// Processes the Exe packages to add properties and payloads from the Exe packages.
        /// </summary>
        public void Execute()
        {
            WixBundlePayloadRow packagePayload = this.AuthoredPayloads.Get(this.Facade.Package.PackagePayload);

            if (String.IsNullOrEmpty(this.Facade.Package.CacheId))
            {
                this.Facade.Package.CacheId = packagePayload.Hash;
            }

            this.Facade.Package.Version = packagePayload.Version;
        }
    }
}
