// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Burn
{
    public static class BurnConstants
    {
        public const string BurnUXContainerName = "WixUXContainer";
        public const string BurnDefaultAttachedContainerName = "WixAttachedContainer";
        public const string BundleChainPackageGroupId = "WixChain";
        public const string BundleDefaultBoundaryId = "WixDefaultBoundary";
        public const string BundleLayoutOnlyPayloadsName = "BundleLayoutOnlyPayloads";

        public const string BurnManifestWixOutputStreamName = "wix-burndata.xml";
        public const string BootstrapperExtensionDataWixOutputStreamName = "wix-bextdata";
        public const string BootstrapperApplicationDataWixOutputStreamName = "wix-badata.xml";

        public const string BootstrapperApplicationDataNamespace = "http://wixtoolset.org/schemas/v4/BootstrapperApplicationData";
        public const string BootstrapperExtensionDataNamespace = "http://wixtoolset.org/schemas/v4/BootstrapperExtensionData";

        public const string BootstrapperApplicationDataSymbolDefinitionTag = "WixBootstrapperApplicationData";
        public const string BootstrapperExtensionSearchSymbolDefinitionTag = "WixBootstrapperExtensionSearch";
    }
}
