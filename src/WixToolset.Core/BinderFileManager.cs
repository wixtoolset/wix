// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Extensibility;

#if DEAD_CODE
    /// <summary>
    /// Base class for creating a binder file manager.
    /// </summary>
    public class BinderFileManager : IBinderFileManager
    {
        /// <summary>
        /// Gets or sets the file manager core.
        /// </summary>
        public IBinderFileManagerCore Core { get; set; }

        /// <summary>
        /// Compares two files to determine if they are equivalent.
        /// </summary>
        /// <param name="targetFile">The target file.</param>
        /// <param name="updatedFile">The updated file.</param>
        /// <returns>true if the files are equal; false otherwise.</returns>
        public virtual bool? CompareFiles(string targetFile, string updatedFile)
        {
            FileInfo targetFileInfo = new FileInfo(targetFile);
            FileInfo updatedFileInfo = new FileInfo(updatedFile);

            if (targetFileInfo.Length != updatedFileInfo.Length)
            {
                return false;
            }

            using (FileStream targetStream = File.OpenRead(targetFile))
            {
                using (FileStream updatedStream = File.OpenRead(updatedFile))
                {
                    if (targetStream.Length != updatedStream.Length)
                    {
                        return false;
                    }

                    // Using a larger buffer than the default buffer of 4 * 1024 used by FileStream.ReadByte improves performance.
                    // The buffer size is based on user feedback. Based on performance results, a better buffer size may be determined.
                    byte[] targetBuffer = new byte[16 * 1024];
                    byte[] updatedBuffer = new byte[16 * 1024];
                    int targetReadLength;
                    int updatedReadLength;
                    do
                    {
                        targetReadLength = targetStream.Read(targetBuffer, 0, targetBuffer.Length);
                        updatedReadLength = updatedStream.Read(updatedBuffer, 0, updatedBuffer.Length);

                        if (targetReadLength != updatedReadLength)
                        {
                            return false;
                        }

                        for (int i = 0; i < targetReadLength; ++i)
                        {
                            if (targetBuffer[i] != updatedBuffer[i])
                            {
                                return false;
                            }
                        }

                    } while (0 < targetReadLength);
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves the source path of a file.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (BinderFileManager.CheckFileExists(source)) // if the file exists, we're good to go.
            {
                return source;
            }
            else if (Path.IsPathRooted(source)) // path is rooted so bindpaths won't help, bail since the file apparently doesn't exist.
            {
                return null;
            }
            else // not a rooted path so let's try applying all the different source resolution options.
            {
                const string bindPathOpenString = "!(bindpath.";

                string bindName = String.Empty;
                string path = source;
                string pathWithoutSourceDir = null;

                if (source.StartsWith(bindPathOpenString, StringComparison.Ordinal))
                {
                    int closeParen = source.IndexOf(')', bindPathOpenString.Length);
                    if (-1 != closeParen)
                    {
                        bindName = source.Substring(bindPathOpenString.Length, closeParen - bindPathOpenString.Length);
                        path = source.Substring(bindPathOpenString.Length + bindName.Length + 1); // +1 for the closing brace.
                        path = path.TrimStart('\\'); // remove starting '\\' char so the path doesn't look rooted.
                    }
                }
                else if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    pathWithoutSourceDir = path.Substring(10);
                }

                var bindPaths = this.Core.GetBindPaths(bindStage, bindName);
                foreach (string bindPath in bindPaths)
                {
                    string filePath;
                    if (!String.IsNullOrEmpty(pathWithoutSourceDir))
                    {
                        filePath = Path.Combine(bindPath, pathWithoutSourceDir);
                        if (BinderFileManager.CheckFileExists(filePath))
                        {
                            return filePath;
                        }
                    }

                    filePath = Path.Combine(bindPath, path);
                    if (BinderFileManager.CheckFileExists(filePath))
                    {
                        return filePath;
                    }
                }
            }

            // Didn't find the file.
            return null;
        }

        /// <summary>
        /// Resolves the source path of a file related to another file's source.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="relatedSource">Source related to original source.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            string resolvedSource = this.ResolveFile(source, type, sourceLineNumbers, bindStage);
            return Path.Combine(Path.GetDirectoryName(resolvedSource), relatedSource);
        }

        /// <summary>
        /// Resolves the source path of a cabinet file.
        /// </summary>
        /// <param name="cabinetPath">Default path to cabinet to generate.</param>
        /// <param name="filesWithPath">Collection of files in this cabinet.</param>
        /// <returns>The CabinetBuildOption and path to build the .  By default the cabinet is built and moved to its target location.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public virtual ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<BindFileWithPath> filesWithPath)
        {
            if (null == filesWithPath)
            {
                throw new ArgumentNullException("fileRows");
            }

            // By default cabinet should be built and moved to the suggested location.
            ResolvedCabinet resolved = new ResolvedCabinet() { BuildOption = CabinetBuildOption.BuildAndMove, Path = cabinetPath };

            // If a cabinet cache path was provided, change the location for the cabinet
            // to be built to and check if there is a cabinet that can be reused.
            if (!String.IsNullOrEmpty(this.Core.CabCachePath))
            {
                string cabinetName = Path.GetFileName(cabinetPath);
                resolved.Path = Path.Combine(this.Core.CabCachePath, cabinetName);

                if (BinderFileManager.CheckFileExists(resolved.Path))
                {
                    // Assume that none of the following are true:
                    // 1. any files are added or removed
                    // 2. order of files changed or names changed
                    // 3. modified time changed
                    bool cabinetValid = true;

                    // Need to force garbage collection of WixEnumerateCab to ensure the handle
                    // associated with it is closed before it is reused.
                    using (Cab.WixEnumerateCab wixEnumerateCab = new Cab.WixEnumerateCab())
                    {
                        List<CabinetFileInfo> fileList = wixEnumerateCab.Enumerate(resolved.Path);

                        if (filesWithPath.Count() != fileList.Count)
                        {
                            cabinetValid = false;
                        }
                        else
                        {
                            int i = 0;
                            foreach (BindFileWithPath file in filesWithPath)
                            {
                                // First check that the file identifiers match because that is quick and easy.
                                CabinetFileInfo cabFileInfo = fileList[i];
                                cabinetValid = (cabFileInfo.FileId == file.Id);
                                if (cabinetValid)
                                {
                                    // Still valid so ensure the file sizes are the same.
                                    FileInfo fileInfo = new FileInfo(file.Path);
                                    cabinetValid = (cabFileInfo.Size == fileInfo.Length);
                                    if (cabinetValid)
                                    {
                                        // Still valid so ensure the source time stamp hasn't changed. Thus we need
                                        // to convert the source file time stamp into a cabinet compatible data/time.
                                        ushort sourceCabDate;
                                        ushort sourceCabTime;

                                        WixToolset.Core.Native.CabInterop.DateTimeToCabDateAndTime(fileInfo.LastWriteTime, out sourceCabDate, out sourceCabTime);
                                        cabinetValid = (cabFileInfo.Date == sourceCabDate && cabFileInfo.Time == sourceCabTime);
                                    }
                                }

                                if (!cabinetValid)
                                {
                                    break;
                                }

                                i++;
                            }
                        }
                    }

                    resolved.BuildOption = cabinetValid ? CabinetBuildOption.Copy : CabinetBuildOption.BuildAndCopy;
                }
            }

            return resolved;
        }

        /// <summary>
        /// Resolve the layout path of a media.
        /// </summary>
        /// <param name="mediaRow">The media's row.</param>
        /// <param name="mediaLayoutDirectory">The layout directory provided by the Media element.</param>
        /// <param name="layoutDirectory">The layout directory for the setup image.</param>
        /// <returns>The layout path for the media.</returns>
        public virtual string ResolveMedia(MediaRow mediaRow, string mediaLayoutDirectory, string layoutDirectory)
        {
            return null;
        }

        /// <summary>
        /// Resolves the URL to a file.
        /// </summary>
        /// <param name="url">URL that may be a format string for the id and fileName.</param>
        /// <param name="packageId">Identity of the package (if payload is not part of a package) the URL points to. NULL if not part of a package.</param>
        /// <param name="payloadId">Identity of the payload the URL points to.</param>
        /// <param name="fileName">File name the URL points at.</param>
        /// <param name="fallbackUrl">Optional URL to use if the URL provided is empty.</param>
        /// <returns>An absolute URL or null if no URL is provided.</returns>
        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            // If a URL was not specified but there is a fallback URL that has a format specifier in it
            // then use the fallback URL formatter for this URL.
            if (String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(fallbackUrl))
            {
                string formattedFallbackUrl = String.Format(fallbackUrl, packageId, payloadId, fileName);
                if (!String.Equals(fallbackUrl, formattedFallbackUrl, StringComparison.OrdinalIgnoreCase))
                {
                    url = fallbackUrl;
                }
            }

            if (!String.IsNullOrEmpty(url))
            {
                string formattedUrl = String.Format(url, packageId, payloadId, fileName);

                Uri canonicalUri;
                if (Uri.TryCreate(formattedUrl, UriKind.Absolute, out canonicalUri))
                {
                    url = canonicalUri.AbsoluteUri;
                }
                else
                {
                    url = null;
                }
            }

            return url;
        }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false.</param>
        public virtual bool CopyFile(string source, string destination, bool overwrite)
        {
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
        public virtual bool MoveFile(string source, string destination, bool overwrite)
        {
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

        /// <summary>
        /// Checks if a path exists, and throws a well known error for invalid paths.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if path exists.</returns>
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
#endif
}
