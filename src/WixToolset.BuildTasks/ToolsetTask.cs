// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Utilities;

    public abstract partial class ToolsetTask : ToolTask
    {
        private static readonly string ThisDllPath = new Uri(typeof(ToolsetTask).Assembly.CodeBase).AbsolutePath;

        /// <summary>
        /// Gets or sets additional options that are appended the the tool command-line.
        /// </summary>
        /// <remarks>
        /// This allows the task to support extended options in the tool which are not
        /// explicitly implemented as properties on the task.
        /// </remarks>
        public string AdditionalOptions { get; set; }

        /// <summary>
        /// Gets or sets whether to display the logo.
        /// </summary>
        public bool NoLogo { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the task
        /// should be run as separate process or in-proc.
        /// </summary>
        public virtual bool RunAsSeparateProcess { get; set; }

        /// <summary>
        /// Gets or sets whether all warnings should be suppressed.
        /// </summary>
        public bool SuppressAllWarnings { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to be suppressed.
        /// </summary>
        public string[] SuppressSpecificWarnings { get; set; }

        /// <summary>
        /// Gets or sets whether all warnings should be treated as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to treat as errors.
        /// </summary>
        public string[] TreatSpecificWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets whether to display verbose output.
        /// </summary>
        public bool VerboseOutput { get; set; }

        private string DefaultToolFullPath => Path.Combine(Path.GetDirectoryName(ThisDllPath), this.ToolExe);

        private string ToolFullPath
        {
            get
            {
                if (String.IsNullOrEmpty(this.ToolPath))
                {
                    return this.DefaultToolFullPath;
                }
                return Path.Combine(this.ToolPath, this.ToolExe);
            }
        }

        /// <summary>
        /// Get the path to the executable.
        /// </summary>
        /// <remarks>
        /// ToolTask only calls GenerateFullPathToTool when the ToolPath property is not set.
        /// WiX never sets the ToolPath property, but the user can through $(WixToolDir).
        /// If we return only a file name, ToolTask will search the system paths for it.
        /// </remarks>
        protected sealed override string GenerateFullPathToTool()
        {
#if !NETCOREAPP
            if (!this.RunAsSeparateProcess)
            {
                // We need to return a path that exists, so if we're not actually going to run the tool then just return this dll path.
                return ThisDllPath;
            }
            return this.DefaultToolFullPath;
#else
            if (IsSelfExecutable(this.DefaultToolFullPath, out var toolFullPath))
            {
                return toolFullPath;
            }
            return DotnetFullPath;
#endif
        }

        protected sealed override string GenerateResponseFileCommands()
        {
            var commandLineBuilder = new WixCommandLineBuilder();
            this.BuildCommandLine(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        /// <summary>
        /// Builds a command line from options in this and derivative tasks.
        /// </summary>
        /// <remarks>
        /// Derivative classes should call BuildCommandLine() on the base class to ensure that common command line options are added to the command.
        /// </remarks>
        protected virtual void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendIfTrue("-nologo", this.NoLogo);
            commandLineBuilder.AppendArrayIfNotNull("-sw", this.SuppressSpecificWarnings);
            commandLineBuilder.AppendIfTrue("-sw", this.SuppressAllWarnings);
            commandLineBuilder.AppendIfTrue("-v", this.VerboseOutput);
            commandLineBuilder.AppendArrayIfNotNull("-wx", this.TreatSpecificWarningsAsErrors);
            commandLineBuilder.AppendIfTrue("-wx", this.TreatWarningsAsErrors);
        }

#if NETCOREAPP
        private static readonly string DotnetFullPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";

        protected override string GenerateCommandLineCommands()
        {
            if (IsSelfExecutable(this.ToolFullPath, out var toolFullPath))
            {
                return null;
            }
            else
            {
                return $"exec \"{toolFullPath}\"";
            }
        }

        private static bool IsSelfExecutable(string proposedToolFullPath, out string toolFullPath)
        {
            var toolFullPathWithoutExtension = Path.Combine(Path.GetDirectoryName(proposedToolFullPath), Path.GetFileNameWithoutExtension(proposedToolFullPath));
            var exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : String.Empty;
            var exeToolFullPath = $"{toolFullPathWithoutExtension}{exeExtension}";
            if (File.Exists(exeToolFullPath))
            {
                toolFullPath = exeToolFullPath;
                return true;
            }

            toolFullPath = $"{toolFullPathWithoutExtension}.dll";
            return false;
        }
#endif
    }
}
