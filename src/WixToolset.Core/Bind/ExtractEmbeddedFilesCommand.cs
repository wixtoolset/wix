// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind
{
    using System.IO;
    using System.Reflection;
    using WixToolset.Data;

    internal class ExtractEmbeddedFilesCommand : ICommand
    {
        public ExtractEmbeddedFiles FilesWithEmbeddedFiles { private get; set; }

        public void Execute()
        {
            foreach (var baseUri in this.FilesWithEmbeddedFiles.Uris)
            {
                Stream stream = null;
                try
                {
                    // If the embedded files are stored in an assembly resource stream (usually
                    // a .wixlib embedded in a WixExtension).
                    if ("embeddedresource" == baseUri.Scheme)
                    {
                        string assemblyPath = Path.GetFullPath(baseUri.LocalPath);
                        string resourceName = baseUri.Fragment.TrimStart('#');

                        Assembly assembly = Assembly.LoadFile(assemblyPath);
                        stream = assembly.GetManifestResourceStream(resourceName);
                    }
                    else // normal file (usually a binary .wixlib on disk).
                    {
                        stream = File.OpenRead(baseUri.LocalPath);
                    }

                    using (FileStructure fs = FileStructure.Read(stream))
                    {
                        foreach (var embeddedFile in this.FilesWithEmbeddedFiles.GetExtractFilesForUri(baseUri))
                        {
                            fs.ExtractEmbeddedFile(embeddedFile.EmbeddedFileIndex, embeddedFile.OutputPath);
                        }
                    }
                }
                finally
                {
                    if (null != stream)
                    {
                        stream.Close();
                    }
                }
            }
        }
    }
}
