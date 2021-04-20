// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Data;

    internal static class BurnBackendErrors
    {
        public static Message DuplicateCacheIds(SourceLineNumber originalLineNumber, string cacheId, string packageId)
        {
            return Message(originalLineNumber, Ids.DuplicateCacheIds, "The CacheId '{0}' for package '{1}' is duplicated. Each package must have a unique CacheId.", cacheId, packageId);
        }

        public static Message DuplicateCacheIds2(SourceLineNumber duplicateLineNumber)
        {
            return Message(duplicateLineNumber, Ids.DuplicateCacheIds2, "The location of the package related to the previous error.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            DuplicateCacheIds = 8000,
            DuplicateCacheIds2 = 8001,
        }
    }
}
