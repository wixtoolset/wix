// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class FileResolver : IFileResolver
    {
        private const string BindPathOpenString = "!(bindpath.";

        public string ResolveFile(string source, IEnumerable<ILibrarianExtension> librarianExtensions, IEnumerable<IBindPath> bindPaths, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition)
        {
            var checkedPaths = new List<string>();

            foreach (var extension in librarianExtensions)
            {
                var resolved = extension.ResolveFile(sourceLineNumbers, symbolDefinition, source);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            return this.MustResolveUsingBindPaths(source, symbolDefinition, sourceLineNumbers, bindPaths, checkedPaths);
        }

        public string ResolveFile(string source, IEnumerable<IResolverExtension> resolverExtensions, IEnumerable<IBindPath> bindPaths, BindStage bindStage, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, IEnumerable<string> alreadyCheckedPaths = null)
        {
            var checkedPaths = new List<string>();

            if (alreadyCheckedPaths != null)
            {
                checkedPaths.AddRange(alreadyCheckedPaths);
            }

            foreach (var extension in resolverExtensions)
            {
                var resolved = extension.ResolveFile(source, symbolDefinition, sourceLineNumbers, bindStage);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            return this.MustResolveUsingBindPaths(source, symbolDefinition, sourceLineNumbers, bindPaths, checkedPaths);
        }

        private string MustResolveUsingBindPaths(string source, IntermediateSymbolDefinition symbolDefinition, SourceLineNumber sourceLineNumbers, IEnumerable<IBindPath> bindPaths, List<string> checkedPaths)
        {
            string resolved = null;

            // If the file exists, we're good to go.
            checkedPaths.Add(source);
            if (CheckFileExists(source))
            {
                resolved = source;
            }
            else if (Path.IsPathRooted(source)) // path is rooted so bindpaths won't help, bail since the file apparently doesn't exist.
            {
                resolved = null;
            }
            else // not a rooted path so let's try applying all the different source resolution options.
            {
                var bindName = String.Empty;
                var path = source;
                var pathWithoutSourceDir = String.Empty;

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
                else if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    pathWithoutSourceDir = path.Substring(10);
                }

                foreach (var bindPath in bindPaths)
                {
                    if (String.IsNullOrEmpty(bindName))
                    {
                        if (String.IsNullOrEmpty(bindPath.Name))
                        {
                            if (!String.IsNullOrEmpty(pathWithoutSourceDir))
                            {
                                resolved = ResolveWithBindPath(bindPath.Path, pathWithoutSourceDir, checkedPaths);
                            }

                            if (String.IsNullOrEmpty(resolved))
                            {
                                resolved = ResolveWithBindPath(bindPath.Path, path, checkedPaths);
                            }
                        }
                    }
                    else if (bindName.Equals(bindPath.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        resolved = ResolveWithBindPath(bindPath.Path, path, checkedPaths);
                    }

                    if (!String.IsNullOrEmpty(resolved))
                    {
                        break;
                    }
                }
            }

            if (null == resolved)
            {
                throw new WixException(ErrorMessages.FileNotFound(sourceLineNumbers, source, symbolDefinition?.Name, checkedPaths));
            }

            return resolved;
        }

        private static string ResolveWithBindPath(string bindPath, string relativePath, List<string> checkedPaths)
        {
            var filePath = Path.Combine(bindPath, relativePath);

            checkedPaths.Add(filePath);

            if (CheckFileExists(filePath))
            {
                return filePath;
            }

            return null;
        }

        private static bool CheckFileExists(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch (ArgumentException)
            {
                throw new WixException(ErrorMessages.IllegalCharactersInPath(path));
            }
        }
    }
}
