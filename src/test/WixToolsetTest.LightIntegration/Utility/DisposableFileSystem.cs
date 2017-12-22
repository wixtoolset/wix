// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.LightIntegration.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class DisposableFileSystem : IDisposable
    {
        protected bool Disposed { get; private set; }

        private List<string> CleanupPaths { get; } = new List<string>();

        protected string GetFile(bool create = false)
        {
            var path = Path.GetTempFileName();

            if (!create)
            {
                File.Delete(path);
            }

            this.CleanupPaths.Add(path);

            return path;
        }

        public string GetFolder(bool create = false)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            if (create)
            {
                Directory.CreateDirectory(path);
            }

            this.CleanupPaths.Add(path);

            return path;
        }


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
                foreach (var path in this.CleanupPaths)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch
                    {
                        // Best effort delete, so ignore any failures.
                    }
                }
            }

            this.Disposed = true;
        }

        #endregion
    }
}
