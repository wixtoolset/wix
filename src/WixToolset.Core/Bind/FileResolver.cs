// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class FileResolver
    {
        private const string BindPathOpenString = "!(bindpath.";

        private FileResolver(IEnumerable<IBindPath> bindPaths)
        {
            this.BindPaths = (bindPaths ?? Array.Empty<IBindPath>()).ToLookup(b => b.Stage);
            this.RebaseTarget = this.BindPaths[BindStage.Target].Any();
            this.RebaseUpdated = this.BindPaths[BindStage.Updated].Any();
        }

        public FileResolver(IEnumerable<IBindPath> bindPaths, IEnumerable<IResolverExtension> extensions) : this(bindPaths)
        {
            this.ResolverExtensions = extensions ?? Array.Empty<IResolverExtension>();
        }

        public FileResolver(IEnumerable<IBindPath> bindPaths, IEnumerable<ILibrarianExtension> extensions) : this(bindPaths)
        {
            this.LibrarianExtensions = extensions ?? Array.Empty<ILibrarianExtension>();
        }

        private ILookup<BindStage, IBindPath> BindPaths { get; }

        public bool RebaseTarget { get; }

        public bool RebaseUpdated { get; }

        private IEnumerable<IResolverExtension> ResolverExtensions { get; }

        private IEnumerable<ILibrarianExtension> LibrarianExtensions { get; }

        public string Resolve(SourceLineNumber sourceLineNumbers, IntermediateTupleDefinition tupleDefinition, string source)
        {
            var checkedPaths = new List<string>();

            foreach (var extension in this.LibrarianExtensions)
            {
                var resolved = extension.ResolveFile(sourceLineNumbers, tupleDefinition, source);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            return this.MustResolveUsingBindPaths(source, tupleDefinition, sourceLineNumbers, BindStage.Normal, checkedPaths);
        }

        /// <summary>
        /// Resolves the source path of a file using binder extensions.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <param name="alreadyCheckedPaths">Optional collection of paths already checked.</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public string ResolveFile(string source, IntermediateTupleDefinition tupleDefinition, SourceLineNumber sourceLineNumbers, BindStage bindStage, IEnumerable<string> alreadyCheckedPaths = null)
        {
            var checkedPaths = new List<string>();

            if (alreadyCheckedPaths != null)
            {
                checkedPaths.AddRange(alreadyCheckedPaths);
            }

            foreach (var extension in this.ResolverExtensions)
            {
                var resolved = extension.ResolveFile(source, tupleDefinition, sourceLineNumbers, bindStage);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            return this.MustResolveUsingBindPaths(source, tupleDefinition, sourceLineNumbers, bindStage, checkedPaths);
        }

        private string MustResolveUsingBindPaths(string source, IntermediateTupleDefinition tupleDefinition, SourceLineNumber sourceLineNumbers, BindStage bindStage, List<string> checkedPaths)
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
                string bindName = String.Empty;
                var path = source;
                string pathWithoutSourceDir = null;

                if (source.StartsWith(BindPathOpenString, StringComparison.Ordinal))
                {
                    int closeParen = source.IndexOf(')', BindPathOpenString.Length);
                    if (-1 != closeParen)
                    {
                        bindName = source.Substring(BindPathOpenString.Length, closeParen - BindPathOpenString.Length);
                        path = source.Substring(BindPathOpenString.Length + bindName.Length + 1); // +1 for the closing brace.
                        path = path.TrimStart('\\'); // remove starting '\\' char so the path doesn't look rooted.
                    }
                }
                else if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    pathWithoutSourceDir = path.Substring(10);
                }

                var bindPaths = this.BindPaths[bindStage];

                foreach (var bindPath in bindPaths)
                {
                    if (!String.IsNullOrEmpty(pathWithoutSourceDir))
                    {
                        var filePath = Path.Combine(bindPath.Path, pathWithoutSourceDir);

                        checkedPaths.Add(filePath);
                        if (CheckFileExists(filePath))
                        {
                            resolved = filePath;
                        }
                    }

                    if (String.IsNullOrEmpty(resolved))
                    {
                        var filePath = Path.Combine(bindPath.Path, path);

                        checkedPaths.Add(filePath);
                        if (CheckFileExists(filePath))
                        {
                            resolved = filePath;
                        }
                    }
                }
            }

            if (null == resolved)
            {
                throw new WixException(ErrorMessages.FileNotFound(sourceLineNumbers, source, tupleDefinition.Name, checkedPaths));
            }

            return resolved;
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
