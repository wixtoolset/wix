// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Layout for the WiX toolset.
    /// </summary>
    internal class Layout
    {
        internal Layout(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public IEnumerable<ITrackedFile> TrackedFiles { get; set; }

        public IEnumerable<IFileTransfer> FileTransfers { get; set; }

        public string IntermediateFolder { get; set; }

        public string ContentsFile { get; set; }

        public string OutputsFile { get; set; }

        public string BuiltOutputsFile { get; set; }

        public bool SuppressAclReset { get; set; }

        public void Execute()
        {
            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

            var context = this.ServiceProvider.GetService<ILayoutContext>();
            context.Extensions = extensionManager.Create<ILayoutExtension>();
            context.TrackedFiles = this.TrackedFiles;
            context.FileTransfers = this.FileTransfers;
            context.ContentsFile = this.ContentsFile;
            context.OutputsFile = this.OutputsFile;
            context.BuiltOutputsFile = this.BuiltOutputsFile;
            context.SuppressAclReset = this.SuppressAclReset;

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
                    this.CleanTempFiles(context.TrackedFiles);
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
                foreach (string inputPath in uniqueInputFilePaths)
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

        private void CleanTempFiles(IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueTempPaths = new SortedSet<string>(trackedFiles.Where(t => t.Type == TrackedFileType.Temporary).Select(t => t.Path), StringComparer.OrdinalIgnoreCase);

            if (!uniqueTempPaths.Any())
            {
                return;
            }

            var uniqueFolders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                this.IntermediateFolder
            };

            // Clean up temp files.
            foreach (var tempPath in uniqueTempPaths)
            {
                try
                {
                    this.SplitUniqueFolders(tempPath, uniqueFolders);

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

        private void SplitUniqueFolders(string tempPath, SortedSet<string> uniqueFolders)
        {
            if (tempPath.StartsWith(this.IntermediateFolder, StringComparison.OrdinalIgnoreCase))
            {
                var folder = Path.GetDirectoryName(tempPath).Substring(this.IntermediateFolder.Length);

                var parts = folder.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                folder = this.IntermediateFolder;

                foreach (var part in parts)
                {
                    folder = Path.Combine(folder, part);

                    uniqueFolders.Add(folder);
                }
            }
        }
    }
}
