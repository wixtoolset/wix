// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using Microsoft.Build.Framework;

    public sealed class HeatFile : HeatTask
    {
        public string ComponentGroupName { get; set; }

        public string DirectoryRefId { get; set; }

        [Required]
        public string File { get; set; }

        public string PreprocessorVariable { get; set; }

        public bool SuppressCom { get; set; }

        public bool SuppressRegistry { get; set; }

        public bool SuppressRootDirectory { get; set; }

        public string Template { get; set; }

        protected override string OperationName => "file";

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch(this.OperationName);
            commandLineBuilder.AppendFileNameIfNotNull(this.File);

            commandLineBuilder.AppendSwitchIfNotNull("-cg ", this.ComponentGroupName);
            commandLineBuilder.AppendSwitchIfNotNull("-dr ", this.DirectoryRefId);
            commandLineBuilder.AppendIfTrue("-scom", this.SuppressCom);
            commandLineBuilder.AppendIfTrue("-srd", this.SuppressRootDirectory);
            commandLineBuilder.AppendIfTrue("-sreg", this.SuppressRegistry);
            commandLineBuilder.AppendSwitchIfNotNull("-template ", this.Template);
            commandLineBuilder.AppendSwitchIfNotNull("-var ", this.PreprocessorVariable);

            base.BuildCommandLine(commandLineBuilder);
        }
    }
}
