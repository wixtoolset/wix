// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Data;

    internal static class BurnBackendWarnings
    {
        public static Message AttachedContainerPayloadCollision(SourceLineNumber sourceLineNumbers, string payloadId, string payloadName)
        {
            return Message(sourceLineNumbers, Ids.AttachedContainerPayloadCollision, "The Payload '{0}' has a duplicate Name '{1}' in the attached container. When extracting the bundle with dark.exe, the file will get overwritten.", payloadId, payloadName);
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

        public static Message FailedToExtractAttachedContainers(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.FailedToExtractAttachedContainers, "Failed to extract attached container. This most often happens when extracting a stripped bundle from the package cache, which is not supported.");
        }

        public static Message FailedToExtractDetachedContainers(SourceLineNumber sourceLineNumbers, string error)
        {
            return Message(sourceLineNumbers, Ids.FailedToExtractDetachedContainers, "Failed to extract detached containers. {0}", error);
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

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            AttachedContainerPayloadCollision = 8500,
            AttachedContainerPayloadCollision2 = 8501,
            EmptyContainer = 8502,
            FailedToExtractAttachedContainers = 8503,
            UnknownCoffMachineType = 8504,
            UnknownBundleRelationAction = 8505,
            HiddenBundleNotSupported = 8506,
            UnknownMsiPackagePlatform = 8507,
            CannotParseBundleVersionAsFourPartVersion = 8508,
            FailedToExtractDetachedContainers = 8509,
        } // last available is 8999. 9000 is VerboseMessages.
    }
}
