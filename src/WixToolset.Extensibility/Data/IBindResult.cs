// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// Result of bind operation.
    /// </summary>
    public interface IBindResult : IDisposable
    {
        /// <summary>
        /// Collection of file transfers to complete.
        /// </summary>
        IReadOnlyCollection<IFileTransfer> FileTransfers { get; set; }

        /// <summary>
        /// Collection of files tracked during binding.
        /// </summary>
        IReadOnlyCollection<ITrackedFile> TrackedFiles { get; set; }

        /// <summary>
        /// Ouput of binding.
        /// </summary>
        WixOutput Wixout { get; set; }
    }
}
