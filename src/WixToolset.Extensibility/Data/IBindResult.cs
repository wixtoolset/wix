// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    public interface IBindResult : IDisposable
    {
        IEnumerable<IFileTransfer> FileTransfers { get; set; }

        IEnumerable<ITrackedFile> TrackedFiles { get; set; }

        WixOutput Wixout { get; set; }
    }
}
