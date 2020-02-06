// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class BindResult : IBindResult
    {
        public IEnumerable<IFileTransfer> FileTransfers { get; set; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; set; }

        public WixOutput Wixout { get; set; }
    }
}
