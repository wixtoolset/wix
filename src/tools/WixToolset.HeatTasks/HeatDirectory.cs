// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    public sealed class HeatDirectory : HeatTask
    {
        public string ComponentGroupName { get; set; }

        [Required]
        public string Directory { get; set; }

        public string DirectoryRefId { get; set; }

        public bool KeepEmptyDirectories { get; set; }

        public string PreprocessorVariable { get; set; }

        public bool SuppressCom { get; set; }

        public bool SuppressRootDirectory { get; set; }

        public bool SuppressRegistry { get; set; }

        public string Template { get; set; }

        protected override string OperationName => "dir";

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch(this.OperationName);
            commandLineBuilder.AppendFileNameIfNotNull(this.Directory);

            commandLineBuilder.AppendSwitchIfNotNull("-cg ", this.ComponentGroupName);
            commandLineBuilder.AppendSwitchIfNotNull("-dr ", this.DirectoryRefId);
            commandLineBuilder.AppendIfTrue("-ke", this.KeepEmptyDirectories);
            commandLineBuilder.AppendIfTrue("-scom", this.SuppressCom);
            commandLineBuilder.AppendIfTrue("-sreg", this.SuppressRegistry);
            commandLineBuilder.AppendIfTrue("-srd", this.SuppressRootDirectory);
            commandLineBuilder.AppendSwitchIfNotNull("-template ", this.Template);
            commandLineBuilder.AppendSwitchIfNotNull("-var ", this.PreprocessorVariable);

            base.BuildCommandLine(commandLineBuilder);
        }
    }
}
