// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
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
    }
}
