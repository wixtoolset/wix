// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using System.ComponentModel;
    using System.Text;

    /// <summary>
    /// Wrapper class around msi.dll interop for a record.
    /// </summary>
    public sealed class Record : MsiHandle
    {
        /// <summary>
        /// Creates a record with the specified number of fields.
        /// </summary>
        /// <param name="fieldCount">Number of fields in record.</param>
        public Record(int fieldCount)
        {
            this.Handle = MsiInterop.MsiCreateRecord(fieldCount);
            if (IntPtr.Zero == this.Handle)
            {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>
        /// Creates a record from a handle.
        /// </summary>
        /// <param name="handle">Handle to create record from.</param>
        internal Record(IntPtr handle)
        {
            this.Handle = handle;
        }

        /// <summary>
        /// Gets a string value at specified location.
        /// </summary>
        /// <param name="field">Index into record to get string.</param>
        public string this[int field]
        {
            get => this.GetString(field);
            set => this.SetString(field, value);
        }

        /// <summary>
        /// Determines if the value is null at the specified location.
        /// </summary>
        /// <param name="field">Index into record of the field to query.</param>
        /// <returns>true if the value is null, false otherwise.</returns>
        public bool IsNull(int field)
        {
            var error = MsiInterop.MsiRecordIsNull(this.Handle, field);

            switch (error)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Gets integer value at specified location.
        /// </summary>
        /// <param name="field">Index into record to get integer</param>
        /// <returns>Integer value</returns>
        public int GetInteger(int field)
        {
            return MsiInterop.MsiRecordGetInteger(this.Handle, field);
        }

        /// <summary>
        /// Sets integer value at specified location.
        /// </summary>
        /// <param name="field">Index into record to set integer.</param>
        /// <param name="value">Value to set into record.</param>
        public void SetInteger(int field, int value)
        {
            var error = MsiInterop.MsiRecordSetInteger(this.Handle, field, value);
            if (0 != error)
            {
                throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Gets string value at specified location.
        /// </summary>
        /// <param name="field">Index into record to get string.</param>
        /// <returns>String value</returns>
        public string GetString(int field)
        {
            var bufferSize = 256;
            var buffer = new StringBuilder(bufferSize);
            var error = MsiInterop.MsiRecordGetString(this.Handle, field, buffer, ref bufferSize);
            if (234 == error)
            {
                buffer.EnsureCapacity(++bufferSize);
                error = MsiInterop.MsiRecordGetString(this.Handle, field, buffer, ref bufferSize);
            }

            if (0 != error)
            {
                throw new Win32Exception(error);
            }

            return (0 < buffer.Length ? buffer.ToString() : null);
        }

        /// <summary>
        /// Set string value at specified location
        /// </summary>
        /// <param name="field">Index into record to set string.</param>
        /// <param name="value">Value to set into record</param>
        public void SetString(int field, string value)
        {
            var error = MsiInterop.MsiRecordSetString(this.Handle, field, value);
            if (0 != error)
            {
                throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Get stream at specified location.
        /// </summary>
        /// <param name="field">Index into record to get stream.</param>
        /// <param name="buffer">buffer to receive bytes from stream.</param>
        /// <param name="requestedBufferSize">Buffer size to read.</param>
        /// <returns>Stream read into string.</returns>
        public int GetStream(int field, byte[] buffer, int requestedBufferSize)
        {
            var bufferSize = buffer.Length;
            if (requestedBufferSize > 0)
            {
                bufferSize = requestedBufferSize;
            }

            var error = MsiInterop.MsiRecordReadStream(this.Handle, field, buffer, ref bufferSize);
            if (0 != error)
            {
                throw new Win32Exception(error);
            }

            return bufferSize;
        }

        /// <summary>
        /// Sets a stream at a specified location.
        /// </summary>
        /// <param name="field">Index into record to set stream.</param>
        /// <param name="path">Path to file to read into stream.</param>
        public void SetStream(int field, string path)
        {
            var error = MsiInterop.MsiRecordSetStream(this.Handle, field, path);
            if (0 != error)
            {
                throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Gets the number of fields in record.
        /// </summary>
        /// <returns>Count of fields in record.</returns>
        public int GetFieldCount()
        {
            var size = MsiInterop.MsiRecordGetFieldCount(this.Handle);
            if (0 > size)
            {
                throw new Win32Exception();
            }

            return size;
        }
    }
}
