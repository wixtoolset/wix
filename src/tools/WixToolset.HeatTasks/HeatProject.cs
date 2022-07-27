// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using Microsoft.Build.Framework;

    public sealed class HeatProject : HeatTask
    {
        private string[] projectOutputGroups;

        public string Configuration { get; set; }

        public string DirectoryIds { get; set; }

        public bool GenerateWixVariables { get; set; }

        public string GenerateType { get; set; }

        public string MsbuildBinPath { get; set; }

        public string Platform { get; set; }

        [Required]
        public string Project { get; set; }

        public string ProjectName { get; set; }

        public string[] ProjectOutputGroups
        {
            get
            {
                return this.projectOutputGroups;
            }
            set
            {
                this.projectOutputGroups = value;

                // If it's just one string and it contains semicolons, let's
                // split it into separate items.
                if (this.projectOutputGroups.Length == 1)
                {
                    this.projectOutputGroups = this.projectOutputGroups[0].Split(new char[] { ';' });
                }
            }
        }

        public bool UseToolsVersion { get; set; }

        protected override string OperationName => "project";

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch(this.OperationName);
            commandLineBuilder.AppendFileNameIfNotNull(this.Project);

            commandLineBuilder.AppendSwitchIfNotNull("-configuration ", this.Configuration);
            commandLineBuilder.AppendSwitchIfNotNull("-directoryid ", this.DirectoryIds);
            commandLineBuilder.AppendSwitchIfNotNull("-generate ", this.GenerateType);
            commandLineBuilder.AppendSwitchIfNotNull("-msbuildbinpath ", this.MsbuildBinPath);
            commandLineBuilder.AppendSwitchIfNotNull("-platform ", this.Platform);
            commandLineBuilder.AppendArrayIfNotNull("-pog ", this.ProjectOutputGroups);
            commandLineBuilder.AppendSwitchIfNotNull("-projectname ", this.ProjectName);
            commandLineBuilder.AppendIfTrue("-wixvar", this.GenerateWixVariables);

#if !NETCOREAPP
            commandLineBuilder.AppendIfTrue("-usetoolsversion", this.UseToolsVersion);
#endif

            base.BuildCommandLine(commandLineBuilder);
        }
    }
}
