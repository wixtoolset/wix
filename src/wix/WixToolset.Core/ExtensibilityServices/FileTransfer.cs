// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class FileTransfer : IFileTransfer
    {
        public string Source { get; set; }

        public string Destination { get; set; }

        public bool Move { get; set; }

        public SourceLineNumber SourceLineNumbers { get; set; }

        public bool Redundant { get; set; }
    }
}
