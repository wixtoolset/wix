// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;

    internal class UpdateMediaSequencesCommand
    {
        public UpdateMediaSequencesCommand(Output output, List<FileFacade> fileFacades)
        {
            this.Output = output;
            this.FileFacades = fileFacades;
        }

        private Output Output { get; }

        private List<FileFacade> FileFacades { get; }

        public void Execute()
        {
            var fileRows = new RowDictionary<FileRow>(this.Output.Tables["File"]);
            var mediaRows = new RowDictionary<MediaRow>(this.Output.Tables["Media"]);

            // Calculate sequence numbers and media disk id layout for all file media information objects.
            if (OutputType.Module == this.Output.Type)
            {
                var lastSequence = 0;

                // Order by Component to group the files by directory.
                var optimized = this.OptimizedFileFacades();
                foreach (var fileId in optimized.Select(f => f.File.Id.Id))
                {
                    var fileRow = fileRows.Get(fileId);
                    fileRow.Sequence = ++lastSequence;
                }
            }
            else
            {
                int lastSequence = 0;
                MediaRow mediaRow = null;
                Dictionary<int, List<FileFacade>> patchGroups = new Dictionary<int, List<FileFacade>>();

                // sequence the non-patch-added files
                var optimized = this.OptimizedFileFacades();
                foreach (FileFacade facade in optimized)
                {
                    if (null == mediaRow)
                    {
                        mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        if (OutputType.Patch == this.Output.Type)
                        {
                            // patch Media cannot start at zero
                            lastSequence = mediaRow.LastSequence;
                        }
                    }
                    else if (mediaRow.DiskId != facade.WixFile.DiskId)
                    {
                        mediaRow.LastSequence = lastSequence;
                        mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                    }

                    if (0 < facade.WixFile.PatchGroup)
                    {
                        if (patchGroups.TryGetValue(facade.WixFile.PatchGroup, out var patchGroup))
                        {
                            patchGroup = new List<FileFacade>();
                            patchGroups.Add(facade.WixFile.PatchGroup, patchGroup);
                        }

                        patchGroup.Add(facade);
                    }
                    else
                    {
                        var fileRow = fileRows.Get(facade.File.Id.Id);
                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                    mediaRow = null;
                }

                // sequence the patch-added files
                foreach (var patchGroup in patchGroups.Values)
                {
                    foreach (var facade in patchGroup)
                    {
                        if (null == mediaRow)
                        {
                            mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        }
                        else if (mediaRow.DiskId != facade.WixFile.DiskId)
                        {
                            mediaRow.LastSequence = lastSequence;
                            mediaRow = mediaRows.Get(facade.WixFile.DiskId);
                        }

                        var fileRow = fileRows.Get(facade.File.Id.Id);
                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                }
            }
        }

        private IEnumerable<FileFacade> OptimizedFileFacades()
        {
            // TODO: Sort these facades even smarter by directory path and component id 
            //       and maybe file size or file extension and other creative ideas to
            //       get optimal install speed out of MSI.
            return this.FileFacades.OrderBy(f => f.File.ComponentRef);
        }
    }
}
