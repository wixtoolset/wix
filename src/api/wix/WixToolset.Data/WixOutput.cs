// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Class that understands the standard file structure of the WiX toolset.
    /// </summary>
    public class WixOutput : IDisposable
    {
        private readonly Stream stream;
        private ZipArchive archive;
        private bool disposed;

        private WixOutput(Uri uri, ZipArchive archive, Stream stream)
        {
            this.Uri = uri;
            this.archive = archive;
            this.stream = stream;
        }

        /// <summary>
        ///
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Creates a new file structure in memory.
        /// </summary>
        /// <returns>Newly created <c>WixOutput</c>.</returns>
        public static WixOutput Create()
        {
            var uri = new Uri("memorystream:");

            var stream = new MemoryStream();

            return WixOutput.Create(uri, stream);
        }

        /// <summary>
        /// Creates a new file structure on disk.
        /// </summary>
        /// <param name="path">Path to write file structure to.</param>
        /// <returns>Newly created <c>WixOutput</c>.</returns>
        public static WixOutput Create(string path)
        {
            var fullPath = Path.GetFullPath(path);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            var uri = new Uri(fullPath);

            var stream = File.Create(path);

            return WixOutput.Create(uri, stream);
        }

        /// <summary>
        /// Creates a new file structure.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="stream">Stream to write the file structure to.</param>
        /// <returns>Newly created <c>WixOutput</c>.</returns>
        public static WixOutput Create(Uri uri, Stream stream)
        {
            var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true);

            return new WixOutput(uri, archive, stream);
        }

        /// <summary>
        /// Loads a wixout from a path on disk.
        /// </summary>
        /// <param name="path">Path to wixout file saved on disk.</param>
        /// <returns>Loaded created <c>WixOutput</c>.</returns>
        public static WixOutput Read(string path)
        {
            var uri = new Uri(Path.GetFullPath(path));

            var stream = File.OpenRead(path);

            return Read(uri, stream);
        }

        /// <summary>
        /// Loads a wixout from a path on disk or embedded resource in assembly.
        /// </summary>
        /// <param name="baseUri">Uri with local path to wixout file saved on disk or embedded resource in assembly.</param>
        /// <returns>Loaded created <c>WixOutput</c>.</returns>
        public static WixOutput Read(Uri baseUri)
        {
            // If the embedded files are stored in an assembly resource stream (usually
            // a .wixlib embedded in a WixExtension).
            if ("embeddedresource" == baseUri.Scheme)
            {
                var assemblyPath = Path.GetFullPath(baseUri.LocalPath);
                var resourceName = baseUri.Fragment.TrimStart('#');

                var assembly = Assembly.LoadFile(assemblyPath);
                return WixOutput.Read(assembly, resourceName);
            }
            else // normal file (usually a binary .wixlib on disk).
            {
                var stream = File.OpenRead(baseUri.LocalPath);
                return WixOutput.Read(baseUri, stream);
            }
        }

        /// <summary>
        /// Loads a wixout from an assembly resource stream.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceName"></param>
        /// <returns>Loaded created <c>WixOutput</c>.</returns>
        public static WixOutput Read(Assembly assembly, string resourceName)
        {
            var resourceStream = assembly.GetManifestResourceStream(resourceName);

            var uriBuilder = new UriBuilder(assembly.CodeBase)
            {
                Scheme = "embeddedresource",
                Fragment = resourceName
            };

            return Read(uriBuilder.Uri, resourceStream);
        }

        /// <summary>
        /// Reads a file structure from an open stream.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Loaded created <c>WixOutput</c>.</returns>
        public static WixOutput Read(Uri uri, Stream stream)
        {
            try
            {
                var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

                return new WixOutput(uri, archive, stream);
            }
            catch (InvalidDataException)
            {
                throw new WixException(ErrorMessages.CorruptFileFormat(uri.AbsoluteUri, "wixout"));
            }
        }

        /// <summary>
        /// Reopen the underlying archive for read-only or read-write access.
        /// </summary>
        /// <param name="writable">Indicates whether the output can be modified. Defaults to false.</param>
        public void Reopen(bool writable = false)
        {
            this.archive?.Dispose();
            this.archive = null;

            this.archive = new ZipArchive(this.stream, writable ? ZipArchiveMode.Update : ZipArchiveMode.Read, leaveOpen: true);
        }

        /// <summary>
        /// Extracts an embedded file.
        /// </summary>
        /// <param name="embeddedId">Id to the file to extract.</param>
        /// <param name="outputPath">Path to write the extracted file to.</param>
        public void ExtractEmbeddedFile(string embeddedId, string outputPath)
        {
            var entry = this.archive.GetEntry(embeddedId);

            if (entry == null)
            {
                throw new ArgumentOutOfRangeException(nameof(embeddedId));
            }

            var folder = Path.GetDirectoryName(outputPath);

            Directory.CreateDirectory(folder);

            entry.ExtractToFile(outputPath, overwrite: true);
        }

        /// <summary>
        /// Creates a data stream in the wixout.
        /// </summary>
        /// <returns>Stream to the data of the file.</returns>
        public Stream CreateDataStream(string name)
        {
            if (this.archive.Mode == ZipArchiveMode.Update)
            {
                this.DeleteExistingEntry(name);
            }

            var entry = this.archive.CreateEntry(name);

            return entry.Open();
        }

        /// <summary>
        /// Imports a file from disk into the output.
        /// </summary>
        /// <param name="name">Name of the stream in the output.</param>
        /// <param name="path">Path to file on disk to include in the output.</param>
        public void ImportDataStream(string name, string path)
        {
            if (this.archive.Mode == ZipArchiveMode.Update)
            {
                this.DeleteExistingEntry(name);
            }

            this.archive.CreateEntryFromFile(path, name, System.IO.Compression.CompressionLevel.Optimal);
        }

        /// <summary>
        /// Gets a non-closing stream to the data of the file.
        /// </summary>
        /// <returns>Stream to the data of the file.</returns>
        public Stream GetDataStream(string name)
        {
            var entry = this.archive.GetEntry(name);

            if (entry == null)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            return entry.Open();
        }

        /// <summary>
        /// Gets the data of the file as a string.
        /// </summary>
        /// <returns>String contents data of the file.</returns>
        public string GetData(string name)
        {
            var entry = this.archive.GetEntry(name);

            // Use StreamReader to "swallow" BOM if present.
            using (var stream = entry.Open())
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Creates a new file structure on disk that can only be written to.
        /// </summary>
        /// <param name="path">Path to write file structure to.</param>
        /// <returns>Newly created <c>WixOutput</c>.</returns>
        internal static WixOutput CreateNew(string path)
        {
            var fullPath = Path.GetFullPath(path);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            var uri = new Uri(fullPath);

            var stream = File.Create(path);

            var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

            return new WixOutput(uri, archive, stream);
        }

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
                    this.archive?.Dispose();
                    this.stream?.Dispose();
                }
            }

            this.disposed = true;
        }

        private void DeleteExistingEntry(string name)
        {
            var entry = this.archive.GetEntry(name);
            if (entry != null)
            {
                entry.Delete();
            }
        }
    }
}
