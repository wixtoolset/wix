// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Builder
    {
        public Builder(string sourceFolder, Type extensionType = null, string[] bindPaths = null, string outputFile = null)
        {
            this.SourceFolder = sourceFolder;
            this.ExtensionType = extensionType;
            this.BindPaths = bindPaths;
            this.OutputFile = outputFile ?? "test.msi";
        }

        public string[] BindPaths { get; set; }

        public Type ExtensionType { get; set; }

        public string OutputFile { get; set; }

        public string SourceFolder { get; }

        public string[] BuildAndQuery(Action<string[]> buildFunc, params string[] tables)
        {
            var sourceFiles = Directory.GetFiles(this.SourceFolder, "*.wxs");
            var wxlFiles = Directory.GetFiles(this.SourceFolder, "*.wxl");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, "bin", this.OutputFile);

                var args = new List<string>
                {
                    "build",
                    "-o", outputPath,
                    "-intermediateFolder", intermediateFolder,
                };

                if (this.ExtensionType != null)
                {
                    args.Add("-ext");
                    args.Add(Path.GetFullPath(new Uri(this.ExtensionType.Assembly.CodeBase).LocalPath));
                }

                args.AddRange(sourceFiles);

                foreach (var wxlFile in wxlFiles)
                {
                    args.Add("-loc");
                    args.Add(wxlFile);
                }

                foreach (var bindPath in this.BindPaths)
                {
                    args.Add("-bindpath");
                    args.Add(bindPath);
                }

                buildFunc(args.ToArray());

                return Query.QueryDatabase(outputPath, tables);
            }
        }

        public void BuildAndDecompileAndBuild(Action<string[]> buildFunc, Action<string[]> decompileFunc, string decompilePath)
        {
            var sourceFiles = Directory.GetFiles(this.SourceFolder, "*.wxs");
            var wxlFiles = Directory.GetFiles(this.SourceFolder, "*.wxl");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputFolder = Path.Combine(intermediateFolder, "bin");
                var decompileExtractFolder = Path.Combine(intermediateFolder, "decompiled", "extract");
                var decompileIntermediateFolder = Path.Combine(intermediateFolder, "decompiled", "obj");
                var decompileBuildFolder = Path.Combine(intermediateFolder, "decompiled", "bin");
                var outputPath = Path.Combine(outputFolder, this.OutputFile);
                var decompileBuildPath = Path.Combine(decompileBuildFolder, this.OutputFile);

                // First build.
                var firstBuildArgs = new List<string>
                {
                    "build",
                    "-o", outputPath,
                    "-intermediateFolder", intermediateFolder,
                };

                if (this.ExtensionType != null)
                {
                    firstBuildArgs.Add("-ext");
                    firstBuildArgs.Add(Path.GetFullPath(new Uri(this.ExtensionType.Assembly.CodeBase).LocalPath));
                }

                firstBuildArgs.AddRange(sourceFiles);

                foreach (var wxlFile in wxlFiles)
                {
                    firstBuildArgs.Add("-loc");
                    firstBuildArgs.Add(wxlFile);
                }

                foreach (var bindPath in this.BindPaths)
                {
                    firstBuildArgs.Add("-bindpath");
                    firstBuildArgs.Add(bindPath);
                }

                buildFunc(firstBuildArgs.ToArray());

                // Decompile built output.
                var decompileArgs = new List<string>
                {
                    "msi", "decompile",
                    outputPath,
                    "-intermediateFolder", decompileIntermediateFolder,
                    "-x", decompileExtractFolder,
                    "-o", decompilePath
                };

                if (this.ExtensionType != null)
                {
                    decompileArgs.Add("-ext");
                    decompileArgs.Add(Path.GetFullPath(new Uri(this.ExtensionType.Assembly.CodeBase).LocalPath));
                }

                decompileFunc(decompileArgs.ToArray());

                // Build decompiled output.
                var secondBuildArgs = new List<string>
                {
                    "build",
                    decompilePath,
                    "-o", decompileBuildPath,
                    "-intermediateFolder", decompileIntermediateFolder
                };

                if (this.ExtensionType != null)
                {
                    secondBuildArgs.Add("-ext");
                    secondBuildArgs.Add(Path.GetFullPath(new Uri(this.ExtensionType.Assembly.CodeBase).LocalPath));
                }

                secondBuildArgs.Add("-bindpath");
                secondBuildArgs.Add(outputFolder);

                secondBuildArgs.Add("-bindpath");
                secondBuildArgs.Add(decompileExtractFolder);

                buildFunc(secondBuildArgs.ToArray());
            }
        }
    }
}
