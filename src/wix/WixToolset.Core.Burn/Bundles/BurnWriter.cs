// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Burn PE writer for the WiX toolset.
    /// </summary>
    /// <remarks>This class encapsulates reading/writing to a stub EXE for
    /// creating bundled/chained setup packages.</remarks>
    /// <example>
    /// using (BurnWriter writer = new BurnWriter(fileExe, this.core, guid))
    /// {
    ///     writer.AppendContainer(file1, BurnWriter.Container.UX);
    ///     writer.AppendContainer(file2, BurnWriter.Container.Attached);
    /// }
    /// </example>
    internal class BurnWriter : BurnCommon
    {
        private bool disposed;
        private BinaryWriter binaryWriter;
        private readonly IFileSystem fileSystem;

        private BurnWriter(IMessaging messaging, IFileSystem fileSystem, string fileExe)
            : base(messaging, fileExe)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Opens a Burn writer.
        /// </summary>
        /// <param name="messaging">Messaging system.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="fileExe">Path to file.</param>
        /// <returns>Burn writer.</returns>
        public static BurnWriter Open(IMessaging messaging, IFileSystem fileSystem, string fileExe)
        {
            var writer = new BurnWriter(messaging, fileSystem, fileExe);

            using (var binaryReader = new BinaryReader(fileSystem.OpenFile(null, fileExe, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)))
            {
                writer.Initialize(binaryReader);
            }

            if (!writer.Invalid)
            {
                writer.binaryWriter = new BinaryWriter(fileSystem.OpenFile(null, fileExe, FileMode.Open, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete));
            }

            return writer;
        }

        /// <summary>
        /// Update the ".wixburn" section data.
        /// </summary>
        /// <param name="stubSize">Size of the stub engine "burn.exe".</param>
        /// <param name="bundleId">Unique identifier for this bundle.</param>
        /// <returns></returns>
        public bool InitializeBundleSectionData(long stubSize, string bundleId)
        {
            if (this.Invalid)
            {
                return false;
            }

            var bundleGuid = Guid.Parse(bundleId);

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_MAGIC, BURN_SECTION_MAGIC);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_VERSION, BURN_SECTION_VERSION);

            this.Messaging.Write(VerboseMessages.BundleGuid(bundleId));
            this.binaryWriter.BaseStream.Seek(this.wixburnDataOffset + BURN_SECTION_OFFSET_BUNDLEGUID, SeekOrigin.Begin);
            this.binaryWriter.Write(bundleGuid.ToByteArray());

            this.BundleId = bundleGuid;
            this.StubSize = (uint)stubSize;

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_STUBSIZE, this.StubSize);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALCHECKSUM, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATUREOFFSET, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATURESIZE, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_FORMAT, 1); // Hard-coded to CAB for now.
            this.AttachedContainers.Clear();
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_COUNT, 0);
            for (var i = BURN_SECTION_OFFSET_UXSIZE; i < this.wixburnMaxContainers; i += sizeof(uint))
            {
                this.WriteToBurnSectionOffset(i, 0);
            }
            this.binaryWriter.BaseStream.Flush();

            this.EngineSize = this.StubSize;

            return true;
        }

        /// <summary>
        /// Appends a UX or Attached container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the container.</param>
        /// <param name="fileContainer">File path to append to the current exe.</param>
        /// <param name="container">Container section represented by the fileContainer.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        public bool AppendContainer(SourceLineNumber sourceLineNumbers, string fileContainer, BurnCommon.Container container)
        {
            using (var reader = this.fileSystem.OpenFile(sourceLineNumbers, fileContainer, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return this.AppendContainer(reader, reader.Length, container);
            }
        }

        /// <summary>
        /// Appends the non-UX attached containers from the reader to this bundle.
        /// </summary>
        /// <param name="reader">The source bundle.</param>
        /// <returns>true if the container data is successfully appended; false otherwise.</returns>
        public bool ReattachContainers(BurnReader reader)
        {
            if (this.AttachedContainers.Count == 0 || reader.AttachedContainers.Count < 2)
            {
                return false;
            }

            this.RememberThenResetSignature();

            var uxContainerSlot = this.AttachedContainers[0];
            this.AttachedContainers.Clear();
            this.AttachedContainers.Add(uxContainerSlot);

            var nextAddress = this.EngineSize;
            for (var i = 1; i < reader.AttachedContainers.Count; i++)
            {
                var slot = reader.AttachedContainers[i];

                reader.Stream.Seek(nextAddress, SeekOrigin.Begin);
                // TODO: verify that the size in the section data is 0 or the same size.
                this.AppendContainer(reader.Stream, slot.Size, BurnCommon.Container.Attached);

                nextAddress += slot.Size;
            }

            return true;
        }

        /// <summary>
        /// Appends a UX or Attached container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="containerStream">File stream to append to the current exe.</param>
        /// <param name="containerSize">Size of container to append.</param>
        /// <param name="container">Container section represented by the fileContainer.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        public bool AppendContainer(Stream containerStream, long containerSize, BurnCommon.Container container)
        {
            var containerCount = (uint)this.AttachedContainers.Count;
            var burnSectionOffsetSize = BURN_SECTION_OFFSET_UXSIZE + (containerCount * sizeof(uint));
            var containerSlot = new ContainerSlot((uint)containerSize);

            switch (container)
            {
                case Container.UX:
                    if (containerCount != 0)
                    {
                        Debug.Assert(false);
                        return false;
                    }

                    this.EngineSize += containerSlot.Size;
                    break;

                case Container.Attached:
                    break;

                default:
                    Debug.Assert(false);
                    return false;
            }

            this.AttachedContainers.Add(containerSlot);
            ++containerCount;
            return this.AppendContainer(containerStream, containerSlot.Size, burnSectionOffsetSize, containerCount);
        }

        public void RememberThenResetSignature()
        {
            if (this.Invalid)
            {
                return;
            }

            this.OriginalChecksum = this.Checksum;
            this.OriginalSignatureOffset = this.SignatureOffset;
            this.OriginalSignatureSize = this.SignatureSize;

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALCHECKSUM, this.OriginalChecksum);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATUREOFFSET, this.OriginalSignatureOffset);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATURESIZE, this.OriginalSignatureSize);

            this.Checksum = 0;
            this.SignatureOffset = 0;
            this.SignatureSize = 0;

            this.WriteToOffset(this.checksumOffset, this.Checksum);
            this.WriteToOffset(this.certificateTableSignatureOffset, this.SignatureOffset);
            this.WriteToOffset(this.certificateTableSignatureSize, this.SignatureSize);
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        /// <param name="disposing">True when releasing managed objects.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing && this.binaryWriter != null)
                {
                    this.binaryWriter.Close();
                    this.binaryWriter = null;
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Appends a container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="containerStream">File stream to append to the current exe.</param>
        /// <param name="containerSize">Size of the container.</param>
        /// <param name="burnSectionOffsetSize">Offset of size field for this container in ".wixburn" section data.</param>
        /// <param name="burnSectionCount">Number of Burn sections.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        private bool AppendContainer(Stream containerStream, uint containerSize, uint burnSectionOffsetSize, uint burnSectionCount)
        {
            if (this.Invalid)
            {
                return false;
            }

            if (burnSectionOffsetSize > (this.wixburnRawDataSize - sizeof(uint)))
            {
                this.Invalid = true;
                this.Messaging.Write(BurnBackendErrors.TooManyAttachedContainers(this.wixburnMaxContainers));
                return false;
            }

            // Update the ".wixburn" section data
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_COUNT, burnSectionCount);
            this.WriteToBurnSectionOffset(burnSectionOffsetSize, containerSize);

            // Append the container to the end of the existing bits.
            this.binaryWriter.BaseStream.Seek(0, SeekOrigin.End);
            BurnCommon.CopyStream(containerStream, this.binaryWriter.BaseStream, containerSize);
            this.binaryWriter.BaseStream.Flush();

            return true;
        }

        /// <summary>
        /// Writes the value to an offset in the Burn section data.
        /// </summary>
        /// <param name="offset">Offset in to the Burn section data.</param>
        /// <param name="value">Value to write.</param>
        private void WriteToBurnSectionOffset(uint offset, uint value)
        {
            this.WriteToOffset(this.wixburnDataOffset + offset, value);
        }

        /// <summary>
        /// Writes the value to an offset in the Burn stub.
        /// </summary>
        /// <param name="offset">Offset in to the Burn stub.</param>
        /// <param name="value">Value to write.</param>
        private void WriteToOffset(uint offset, uint value)
        {
            this.binaryWriter.BaseStream.Seek((int)offset, SeekOrigin.Begin);
            this.binaryWriter.Write(value);
        }
    }
}
