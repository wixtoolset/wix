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
        private const string TrackedLineTypePathSeparator = "\t";

        internal LayoutCreator(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.FileSystem = serviceProvider.GetService<IFileSystem>();
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

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

                    var command = new TransferFilesCommand(this.Messaging, this.FileSystem, context.Extensions, context.FileTransfers, context.ResetAcls);
                    command.Execute();
                }

                if (context.TrackedFiles != null)
                {
                    this.CleanTempFiles(context.IntermediateFolder, context.TrackedFiles);
                }
            }
            finally
            {
                if (context.TrackedFiles != null && !String.IsNullOrEmpty(context.TrackingFile))
                {
                    this.CreateTrackingFile(context.TrackingFile, context.TrackedFiles);
                }
            }

            // Post-layout.
            foreach (var extension in context.Extensions)
            {
                extension.PostLayout();
            }
        }

        /// <summary>
        /// Writes the paths of the track files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="trackedFiles">Collection of files that were tracked.</param>
        private void CreateTrackingFile(string path, IEnumerable<ITrackedFile> trackedFiles)
        {
            var uniqueTrackingLines = new SortedSet<string>(trackedFiles.Where(t => t.Type != TrackedFileType.Temporary).Select(TrackedFileLine), StringComparer.OrdinalIgnoreCase);

            if (!uniqueTrackingLines.Any())
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var stream = new StreamWriter(path, false))
            {
                foreach (var trackingLine in uniqueTrackingLines)
                {
                    stream.WriteLine(trackingLine);
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
                this.SplitUniqueFolders(intermediateFolder, tempPath, uniqueFolders);

                this.FileSystem.DeleteFile(tempPath);
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
                var folder = Path.GetDirectoryName(tempPath.Substring(intermediateFolder.Length));

                var parts = folder.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                folder = intermediateFolder;

                foreach (var part in parts)
                {
                    folder = Path.Combine(folder, part);

                    uniqueFolders.Add(folder);
                }
            }
        }

        private static string TrackedFileLine(ITrackedFile trackedFile)
        {
            return trackedFile.Type + TrackedLineTypePathSeparator + trackedFile.Path;
        }
    }
}
