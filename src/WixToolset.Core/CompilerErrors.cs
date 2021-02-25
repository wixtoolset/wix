// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class CompilerErrors
    {
        public static Message IllegalName(SourceLineNumber sourceLineNumbers, string parentElement, string name)
        {
            return Message(sourceLineNumbers, Ids.IllegalName, "The Tag/@Name attribute value, '{1}', contains invalid filename identifiers. The Tag/@Name may have defaulted from the {0}/@Name attrbute. If so, use the Tag/@Name attribute to provide a valid filename. Any character except for the follow may be used: \\ ? | > < : / * \".", parentElement, name);
        }

        public static Message ExampleRegid(SourceLineNumber sourceLineNumbers, string regid)
        {
            return Message(sourceLineNumbers, Ids.ExampleRegid, "Regid '{0}' is a placeholder that must be replaced with an appropriate value for your installation. Use the simplified URI for your organization or project.", regid);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            IllegalName = 6601,
            ExampleRegid = 6602,
        }
    }
}
