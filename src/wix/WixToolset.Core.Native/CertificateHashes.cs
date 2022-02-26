// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Read certificates' public key and thumbprint hashes.
    /// </summary>
    public sealed class CertificateHashes
    {
        private static readonly char[] TextLineSplitter = new[] { '\t' };

        private CertificateHashes(string path, string publicKey, string thumbprint, Exception exception)
        {
            this.Path = path;
            this.PublicKey = publicKey;
            this.Thumbprint = thumbprint;
            this.Exception = exception;
        }

        /// <summary>
        /// Path to the file read.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Hash of the certificate's public key.
        /// </summary>
        public string PublicKey { get; }

        /// <summary>
        /// Hash of the certificate's thumbprint.
        /// </summary>
        public string Thumbprint { get; }

        /// <summary>
        /// Exception encountered while trying to read certificate's hash.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Read the certificate hashes from the provided paths.
        /// </summary>
        /// <param name="paths">Paths to read for certificates.</param>
        /// <returns>Certificate hashes for the provided paths.</returns>
        public static IReadOnlyList<CertificateHashes> Read(IEnumerable<string> paths)
        {
            var result = new List<CertificateHashes>();

            var wixnative = new WixNativeExe("certhashes");

            foreach (var path in paths)
            {
                wixnative.AddStdinLine(path);
            }

            try
            {
                var outputLines = wixnative.Run();
                foreach (var line in outputLines.Where(l => !String.IsNullOrEmpty(l)))
                {
                    var data = line.Split(TextLineSplitter, StringSplitOptions.None);

                    var error = Int32.Parse(data[3].Substring(2), NumberStyles.HexNumber);
                    var exception = error != 0 ? new Win32Exception(error) : null;

                    result.Add(new CertificateHashes(data[0], data[1], data[2], exception));
                }
            }
            catch (Exception e)
            {
                result.Add(new CertificateHashes(null, null, null, e));
            }

            return result;
        }
    }
}
