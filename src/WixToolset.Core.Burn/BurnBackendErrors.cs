// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Data;

    internal static class BurnBackendErrors
    {
        public static Message DuplicateCacheIds(SourceLineNumber originalLineNumber, string cacheId)
        {
            return Message(originalLineNumber, Ids.DuplicateCacheIds, "The cache id '{0}' has been duplicated as indicated in the following message.", cacheId);
        }

        public static Message DuplicateCacheIds2(SourceLineNumber duplicateLineNumber, string cacheId)
        {
            return Message(duplicateLineNumber, Ids.DuplicateCacheIds2, "Each cache id must be unique. '{0}' has been used before as indicated in the previous message.", cacheId);
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
