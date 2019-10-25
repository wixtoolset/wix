// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    public class ExtractEmbeddedFilesCommand
    {
        public ExtractEmbeddedFilesCommand(IEnumerable<IExpectedExtractFile> embeddedFiles)
        {
            this.FilesWithEmbeddedFiles = embeddedFiles;
        }

        private IEnumerable<IExpectedExtractFile> FilesWithEmbeddedFiles { get; }

        public void Execute()
        {
            var group = this.FilesWithEmbeddedFiles.GroupBy(e => e.Uri);

            foreach (var expectedEmbeddedFileByUri in group)
            {
                var baseUri = expectedEmbeddedFileByUri.Key;

                using (var wixout = WixOutput.Read(baseUri))
                {
                    var uniqueIds = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var embeddedFile in expectedEmbeddedFileByUri)
                    {
                        if (uniqueIds.Add(embeddedFile.EmbeddedFileId))
                        {
                            wixout.ExtractEmbeddedFile(embeddedFile.EmbeddedFileId, embeddedFile.OutputPath);
                        }
                    }
                }
            }
        }
    }
}
