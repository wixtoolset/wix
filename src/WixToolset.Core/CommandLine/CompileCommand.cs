// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IServiceProvider serviceProvider, IExtensionManager extensions, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables)
        {
            this.PreprocessorVariables = preprocessorVariables;
            this.ServiceProvider = serviceProvider;
            this.ExtensionManager = extensions;
            this.SourceFiles = sources;
        }

        private IServiceProvider ServiceProvider { get; }

        private IExtensionManager ExtensionManager { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        public int Execute()
        {
            foreach (var sourceFile in this.SourceFiles)
            {
                var preprocessor = new Preprocessor();
                var document = preprocessor.Process(sourceFile.SourcePath, this.PreprocessorVariables);

                var compileContext = this.ServiceProvider.GetService<ICompileContext>();
                compileContext.Messaging = Messaging.Instance;
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
