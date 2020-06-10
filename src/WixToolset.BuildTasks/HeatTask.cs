// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Framework;

    /// <summary>
    /// A base MSBuild task to run the WiX harvester.
    /// Specific harvester tasks should extend this class.
    /// </summary>
    public abstract partial class HeatTask : ToolsetTask
    {
        private bool autogenerageGuids;
        private bool generateGuidsNow;
        private ITaskItem outputFile;
        private bool suppressFragments;
        private bool suppressUniqueIds;
        private string[] transforms;

        public HeatTask()
        {
            this.RunAsSeparateProcess = true;
        }

        public bool AutogenerateGuids
        {
            get { return this.autogenerageGuids; }
            set { this.autogenerageGuids = value; }
        }

        public bool GenerateGuidsNow
        {
            get { return this.generateGuidsNow; }
            set { this.generateGuidsNow = value; }
        }

        [Required]
        [Output]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        public bool SuppressFragments
        {
            get { return this.suppressFragments; }
            set { this.suppressFragments = value; }
        }

        public bool SuppressUniqueIds
        {
            get { return this.suppressUniqueIds; }
            set { this.suppressUniqueIds = value; }
        }

        public string[] Transforms
        {
            get { return this.transforms; }
            set { this.transforms = value; }
        }

        protected sealed override string ToolName => "heat.exe";

        /// <summary>
        /// Gets the name of the heat operation performed by the task.
        /// </summary>
        /// <remarks>This is the first parameter passed on the heat.exe command-line.</remarks>
        /// <value>The name of the heat operation performed by the task.</value>
        protected abstract string OperationName
        {
            get;
        }

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
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
        }
    }
}
