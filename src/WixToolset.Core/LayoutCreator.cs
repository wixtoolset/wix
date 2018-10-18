// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Layout for the WiX toolset.
    /// </summary>
    internal class LayoutCreator : ILayoutCreator
    {
        internal LayoutCreator(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public void Layout(ILayoutContext context)
        {
            // Pre-layout.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreLayout(context);
            }

            try
            {
                // Final step in binding that transfers (moves/copies) all files generated into the appropriate
                // location in the source image.
                if (context.FileTransfers?.Any() == true)
                {
                    this.Messaging.Write(VerboseMessages.LayingOutMedia());

                    var command = new TransferFilesCommand(this.Messaging, context.Extensions, context.FileTransfers, context.SuppressAclReset);
                    command.Execute();
                }

                if (context.TrackedFiles != null)
                {
                    this.CleanTempFiles(context.IntermediateFolder, context.TrackedFiles);
                }
            }
            finally
            {
                if (context.TrackedFiles != null)
                {
                    if (!String.IsNullOrEmpty(context.ContentsFile))
                    {
                        this.CreateContentsFile(context.ContentsFile, context.TrackedFiles);
                    }

                    if (!String.IsNullOrEmpty(context.OutputsFile))
                    {
                        this.CreateOutputsFile(context.OutputsFile, context.TrackedFiles);
                    }

                    if (!String.IsNullOrEmpty(context.BuiltOutputsFile))
                    {
                        this.CreateBuiltOutputsFile(context.BuiltOutputsFile, context.TrackedFiles);
                    }
                }
            }

            // Post-layout.
            foreach (var extension in context.Extensions)
            {
                extension.PostLayout();
            }
        }

        /// <summary>
        /// Writes the paths to the content files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="contentFilePaths">Collection of paths to content files that will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueInputFilePaths = new SortedSet<string>(trackedFiles.Where(t => t.Type == TrackedFileType.Input).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            if (!uniqueInputFilePaths.Any())
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var contents = new StreamWriter(path, false))
            {
                foreach (var inputPath in uniqueInputFilePaths)
                {
                    contents.WriteLine(inputPath);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        private void CreateOutputsFile(string path, IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueOutputPaths = new SortedSet<string>(trackedFiles.Where(t => t.Clean).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            if (!uniqueOutputPaths.Any())
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var outputs = new StreamWriter(path, false))
            {
                //// Don't list files where the source is the same as the destination since
                //// that might be the only place the file exists. The outputs file is often
                //// used to delete stuff and losing the original source would be bad.
                //var uniqueOutputPaths = new SortedSet<string>(fileTransfers.Where(ft => !ft.Redundant).Select(ft => ft.Destination), StringComparer.OrdinalIgnoreCase);

                foreach (var outputPath in uniqueOutputPaths)
                {
                    outputs.WriteLine(outputPath);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the built output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        private void CreateBuiltOutputsFile(string path, IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueBuiltPaths = new SortedSet<string>(trackedFiles.Where(t => t.Type == TrackedFileType.Final).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            if (!uniqueBuiltPaths.Any())
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var outputs = new StreamWriter(path, false))
            {
                foreach (var builtPath in uniqueBuiltPaths)
                {
                    outputs.WriteLine(builtPath);
                }
            }
        }

        private void CleanTempFiles(string intermediateFolder, IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueTempPaths = new SortedSet<string>(trackedFiles.Where(t => t.Type == TrackedFileType.Temporary).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            if (!uniqueTempPaths.Any())
            {
                return;
            }

            var uniqueFolders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                intermediateFolder
            };

            // Clean up temp files.
            foreach (var tempPath in uniqueTempPaths)
            {
                try
                {
                    this.SplitUniqueFolders(intermediateFolder, tempPath, uniqueFolders);

                    File.Delete(tempPath);
                }
                catch // delete is best effort.
                {
                }
            }

            // Clean up empty temp folders.
            foreach (var folder in uniqueFolders.Reverse())
            {
                try
                {
                    Directory.Delete(folder);
                }
                catch // delete is best effort.
                {
                }
            }
        }

        private void SplitUniqueFolders(string intermediateFolder, string tempPath, SortedSet<string> uniqueFolders)
        {
            if (tempPath.StartsWith(intermediateFolder, StringComparison.OrdinalIgnoreCase))
            {
                var folder = Path.GetDirectoryName(tempPath).Substring(intermediateFolder.Length);

                var parts = folder.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                folder = intermediateFolder;

                foreach (var part in parts)
                {
                    folder = Path.Combine(folder, part);

                    uniqueFolders.Add(folder);
                }
            }
        }
    }
}
