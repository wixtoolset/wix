// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Bind
{
    using System.Collections.Generic;

    public class BindResult
    {
        public BindResult(IEnumerable<FileTransfer> fileTransfers, IEnumerable<string> contentFilePaths)
        {
            this.FileTransfers = fileTransfers;
            this.ContentFilePaths = contentFilePaths;
        }

        public IEnumerable<FileTransfer> FileTransfers { get; }

        public IEnumerable<string> ContentFilePaths { get; }
    }
}