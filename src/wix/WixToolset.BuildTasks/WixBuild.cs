// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class WixBuild : WixExeBaseTask
    {
        public string[] Cultures { get; set; }

        public string[] DefineConstants { get; set; }

        public ITaskItem[] Extensions { get; set; }

        public string[] IncludeSearchPaths { get; set; }

        public string InstallerPlatform { get; set; }

        [Required]
        public ITaskItem IntermediateDirectory { get; set; }

        public ITaskItem[] LocalizationFiles { get; set; }

        public ITaskItem[] LibraryFiles { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        public string OutputType { get; set; }

        public ITaskItem PdbFile { get; set; }

        public string PdbType { get; set; }

        public bool Pedantic { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public ITaskItem[] BindPaths { get; set; }

        public ITaskItem[] BindVariables { get; set; }

        public bool BindFiles { get; set; }

        public ITaskItem BindTrackingFile { get; set; }

        public string CabinetCachePath { get; set; }

        public int CabinetCreationThreadCount { get; set; }

        public string DefaultCompressionLevel { get; set; }

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendTextUnquoted("build");

            commandLineBuilder.AppendSwitchIfNotNull("-platform ", this.InstallerPlatform);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputType ", this.OutputType);
            commandLineBuilder.AppendSwitchIfNotNull("-pdb ", this.PdbFile);
            commandLineBuilder.AppendSwitchIfNotNull("-pdbType ", this.PdbType);
            commandLineBuilder.AppendArrayIfNotNull("-culture ", this.Cultures);
            commandLineBuilder.AppendArrayIfNotNull("-d ", this.DefineConstants);
            commandLineBuilder.AppendArrayIfNotNull("-I ", this.IncludeSearchPaths);
            commandLineBuilder.AppendArrayIfNotNull("-ext ", this.Extensions);
            commandLineBuilder.AppendSwitchIfNotNull("-cc ", this.CabinetCachePath);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);
            commandLineBuilder.AppendSwitchIfNotNull("-trackingfile ", this.BindTrackingFile);
            commandLineBuilder.AppendSwitchIfNotNull("-defaultcompressionlevel ", this.DefaultCompressionLevel);

            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendIfTrue("-bindFiles", this.BindFiles);
            commandLineBuilder.AppendArrayIfNotNull("-bindPath ", this.CalculateBindPathStrings());
            commandLineBuilder.AppendArrayIfNotNull("-bindVariable ", this.CalculateBindVariableStrings());
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendArrayIfNotNull("-lib ", this.LibraryFiles);
            commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");
        }

        private IEnumerable<string> CalculateBindPathStrings()
        {
            if (null != this.BindPaths)
            {
                foreach (var item in this.BindPaths)
                {
                    var path = item.GetMetadata("FullPath");

                    var bindName = item.GetMetadata("BindName");
                    if (!String.IsNullOrEmpty(bindName))
                    {
                        yield return String.Concat(bindName, "=", path);
                    }
                    else
                    {
                        yield return path;
                    }
                }
            }
        }

        private IEnumerable<string> CalculateBindVariableStrings()
        {
            if (null != this.BindVariables)
            {
                foreach (var item in this.BindVariables)
                {
                    var value = item.ItemSpec;

                    var variableName = item.GetMetadata("Name");
                    if (!String.IsNullOrEmpty(variableName))
                    {
                        value = String.Concat(variableName, "=", value);
                    }

                    yield return value;
                }
            }
        }
    }
}
