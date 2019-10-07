// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    internal static class BundleHashAlgorithm
    {
        public static string Hash(FileInfo fileInfo)
        {
            byte[] hashBytes;

            using (var managed = new SHA1Managed())
            using (var stream = fileInfo.OpenRead())
            {
                hashBytes = managed.ComputeHash(stream);
            }

            var sb = new StringBuilder(hashBytes.Length * 2);
            for (var i = 0; i < hashBytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", hashBytes[i]);
            }

            return sb.ToString();
        }
    }
}
