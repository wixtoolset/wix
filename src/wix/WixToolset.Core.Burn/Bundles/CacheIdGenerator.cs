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

        public static string GenerateCacheIdFromPayloadHashAndThumbprint(WixBundlePayloadSymbol payloadSymbol)
        {
            var takeFromThumbprint = Math.Min(ReasonableCountOfCharsFromCertificateThumbprint, payloadSymbol.CertificateThumbprint.Length);
            var takeFromHash = Math.Min(ReasonableUpperLimitForCacheId - takeFromThumbprint, payloadSymbol.Hash.Length);

            return payloadSymbol.Hash.Substring(0, takeFromHash) + payloadSymbol.CertificateThumbprint.Substring(0, takeFromThumbprint);
        }

        public static string GenerateCacheIdFromPackagePayloadHash(IMessaging messaging, WixBundlePayloadSymbol packagePayload, string elementName)
        {
            string cacheId = null;

            // If we are validating the package via certificate, the CacheId must be specified
            // in source code.
            if (!String.IsNullOrEmpty(packagePayload.CertificatePublicKey) || !String.IsNullOrEmpty(packagePayload.CertificateThumbprint))
            {
                var oneOfCertificateAttributeNames = !String.IsNullOrEmpty(packagePayload.CertificatePublicKey) ? "CertificatePublicKey" : "CertificateThumbprint";

                messaging.Write(ErrorMessages.ExpectedAttribute(packagePayload.SourceLineNumbers, elementName, "CacheId", oneOfCertificateAttributeNames));
            }
            else // validating package by hashing, so the CacheId can be defaulted to the hash with a "reasonable" upper limit since the CacheId is in the cached file path.
            {
                cacheId = packagePayload.Hash.Length > ReasonableUpperLimitForCacheId ? packagePayload.Hash.Substring(0, ReasonableUpperLimitForCacheId) : packagePayload.Hash;
            }

            return cacheId;
        }
    }
}
