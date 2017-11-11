// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility;

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

                Stream stream = null;
                try
                {
                    // If the embedded files are stored in an assembly resource stream (usually
                    // a .wixlib embedded in a WixExtension).
                    if ("embeddedresource" == baseUri.Scheme)
                    {
                        var assemblyPath = Path.GetFullPath(baseUri.LocalPath);
                        var resourceName = baseUri.Fragment.TrimStart('#');

                        var assembly = Assembly.LoadFile(assemblyPath);
                        stream = assembly.GetManifestResourceStream(resourceName);
                    }
                    else // normal file (usually a binary .wixlib on disk).
                    {
                        stream = File.OpenRead(baseUri.LocalPath);
                    }

                    using (var fs = FileStructure.Read(stream))
                    {
                        var uniqueIndicies = new SortedSet<int>();

                        foreach (var embeddedFile in expectedEmbeddedFileByUri)
                        {
                            if (uniqueIndicies.Add(embeddedFile.EmbeddedFileIndex))
                            {
                                fs.ExtractEmbeddedFile(embeddedFile.EmbeddedFileIndex, embeddedFile.OutputPath);
                            }
                        }
                    }
                }
                finally
                {
                    stream?.Close();
                }
            }
        }
    }
}
