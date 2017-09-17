// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if false
namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Framework;
    using WixToolset.Data;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class Candle : TaskBase
    {
        public string[] DefineConstants { get; set; }

        public ITaskItem[] Extensions { get; set; }

        public string[] IncludeSearchPaths { get; set; }

        public string InstallerPlatform { get; set; }

        [Output]
        [Required]
        public ITaskItem OutputFile { get; set; }

        public bool Pedantic { get; set; }

        public string PreprocessToFile { get; set; }

        public bool PreprocessToStdOut { get; set; }

        [Required]
        public ITaskItem IntermediateDirectory { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public string ExtensionDirectory { get; set; }

        public string[] ReferencePaths { get; set; }

        protected override void ExecuteCore()
        {
            Messaging.Instance.InitializeAppName("WIX", "wix.exe");

            Messaging.Instance.Display += this.DisplayMessage;

            var preprocessor = new Preprocessor();

            var compiler = new Compiler();

            var sourceFiles = this.GatherSourceFiles();

            var preprocessorVariables = this.GatherPreprocessorVariables();

            foreach (var sourceFile in sourceFiles)
            {
                var document = preprocessor.Process(sourceFile.SourcePath, preprocessorVariables);

                var intermediate = compiler.Compile(document);

                intermediate.Save(sourceFile.OutputPath);
            }
        }

        private void DisplayMessage(object sender, DisplayEventArgs e)
        {
            this.Log.LogMessageFromText(e.Message, MessageImportance.Normal);
        }

        private IEnumerable<SourceFile> GatherSourceFiles()
        {
            var files = new List<SourceFile>();

            foreach (var item in this.SourceFiles)
            {
                var sourcePath = item.ItemSpec;
                var outputPath = item.GetMetadata("CandleOutput") ?? this.OutputFile?.ItemSpec;

                if (String.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(this.IntermediateDirectory.ItemSpec, Path.GetFileNameWithoutExtension(sourcePath) + ".wir");
                }

                files.Add(new SourceFile(sourcePath, outputPath));
            }

            return files;
        }

        private IDictionary<string, string> GatherPreprocessorVariables()
        {
            var variables = new Dictionary<string, string>();

            foreach (var pair in this.DefineConstants)
            {
                string[] value = pair.Split(new[] { '=' }, 2);

                if (variables.ContainsKey(value[0]))
                {
                    //Messaging.Instance.OnMessage(WixErrors.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], this.PreprocessorVariables[value[0]]));
                    break;
                }

                if (1 == value.Length)
                {
                    variables.Add(value[0], String.Empty);
                }
                else
                {
                    variables.Add(value[0], value[1]);
                }
            }

            return variables;
        }

        ///// <summary>
        ///// Builds a command line from options in this task.
        ///// </summary>
        //protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        //{
        //    base.BuildCommandLine(commandLineBuilder);

        //    commandLineBuilder.AppendIfTrue("-p", this.PreprocessToStdOut);
        //    commandLineBuilder.AppendSwitchIfNotNull("-p", this.PreprocessToFile);
        //    commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
        //    commandLineBuilder.AppendArrayIfNotNull("-d", this.DefineConstants);
        //    commandLineBuilder.AppendArrayIfNotNull("-I", this.IncludeSearchPaths);
        //    commandLineBuilder.AppendIfTrue("-pedantic", this.Pedantic);
        //    commandLineBuilder.AppendSwitchIfNotNull("-arch ", this.InstallerPlatform);
        //    commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.referencePaths);
        //    commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);

        //    // Support per-source-file output by looking at the SourceFiles items to
        //    // see if there is any "CandleOutput" metadata.  If there is, we do our own
        //    // appending, otherwise we fall back to the built-in "append file names" code.
        //    // Note also that the wix.targets "Compile" target does *not* automagically
        //    // fix the "@(CompileObjOutput)" list to include these new output names.
        //    // If you really want to use this, you're going to have to clone the target
        //    // in your own .targets file and create the output list yourself.
        //    bool usePerSourceOutput = false;
        //    if (this.SourceFiles != null)
        //    {
        //        foreach (ITaskItem item in this.SourceFiles)
        //        {
        //            if (!String.IsNullOrEmpty(item.GetMetadata("CandleOutput")))
        //            {
        //                usePerSourceOutput = true;
        //                break;
        //            }
        //        }
        //    }

        //    if (usePerSourceOutput)
        //    {
        //        string[] newSourceNames = new string[this.SourceFiles.Length];
        //        for (int iSource = 0; iSource < this.SourceFiles.Length; ++iSource)
        //        {
        //            ITaskItem item = this.SourceFiles[iSource];
        //            if (null == item)
        //            {
        //                newSourceNames[iSource] = null;
        //            }
        //            else
        //            {
        //                string output = item.GetMetadata("CandleOutput");

        //                if (!String.IsNullOrEmpty(output))
        //                {
        //                    newSourceNames[iSource] = String.Concat(item.ItemSpec, ";", output);
        //                }
        //                else
        //                {
        //                    newSourceNames[iSource] = item.ItemSpec;
        //                }
        //            }
        //        }

        //        commandLineBuilder.AppendFileNamesIfNotNull(newSourceNames, " ");
        //    }
        //    else
        //    {
        //        commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");
        //    }
        //}
    }
}
#endif
