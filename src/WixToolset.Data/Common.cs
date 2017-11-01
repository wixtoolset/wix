// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    internal static class Common
    {
        public const int IntegerNotSet = Int32.MinValue;

        internal static readonly XNamespace W3SchemaPrefix = "http://www.w3.org/";

        internal static readonly string[] ReservedFileNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        internal static string GetFileHash(FileInfo fileInfo)
        {
            byte[] hashBytes;
            using (SHA1Managed managed = new SHA1Managed())
            {
                using (FileStream stream = fileInfo.OpenRead())
                {
                    hashBytes = managed.ComputeHash(stream);
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", hashBytes[i]);
            }

            return sb.ToString();
        }
    }
}
