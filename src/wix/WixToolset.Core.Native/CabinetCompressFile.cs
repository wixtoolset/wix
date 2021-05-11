// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    /// <summary>
    /// Information to compress file into a cabinet.
    /// </summary>
    public sealed class CabinetCompressFile
    {
        /// <summary>
        /// Cabinet compress file.
        /// </summary>
        /// <param name="path">Path to file to add.</param>
        /// <param name="token">The token for the file.</param>
        public CabinetCompressFile(string path, string token)
        {
            this.Path = path;
            this.Token = token;
            this.Hash = null;
        }

        /// <summary>
        /// Cabinet compress file.
        /// </summary>
        /// <param name="path">Path to file to add.</param>
        /// <param name="token">The token for the file.</param>
        /// <param name="hash1">Hash 1</param>
        /// <param name="hash2">Hash 2</param>
        /// <param name="hash3">Hash 3</param>
        /// <param name="hash4">Hash 4</param>
        public CabinetCompressFile(string path, string token, int hash1, int hash2, int hash3, int hash4)
        {
            this.Path = path;
            this.Token = token;
            this.Hash = new[] { hash1, hash2, hash3, hash4 };
        }

        /// <summary>
        /// Gets the path to the file to compress.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the token for the file to compress.
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// Gets the hash of the file to compress.
        /// </summary>
        public int[] Hash { get; }

        internal string ToWixNativeStdinLine()
        {
            if (this.Hash == null)
            {
                return $"{this.Path}\t{this.Token}";
            }
            else
            {
                return $"{this.Path}\t{this.Token}\t{this.Hash[0]}\t{this.Hash[1]}\t{this.Hash[2]}\t{this.Hash[3]}";
            }
        }
    }
}
