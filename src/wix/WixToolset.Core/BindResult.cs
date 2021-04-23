// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class BindResult : IBindResult
    {
        private bool disposed;

        public IReadOnlyCollection<IFileTransfer> FileTransfers { get; set; }

        public IReadOnlyCollection<ITrackedFile> TrackedFiles { get; set; }

        public WixOutput Wixout { get; set; }

        #region IDisposable Support
        /// <summary>
        /// Disposes of the internal state of the file structure.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the internsl state of the file structure.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Wixout?.Dispose();
                }
            }

            this.disposed = true;
        }
        #endregion
    }
}
