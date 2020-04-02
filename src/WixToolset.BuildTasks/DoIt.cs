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
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class DoIt : Task
    {
        public string AdditionalOptions { get; set; }

        public string[] Cultures { get; set; }

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

        public ITaskItem PdbFile { get; set; }

        public string PdbType { get; set; }

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
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();

            var listener = new MsbuildMessageListener(this.Log, "WIX", this.BuildEngine.ProjectFileOfTaskNode);

            try
            {
                this.ExecuteCore(serviceProvider, listener);
            }
            catch (WixException e)
            {
                listener.Write(e.Error);
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, null);

                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return !this.Log.HasLoggedErrors;
        }

        private void ExecuteCore(IWixToolsetServiceProvider serviceProvider, IMessageListener listener)
        {
            var commandLineString = this.BuildCommandLine();

            this.Log.LogMessage(MessageImportance.Normal, "wix.exe " + commandLineString);

            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(commandLineString);

            var commandLine = serviceProvider.GetService<ICommandLine>();
            commandLine.ExtensionManager = this.CreateExtensionManagerWithStandardBackends(serviceProvider, messaging, arguments.Extensions);
            commandLine.Arguments = arguments;
            var command = commandLine.ParseStandardCommandLine();
            command?.Execute();
        }

        private string BuildCommandLine()
        {
            var commandLineBuilder = new WixCommandLineBuilder();

            commandLineBuilder.AppendTextUnquoted("build");

            commandLineBuilder.AppendSwitchIfNotNull("-platform ", this.InstallerPlatform);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputType ", this.OutputType);
            commandLineBuilder.AppendSwitchIfNotNull("-pdb ", this.PdbFile);
            commandLineBuilder.AppendSwitchIfNotNull("-pdbType ", this.PdbType);
            commandLineBuilder.AppendIfTrue("-nologo", this.NoLogo);
            commandLineBuilder.AppendArrayIfNotNull("-culture ", this.Cultures);
            commandLineBuilder.AppendArrayIfNotNull("-d ", this.DefineConstants);
            commandLineBuilder.AppendArrayIfNotNull("-I ", this.IncludeSearchPaths);
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.ReferencePaths);
            commandLineBuilder.AppendIfTrue("-sval", this.SuppressValidation);
            commandLineBuilder.AppendArrayIfNotNull("-sice ", this.SuppressIces);
            commandLineBuilder.AppendArrayIfNotNull("-sw ", this.SuppressSpecificWarnings);
            commandLineBuilder.AppendSwitchIfNotNull("-usf ", this.UnreferencedSymbolsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-cc ", this.CabinetCachePath);
            commandLineBuilder.AppendSwitchIfNotNull("-intermediatefolder ", this.IntermediateDirectory);
            commandLineBuilder.AppendSwitchIfNotNull("-contentsfile ", this.BindContentsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputsfile ", this.BindOutputsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-builtoutputsfile ", this.BindBuiltOutputsFile);

            commandLineBuilder.AppendIfTrue("-bindFiles", this.BindFiles);
            commandLineBuilder.AppendArrayIfNotNull("-bindPath ", this.CalculateBindPathStrings());
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendArrayIfNotNull("-lib ", this.LibraryFiles);
            commandLineBuilder.AppendTextIfNotWhitespace(this.AdditionalOptions);
            commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");

            return commandLineBuilder.ToString();
        }

        private IExtensionManager CreateExtensionManagerWithStandardBackends(IWixToolsetServiceProvider serviceProvider, IMessaging messaging, string[] extensions)
        {
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            foreach (var type in new[] { typeof(WixToolset.Core.Burn.WixToolsetStandardBackend), typeof(WixToolset.Core.WindowsInstaller.WixToolsetStandardBackend) })
            {
                extensionManager.Add(type.Assembly);
            }

            foreach (var extension in extensions)
            {
                try
                {
                    extensionManager.Load(extension);
                }
                catch (WixException e)
                {
                    messaging.Write(e.Error);
                }
            }

            return extensionManager;
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

        private class MsbuildMessageListener : IMessageListener
        {
            public MsbuildMessageListener(TaskLoggingHelper logger, string shortName, string longName)
            {
                this.Logger = logger;
                this.ShortAppName = shortName;
                this.LongAppName = longName;
            }

            public string ShortAppName { get; }

            public string LongAppName { get; }

            private TaskLoggingHelper Logger { get; }

            public void Write(Message message)
            {
                switch (message.Level)
                {
                    case MessageLevel.Error:
                        this.Logger.LogError(null, this.ShortAppName + message.Id.ToString(), null, message.SourceLineNumbers?.FileName ?? this.LongAppName, message.SourceLineNumbers?.LineNumber ?? 0, 0, 0, 0, message.ResourceNameOrFormat, message.MessageArgs);
                        break;

                    case MessageLevel.Warning:
                        this.Logger.LogWarning(null, this.ShortAppName + message.Id.ToString(), null, message.SourceLineNumbers?.FileName ?? this.LongAppName, message.SourceLineNumbers?.LineNumber ?? 0, 0, 0, 0, message.ResourceNameOrFormat, message.MessageArgs);
                        break;

                    default:
                        // TODO: Revisit this because something is going horribly awry. The commented out LogMessage call is crashing saying that the "message" parameter is null. When you look at the call stack, the code
                        //       is in the wrong LogMessage override and the "null" subcategory was passed in as the message. Not clear why it is picking the wrong overload.
                        //if (message.Id > 0)
                        //{
                        //    this.Logger.LogMessage(null, code, null, message.SourceLineNumber?.FileName, message.SourceLineNumber?.LineNumber ?? 0, 0, 0, 0, MessageImportance.Normal, message.Format, message.FormatData);
                        //}
                        //else
                        //{
                        this.Logger.LogMessage(MessageImportance.Normal, message.ResourceNameOrFormat, message.MessageArgs);
                        //}
                        break;
                }
            }

            public void Write(string message)
            {
                this.Logger.LogMessage(MessageImportance.Low, message);
            }

            public MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel) => defaultMessageLevel;
        }
    }
}
