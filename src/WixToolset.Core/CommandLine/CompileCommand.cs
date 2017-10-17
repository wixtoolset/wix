// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using WixToolset.Extensibility.Services;

    internal class CompileCommand : ICommandLineCommand
    {
        public CompileCommand(IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables)
        {
            this.PreprocessorVariables = preprocessorVariables;
            this.SourceFiles = sources;
        }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        public int Execute()
        {
            var preprocessor = new Preprocessor();

            var compiler = new Compiler();

            foreach (var sourceFile in this.SourceFiles)
            {
                var document = preprocessor.Process(sourceFile.SourcePath, this.PreprocessorVariables);

                var intermediate = compiler.Compile(document);

                intermediate.Save(sourceFile.OutputPath);
            }

            return 0;
        }
    }
}
