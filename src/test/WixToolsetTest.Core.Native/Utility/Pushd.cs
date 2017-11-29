// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreNative.Utility
{
    using System;
    using System.IO;

    public class Pushd : IDisposable
    {
        protected bool Disposed { get; private set; }

        public Pushd(string path)
        {
            this.PreviousDirectory = Directory.GetCurrentDirectory();

            Directory.SetCurrentDirectory(path);
        }

        public string PreviousDirectory { get; }

        #region // IDisposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.Disposed)
            {
                return;
            }

            if (disposing)
            {
                Directory.SetCurrentDirectory(this.PreviousDirectory);
            }

            this.Disposed = true;
        }

        #endregion
    }
}
