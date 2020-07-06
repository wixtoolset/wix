// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
        }

        public CompileCommand(IWixToolsetServiceProvider serviceProvider, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, Platform platform)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.SourceFiles = sources;
            this.PreprocessorVariables = preprocessorVariables;
            this.Platform = platform;
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        public IExtensionManager ExtensionManager { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private Platform Platform { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

        public bool ShowLogo => throw new NotImplementedException();

        public bool StopParsing => throw new NotImplementedException();

        public bool TryParseArgument(ICommandLineParser parseHelper, string argument) => throw new NotImplementedException();

        public Task<int> ExecuteAsync(CancellationToken _)
        {
            foreach (var sourceFile in this.SourceFiles)
            {
                var context = this.ServiceProvider.GetService<IPreprocessContext>();
                context.Extensions = this.ExtensionManager.GetServices<IPreprocessorExtension>();
                context.Platform = this.Platform;
                context.IncludeSearchPaths = this.IncludeSearchPaths;
                context.SourcePath = sourceFile.SourcePath;
                context.Variables = this.PreprocessorVariables;

                IPreprocessResult result = null;
                try
                {
                    var preprocessor = this.ServiceProvider.GetService<IPreprocessor>();
                    result = preprocessor.Preprocess(context);
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
                compileContext.Extensions = this.ExtensionManager.GetServices<ICompilerExtension>();
                compileContext.Platform = this.Platform;
                compileContext.Source = result?.Document;

                var compiler = this.ServiceProvider.GetService<ICompiler>();
                var intermediate = compiler.Compile(compileContext);

                intermediate.Save(sourceFile.OutputPath);
            }

            return Task.FromResult(0);
        }
    }
}
