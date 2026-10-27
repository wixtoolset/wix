// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using WixToolset.Data;

    internal static class ExtensionCacheInformations
    {
        public static Message RemovedExtension(string extensionFolder)
        {
            return Message(null, Ids.RemovedExtension, "Removed extension folder {0}", extensionFolder);
        }


        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Information, (int)id, format, args);
        }

        public enum Ids
        {
            RemovedExtension = 6200,
        } // see message range from ExtensionCacheWarnings
    }
}
