// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Data;

    internal static class CompilerInformationals
    {
        public static Message PreprocessorMessage(SourceLineNumber sourceLineNumbers, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorMessage, "{0}", message);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Information, (int)id, format, args);
        }

        public enum Ids
        {
            PreprocessorMessage = 8801,
        }
    }
}
