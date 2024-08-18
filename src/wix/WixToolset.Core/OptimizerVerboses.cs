// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class OptimizerVerboses
    {
        public static Message HarvestedFile(SourceLineNumber sourceLineNumbers, string harvestedFile)
        {
            return Message(sourceLineNumbers, Ids.HarvestedFile, "Harvested file: {0}", harvestedFile);
        }

        public static Message ExcludedFile(SourceLineNumber sourceLineNumbers, string excludedFile)
        {
            return Message(sourceLineNumbers, Ids.ExcludedFile, "File excluded from harvesting: {0}", excludedFile);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Verbose, (int)id, format, args);
        }

        public enum Ids
        {
            HarvestedFile = 8700,
            ExcludedFile = 8701,
        }
    }
}
