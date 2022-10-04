// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using WixToolset.Data;

    internal static class BurnBackendErrors
    {
        public static Message BAContainerPayloadCollision(SourceLineNumber sourceLineNumbers, string payloadId, string payloadName)
        {
            return Message(sourceLineNumbers, Ids.BAContainerPayloadCollision, "The Payload '{0}' has a duplicate Name '{1}' in the BA container. When extracting the container at runtime, the file will get overwritten.", payloadId, payloadName);
        }

        public static Message BAContainerPayloadCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.BAContainerPayloadCollision2, "The location of the payload related to the previous error.");
        }

        public static Message BundleMultipleProviders(SourceLineNumber sourceLineNumbers, string extraProviderKey, string originalProviderKey)
        {
            return Message(sourceLineNumbers, Ids.BundleMultipleProviders, "The bundle can only have a single dependency provider, but it has '{0}' and '{1}'.", originalProviderKey, extraProviderKey);
        }

        public static Message DuplicateCacheIds(SourceLineNumber originalLineNumber, string cacheId, string packageId)
        {
            return Message(originalLineNumber, Ids.DuplicateCacheIds, "The CacheId '{0}' for package '{1}' is duplicated. Each package must have a unique CacheId.", cacheId, packageId);
        }

        public static Message DuplicateCacheIds2(SourceLineNumber duplicateLineNumber)
        {
            return Message(duplicateLineNumber, Ids.DuplicateCacheIds2, "The location of the package related to the previous error.");
        }

        public static Message ExternalPayloadCollision(SourceLineNumber sourceLineNumbers, string symbolName, string payloadId, string payloadName)
        {
            return Message(sourceLineNumbers, Ids.ExternalPayloadCollision, "The external {0} '{1}' has a duplicate Name '{2}'. When building the bundle or laying out the bundle, the file will get overwritten.", symbolName, payloadId, payloadName);
        }

        public static Message ExternalPayloadCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExternalPayloadCollision2, "The location of the symbol related to the previous error.");
        }

        public static Message FailedToUpdateBundleResources(SourceLineNumber sourceLineNumbers, string iconPath, string splashScreenPath, string detail)
        {
            var additionalDetail = String.Empty;

            if (String.IsNullOrEmpty(iconPath) && String.IsNullOrEmpty(splashScreenPath))
            {
            }
            else if (String.IsNullOrEmpty(iconPath))
            {
                additionalDetail = $" Ensure the splash screen file is a bitmap file at '{splashScreenPath}'.";
            }
            else if (String.IsNullOrEmpty(splashScreenPath))
            {
                additionalDetail = $" Ensure the bundle icon file is an icon file at '{iconPath}'.";
            }
            else
            {
                additionalDetail = $" Ensure the bundle icon file is an icon file at '{iconPath}' and the splash screen file is a bitmap file at '{splashScreenPath}'.";
            }

            return Message(sourceLineNumbers, Ids.FailedToUpdateBundleResources, "Failed to update resources in the bundle.{0} Detail: {1}", additionalDetail, detail);
        }

        public static Message PackageCachePayloadCollision(SourceLineNumber sourceLineNumbers, string payloadId, string payloadName, string packageId)
        {
            return Message(sourceLineNumbers, Ids.PackageCachePayloadCollision, "The Payload '{0}' has a duplicate Name '{1}' in package '{2}'. When caching the package, the file will get overwritten.", payloadId, payloadName, packageId);
        }

        public static Message PackageCachePayloadCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PackageCachePayloadCollision2, "The location of the payload related to the previous error.");
        }

        public static Message TooManyAttachedContainers(uint maxAllowed)
        {
            return Message(null, Ids.TooManyAttachedContainers, "The bundle has too many attached containers. The maximal attached container count is {0}", maxAllowed);
        }

        public static Message IncompatibleWixBurnSection(string bundleExecutable, long bundleVersion)
        {
            return Message(null, Ids.IncompatibleWixBurnSection, "Unable to read bundle executable '{0}', because this bundle was created with a different version of WiX burn (.wixburn section version '{1}'). You must use the same version of Windows Installer XML in order to read this bundle.", bundleExecutable, bundleVersion);
        }

        public static Message UnsupportedRemotePackagePayload(string extension, string path)
        {
            return Message(null, Ids.UnsupportedRemotePackagePayload, "The first remote payload must be a supported package type of .exe or .msu. Use the -packageType switch to override the inferred extension: {0} from file: {1}", extension, path);
        }

        public static Message InvalidBundleManifest(SourceLineNumber sourceLineNumbers, string bundleExecutable, string reason)
        {
            return Message(sourceLineNumbers, Ids.InvalidBundleManifest, "Unable to read bundle executable '{0}'. Its manifest is invalid. {1}", bundleExecutable, reason);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            DuplicateCacheIds = 8000,
            DuplicateCacheIds2 = 8001,
            BAContainerPayloadCollision = 8002,
            BAContainerPayloadCollision2 = 8003,
            ExternalPayloadCollision = 8004,
            ExternalPayloadCollision2 = 8005,
            PackageCachePayloadCollision = 8006,
            PackageCachePayloadCollision2 = 8007,
            TooManyAttachedContainers = 8008,
            IncompatibleWixBurnSection = 8009,
            UnsupportedRemotePackagePayload = 8010,
            FailedToUpdateBundleResources = 8011,
            InvalidBundleManifest = 8012,
            BundleMultipleProviders = 8013,
        } // last available is 8499. 8500 is BurnBackendWarnings.
    }
}
