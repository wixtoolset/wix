// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class OptimizerWarnings
    {
        public static Message ZeroFilesHarvested(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ZeroFilesHarvested, "Files inclusions and exclusions resulted in zero files harvested. Unless that is expected, you should verify your Files paths, inclusions, and exclusions for accuracy.");
        }

        public static Message ExpectedDirectory(SourceLineNumber sourceLineNumbers, string harvestDirectory)
        {
            return Message(sourceLineNumbers, Ids.ExpectedDirectory, "Missing directory for harvesting files: {0}", harvestDirectory);
        }

        public static Message SkippingDuplicateFile(SourceLineNumber sourceLineNumbers, string duplicateFile)
        {
            return Message(sourceLineNumbers, Ids.SkippingDuplicateFile, "Skipping file that has already been harvested: {0}", duplicateFile);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            ZeroFilesHarvested = 8600,
            ExpectedDirectory = 8601,
            SkippingDuplicateFile = 8602,
        }
    }
}
