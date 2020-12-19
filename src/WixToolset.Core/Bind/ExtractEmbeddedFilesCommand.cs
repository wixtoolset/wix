// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

#pragma warning disable 1591 // TODO: this shouldn't be public, need interface in Extensibility
    public class ExtractEmbeddedFilesCommand
    {
        public ExtractEmbeddedFilesCommand(IBackendHelper backendHelper, IEnumerable<IExpectedExtractFile> embeddedFiles)
        {
            this.BackendHelper = backendHelper;
            this.FilesWithEmbeddedFiles = embeddedFiles;
        }

        public IEnumerable<ITrackedFile> TrackedFiles { get; private set; }

        private IBackendHelper BackendHelper { get; }

        private IEnumerable<IExpectedExtractFile> FilesWithEmbeddedFiles { get; }

        public void Execute()
        {
            var trackedFiles = new List<ITrackedFile>();
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
                            trackedFiles.Add(this.BackendHelper.TrackFile(embeddedFile.OutputPath, TrackedFileType.Temporary));
                        }
                    }
                }
            }

            this.TrackedFiles = trackedFiles;
        }
    }
}
