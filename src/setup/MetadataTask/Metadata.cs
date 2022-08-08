// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tasks
{
    public enum MetadataType
    {
        Unknown,
        Burn,
        Msi,
    }

    public enum ArchitectureType
    {
        Unknown,
        Arm64,
        X64,
        X86,
    }

    //{
    //  "id": [PackageSymbolId] | [BundleSymbolId],
    //  "type": "msi" | "burn"
    //  "name": [ProductName] | [BundleName]
    //  "locale": [ProductLanguage] | [BundleLanguage]
    //  "publisher": [Manufacturer]
    //  "aboutUrl": [ARPURLINFOABOUT] | [AboutUrl]
    //  "supportUrl": [ARPHELPLINK] | [SupportUrl]
    //  "description": [ARPCOMMENTS] | "Installation for" + [BundleName]
    //  "license": [ProductLicense] | [BundleLicense]
    //  "architecture": "x86" | "x64" | "arm64"
    //  "size": ####
    //  "sha256": hex,
    //  "file": <filename>
    //  "created": <ISO timestamp>
    //  "productCode": [ProductCode]
    //  "bundleCode": [BundleId]
    //  "upgradeCode": [UpgradeCode]
    //}

    public class Metadata
    {
        public string Id { get; set; }

        public MetadataType Type { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string Locale { get; set; }

        public string Publisher { get; set; }

        public string AboutUrl { get; set; }

        public string SupportUrl { get; set; }

        public string Description { get; set; }

        public string License { get; set; }

        public ArchitectureType Architecture { get; set; }

        public string File { get; set; }

        public long Size { get; set; }

        public string Sha256 { get; set; }

        public string Created { get; set; }

        public string ProductCode { get; set; }

        public string BundleCode { get; set; }

        public string UpgradeCode { get; set; }
    }
}
