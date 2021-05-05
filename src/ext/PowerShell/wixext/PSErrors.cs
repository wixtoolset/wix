// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.PowerShell
{
    using System.Resources;
    using WixToolset.Data;

    public static class PSErrors
    {
        public static Message NeitherIdSpecified(SourceLineNumber sourceLineNumbers, string element)
        {
            return Message(sourceLineNumbers, Ids.NeitherIdSpecified, "Either the {0}/@FileId attribute must be specified if nested under a SnapIn element, or the {0}/@SnapIn attribute must be specified if nested under under a File element.", element);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            NeitherIdSpecified = 5301,
        }
    }
}
