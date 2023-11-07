// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to run WiX to detach bundle engine to be signed.
    /// </summary>
    public sealed class WindowsInstallerValidation : WixExeBaseTask
    {
        /// <summary>
        /// Gets or sets the path to the database to validate.
        /// </summary>
        [Required]
        public ITaskItem DatabaseFile { get; set; }

        /// <summary>
        /// Gets or sets the intermedidate folder to use.
        /// </summary>
        public ITaskItem IntermediateDirectory { get; set; }

        /// <summary>
        /// Gets or sets the paths to ICE CUBes to execute.
        /// </summary>
        public string[] CubeFiles { get; set; }

        /// <summary>
        /// Gets or sets the ICEs to execute.
        /// </summary>
        public string[] Ices { get; set; }

        /// <summary>
        /// Gets or sets the ICEs to suppress.
        /// </summary>
        public string[] SuppressIces { get; set; }

        /// <summary>
        /// Gets or sets the .wixpdb for the database.
        /// </summary>
        public ITaskItem WixpdbFile { get; set; }

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendTextUnquoted("msi validate");

            commandLineBuilder.AppendFileNameIfNotNull(this.DatabaseFile);
            commandLineBuilder.AppendSwitchIfNotNull("-pdb ", this.WixpdbFile);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);
            commandLineBuilder.AppendArrayIfNotNull("-cub ", this.CubeFiles);
            commandLineBuilder.AppendArrayIfNotNull("-ice ", this.Ices);
            commandLineBuilder.AppendArrayIfNotNull("-sice ", this.SuppressIces);

            base.BuildCommandLine(commandLineBuilder);
        }
    }
}
