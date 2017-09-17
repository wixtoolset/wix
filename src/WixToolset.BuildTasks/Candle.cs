// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class CandleOld : WixToolTask
    {
        private const string CandleToolName = "candle.exe";

        private string[] defineConstants;
        private ITaskItem[] extensions;
        private string[] includeSearchPaths;
        private ITaskItem outputFile;
        private bool pedantic;
        private string installerPlatform;
        private string preprocessToFile;
        private bool preprocessToStdOut;
        private ITaskItem[] sourceFiles;
        private string extensionDirectory;
        private string[] referencePaths;

        public string[] DefineConstants
        {
            get { return this.defineConstants; }
            set { this.defineConstants = value; }
        }

        public ITaskItem[] Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        public string[] IncludeSearchPaths
        {
            get { return this.includeSearchPaths; }
            set { this.includeSearchPaths = value; }
        }

        public string InstallerPlatform
        {
            get { return this.installerPlatform; }
            set { this.installerPlatform = value; }
        }

        [Output]
        [Required]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        public bool Pedantic
        {
            get { return this.pedantic; }
            set { this.pedantic = value; }
        }

        public string PreprocessToFile
        {
            get { return this.preprocessToFile; }
            set { this.preprocessToFile = value; }
        }

        public bool PreprocessToStdOut
        {
            get { return this.preprocessToStdOut; }
            set { this.preprocessToStdOut = value; }
        }

        [Required]
        public ITaskItem[] SourceFiles
        {
            get { return this.sourceFiles; }
            set { this.sourceFiles = value; }
        }

        public string ExtensionDirectory
        {
            get { return this.extensionDirectory; }
            set { this.extensionDirectory = value; }
        }

        public string[] ReferencePaths
        {
            get { return this.referencePaths; }
            set { this.referencePaths = value; }
        }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of candle.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return CandleToolName; }
        }

        /// <summary>
        /// Get the path to the executable.
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply candle.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return CandleToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), CandleToolName);
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendIfTrue("-p", this.PreprocessToStdOut);
            commandLineBuilder.AppendSwitchIfNotNull("-p", this.PreprocessToFile);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendArrayIfNotNull("-d", this.DefineConstants);
            commandLineBuilder.AppendArrayIfNotNull("-I", this.IncludeSearchPaths);
            commandLineBuilder.AppendIfTrue("-pedantic", this.Pedantic);
            commandLineBuilder.AppendSwitchIfNotNull("-arch ", this.InstallerPlatform);
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.referencePaths);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);

            // Support per-source-file output by looking at the SourceFiles items to
            // see if there is any "CandleOutput" metadata.  If there is, we do our own
            // appending, otherwise we fall back to the built-in "append file names" code.
            // Note also that the wix.targets "Compile" target does *not* automagically
            // fix the "@(CompileObjOutput)" list to include these new output names.
            // If you really want to use this, you're going to have to clone the target
            // in your own .targets file and create the output list yourself.
            bool usePerSourceOutput = false;
            if (this.SourceFiles != null)
            {
                foreach (ITaskItem item in this.SourceFiles)
                {
                    if (!String.IsNullOrEmpty(item.GetMetadata("CandleOutput")))
                    {
                        usePerSourceOutput = true;
                        break;
                    }
                }
            }

            if (usePerSourceOutput)
            {
                string[] newSourceNames = new string[this.SourceFiles.Length];
                for (int iSource = 0; iSource < this.SourceFiles.Length; ++iSource)
                {
                    ITaskItem item = this.SourceFiles[iSource];
                    if (null == item)
                    {
                        newSourceNames[iSource] = null;
                    }
                    else
                    {
                        string output = item.GetMetadata("CandleOutput");

                        if (!String.IsNullOrEmpty(output))
                        {
                            newSourceNames[iSource] = String.Concat(item.ItemSpec, ";", output);
                        }
                        else
                        {
                            newSourceNames[iSource] = item.ItemSpec;
                        }
                    }
                }

                commandLineBuilder.AppendFileNamesIfNotNull(newSourceNames, " ");
            }
            else
            {
                commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");
            }
        }
    }
}
