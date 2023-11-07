// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to run WiX to detach bundle engine to be signed.
    /// </summary>
    public sealed partial class DetachBundleEngineForSigning : WixExeBaseTask
    {
        /// <summary>
        /// The bundle from which to detach the bundle engine.
        /// </summary>
        [Required]
        public ITaskItem BundleFile { get; set; }

        /// <summary>
        /// Gets or sets the intermedidate folder to use.
        /// </summary>
        public ITaskItem IntermediateDirectory { get; set; }

        /// <summary>
        /// Gets or sets the path to the output detached bundle.
        /// </summary>
        [Required]
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the output. Only set if the task does work.
        /// </summary>
        [Output]
        public ITaskItem Output { get; set; }

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendTextUnquoted("burn detach");

            commandLineBuilder.AppendFileNameIfNotNull(this.BundleFile);
            commandLineBuilder.AppendSwitchIfNotNull("-engine ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);

            base.BuildCommandLine(commandLineBuilder);
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            var exitCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

            if (exitCode == 0) // successfully did work.
            {
                this.Output = this.OutputFile;
            }
            else if (exitCode == -1000) // no work done.
            {
                exitCode = 0;
            }

            return exitCode;
        }
    }
}
