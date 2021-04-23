// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;

    /// <summary>
    /// Properties of a file in a cabinet.
    /// </summary>
    public sealed class CabinetFileInfo
    {
        /// <summary>
        /// Constructs CabinetFileInfo
        /// </summary>
        /// <param name="fileId">File Id</param>
        /// <param name="size">Size of file</param>
        /// <param name="date">Last modified date</param>
        /// <param name="time">Last modified time</param>
        public CabinetFileInfo(string fileId, int size, int date, int time)
        {
            this.FileId = fileId;
            this.Size = size;
            this.Date = date;
            this.Time = time;
        }

        /// <summary>
        /// Gets the file Id of the file.
        /// </summary>
        /// <value>file Id</value>
        public string FileId { get; }

        /// <summary>
        /// Gets modified date (DOS format).
        /// </summary>
        public int Date { get; }

        /// <summary>
        /// Gets modified time (DOS format).
        /// </summary>
        public int Time { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Compares this file info's date and time with another datetime.
        /// </summary>
        /// <param name="dateTime">Date and time to compare with/</param>
        /// <returns>
        /// For some reason DateTime.ToLocalTime() does not match kernel32.dll FileTimeToLocalFileTime().
        /// Since cabinets store date and time with the kernel32.dll functions, we need to convert DateTime
        /// to local file time using the kernel32.dll functions.
        /// </returns>
        public bool SameAsDateTime(DateTime dateTime)
        {
            DateTimeInterop.DateTimeToCabDateAndTime(dateTime, out var cabDate, out var cabTime);
            return this.Date == cabDate && this.Time == cabTime;
        }
    }
}
