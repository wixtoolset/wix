// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// A base MSBuild task to run the WiX harvester.
    /// Specific harvester tasks should extend this class.
    /// </summary>
    public abstract partial class HeatTask : BaseToolsetTask
    {
        public bool AutogenerateGuids { get; set; }

        public bool GenerateGuidsNow { get; set; }

        [Required]
        [Output]
        public ITaskItem OutputFile { get; set; }

        public bool SuppressFragments { get; set; }

        public bool SuppressUniqueIds { get; set; }

        public string[] Transforms { get; set; }

        /// <summary>
        /// Gets the name of the heat operation performed by the task.
        /// </summary>
        /// <remarks>This is the first parameter passed on the heat.exe command-line.</remarks>
        /// <value>The name of the heat operation performed by the task.</value>
        protected abstract string OperationName { get; }

        protected sealed override string ToolName => "heat.exe";

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendIfTrue("-ag", this.AutogenerateGuids);
            commandLineBuilder.AppendIfTrue("-gg", this.GenerateGuidsNow);
            commandLineBuilder.AppendIfTrue("-sfrag", this.SuppressFragments);
            commandLineBuilder.AppendIfTrue("-suid", this.SuppressUniqueIds);
            commandLineBuilder.AppendArrayIfNotNull("-t ", this.Transforms);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
        }
    }
}
