// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal static class CacheIdGenerator
    {
        // These are "reasonable" limits to trim the very long hashes so
        // when used in a cache id we do not overflow MAX_PATH.
        private const int ReasonableCountOfCharsFromCertificateThumbprint = 20;
        private const int ReasonableUpperLimitForCacheId = 64;

        public static string GenerateLocalCacheId(IMessaging messaging, IntermediateSymbol harvestedPackageSymbol, WixBundlePayloadSymbol payloadSymbol, SourceLineNumber sourceLineNumbers, string elementName)
        {
            string cacheId = null;

            // If we are validating the package via certificate,
            // the CacheId must be specified in source code.
            if (!String.IsNullOrEmpty(payloadSymbol.CertificatePublicKey) || !String.IsNullOrEmpty(payloadSymbol.CertificateThumbprint))
            {
                var oneOfCertificateAttributeNames = !String.IsNullOrEmpty(payloadSymbol.CertificatePublicKey) ? "CertificatePublicKey" : "CertificateThumbprint";

                messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, elementName, "CacheId", oneOfCertificateAttributeNames));
            }
            else
            {
                cacheId = GenerateDefaultCacheId(harvestedPackageSymbol, payloadSymbol, out _);
            }

            return cacheId;
        }

        public static string GenerateRemoteCacheId(IntermediateSymbol harvestedPackageSymbol, WixBundlePayloadSymbol payloadSymbol)
        {
            // If we are not validating the package via certificate,
            // the CacheId can be generated at build time.
            if (String.IsNullOrEmpty(payloadSymbol.CertificateThumbprint))
            {
                return null;
            }

            var defaultCacheId = GenerateDefaultCacheId(harvestedPackageSymbol, payloadSymbol, out var canTruncate);
            var takeFromThumbprint = Math.Min(ReasonableCountOfCharsFromCertificateThumbprint, payloadSymbol.CertificateThumbprint.Length);
            var takeFromDefault = Math.Min(ReasonableUpperLimitForCacheId - takeFromThumbprint, defaultCacheId.Length);

            var defaultPart = !canTruncate ? defaultCacheId : defaultCacheId.Substring(0, takeFromDefault);
            var certificatePart = payloadSymbol.CertificateThumbprint.Substring(0, takeFromThumbprint);
            return defaultPart + certificatePart;
        }

        public static string GenerateDefaultCacheId(IntermediateSymbol harvestedPackageSymbol, WixBundlePayloadSymbol payloadSymbol, out bool canTruncate)
        {
            string cacheId;
            canTruncate = false;

            if (harvestedPackageSymbol is WixBundleHarvestedBundlePackageSymbol harvestedBundlePackageSymbol)
            {
                cacheId = GenerateCacheIdFromGuidAndVersion(harvestedBundlePackageSymbol.BundleCode, harvestedBundlePackageSymbol.Version);
            }
            else if (harvestedPackageSymbol is WixBundleHarvestedMsiPackageSymbol harvestedMsiPackageSymbol)
            {
                cacheId = GenerateCacheIdFromGuidAndVersion(harvestedMsiPackageSymbol.ProductCode, harvestedMsiPackageSymbol.ProductVersion);
            }
            else if (harvestedPackageSymbol is WixBundleHarvestedMspPackageSymbol harvestedMspPackageSymbol)
            {
                cacheId = harvestedMspPackageSymbol.PatchCode;
            }
            else
            {
                // No inherent id is available, so use the hash.
                cacheId = GenerateCacheIdFromHash(payloadSymbol.Hash);
                canTruncate = true;
            }

            return cacheId;
        }

        public static string GenerateCacheIdFromGuidAndVersion(string guid, string version)
        {
            return String.Format("{0}v{1}", guid, version);
        }

        public static string GenerateCacheIdFromHash(string hash)
        {
            // The hash needs to be truncated to a "reasonable" upper limit since the CacheId is in the cached file path.
            return hash.Length > ReasonableUpperLimitForCacheId ? hash.Substring(0, ReasonableUpperLimitForCacheId) : hash;
        }
    }
}
