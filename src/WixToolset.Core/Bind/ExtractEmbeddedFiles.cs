// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Internal helper class used to extract embedded files.
    /// </summary>
    internal class ExtractEmbeddedFiles
    {
        private Dictionary<Uri, SortedList<int, string>> filesWithEmbeddedFiles = new Dictionary<Uri, SortedList<int, string>>();

        public IEnumerable<Uri> Uris => this.filesWithEmbeddedFiles.Keys;

        /// <summary>
        /// Adds an embedded file index to track and returns the path where the embedded file will be extracted. Duplicates will return the same extract path.
        /// </summary>
        /// <param name="uri">Uri to file containing the embedded files.</param>
        /// <param name="embeddedFileIndex">Index of the embedded file to extract.</param>
        /// <param name="tempPath">Path where temporary files should be placed.</param>
        /// <returns>The extract path for the embedded file.</returns>
        public string AddEmbeddedFileIndex(Uri uri, int embeddedFileIndex, string tempPath)
        {
            // If the uri to the file that contains the embedded file does not already have embedded files
            // being extracted, create the dictionary to track that.
            if (!this.filesWithEmbeddedFiles.TryGetValue(uri, out var extracts))
            {
                extracts = new SortedList<int, string>();
                this.filesWithEmbeddedFiles.Add(uri, extracts);
            }

            // If the embedded file is not already tracked in the dictionary of extracts, add it.
            if (!extracts.TryGetValue(embeddedFileIndex, out var extractPath))
            {
                string localFileNameWithoutExtension = Path.GetFileNameWithoutExtension(uri.LocalPath);
                string unique = this.HashUri(uri.AbsoluteUri);
                string extractedName = String.Format(CultureInfo.InvariantCulture, @"{0}_{1}\{2}", localFileNameWithoutExtension, unique, embeddedFileIndex);

                extractPath = Path.Combine(tempPath, extractedName);
                extracts.Add(embeddedFileIndex, extractPath);
            }

            return extractPath;
        }

        public IEnumerable<ExpectedExtractFile> GetExpectedEmbeddedFiles()
        {
            foreach (var uriWithExtracts in this.filesWithEmbeddedFiles)
            {
                foreach (var extracts in uriWithExtracts.Value)
                {
                    yield return new ExpectedExtractFile
                    {
                        Uri = uriWithExtracts.Key,
                        EmbeddedFileIndex = extracts.Key,
                        OutputPath = extracts.Value,
                    };
                }
            }
        }

        public IEnumerable<ExpectedExtractFile> GetExtractFilesForUri(Uri uri)
        {
            if (!this.filesWithEmbeddedFiles.TryGetValue(uri, out var extracts))
            {
                extracts = new SortedList<int, string>();
            }

            return extracts.Select(e => new ExpectedExtractFile() { Uri = uri, EmbeddedFileIndex = e.Key, OutputPath = e.Value });
        }

        private string HashUri(string uri)
        {
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(uri));
                return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }
    }
}
