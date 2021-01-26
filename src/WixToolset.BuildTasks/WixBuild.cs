// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed partial class WixBuild : ToolsetTask
    {
        public string[] Cultures { get; set; }

        public string[] DefineConstants { get; set; }

        public ITaskItem[] Extensions { get; set; }

        public string ExtensionDirectory { get; set; }

        public string[] IncludeSearchPaths { get; set; }

        public string InstallerPlatform { get; set; }

        [Required]
        public ITaskItem IntermediateDirectory { get; set; }

        public ITaskItem[] LocalizationFiles { get; set; }

        public ITaskItem[] LibraryFiles { get; set; }

        [Output]
        [Required]
        public ITaskItem OutputFile { get; set; }

        public string OutputType { get; set; }

        public ITaskItem PdbFile { get; set; }

        public string PdbType { get; set; }

        public bool Pedantic { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public string[] ReferencePaths { get; set; }


        public ITaskItem[] BindInputPaths { get; set; }

        public bool BindFiles { get; set; }

        public ITaskItem BindContentsFile { get; set; }

        public ITaskItem BindOutputsFile { get; set; }

        public ITaskItem BindBuiltOutputsFile { get; set; }

        public string CabinetCachePath { get; set; }

        public int CabinetCreationThreadCount { get; set; }

        public string DefaultCompressionLevel { get; set; }

        [Output]
        public ITaskItem UnreferencedSymbolsFile { get; set; }

        public ITaskItem WixProjectFile { get; set; }
        public string[] WixVariables { get; set; }

        public bool SuppressValidation { get; set; }

        public string[] SuppressIces { get; set; }

        public string AdditionalCub { get; set; }

        protected override string ToolName => "wix.exe";

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
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.ReferencePaths);
            commandLineBuilder.AppendIfTrue("-sval", this.SuppressValidation);
            commandLineBuilder.AppendArrayIfNotNull("-sice ", this.SuppressIces);
            commandLineBuilder.AppendSwitchIfNotNull("-usf ", this.UnreferencedSymbolsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-cc ", this.CabinetCachePath);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);
            commandLineBuilder.AppendSwitchIfNotNull("-contentsfile ", this.BindContentsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputsfile ", this.BindOutputsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-builtoutputsfile ", this.BindBuiltOutputsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-defaultcompressionlevel ", this.DefaultCompressionLevel);

            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendIfTrue("-bindFiles", this.BindFiles);
            commandLineBuilder.AppendArrayIfNotNull("-bindPath ", this.CalculateBindPathStrings());
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendArrayIfNotNull("-lib ", this.LibraryFiles);
            commandLineBuilder.AppendTextIfNotWhitespace(this.AdditionalOptions);
            commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");
        }

        private IEnumerable<string> CalculateBindPathStrings()
        {
            if (null != this.BindInputPaths)
            {
                foreach (var item in this.BindInputPaths)
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
    }
}
