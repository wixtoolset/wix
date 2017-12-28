// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixToolset.Dtf.WindowsInstaller;

    public class Builder
    {
        public Builder(string sourceFolder, Type extensionType = null, string[] bindPaths = null)
        {
            this.SourceFolder = sourceFolder;
            this.ExtensionType = extensionType;
            this.BindPaths = bindPaths;
        }

        public string[] BindPaths { get; }

        public Type ExtensionType { get; }

        public string SourceFolder { get; }

        public string[] BuildAndQuery(Action<string[]> buildFunc, params string[] tables)
        {
            var sourceFiles = Directory.GetFiles(this.SourceFolder, "*.wxs");
            var wxlFiles = Directory.GetFiles(this.SourceFolder, "*.wxl");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"bin\test.msi");

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

                return this.Query(outputPath, tables);
            }
        }

        private string[] Query(string path, string[] tables)
        {
            var results = new List<string>();

            if (tables?.Length > 0)
            {
                var sb = new StringBuilder();
                using (var db = new Database(path))
                {
                    foreach (var table in tables)
                    {
                        using (var view = db.OpenView($"SELECT * FROM `{table}`"))
                        {
                            view.Execute();

                            Record record;
                            while ((record = view.Fetch()) != null)
                            {
                                sb.Clear();
                                sb.AppendFormat("{0}:", table);

                                using (record)
                                {
                                    for (var i = 0; i < record.FieldCount; ++i)
                                    {
                                        if (i > 0)
                                        {
                                            sb.Append("\t");
                                        }

                                        sb.Append(record[i + 1]?.ToString());
                                    }
                                }

                                results.Add(sb.ToString());
                            }
                        }
                    }
                }
            }

            return results.ToArray();
        }
    }
}
