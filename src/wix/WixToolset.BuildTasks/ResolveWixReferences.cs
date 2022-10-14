// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task searches for paths to references using the order specified in SearchPaths.
    /// </summary>
    public class ResolveWixReferences : Task
    {
        /// <summary>
        /// Token value used in SearchPaths to indicate that the item's HintPath metadata should
        /// be searched as a full file path to resolve the reference.
        /// Must match wix.targets, case sensitive.
        /// </summary>
        private const string HintPathToken = "{HintPathFromItem}";

        /// <summary>
        /// Token value used in SearchPaths to indicate that the item's Identity should
        /// be searched as a full file path to resolve the reference.
        /// Must match wix.targets, case sensitive.
        /// </summary>
        private const string RawFileNameToken = "{RawFileName}";

        /// <summary>
        /// The list of references to resolve.
        /// </summary>
        [Required]
        public ITaskItem[] WixReferences { get; set; }

        /// <summary>
        /// The directories or special locations that are searched to find the files
        /// on disk that represent the references. The order in which the search paths are listed
        /// is important. For each reference, the list of paths is searched from left to right.
        /// When a file that represents the reference is found, that search stops and the search
        /// for the next reference starts.
        ///
        /// This parameter accepts the following types of values:
        ///     A directory path.
        ///     {HintPathFromItem}: Specifies that the task will examine the HintPath metadata
        ///                         of the base item.
        ///     {RawFileName}: Specifies the task will consider the Include value of the item to be
        ///                    an exact path and file name.
        /// </summary>
        public string[] SearchPaths { get; set; }

        /// <summary>
        /// The filename extension(s) to be checked when searching.
        /// </summary>
        public string[] SearchFilenameExtensions { get; set; }

        /// <summary>
        /// Output items that contain the same metadata as input references and have been resolved to full paths.
        /// </summary>
        [Output]
        public ITaskItem[] ResolvedWixReferences { get; private set; }

        /// <summary>
        /// Output items that contain the same metadata as input references and cannot be found.
        /// </summary>
        [Output]
        public ITaskItem[] UnresolvedWixReferences { get; private set; }

        /// <summary>
        /// Resolves reference paths by searching for referenced items using the specified SearchPaths.
        /// </summary>
        /// <returns>True on success, or throws an exception on failure.</returns>
        public override bool Execute()
        {
            var resolvedReferences = new List<ITaskItem>();
            var unresolvedReferences = new List<ITaskItem>();
            var uniqueReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var reference in this.WixReferences.Where(r => !String.IsNullOrWhiteSpace(r.ItemSpec)))
            {
                (var resolvedReference, var found) = this.ResolveReference(reference, this.SearchPaths, this.SearchFilenameExtensions);

                if (uniqueReferences.Add(resolvedReference.ItemSpec))
                {
                    if (found)
                    {
                        this.Log.LogMessage(MessageImportance.Low, "Resolved path {0}", resolvedReference.ItemSpec);
                        resolvedReferences.Add(resolvedReference);
                    }
                    else
                    {
                        this.Log.LogWarning(null, "WXE0001", null, null, 0, 0, 0, 0, "Unable to find extension {0}.", resolvedReference.ItemSpec);
                        unresolvedReferences.Add(resolvedReference);
                    }
                }
                else
                {
                    this.Log.LogMessage(MessageImportance.Low, "Resolved duplicate path {0}, discarding it", resolvedReference.ItemSpec);
                }
            }

            this.ResolvedWixReferences = resolvedReferences.ToArray();
            this.UnresolvedWixReferences = unresolvedReferences.ToArray();
            return true;
        }

        /// <summary>
        /// Resolves a single reference item by searcheing for referenced items using the specified SearchPaths.
        /// This method is made public so the resolution logic can be reused by other tasks.
        /// </summary>
        /// <param name="reference">The referenced item.</param>
        /// <param name="searchPaths">The paths to search.</param>
        /// <param name="searchFilenameExtensions">Filename extensions to check.</param>
        /// <returns>The resolved reference item, or the original reference if it could not be resolved.</returns>
        public (ITaskItem, bool) ResolveReference(ITaskItem reference, string[] searchPaths, string[] searchFilenameExtensions)
        {
            // Ensure we first check the reference without adding additional search filename extensions.
            searchFilenameExtensions = searchFilenameExtensions == null ? new[] { String.Empty } : searchFilenameExtensions.Prepend(String.Empty).ToArray();

            // Copy all the metadata from the source
            var resolvedReference = new TaskItem(reference);
            this.Log.LogMessage(MessageImportance.Low, "WixReference: {0}", reference.ItemSpec);

            var found = false;

            // Nothing to search, so just resolve the original reference item.
            if (searchPaths == null)
            {
                if (this.ResolveFilenameExtensions(resolvedReference, resolvedReference.ItemSpec, searchFilenameExtensions))
                {
                    found = true;
                }

                return (resolvedReference, found);
            }

            // Otherwise, now try to find the resolved path based on the order of precedence from search paths.
            foreach (var searchPath in searchPaths)
            {
                this.Log.LogMessage(MessageImportance.Low, "Trying {0}", searchPath);
                if (HintPathToken.Equals(searchPath, StringComparison.Ordinal))
                {
                    var path = reference.GetMetadata("HintPath");
                    if (String.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    this.Log.LogMessage(MessageImportance.Low, "Trying path {0}", path);
                    if (File.Exists(path))
                    {
                        resolvedReference.ItemSpec = path;
                        found = true;
                        break;
                    }
                }
                else if (RawFileNameToken.Equals(searchPath, StringComparison.Ordinal))
                {
                    if (this.ResolveFilenameExtensions(resolvedReference, resolvedReference.ItemSpec, searchFilenameExtensions))
                    {
                        found = true;
                        break;
                    }
                }
                else
                {
                    var path = Path.Combine(searchPath, reference.ItemSpec);

                    if (this.ResolveFilenameExtensions(resolvedReference, path, searchFilenameExtensions))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                // Normalize the item spec to the full path.
                resolvedReference.ItemSpec = resolvedReference.GetMetadata("FullPath");
            }

            return (resolvedReference, found);
        }

        /// <summary>
        /// Helper method for checking filename extensions when resolving references.
        /// </summary>
        /// <param name="reference">The reference being resolved.</param>
        /// <param name="basePath">Full filename path without extension.</param>
        /// <param name="filenameExtensions">Filename extensions to check.</param>
        /// <returns>True if the item was resolved, else false.</returns>
        private bool ResolveFilenameExtensions(ITaskItem reference, string basePath, string[] filenameExtensions)
        {
            foreach (var filenameExtension in filenameExtensions)
            {
                var path = basePath + filenameExtension;
                this.Log.LogMessage(MessageImportance.Low, "Trying path {0}", path);

                if (File.Exists(path))
                {
                    reference.ItemSpec = path;
                    return true;
                }
            }

            return false;
        }
    }
}
