// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;

    internal class FileResolver
    {
        private const string BindPathOpenString = "!(bindpath.";

        private FileResolver(IEnumerable<BindPath> bindPaths)
        {
            this.BindPaths = (bindPaths ?? Array.Empty<BindPath>()).ToLookup(b => b.Stage);
            this.RebaseTarget = this.BindPaths[BindStage.Target].Any();
            this.RebaseUpdated = this.BindPaths[BindStage.Updated].Any();
        }

        public FileResolver(IEnumerable<BindPath> bindPaths, IEnumerable<IBinderExtension> extensions) : this(bindPaths)
        {
            this.BinderExtensions = extensions ?? Array.Empty<IBinderExtension>();
        }

        public FileResolver(IEnumerable<BindPath> bindPaths, IEnumerable<ILibrarianExtension> extensions) : this(bindPaths)
        {
            this.LibrarianExtensions = extensions ?? Array.Empty<ILibrarianExtension>();
        }

        private ILookup<BindStage, BindPath> BindPaths { get; }

        public bool RebaseTarget { get; }

        public bool RebaseUpdated { get; }

        private IEnumerable<IBinderExtension> BinderExtensions { get; }

        private IEnumerable<ILibrarianExtension> LibrarianExtensions { get; }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false.</param>
        public bool CopyFile(string source, string destination, bool overwrite)
        {
            foreach (var extension in this.BinderExtensions)
            {
                if (extension.CopyFile(source, destination, overwrite))
                {
                    return true;
                }
            }

            if (overwrite && File.Exists(destination))
            {
                File.Delete(destination);
            }

            if (!CreateHardLink(destination, source, IntPtr.Zero))
            {
#if DEBUG
                int er = Marshal.GetLastWin32Error();
#endif

                File.Copy(source, destination, overwrite);
            }

            return true;
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        public bool MoveFile(string source, string destination, bool overwrite)
        {
            foreach (var extension in this.BinderExtensions)
            {
                if (extension.MoveFile(source, destination, overwrite))
                {
                    return true;
                }
            }

            if (overwrite && File.Exists(destination))
            {
                File.Delete(destination);
            }

            var directory = Path.GetDirectoryName(destination);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Move(source, destination);

            return true;
        }

        public string Resolve(SourceLineNumber sourceLineNumbers, string table, string path)
        {
            foreach (var extension in this.LibrarianExtensions)
            {
                var resolved = extension.Resolve(sourceLineNumbers, table, path);

                if (null != resolved)
                {
                    return resolved;
                }
            }

            return this.ResolveUsingBindPaths(path, table, sourceLineNumbers, BindStage.Normal);
        }

        /// <summary>
        /// Resolves the source path of a file using binder extensions.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            foreach (var extension in this.BinderExtensions)
            {
                var resolved = extension.ResolveFile(source, type, sourceLineNumbers, bindStage);

                if (null != resolved)
                {
                    return resolved;
                }
            }

            return this.ResolveUsingBindPaths(source, type, sourceLineNumbers, bindStage);
        }

        private string ResolveUsingBindPaths(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            string resolved = null;

            // If the file exists, we're good to go.
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

                        if (CheckFileExists(filePath))
                        {
                            resolved = filePath;
                        }
                    }

                    if (String.IsNullOrEmpty(resolved))
                    {
                        var filePath = Path.Combine(bindPath.Path, path);

                        if (CheckFileExists(filePath))
                        {
                            resolved = filePath;
                        }
                    }
                }
            }

            if (null == resolved)
            {
                throw new WixFileNotFoundException(sourceLineNumbers, source, type);
            }

            // Didn't find the file.
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
                throw new WixException(WixErrors.IllegalCharactersInPath(path));
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
