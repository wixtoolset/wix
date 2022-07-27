// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// A base MSBuild task to run the WiX harvester.
    /// Specific harvester tasks should extend this class.
    /// </summary>
    public abstract partial class HeatTask : ToolTask
    {
        private static readonly string ThisDllPath = new Uri(typeof(HeatTask).Assembly.CodeBase).AbsolutePath;

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

        public bool AutogenerateGuids { get; set; }

        public bool GenerateGuidsNow { get; set; }

        [Required]
        [Output]
        public ITaskItem OutputFile { get; set; }

        public bool SuppressFragments { get; set; }

        public bool SuppressUniqueIds { get; set; }

        public string[] Transforms { get; set; }

        protected sealed override string ToolName => "heat.exe";

        /// <summary>
        /// Gets the name of the heat operation performed by the task.
        /// </summary>
        /// <remarks>This is the first parameter passed on the heat.exe command-line.</remarks>
        /// <value>The name of the heat operation performed by the task.</value>
        protected abstract string OperationName { get; }

        private string ToolFullPath
        {
            get
            {
                if (String.IsNullOrEmpty(this.ToolPath))
                {
                    return Path.Combine(Path.GetDirectoryName(ThisDllPath), this.ToolExe);
                }

                return Path.Combine(this.ToolPath, this.ToolExe);
            }
        }

        /// <summary>
        /// Get the path to the executable.
        /// </summary>
        /// <remarks>
        /// ToolTask only calls GenerateFullPathToTool when the ToolPath property is not set.
        /// WiX never sets the ToolPath property, but the user can through $(HeatToolDir).
        /// If we return only a file name, ToolTask will search the system paths for it.
        /// </remarks>
        protected sealed override string GenerateFullPathToTool()
        {
#if NETCOREAPP
            // If we're not using heat.exe, use dotnet.exe to exec heat.dll.
            // See this.GenerateCommandLine() where "exec heat.dll" is added.
            if (!IsSelfExecutable(this.ToolFullPath, out var toolFullPath))
            {
                return DotnetFullPath;
            }

            return toolFullPath;
#else
            return this.ToolFullPath;
#endif
        }

        protected sealed override string GenerateCommandLineCommands()
        {
            var commandLineBuilder = new WixCommandLineBuilder();

#if NETCOREAPP
            // If we're using dotnet.exe as the target executable, see this.GenerateFullPathToTool(),
            // then add "exec heat.dll" to the beginning of the command-line.
            if (!IsSelfExecutable(this.ToolFullPath, out var toolFullPath))
            {
                //commandLineBuilder.AppendSwitchIfNotNull("exec ", toolFullPath);
                commandLineBuilder.AppendSwitch($"exec \"{toolFullPath}\"");
            }
#endif

            this.BuildCommandLine(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected virtual void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendIfTrue("-nologo", this.NoLogo);
            commandLineBuilder.AppendArrayIfNotNull("-sw", this.SuppressSpecificWarnings);
            commandLineBuilder.AppendIfTrue("-sw", this.SuppressAllWarnings);
            commandLineBuilder.AppendIfTrue("-v", this.VerboseOutput);
            commandLineBuilder.AppendArrayIfNotNull("-wx", this.TreatSpecificWarningsAsErrors);
            commandLineBuilder.AppendIfTrue("-wx", this.TreatWarningsAsErrors);

            commandLineBuilder.AppendIfTrue("-ag", this.AutogenerateGuids);
            commandLineBuilder.AppendIfTrue("-gg", this.GenerateGuidsNow);
            commandLineBuilder.AppendIfTrue("-sfrag", this.SuppressFragments);
            commandLineBuilder.AppendIfTrue("-suid", this.SuppressUniqueIds);
            commandLineBuilder.AppendArrayIfNotNull("-t ", this.Transforms);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
        }

#if NETCOREAPP
        private static readonly string DotnetFullPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";

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
