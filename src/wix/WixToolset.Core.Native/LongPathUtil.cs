// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class PathUtil
    {
        private const int MaxPath = 260;
        private const string LongPathPrefix = @"\\?\";

        public static bool CreateOrGetShortPath(string path, out string shortPath)
        {
            var fileCreated = false;

            // The file must exist so we can get its short path.
            if (!File.Exists(path))
            {
                using (File.Create(path))
                {
                }

                fileCreated = true;
            }

            // Use the short path to avoid issues with long paths in the MSI API.
            shortPath = GetShortPath(path);

            return fileCreated;
        }

        public static string GetPrefixedLongPath(string path)
        {
            if (path.Length > MaxPath && !path.StartsWith(LongPathPrefix))
            {
                path = LongPathPrefix + path;
            }

            return path;
        }

        public static string GetShortPath(string longPath)
        {
            var path = GetPrefixedLongPath(longPath);

            var buffer = new StringBuilder(MaxPath); // start with MAX_PATH.

            var result = GetShortPathName(path, buffer, (uint)buffer.Capacity);

            // If result > buffer.Capacity, reallocate and call again (even though we're usually using short names to avoid long path)
            // so the short path result is still going to end up too long for APIs requiring a short path.
            if (result > buffer.Capacity)
            {
                buffer = new StringBuilder((int)result);

                result = GetShortPathName(path, buffer, (uint)buffer.Capacity);
            }

            // If we succeeded, return the short path without the prefix.
            if (result > 0)
            {
                path = buffer.ToString();

                if (path.StartsWith(LongPathPrefix))
                {
                    path = path.Substring(LongPathPrefix.Length);
                }
            }

            return path;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);
    }
}
