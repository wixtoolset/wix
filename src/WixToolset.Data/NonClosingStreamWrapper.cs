// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.IO;

    /// <summary>
    /// Wrapper around stream to prevent other streams (like BinaryReader/Writer) from prematurely
    /// closing a parent stream.
    /// </summary>
    internal class NonClosingStreamWrapper : Stream
    {
        private Stream stream;

        public NonClosingStreamWrapper(Stream stream)
        {
            this.stream = stream;
        }

        public override bool CanRead { get { return this.stream.CanRead; } }

        public override bool CanSeek { get { return this.stream.CanSeek; } }

        public override bool CanTimeout { get { return this.stream.CanTimeout; } }

        public override bool CanWrite { get { return this.stream.CanWrite; } }

        public override long Length { get { return this.stream.Length; } }

        public override long Position { get { return this.stream.Position; } set { this.stream.Position = value; } }

        public override int ReadTimeout { get { return this.stream.ReadTimeout; } set { this.stream.ReadTimeout = value; } }

        public override int WriteTimeout { get { return this.stream.WriteTimeout; } set { this.stream.WriteTimeout = value; } }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            // Do not pass through the call since this is what we are overriding.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Flush();
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return this.stream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this.stream.WriteByte(value);
        }
    }
}
