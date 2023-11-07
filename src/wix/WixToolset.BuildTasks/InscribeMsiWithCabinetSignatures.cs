// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to run WiX to update cabinet signatures in a MSI.
    /// </summary>
    public sealed partial class InscribeMsiWithCabinetSignatures : WixExeBaseTask
    {
        [Required]
        public ITaskItem DatabaseFile { get; set; }

        [Required]
        public ITaskItem IntermediateDirectory { get; set; }

        public ITaskItem OutputFile { get; set; }

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendTextUnquoted("msi inscribe");

            commandLineBuilder.AppendFileNameIfNotNull(this.DatabaseFile);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);

            base.BuildCommandLine(commandLineBuilder);
        }
    }
}
