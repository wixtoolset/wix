// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

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

        public static Message MultipleSingletonSymbolsFound(SourceLineNumber sourceLineNumbers, string friendlyName, SourceLineNumber collisionSourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultipleSingletonSymbolsFound, "The Bundle requires a single {0}, but found another at: {1}", friendlyName, collisionSourceLineNumbers.ToString());
        }

        public static Message MissingPrimaryBootstrapperApplication()
        {
            return Message(null, Ids.MissingPrimaryBootstrapperApplication, "A BundleApplication is required to build a bundle.");
        }

        public static Message TooManyBootstrapperApplications(SourceLineNumber sourceLineNumbers, WixBootstrapperApplicationSymbol symbol)
        {
            var secondary = symbol.Secondary == true ? "secondary " : String.Empty;

            return Message(sourceLineNumbers, Ids.MultipleSingletonSymbolsFound, "Multiple {0}BootstrapperApplications defined. You can have at most one BootstrapperAppplication of primary and secondary.", secondary);
        }

        public static Message BundleMissingBootstrapperApplicationContainer(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.BundleMissingBootstrapperApplicationContainer, "Bundle is invalid. The BootstrapperApplication attached container is missing from the file: {0}", path);
        }

        public static Message CircularSearchReference(string chain)
        {
            return Message(null, Ids.CircularSearchReference, "A circular reference of search ordering constraints was detected: {0}. Search ordering references must form a directed acyclic graph.", chain);
        }

        public static Message DuplicateProviderDependencyKey(string providerKey, string packageId)
        {
            return Message(null, Ids.DuplicateProviderDependencyKey, "The provider dependency key '{0}' was already imported from the package with Id '{1}'. Please remove the Provides element with the key '{0}' from the package authoring.", providerKey, packageId);
        }

        public static Message InsecureBundleFilename(string filename)
        {
            return Message(null, Ids.InsecureBundleFilename, "The file name '{0}' creates an insecure bundle. Windows will load unnecessary compatibility shims into a bundle with that file name. These compatibility shims can be DLL hijacked allowing attackers to compromise your customers' computer. Choose a different bundle file name.", filename);
        }

        public static Message InvalidBundle(string bundleExecutable)
        {
            return Message(null, Ids.InvalidBundle, "Unable to read bundle executable '{0}'. This is not a valid WiX bundle.", bundleExecutable);
        }

        public static Message InvalidStubExe(string filename)
        {
            return Message(null, Ids.InvalidStubExe, "Stub executable '{0}' is not a valid Win32 executable.", filename);
        }

        public static Message MissingBundleInformation(string friendlyName)
        {
            return Message(null, Ids.MissingBundleInformation, "The Bundle is missing {0} data, and cannot continue.", friendlyName);
        }

        public static Message MissingBundleSearch(SourceLineNumber sourceLineNumbers, string searchId)
        {
            return Message(sourceLineNumbers, Ids.MissingBundleSearch, "Bundle Search with id '{0}' has no corresponding implementation symbol.", searchId);
        }

        public static Message MissingDependencyVersion(string packageId)
        {
            return Message(null, Ids.MissingDependencyVersion, "The provider dependency version was not authored for the package with Id '{0}'. Please author the Provides/@Version attribute for this package.", packageId);
        }

        public static Message MissingPackagePayload(SourceLineNumber sourceLineNumbers, string packageId, string packageType)
        {
            return Message(sourceLineNumbers, Ids.MissingPackagePayload, "There is no payload defined for package '{0}'. This is specified on the {1}Package element or a child {1}PackagePayload element.", packageId, packageType);
        }

        public static Message MsiTransactionInvalidPackage(SourceLineNumber sourceLineNumbers, string packageId, string packageType)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionInvalidPackage, "Invalid package '{0}' in MSI transaction. It is type '{1}' but must be Msi or Msp.", packageId, packageType);
        }

        public static Message MsiTransactionInvalidPackage2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionInvalidPackage2, "Location of rollback boundary related to previous error.");
        }

        public static Message MsiTransactionX86BeforeX64Package(SourceLineNumber sourceLineNumbers, string x64PackageId, string x86PackageId)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionX86BeforeX64Package, "Package '{0}' is x64 but Package '{1}' is x86. MSI transactions must install all x64 packages before any x86 package.", x64PackageId, x86PackageId);
        }

        public static Message MsiTransactionX86BeforeX64Package2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionX86BeforeX64Package2, "Location of x86 package related to previous error.");
        }

        public static Message MultiplePackagePayloads(SourceLineNumber sourceLineNumbers, string packageId, string packagePayloadId1, string packagePayloadId2)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads, "The package '{0}' has multiple PackagePayloads: '{1}' and '{2}'. This normally happens when the payload is defined on the package element and a child PackagePayload element.", packageId, packagePayloadId1, packagePayloadId2);
        }

        public static Message MultiplePackagePayloads2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads2, "The location of the package payload related to previous error.");
        }

        public static Message MultiplePackagePayloads3(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads3, "The location of the package related to previous error.");
        }

        public static Message PackagePayloadUnsupported(SourceLineNumber sourceLineNumbers, string packageType)
        {
            return Message(sourceLineNumbers, Ids.PackagePayloadUnsupported, "The {0}PackagePayload element can only be used for {0}Packages.", packageType);
        }

        public static Message PackagePayloadUnsupported2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PackagePayloadUnsupported2, "The location of the package related to previous error.");
        }

        public static Message PerUserButAllUsersEquals1(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.PerUserButAllUsersEquals1, "The MSI '{0}' is explicitly marked to not elevate so it must be a per-user package but the ALLUSERS Property is set to '1' creating a per-machine package. Remove the Property with Id='ALLUSERS' and use Package/@Scope attribute to be explicit instead.", path);
        }

        public static Message StubMissingWixburnSection(string filename)
        {
            return Message(null, Ids.StubMissingWixburnSection, "Stub executable '{0}' does not contain a .wixburn data section.", filename);
        }

        public static Message StubWixburnSectionTooSmall(string filename)
        {
            return Message(null, Ids.StubWixburnSectionTooSmall, "Stub executable '{0}' .wixburn data section is too small to store the Burn container header.", filename);
        }

        public static Message UnableToReadPackageInformation(SourceLineNumber sourceLineNumbers, string packagePath, string detailedErrorMessage)
        {
            return Message(sourceLineNumbers, Ids.UnableToReadPackageInformation, "Unable to read package '{0}'. {1}", packagePath, detailedErrorMessage);
        }

        public static Message UnsupportedAllUsersValue(SourceLineNumber sourceLineNumbers, string path, string value)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedAllUsersValue, "The MSI '{0}' set the ALLUSERS Property to '{0}' which is not supported. Remove the Property with Id='ALLUSERS' and use Package/@Scope attribute instead.", path, value);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            InvalidStubExe = 338,
            StubMissingWixburnSection = 339,
            StubWixburnSectionTooSmall = 340,
            MissingBundleInformation = 341,
            UnableToReadPackageInformation = 352,
            InvalidBundle = 354,
            PerUserButAllUsersEquals1 = 363,
            UnsupportedAllUsersValue = 364,
            DuplicateProviderDependencyKey = 370,
            MissingDependencyVersion = 371,
            InsecureBundleFilename = 388,
            MsiTransactionX86BeforeX64Package = 390,
            MissingBundleSearch = 397,
            CircularSearchReference = 398,
            PackagePayloadUnsupported = 402,
            PackagePayloadUnsupported2 = 403,
            MultiplePackagePayloads = 404,
            MultiplePackagePayloads2 = 405,
            MultiplePackagePayloads3 = 406,
            MissingPackagePayload = 407,
            MsiTransactionX86BeforeX64Package2 = 410,
            MsiTransactionInvalidPackage = 411,
            MsiTransactionInvalidPackage2 = 412,
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
            MultipleSingletonSymbolsFound = 8014,
            MissingPrimaryBootstrapperApplication = 8015,
            TooManyBootstrapperApplications = 8016,
            BundleMissingBootstrapperApplicationContainer = 8017,
        } // last available is 8499. 8500 is BurnBackendWarnings.
    }
}
