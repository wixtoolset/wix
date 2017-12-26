// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Layout for the WiX toolset.
    /// </summary>
    public sealed class Layout
    {
        public Layout(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        public IEnumerable<FileTransfer> FileTransfers { get; set; }

        public IEnumerable<string> ContentFilePaths { get; set; }

        public string ContentsFile { get; set; }

        public string OutputsFile { get; set; }

        public string BuiltOutputsFile { get; set; }

        public bool SuppressAclReset { get; set; }

        private IMessaging Messaging { get; set; }

        public void Execute()
        {
            this.Messaging = this.ServiceProvider.GetService<IMessaging>();

            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

            var context = this.ServiceProvider.GetService<ILayoutContext>();
            context.Messaging = this.Messaging;
            context.Extensions = extensionManager.Create<ILayoutExtension>();
            context.FileTransfers = this.FileTransfers;
            context.ContentFilePaths = this.ContentFilePaths;
            context.ContentsFile = this.ContentsFile;
            context.OutputPdbPath = this.OutputsFile;
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

                    var command = new TransferFilesCommand(context.Messaging, context.Extensions, context.FileTransfers, context.SuppressAclReset);
                    command.Execute();
                }
            }
            finally
            {
                if (!String.IsNullOrEmpty(context.ContentsFile) && context.ContentFilePaths != null)
                {
                    this.CreateContentsFile(context.ContentsFile, context.ContentFilePaths);
                }

                if (!String.IsNullOrEmpty(context.OutputsFile) && context.FileTransfers != null)
                {
                    this.CreateOutputsFile(context.OutputsFile, context.FileTransfers, context.OutputPdbPath);
                }

                if (!String.IsNullOrEmpty(context.BuiltOutputsFile) && context.FileTransfers != null)
                {
                    this.CreateBuiltOutputsFile(context.BuiltOutputsFile, context.FileTransfers, context.OutputPdbPath);
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
        private void CreateContentsFile(string path, IEnumerable<string> contentFilePaths)
        {
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var contents = new StreamWriter(path, false))
            {
                foreach (string contentPath in contentFilePaths)
                {
                    contents.WriteLine(contentPath);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateOutputsFile(string path, IEnumerable<FileTransfer> fileTransfers, string pdbPath)
        {
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Don't list files where the source is the same as the destination since
                    // that might be the only place the file exists. The outputs file is often
                    // used to delete stuff and losing the original source would be bad.
                    if (!fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Writes the paths to the built output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateBuiltOutputsFile(string path, IEnumerable<FileTransfer> fileTransfers, string pdbPath)
        {
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (var outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Only write the built file transfers. Also, skip redundant
                    // files for the same reason spelled out in this.CreateOutputsFile().
                    if (fileTransfer.Built && !fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }
    }
}
