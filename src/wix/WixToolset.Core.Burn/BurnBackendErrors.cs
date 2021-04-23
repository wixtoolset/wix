// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
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

        public static Message MultipleAttachedContainersUnsupported(SourceLineNumber sourceLineNumbers, string containerId)
        {
            return Message(sourceLineNumbers, Ids.MultipleAttachedContainersUnsupported, "Bundles don't currently support having more than one attached container. Either remove all authored attached containers to use the default attached container, or make sure all compressed payloads are included in this Container '{0}'.", containerId);
        }

        public static Message PackageCachePayloadCollision(SourceLineNumber sourceLineNumbers, string payloadId, string payloadName, string packageId)
        {
            return Message(sourceLineNumbers, Ids.PackageCachePayloadCollision, "The Payload '{0}' has a duplicate Name '{1}' in package '{2}'. When caching the package, the file will get overwritten.", payloadId, payloadName, packageId);
        }

        public static Message PackageCachePayloadCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PackageCachePayloadCollision2, "The location of the payload related to the previous error.");
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
            MultipleAttachedContainersUnsupported = 8008,
        } // last available is 8499. 8500 is BurnBackendWarnings.
    }
}
