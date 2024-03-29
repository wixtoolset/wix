// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using System.ComponentModel;
#if !DEBUG
    using System.Diagnostics;
#endif
    using System.Threading;

    /// <summary>
    /// Wrapper class for MSI handle.
    /// </summary>
    public abstract class MsiHandle : IDisposable
    {
        private bool disposed;
        private IntPtr handle;
        private int owningThread;
#if DEBUG
        private string creationStack;
#endif

        /// <summary>
        /// MSI handle destructor.
        /// </summary>
        ~MsiHandle()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the MSI handle.
        /// </summary>
        /// <value>The MSI handle.</value>
        internal IntPtr Handle
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("MsiHandle");
                }

                return this.handle;
            }

            set
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("MsiHandle");
                }

                this.handle = value;
                this.owningThread = Thread.CurrentThread.ManagedThreadId;
#if DEBUG
                this.creationStack = Environment.StackTrace;
#endif
            }
        }

        /// <summary>
        /// Close the MSI handle.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Disposes the managed and unmanaged objects in this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the managed and unmanaged objects in this object.
        /// </summary>
        /// <param name="disposing">true to dispose the managed objects.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (IntPtr.Zero != this.handle)
                {
                    if (Thread.CurrentThread.ManagedThreadId == this.owningThread)
                    {
                        int error = MsiInterop.MsiCloseHandle(this.handle);
                        if (0 != error)
                        {
                            throw new Win32Exception(error);
                        }
                        this.handle = IntPtr.Zero;
                    }
                    else
                    {
                        // Don't try to close the handle on a different thread than it was opened.
                        // This will occasionally cause MSI to AV.
                        string message = String.Format("Leaked msi handle {0} created on thread {1} by type {2}.  This handle cannot be closed on thread {3}",
                            this.handle, this.owningThread, this.GetType(), Thread.CurrentThread.ManagedThreadId);
#if DEBUG
                        throw new InvalidOperationException(String.Format("{0}.  Created {1}", message, this.creationStack));
#else
                        Debug.WriteLine(message);
#endif
                    }
                }

                this.disposed = true;
            }
        }
    }
}
