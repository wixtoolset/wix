// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IServiceProvider serviceProvider, IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, Platform platform)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.SourceFiles = sources;
            this.PreprocessorVariables = preprocessorVariables;
            this.Platform = platform;
        }

        private IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private Platform Platform { get; }

        public IEnumerable<string> IncludeSearchPaths { get; }

        public int Execute()
        {
            foreach (var sourceFile in this.SourceFiles)
            {
                var preprocessor = new Preprocessor(this.ServiceProvider);
                preprocessor.IncludeSearchPaths = this.IncludeSearchPaths;
                preprocessor.Platform = Platform.X86; // TODO: set this correctly
                preprocessor.SourcePath = sourceFile.SourcePath;
                preprocessor.Variables = new Dictionary<string, string>(this.PreprocessorVariables);

                XDocument document = null;
                try
                {
                    document = preprocessor.Execute();
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }

                if (this.Messaging.EncounteredError)
                {
                    continue;
                }

                var compiler = new Compiler(this.ServiceProvider);
                compiler.OutputPath = sourceFile.OutputPath;
                compiler.Platform = this.Platform;
                compiler.SourceDocument = document;
                var intermediate = compiler.Execute();

                intermediate.Save(sourceFile.OutputPath);
            }

            return 0;
        }
    }
}
