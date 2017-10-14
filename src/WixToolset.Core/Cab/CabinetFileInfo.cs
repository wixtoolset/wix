// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Cab
{
    /// <summary>
    /// Properties of a file in a cabinet.
    /// </summary>
    public sealed class CabinetFileInfo
    {
        /// <summary>
        /// Constructs CabinetFileInfo
        /// </summary>
        /// <param name="fileId">File Id</param>
        /// <param name="date">Last modified date (MS-DOS time)</param>
        /// <param name="time">Last modified time (MS-DOS time)</param>
        public CabinetFileInfo(string fileId, ushort date, ushort time, int size)
        {
            this.FileId = fileId;
            this.Date = date;
            this.Time = time;
            this.Size = size;
        }

        /// <summary>
        /// Gets the file Id of the file.
        /// </summary>
        /// <value>file Id</value>
        public string FileId { get; }

        /// <summary>
        /// Gets modified date (DOS format).
        /// </summary>
        public ushort Date { get; }

        /// <summary>
        /// Gets modified time (DOS format).
        /// </summary>
        public ushort Time { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public int Size { get; }
    }
}
