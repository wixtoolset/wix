// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class that understands the standard file structures in the WiX toolset.
    /// </summary>
    public class FileStructure : IDisposable
    {
        private long dataStreamOffset;
        private long[] embeddedFileSizes;
        private Stream stream;
        private bool disposed;

        private static readonly Dictionary<string, FileFormat> SupportedFileFormats = new Dictionary<string, FileFormat>()
        {
            { "wixobj", FileFormat.Wixobj },
            { "wixlib", FileFormat.Wixlib },
            { "wixout", FileFormat.Wixout },
            { "wixpdb", FileFormat.Wixpdb },
            { "wixmst", FileFormat.Wixout },
            { "wixmsp", FileFormat.Wixout },
        };

        /// <summary>
        /// Use Create or Read to create a FileStructure.
        /// </summary>
        private FileStructure() { }

        /// <summary>
        /// Count of embedded files in the file structure.
        /// </summary>
        public int EmbeddedFileCount { get { return this.embeddedFileSizes.Length; } }

        /// <summary>
        /// File format of the file structure.
        /// </summary>
        public FileFormat FileFormat { get; private set; }

        /// <summary>
        /// Creates a new file structure.
        /// </summary>
        /// <param name="stream">Stream to write the file structure to.</param>
        /// <param name="fileFormat">File format for the file structure.</param>
        /// <param name="embedFilePaths">Paths to files to embedd in the file structure.</param>
        /// <returns>Newly created file structure.</returns>
        public static FileStructure Create(Stream stream, FileFormat fileFormat, List<string> embedFilePaths)
        {
            FileStructure fs = new FileStructure();
            using (NonClosingStreamWrapper wrapper = new NonClosingStreamWrapper(stream))
            using (BinaryWriter writer = new BinaryWriter(wrapper))
            {
                fs.WriteType(writer, fileFormat);

                fs.WriteEmbeddedFiles(writer, embedFilePaths ?? new List<string>());

                // Remember the data stream offset, which is right after the embedded files have been written.
                fs.dataStreamOffset = stream.Position;
            }

            fs.stream = stream;

            return fs;
        }

        /// <summary>
        /// Reads a file structure from an open stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>File structure populated from the stream.</returns>
        public static FileStructure Read(Stream stream)
        {
            FileStructure fs = new FileStructure();
            using (NonClosingStreamWrapper wrapper = new NonClosingStreamWrapper(stream))
            using (BinaryReader reader = new BinaryReader(wrapper))
            {
                fs.FileFormat = FileStructure.ReadFileFormat(reader);

                if (FileFormat.Unknown != fs.FileFormat)
                {
                    fs.embeddedFileSizes = FileStructure.ReadEmbeddedFileSizes(reader);

                    // Remember the data stream offset, which is right after the embedded files have been written.
                    fs.dataStreamOffset = stream.Position;
                    foreach (long size in fs.embeddedFileSizes)
                    {
                        fs.dataStreamOffset += size;
                    }
                }
            }

            fs.stream = stream;

            return fs;
        }

        /// <summary>
        /// Guess at the file format based on the file extension.
        /// </summary>
        /// <param name="extension">File extension to guess the file format for.</param>
        /// <returns>Best guess at file format.</returns>
        public static FileFormat GuessFileFormatFromExtension(string extension)
        {
            FileFormat format;
            return FileStructure.SupportedFileFormats.TryGetValue(extension.TrimStart('.').ToLowerInvariant(), out format) ? format : FileFormat.Unknown;
        }

        /// <summary>
        /// Probes a stream to determine the file format.
        /// </summary>
        /// <param name="stream">Stream to test.</param>
        /// <returns>The file format.</returns>
        public static FileFormat TestFileFormat(Stream stream)
        {
            FileFormat format = FileFormat.Unknown;

            long position = stream.Position;

            try
            {
                using (NonClosingStreamWrapper wrapper = new NonClosingStreamWrapper(stream))
                using (BinaryReader reader = new BinaryReader(wrapper))
                {
                    format = FileStructure.ReadFileFormat(reader);
                }
            }
            finally
            {
                stream.Seek(position, SeekOrigin.Begin);
            }

            return format;
        }

        /// <summary>
        /// Extracts an embedded file.
        /// </summary>
        /// <param name="embeddedIndex">Index to the file to extract.</param>
        /// <param name="outputPath">Path to write the extracted file to.</param>
        public void ExtractEmbeddedFile(int embeddedIndex, string outputPath)
        {
            if (this.EmbeddedFileCount <= embeddedIndex)
            {
                throw new ArgumentOutOfRangeException("embeddedIndex");
            }

            long header = 6 + 4 + (this.embeddedFileSizes.Length * 8); // skip the type + the count of embedded files + all the sizes of embedded files.
            long position = this.embeddedFileSizes.Take(embeddedIndex).Sum(); // skip to the embedded file we want.
            long size = this.embeddedFileSizes[embeddedIndex];

            this.stream.Seek(header + position, SeekOrigin.Begin);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (FileStream output = File.OpenWrite(outputPath))
            {
                int read;
                int total = 0;
                byte[] buffer = new byte[64 * 1024];
                while (0 < (read = this.stream.Read(buffer, 0, (int)Math.Min(buffer.Length, size - total))))
                {
                    output.Write(buffer, 0, read);
                    total += read;
                }
            }
        }

        /// <summary>
        /// Gets a non-closing stream to the data of the file.
        /// </summary>
        /// <returns>Stream to the data of the file.</returns>
        public Stream GetDataStream()
        {
            this.stream.Seek(this.dataStreamOffset, SeekOrigin.Begin);
            return new NonClosingStreamWrapper(this.stream);
        }

        /// <summary>
        /// Disposes of the internsl state of the file structure.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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
                    if (null != this.stream)
                    {
                        // We do not own the stream, so we don't close it. We're just resetting our internal state.
                        this.embeddedFileSizes = null;
                        this.dataStreamOffset = 0;
                        this.stream = null;
                    }
                }
            }

            this.disposed = true;
        }

        private static FileFormat ReadFileFormat(BinaryReader reader)
        {
            FileFormat format = FileFormat.Unknown;

            string type = new string(reader.ReadChars(6));
            FileStructure.SupportedFileFormats.TryGetValue(type, out format);

            return format;
        }

        private static long[] ReadEmbeddedFileSizes(BinaryReader reader)
        {
            uint count = reader.ReadUInt32();

            long[] embeddedFileSizes = new long[count];

            for (int i = 0; i < embeddedFileSizes.Length; ++i)
            {
                embeddedFileSizes[i] = (long)reader.ReadUInt64();
            }

            return embeddedFileSizes;
        }

        private BinaryWriter WriteType(BinaryWriter writer, FileFormat fileFormat)
        {
            string type = null;
            foreach (var supported in FileStructure.SupportedFileFormats)
            {
                if (supported.Value.Equals(fileFormat))
                {
                    type = supported.Key;
                    break;
                }
            }

            if (String.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Unknown file format type", "fileFormat");
            }

            this.FileFormat = fileFormat;

            Debug.Assert(6 == type.ToCharArray().Length);
            writer.Write(type.ToCharArray());
            return writer;
        }

        private BinaryWriter WriteEmbeddedFiles(BinaryWriter writer, List<string> embedFilePaths)
        {
            // First write the count of embedded files as a Uint32;
            writer.Write((uint)embedFilePaths.Count);

            this.embeddedFileSizes = new long[embedFilePaths.Count];

            // Next write out the size of each file as a Uint64 in order.
            FileInfo[] files = new FileInfo[embedFilePaths.Count];
            for (int i = 0; i < embedFilePaths.Count; ++i)
            {
                files[i] = new FileInfo(embedFilePaths[i]);

                this.embeddedFileSizes[i] = files[i].Length;
                writer.Write((ulong)this.embeddedFileSizes[i]);
            }

            // Next write out the content of each file *after* the sizes of
            // *all* of the files were written.
            foreach (FileInfo file in files)
            {
                using (FileStream stream = file.OpenRead())
                {
                    stream.CopyTo(writer.BaseStream);
                }
            }

            return writer;
        }
    }
}
