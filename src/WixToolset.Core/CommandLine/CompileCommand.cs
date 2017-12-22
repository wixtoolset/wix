// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IServiceProvider serviceProvider, IMessaging messaging, IExtensionManager extensions, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables)
        {
            this.PreprocessorVariables = preprocessorVariables;
            this.ServiceProvider = serviceProvider;
            this.Messaging = messaging;
            this.ExtensionManager = extensions;
            this.SourceFiles = sources;
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IExtensionManager ExtensionManager { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        public int Execute()
        {
            foreach (var sourceFile in this.SourceFiles)
            {
                var preprocessContext = this.ServiceProvider.GetService<IPreprocessContext>();
                preprocessContext.Messaging = this.Messaging;
                preprocessContext.Extensions = this.ExtensionManager.Create<IPreprocessorExtension>();
                preprocessContext.Platform = Platform.X86; // TODO: set this correctly
                preprocessContext.IncludeSearchPaths = this.IncludeSearchPaths?.ToList() ?? new List<string>();
                preprocessContext.SourceFile = sourceFile.SourcePath;
                preprocessContext.Variables = new Dictionary<string, string>(this.PreprocessorVariables);

                var preprocessor = new Preprocessor();
                var document = preprocessor.Process(preprocessContext);

                var compileContext = this.ServiceProvider.GetService<ICompileContext>();
                compileContext.Messaging = this.Messaging;
                compileContext.CompilationId = Guid.NewGuid().ToString("N");
                compileContext.Extensions = this.ExtensionManager.Create<ICompilerExtension>();
                compileContext.OutputPath = sourceFile.OutputPath;
                compileContext.Platform = Platform.X86; // TODO: set this correctly
                compileContext.Source = document;

                var compiler = new Compiler();
                var intermediate = compiler.Compile(compileContext);

                intermediate.Save(sourceFile.OutputPath);
            }

            return 0;
        }
    }
}
