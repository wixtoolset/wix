// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;

    internal class BuildCommand : ICommand
    {
        public BuildCommand(IEnumerable<SourceFile> sources, IDictionary<string, string> preprocessorVariables, IEnumerable<string> locFiles, string outputPath, IEnumerable<string> cultures, string contentsFile, string outputsFile, string builtOutputsFile, string wixProjectFile)
        {
            this.LocFiles = locFiles;
            this.PreprocessorVariables = preprocessorVariables;
            this.SourceFiles = sources;
            this.OutputPath = outputPath;

            this.Cultures = cultures;
            this.ContentsFile = contentsFile;
            this.OutputsFile = outputsFile;
            this.BuiltOutputsFile = builtOutputsFile;
            this.WixProjectFile = wixProjectFile;
        }

        public IEnumerable<string> LocFiles { get; }

        private IEnumerable<SourceFile> SourceFiles { get; }

        private IDictionary<string, string> PreprocessorVariables { get; }

        private string OutputPath { get; }

        public IEnumerable<string> Cultures { get; }

        public string ContentsFile { get; }

        public string OutputsFile { get; }

        public string BuiltOutputsFile { get; }

        public string WixProjectFile { get; }

        public int Execute()
        {
            var intermediates = CompilePhase();

            var sections = intermediates.SelectMany(i => i.Sections).ToList();

            var linker = new Linker();

            var output = linker.Link(sections, OutputType.Product);

            var localizer = new Localizer();

            var binder = new Binder();
            binder.TempFilesLocation = Path.GetTempPath();
            binder.WixVariableResolver = new WixVariableResolver();
            binder.WixVariableResolver.Localizer = localizer;
            binder.AddExtension(new BinderFileManager());
            binder.SuppressValidation = true;

            binder.ContentsFile = this.ContentsFile;
            binder.OutputsFile = this.OutputsFile;
            binder.BuiltOutputsFile = this.BuiltOutputsFile;
            binder.WixprojectFile = this.WixProjectFile;

            foreach (var loc in this.LocFiles)
            {
                var localization = Localizer.ParseLocalizationFile(loc, linker.TableDefinitions);
                binder.WixVariableResolver.Localizer.AddLocalization(localization);
            }

            binder.Bind(output, this.OutputPath);

            return 0;
        }

        private IEnumerable<Intermediate> CompilePhase()
        {
            var intermediates = new List<Intermediate>();

            var preprocessor = new Preprocessor();

            var compiler = new Compiler();

            foreach (var sourceFile in this.SourceFiles)
            {
                var document = preprocessor.Process(sourceFile.SourcePath, this.PreprocessorVariables);

                var intermediate = compiler.Compile(document);

                intermediates.Add(intermediate);
            }

            return intermediates;
        }
    }
}
