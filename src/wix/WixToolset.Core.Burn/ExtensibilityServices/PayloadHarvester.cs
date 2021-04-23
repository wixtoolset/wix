// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.ExtensibilityServices
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Data.Symbols;

    internal class PayloadHarvester : IPayloadHarvester
    {
        private static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

        /// <inheritdoc />
        public bool HarvestStandardInformation(WixBundlePayloadSymbol payload)
        {
            var filePath = payload.SourceFile?.Path;

            if (String.IsNullOrEmpty(filePath))
            {
                return false;
            }

            this.UpdatePayloadFileInformation(payload, filePath);

            this.UpdatePayloadVersionInformation(payload, filePath);

            return true;
        }

        private void UpdatePayloadFileInformation(WixBundlePayloadSymbol payload, string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if (null != fileInfo)
            {
                payload.FileSize = fileInfo.Length;

                payload.Hash = BundleHashAlgorithm.Hash(fileInfo);
            }
            else
            {
                payload.FileSize = 0;
            }
        }

        private void UpdatePayloadVersionInformation(WixBundlePayloadSymbol payload, string filePath)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

            if (null != versionInfo)
            {
                // Use the fixed version info block for the file since the resource text may not be a dotted quad.
                var version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);

                if (PayloadHarvester.EmptyVersion != version)
                {
                    payload.Version = version.ToString();
                }

                payload.Description = versionInfo.FileDescription;
                payload.DisplayName = versionInfo.ProductName;
            }
        }
    }
}
