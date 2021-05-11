// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Framework;

    public sealed class HeatProject : HeatTask
    {
        private string configuration;
        private string directoryIds;
        private string generateType;
        private bool generateWixVariables;
        private string platform;
        private string project;
        private string projectName;
        private string[] projectOutputGroups;

        public string Configuration
        {
            get { return this.configuration; }
            set { this.configuration = value; }
        }

        public string DirectoryIds
        {
            get { return this.directoryIds; }
            set { this.directoryIds = value; }
        }

        public bool GenerateWixVariables
        {
            get { return this.generateWixVariables; }
            set { this.generateWixVariables = value; }
        }

        public string GenerateType
        {
            get { return this.generateType; }
            set { this.generateType = value; }
        }

        public string MsbuildBinPath { get; set; }

        public string Platform
        {
            get { return this.platform; }
            set { this.platform = value; }
        }

        [Required]
        public string Project
        {
            get { return this.project; }
            set { this.project = value; }
        }

        public string ProjectName
        {
            get { return this.projectName; }
            set { this.projectName = value; }
        }

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

        protected override string OperationName
        {
            get { return "project"; }
        }

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
