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
        public const string BundleExtensionDataWixOutputStreamName = "wix-bextdata";
        public const string BootstrapperApplicationDataWixOutputStreamName = "wix-badata.xml";

        public const string BootstrapperApplicationDataSymbolDefinitionTag = "WixBootstrapperApplicationData";
        public const string BundleExtensionSearchSymbolDefinitionTag = "WixBundleExtensionSearch";

        // The following constants must stay in sync with src\burn\engine\core.h
        public const string BURN_BUNDLE_NAME = "WixBundleName";
        public const string BURN_BUNDLE_ORIGINAL_SOURCE = "WixBundleOriginalSource";
        public const string BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = "WixBundleOriginalSourceFolder";
        public const string BURN_BUNDLE_LAST_USED_SOURCE = "WixBundleLastUsedSource";
    }
}
