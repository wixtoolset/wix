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
        private readonly Dictionary<Uri, SortedList<string, string>> filesWithEmbeddedFiles = new Dictionary<Uri, SortedList<string, string>>();

        public IEnumerable<Uri> Uris => this.filesWithEmbeddedFiles.Keys;

        /// <summary>
        /// Adds an embedded file index to track and returns the path where the embedded file will be extracted. Duplicates will return the same extract path.
        /// </summary>
        /// <param name="uri">Uri to file containing the embedded files.</param>
        /// <param name="embeddedFileId">Id of the embedded file to extract.</param>
        /// <param name="extractFolder">Folder where extracted files should be placed.</param>
        /// <returns>The extract path for the embedded file.</returns>
        public string AddEmbeddedFileToExtract(Uri uri, string embeddedFileId, string extractFolder)
        {
            // If the uri to the file that contains the embedded file does not already have embedded files
            // being extracted, create the dictionary to track that.
            if (!this.filesWithEmbeddedFiles.TryGetValue(uri, out var extracts))
            {
                extracts = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
                this.filesWithEmbeddedFiles.Add(uri, extracts);
            }

            // If the embedded file is not already tracked in the dictionary of extracts, add it.
            if (!extracts.TryGetValue(embeddedFileId, out var extractPath))
            {
                var localFileNameWithoutExtension = Path.GetFileNameWithoutExtension(uri.LocalPath);
                var unique = this.HashUri(uri.AbsoluteUri);
                var extractedName = String.Format(CultureInfo.InvariantCulture, @"{0}_{1}\{2}", localFileNameWithoutExtension, unique, embeddedFileId);

                extractPath = Path.GetFullPath(Path.Combine(extractFolder, extractedName));
                extracts.Add(embeddedFileId, extractPath);
            }

            return extractPath;
        }

        public IEnumerable<ExpectedExtractFile> GetExpectedEmbeddedFiles()
        {
            var files = new List<ExpectedExtractFile>();

            foreach (var uriWithExtracts in this.filesWithEmbeddedFiles)
            {
                foreach (var extracts in uriWithExtracts.Value)
                {
                    files.Add(new ExpectedExtractFile
                    {
                        Uri = uriWithExtracts.Key,
                        EmbeddedFileId = extracts.Key,
                        OutputPath = extracts.Value,
                    });
                }
            }

            return files;
        }

        public IEnumerable<ExpectedExtractFile> GetExtractFilesForUri(Uri uri)
        {
            if (!this.filesWithEmbeddedFiles.TryGetValue(uri, out var extracts))
            {
                extracts = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return extracts.Select(e => new ExpectedExtractFile { Uri = uri, EmbeddedFileId = e.Key, OutputPath = e.Value });
        }

        private string HashUri(string uri)
        {
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(uri));
                return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }
    }
}
