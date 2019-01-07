// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class IIsWarnings
    {
        public static Message EncounteredNullDirectoryForWebSite(string directory)
        {
            return Message(null, Ids.EncounteredNullDirectoryForWebSite, "Could not harvest website directory: {0}.  Please update the output with the appropriate directory ID before using.", directory);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            EncounteredNullDirectoryForWebSite = 5400,
        }
    }
}
