// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using WixToolset.Data;

    internal static class BurnBackendWarnings
    {
        //public static Message ReplaceThisWithTheFirstWarning(SourceLineNumber sourceLineNumbers)
        //{
        //    return Message(sourceLineNumbers, Ids.ReplaceThisWithTheFirstWarning, "format string", arg1, arg2);
        //}

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            // ReplaceThisWithTheFirstWarning = 8500,
        }
    }
}
