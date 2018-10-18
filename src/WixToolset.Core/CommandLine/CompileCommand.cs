// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IServiceProvider serviceProvider, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, Platform platform)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.SourceFiles = sources;
            this.PreprocessorVariables = preprocessorVariables;
            this.Platform = platform;
        }

        private IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        public IExtensionManager ExtensionManager { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private Platform Platform { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

        public int Execute()
        {
            foreach (var sourceFile in this.SourceFiles)
            {
                var context = this.ServiceProvider.GetService<IPreprocessContext>();
                context.Extensions = this.ExtensionManager.Create<IPreprocessorExtension>();
                context.Platform = this.Platform;
                context.IncludeSearchPaths = this.IncludeSearchPaths;
                context.SourcePath = sourceFile.SourcePath;
                context.Variables = this.PreprocessorVariables;

                XDocument document = null;
                try
                {
                    var preprocessor = this.ServiceProvider.GetService<IPreprocessor>();
                    document = preprocessor.Preprocess(context);
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var compileContext = this.ServiceProvider.GetService<ICompileContext>();
                compileContext.Extensions = this.ExtensionManager.Create<ICompilerExtension>();
                compileContext.OutputPath = sourceFile.OutputPath;
                compileContext.Platform = this.Platform;
                compileContext.Source = document;

                var compiler = this.ServiceProvider.GetService<ICompiler>();
                var intermediate = compiler.Compile(compileContext);

                intermediate.Save(sourceFile.OutputPath);
            }

            return 0;
        }
    }
}
