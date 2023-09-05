// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class CoreErrors
    {
        public static Message UnableToCopyFile(SourceLineNumber sourceLineNumbers, string source, string destination, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToCopyFile, "Unable to copy file from: {0}, to: {1}. Error detail: {2}", source, destination, detail);
        }

        public static Message UnableToDeleteFile(SourceLineNumber sourceLineNumbers, string path, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToDeleteFile, "Unable to delete file: {0}. Error detail: {1}", path, detail);
        }

        public static Message UnableToMoveFile(SourceLineNumber sourceLineNumbers, string source, string destination, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToMoveFile, "Unable to move file from: {0}, to: {1}. Error detail: {2}", source, destination, detail);
        }

        public static Message UnableToOpenFile(SourceLineNumber sourceLineNumbers, string path, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToOpenFile, "Unable to open file: {0}. Error detail: {1}", path, detail);
        }

        public static Message BackendNotFound(string outputType, string outputPath)
        {
            return Message(null, Ids.BackendNotFound, "Unable to find a backend to process output type: {0} for output file: {1}. Specify a different output type or output file extension.", outputType, outputPath);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            UnableToCopyFile = 7010,
            UnableToDeleteFile = 7011,
            UnableToMoveFile = 7012,
            UnableToOpenFile = 7013,
            BackendNotFound = 7014,
        } // last available is 7099. 7100 is WindowsInstallerBackendWarnings.
    }
}
