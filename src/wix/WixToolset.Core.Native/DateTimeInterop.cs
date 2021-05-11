// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interop class for the date/time handling.
    /// </summary>
    internal static class DateTimeInterop
    {
        /// <summary>
        /// Converts DateTime to MS-DOS date and time which cabinet uses.
        /// </summary>
        /// <param name="dateTime">DateTime</param>
        /// <param name="cabDate">MS-DOS date</param>
        /// <param name="cabTime">MS-DOS time</param>
        public static void DateTimeToCabDateAndTime(DateTime dateTime, out ushort cabDate, out ushort cabTime)
        {
            // dateTime.ToLocalTime() does not match FileTimeToLocalFileTime() for some reason.
            // so we need to call FileTimeToLocalFileTime() from kernel32.dll.
            long filetime = dateTime.ToFileTime();
            long localTime = 0;
            FileTimeToLocalFileTime(ref filetime, ref localTime);
            FileTimeToDosDateTime(ref localTime, out cabDate, out cabTime);
        }

        /// <summary>
        /// Converts file time to a local file time.
        /// </summary>
        /// <param name="fileTime">file time</param>
        /// <param name="localTime">local file time</param>
        /// <returns>true if successful, false otherwise</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FileTimeToLocalFileTime(ref long fileTime, ref long localTime);

        /// <summary>
        /// Converts file time to a MS-DOS time.
        /// </summary>
        /// <param name="fileTime">file time</param>
        /// <param name="wFatDate">MS-DOS date</param>
        /// <param name="wFatTime">MS-DOS time</param>
        /// <returns>true if successful, false otherwise</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FileTimeToDosDateTime(ref long fileTime, out ushort wFatDate, out ushort wFatTime);
    }
}
