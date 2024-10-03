// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class HarvestFilesCommand
    {
        private const string BindPathOpenString = "!(bindpath.";

        public HarvestFilesCommand(IOptimizeContext context)
        {
            this.Context = context;
            this.Messaging = this.Context.ServiceProvider.GetService<IMessaging>();
            this.ParseHelper = this.Context.ServiceProvider.GetService<IParseHelper>();
        }

        public IOptimizeContext Context { get; }

        public IMessaging Messaging { get; }

        public IParseHelper ParseHelper { get; }

        internal void Execute()
        {
            var harvestedFiles = new HashSet<string>();

            foreach (var section in this.Context.Intermediates.SelectMany(i => i.Sections))
            {
                foreach (var harvestFiles in section.Symbols.OfType<HarvestFilesSymbol>().ToList())
                {
                    this.HarvestFiles(harvestFiles, section, harvestedFiles);
                }
            }
        }

        private void HarvestFiles(HarvestFilesSymbol harvestFile, IntermediateSection section, ISet<string> harvestedFiles)
        {
            var unusedSectionCachedInlinedDirectoryIds = new Dictionary<string, string>();

            var inclusions = harvestFile.Inclusions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var exclusions = harvestFile.Exclusions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var comparer = new WildcardFileComparer();

            var resolvedFiles = Enumerable.Empty<WildcardFile>();

            try
            {
                var included = this.GetWildcardFiles(harvestFile, inclusions);
                var excluded = this.GetWildcardFiles(harvestFile, exclusions);

                foreach (var excludedFile in excluded)
                {
                    this.Messaging.Write(OptimizerVerboses.ExcludedFile(harvestFile.SourceLineNumbers, excludedFile.Path));
                }

                resolvedFiles = included.Except(excluded, comparer).ToList();

                if (!resolvedFiles.Any())
                {
                    this.Messaging.Write(OptimizerWarnings.ZeroFilesHarvested(harvestFile.SourceLineNumbers));
                }
            }
            catch (DirectoryNotFoundException e)
            {
                this.Messaging.Write(OptimizerWarnings.ExpectedDirectory(harvestFile.SourceLineNumbers, e.Message));

                return;
            }

            foreach (var fileByRecursiveDir in resolvedFiles.GroupBy(resolvedFile => resolvedFile.RecursiveDir, resolvedFile => resolvedFile.Path))
            {
                var directoryId = harvestFile.DirectoryRef;

                var recursiveDir = fileByRecursiveDir.Key;

                if (!String.IsNullOrEmpty(recursiveDir))
                {
                    directoryId = this.ParseHelper.CreateDirectoryReferenceFromInlineSyntax(section, harvestFile.SourceLineNumbers, attribute: null, directoryId, recursiveDir, unusedSectionCachedInlinedDirectoryIds);
                }

                foreach (var file in fileByRecursiveDir)
                {
                    if (harvestedFiles.Add(file))
                    {
                        var name = Path.GetFileName(file);

                        var id = this.ParseHelper.CreateIdentifier("fls", directoryId, name);

                        this.Messaging.Write(OptimizerVerboses.HarvestedFile(harvestFile.SourceLineNumbers, file));

                        section.AddSymbol(new FileSymbol(harvestFile.SourceLineNumbers, id)
                        {
                            ComponentRef = id.Id,
                            Name = name,
                            Attributes = FileSymbolAttributes.None | FileSymbolAttributes.Vital,
                            DirectoryRef = directoryId,
                            Source = new IntermediateFieldPathValue { Path = file },
                        });

                        section.AddSymbol(new ComponentSymbol(harvestFile.SourceLineNumbers, id)
                        {
                            ComponentId = "*",
                            DirectoryRef = directoryId,
                            Location = ComponentLocation.LocalOnly,
                            KeyPath = id.Id,
                            KeyPathType = ComponentKeyPathType.File,
                            Win64 = this.Context.Platform == Platform.ARM64 || this.Context.Platform == Platform.X64,
                        });

                        if (Enum.TryParse<ComplexReferenceParentType>(harvestFile.ComplexReferenceParentType, out var parentType)
                            && ComplexReferenceParentType.Unknown != parentType && null != harvestFile.ParentId)
                        {
                            // If the parent was provided, add a complex reference to that, and, if 
                            // the Files is under a feature, then mark the complex reference primary.
                            this.ParseHelper.CreateComplexReference(section, harvestFile.SourceLineNumbers, parentType, harvestFile.ParentId, null, ComplexReferenceChildType.Component, id.Id, ComplexReferenceParentType.Feature == parentType);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(OptimizerWarnings.SkippingDuplicateFile(harvestFile.SourceLineNumbers, file));
                    }
                }
            }
        }

        private IEnumerable<WildcardFile> GetWildcardFiles(HarvestFilesSymbol harvestFile, IEnumerable<string> patterns)
        {
            var sourceLineNumbers = harvestFile.SourceLineNumbers;
            var sourcePath = harvestFile.SourcePath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var files = new List<WildcardFile>();

            foreach (var pattern in patterns)
            {
                // Resolve bind paths, if any, which might result in multiple directories.
                foreach (var path in this.ResolveBindPaths(sourceLineNumbers, pattern))
                {
                    var sourceDirectory = String.IsNullOrEmpty(sourcePath) ? Path.GetDirectoryName(sourceLineNumbers.FileName) : sourcePath;
                    var recursive = path.IndexOf("**") >= 0;
                    var filePortion = Path.GetFileName(path);
                    var directoryPortion = Path.GetDirectoryName(path);

                    if (directoryPortion?.EndsWith(@"\**") == true)
                    {
                        directoryPortion = directoryPortion.Substring(0, directoryPortion.Length - 3);
                    }

                    var recursiveDirOffset = directoryPortion.Length + 1;

                    if (directoryPortion is null || directoryPortion.Length == 0 || directoryPortion == "**")
                    {
                        directoryPortion = sourceDirectory;
                        recursiveDirOffset = sourceDirectory.Length + 1;

                    }
                    else if (!Path.IsPathRooted(directoryPortion))
                    {
                        directoryPortion = Path.Combine(sourceDirectory, directoryPortion);
                        recursiveDirOffset = directoryPortion.Length + 1;
                    }

                    var foundFiles = Directory.EnumerateFiles(directoryPortion, filePortion, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                    foreach (var foundFile in foundFiles)
                    {
                        var recursiveDir = Path.GetDirectoryName(foundFile.Substring(recursiveDirOffset));
                        files.Add(new WildcardFile()
                        {
                            RecursiveDir = recursiveDir,
                            Path = foundFile,
                        });
                    }
                }
            }

            return files;
        }

        private IEnumerable<string> ResolveBindPaths(SourceLineNumber sourceLineNumbers, string source)
        {
            var resultingDirectories = new List<string>();

            var bindName = String.Empty;
            var path = source;

            if (source.StartsWith(BindPathOpenString, StringComparison.Ordinal))
            {
                var closeParen = source.IndexOf(')', BindPathOpenString.Length);

                if (-1 != closeParen)
                {
                    bindName = source.Substring(BindPathOpenString.Length, closeParen - BindPathOpenString.Length);
                    path = source.Substring(BindPathOpenString.Length + bindName.Length + 1); // +1 for the closing paren.
                    path = path.TrimStart('\\'); // remove starting '\\' char so the path doesn't look rooted.
                }
            }

            if (String.IsNullOrEmpty(bindName))
            {
                var unnamedBindPath = this.Context.BindPaths.SingleOrDefault(bp => bp.Name == null)?.Path;

                resultingDirectories.Add(unnamedBindPath is null ? path : Path.Combine(unnamedBindPath, path));
            }
            else
            {
                var foundBindPath = false;

                foreach (var bindPath in this.Context.BindPaths)
                {
                    if (bindName.Equals(bindPath.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var resolved = Path.Combine(bindPath.Path, path);
                        resultingDirectories.Add(resolved);

                        foundBindPath = true;
                    }
                }

                if (!foundBindPath)
                {
                    this.Messaging.Write(OptimizerWarnings.ExpectedDirectory(sourceLineNumbers, source));
                }
            }

            return resultingDirectories;
        }

        private class WildcardFile
        {
            public string RecursiveDir { get; set; }

            public string Path { get; set; }
        }

        private class WildcardFileComparer : IEqualityComparer<WildcardFile>
        {
            public bool Equals(WildcardFile x, WildcardFile y)
            {
                return x?.Path == y?.Path;
            }

            public int GetHashCode(WildcardFile obj)
            {
                return obj?.Path?.GetHashCode() ?? 0;
            }
        }
    }
}
