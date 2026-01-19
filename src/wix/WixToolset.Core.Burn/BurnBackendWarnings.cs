// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Data;

    internal static class BurnBackendWarnings
    {
        public static Message AttachedContainerPayloadCollision(SourceLineNumber sourceLineNumbers, string payloadId, string payloadName)
        {
            return Message(sourceLineNumbers, Ids.AttachedContainerPayloadCollision, "The Payload '{0}' has a duplicate Name '{1}' in the attached container. When extracting the bundle with `wix burn extract`, the file will get overwritten.", payloadId, payloadName);
        }

        public static Message AttachedContainerPayloadCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.AttachedContainerPayloadCollision2, "The location of the payload related to the previous error.");
        }

        public static Message CannotParseBundleVersionAsFourPartVersion(SourceLineNumber originalLineNumber, string version)
        {
            return Message(originalLineNumber, Ids.CannotParseBundleVersionAsFourPartVersion, "The Bundle/@Version was not be able to be used as a four-part version. A valid four-part version has a max value of \"65535.65535.65535.65535\" and must be all numeric.", version);
        }

        public static Message EmptyContainer(SourceLineNumber sourceLineNumbers, string containerId)
        {
            return Message(sourceLineNumbers, Ids.EmptyContainer, "The Container '{0}' is being ignored because it doesn't have any payloads.", containerId);
        }

        public static Message FailedToExtractAttachedContainers(SourceLineNumber sourceLineNumbers, string message)
        {
            return Message(sourceLineNumbers, Ids.FailedToExtractAttachedContainers, "Failed to extract attached container. This most often happens when extracting a stripped bundle from the package cache, which is not supported. Detail: {0}", message);
        }

        public static Message HiddenBundleNotSupported(SourceLineNumber sourceLineNumbers, string packageId)
        {
            return Message(sourceLineNumbers, Ids.HiddenBundleNotSupported, "The BundlePackage '{0}' does not support hiding its ARP registration.", packageId);
        }

        public static Message UnknownBundleRelationAction(SourceLineNumber sourceLineNumbers, string bundleExecutable, string action)
        {
            return Message(sourceLineNumbers, Ids.UnknownBundleRelationAction, "The manifest for the bundle '{0}' contains an unknown related bundle action '{1}'. It will be ignored.", bundleExecutable, action);
        }

        public static Message UnknownCoffMachineType(SourceLineNumber sourceLineNumbers, string bundleExecutable, ushort machineType)
        {
            return Message(sourceLineNumbers, Ids.UnknownCoffMachineType, "The bundle '{0}' has an unknown COFF machine type: {1}. It is assumed to be 32-bit.", bundleExecutable, machineType);
        }

        public static Message UnknownMsiPackagePlatform(SourceLineNumber sourceLineNumbers, string msiPath, string platform)
        {
            return Message(sourceLineNumbers, Ids.UnknownMsiPackagePlatform, "The MsiPackage '{0}' has an unknown platform: '{1}'. It is assumed to be 64-bit.", msiPath, platform);
        }

        public static Message DiscardedRollbackBoundary(SourceLineNumber sourceLineNumbers, string rollbackBoundaryId)
        {
            return Message(sourceLineNumbers, Ids.DiscardedRollbackBoundary, "The RollbackBoundary '{0}' was discarded because it was not followed by a package. Without a package the rollback boundary doesn't do anything. Verify that the RollbackBoundary element is not followed by another RollbackBoundary and that the element is not at the end of the chain.", rollbackBoundaryId);
        }

        public static Message DiscardedRollbackBoundary2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DiscardedRollbackBoundary2, "Location of rollback boundary related to previous warning.");
        }

        public static Message DiscouragedAllUsersValue(SourceLineNumber sourceLineNumbers, string path, string machineOrUser)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedAllUsersValue, "Bundles require a package to be either per-machine or per-user. The MSI '{0}' ALLUSERS Property is set to '2' which may change from per-user to per-machine at install time. The Bundle will assume the package is per-{1} and will not work correctly if that changes. If possible, use the Package/@Scope attribute values 'perUser' or 'perMachine' instead.", path, machineOrUser);
        }

        public static Message DownloadUrlNotSupportedForAttachedContainers(SourceLineNumber sourceLineNumbers, string containerId)
        {
            return Message(sourceLineNumbers, Ids.DownloadUrlNotSupportedForAttachedContainers, "The Container '{0}' is attached but included a @DownloadUrl attribute. Attached Containers cannot be downloaded so the download URL is being ignored.", containerId);
        }

        public static Message ImplicitlyPerUser(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.ImplicitlyPerUser, "The MSI '{0}' does not explicitly indicate that it is a per-user package even though the ALLUSERS Property is blank. This suggests a per-user package so the Bundle will assume the package is per-user. If possible, use the Package/@InstallScope attribute to be explicit instead.", path);
        }

        public static Message MsiTransactionLimitations(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionLimitations, "MSI transactions have limitations that make it hard to use them successfully in a bundle. Test the bundle thoroughly, especially in upgrade scenarios and the scenario that required them in the first place.");
        }

        public static Message NoPerMachineDependencies(SourceLineNumber sourceLineNumbers, string packageId)
        {
            return Message(sourceLineNumbers, Ids.NoPerMachineDependencies, "Bundle dependencies will not be registered on per-machine package '{0}' for a per-user bundle. Either make sure that all packages are installed per-machine, or author any per-machine dependencies as permanent packages.", packageId);
        }

        public static Message PerUserButForcingPerMachine(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.PerUserButForcingPerMachine, "The MSI '{0}' is a per-user package being forced to per-machine. Verify that the MsiPackage/@ForcePerMachine attribute is expected and that the per-user package works correctly when forced to install per-machine.", path);
        }

        public static Message InvalidWixVersion(SourceLineNumber sourceLineNumbers, string version, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixVersion, "Invalid WixVersion '{0}' in {1}/@'{2}'. Comparisons may yield unexpected results.", version, elementName, attributeName);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            DiscardedRollbackBoundary = 1129,
            DiscouragedAllUsersValue = 1133,
            ImplicitlyPerUser = 1134,
            PerUserButForcingPerMachine = 1135,
            NoPerMachineDependencies = 1140,
            DownloadUrlNotSupportedForAttachedContainers = 1141,
            MsiTransactionLimitations = 1151,
            DiscardedRollbackBoundary2 = 1160,
            InvalidWixVersion = 1162,
            AttachedContainerPayloadCollision = 8500,
            AttachedContainerPayloadCollision2 = 8501,
            EmptyContainer = 8502,
            FailedToExtractAttachedContainers = 8503,
            UnknownCoffMachineType = 8504,
            UnknownBundleRelationAction = 8505,
            HiddenBundleNotSupported = 8506,
            UnknownMsiPackagePlatform = 8507,
            CannotParseBundleVersionAsFourPartVersion = 8508,
        } // last available is 8999. 9000 is VerboseMessages.
    }
}
