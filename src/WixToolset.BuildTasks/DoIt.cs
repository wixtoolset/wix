// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class DoIt : Task
    {
        public DoIt()
        {
            Messaging.Instance.InitializeAppName("WIX", "wix.exe");

            Messaging.Instance.Display += this.DisplayMessage;
        }

        public string AdditionalOptions { get; set; }

        public string Cultures { get; set; }

        public string[] DefineConstants { get; set; }

        public ITaskItem[] Extensions { get; set; }

        public string ExtensionDirectory { get; set; }

        public string[] IncludeSearchPaths { get; set; }

        public string InstallerPlatform { get; set; }

        [Required]
        public ITaskItem IntermediateDirectory { get; set; }

        public ITaskItem[] LocalizationFiles { get; set; }

        public bool NoLogo { get; set; }

        public ITaskItem[] LibraryFiles { get; set; }

        [Output]
        [Required]
        public ITaskItem OutputFile { get; set; }

        public string OutputType { get; set; }

        public string PdbOutputFile { get; set; }

        public bool Pedantic { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public string[] ReferencePaths { get; set; }


        /// <summary>
        /// Gets or sets whether all warnings should be suppressed.
        /// </summary>
        public bool SuppressAllWarnings { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to be suppressed.
        /// </summary>
        public string[] SuppressSpecificWarnings { get; set; }

        /// <summary>
        /// Gets or sets whether all warnings should be treated as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets a list of specific warnings to treat as errors.
        /// </summary>
        public string[] TreatSpecificWarningsAsErrors { get; set; }

        /// <summary>
        /// Gets or sets whether to display verbose output.
        /// </summary>
        public bool VerboseOutput { get; set; }


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

        public override bool Execute()
        {
            try
            {
                this.ExecuteCore();
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);

                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return !this.Log.HasLoggedErrors;
        }

        private void ExecuteCore()
        {
            var commandLineBuilder = new WixCommandLineBuilder();

            commandLineBuilder.AppendTextUnquoted("build");

            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputType ", this.OutputType);
            commandLineBuilder.AppendIfTrue("-nologo", this.NoLogo);
            commandLineBuilder.AppendSwitchIfNotNull("-cultures ", this.Cultures);
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
            commandLineBuilder.AppendSwitchIfNotNull("-wixprojectfile ", this.WixProjectFile);

            commandLineBuilder.AppendIfTrue("-bindFiles", this.BindFiles);
            commandLineBuilder.AppendArrayIfNotNull("-bindPath ", this.CalculateBindPathStrings());
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendArrayIfNotNull("-lib ", this.LibraryFiles);
            commandLineBuilder.AppendTextIfNotWhitespace(this.AdditionalOptions);
            commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");

            var commandLineString = commandLineBuilder.ToString();

            this.Log.LogMessage(MessageImportance.Normal, "wix.exe " + commandLineString);

            var serviceProvider = new WixToolsetServiceProvider();

            var context = serviceProvider.GetService<ICommandLineContext>();
            context.Messaging = Messaging.Instance;
            context.ExtensionManager = this.CreateExtensionManagerWithStandardBackends(serviceProvider);
            context.Arguments = commandLineString;

            var commandLine = serviceProvider.GetService<ICommandLine>();
            var command = commandLine.ParseStandardCommandLine(context);
            command?.Execute();
        }

        private IExtensionManager CreateExtensionManagerWithStandardBackends(IServiceProvider serviceProvider)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            return extensionManager;
        }

        private void DisplayMessage(object sender, DisplayEventArgs e)
        {
            this.Log.LogMessageFromText(e.Message, MessageImportance.Normal);
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
