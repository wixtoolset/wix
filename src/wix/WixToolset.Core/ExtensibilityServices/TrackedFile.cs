// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class TrackedFile : ITrackedFile
    {
        public TrackedFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers)
        {
            this.Path = path;
            this.Type = type;
            this.SourceLineNumbers = sourceLineNumbers;
            this.Clean = (type == TrackedFileType.Intermediate || type == TrackedFileType.Final);
        }

        public bool Clean { get; set; }

        public string Path { get; set; }

        public SourceLineNumber SourceLineNumbers { get; set; }

        public TrackedFileType Type { get; set; }
    }
}
