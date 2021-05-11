// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    internal static class AssemblyExtensions
    {
        internal static FindAssemblyRelativeFileResult FindFileRelativeToAssembly(this Assembly assembly, string relativePath, bool searchNativeDllDirectories)
        {
            // First try using the Assembly.Location. This works in almost all cases with
            // no side-effects.
            var path = Path.Combine(Path.GetDirectoryName(assembly.Location), relativePath);
            var possiblePaths = new StringBuilder(path);

            var found = File.Exists(path);
            if (!found)
            {
                // Fallback to the Assembly.CodeBase to handle "shadow copy" scenarios (like unit tests) but
                // only check codebase if it is different from the Assembly.Location path.
                var codebase = Path.Combine(Path.GetDirectoryName(new Uri(assembly.CodeBase).LocalPath), relativePath);

                if (!codebase.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    path = codebase;
                    possiblePaths.Append(Path.PathSeparator + path);

                    found = File.Exists(path);
                }

                if (!found && searchNativeDllDirectories && AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string searchDirectoriesString)
                {
                    // If instructed to search native DLL search directories, try to find our file there.
                    possiblePaths.Append(Path.PathSeparator + searchDirectoriesString);

                    var searchDirectories = searchDirectoriesString?.Split(Path.PathSeparator);
                    foreach (var directoryPath in searchDirectories)
                    {
                        var possiblePath = Path.Combine(directoryPath, relativePath);
                        if (File.Exists(possiblePath))
                        {
                            path = possiblePath;
                            found = true;
                            break;
                        }
                    }
                }
            }

            return new FindAssemblyRelativeFileResult
            {
                Found = found,
                Path = found ? path : null,
                PossiblePaths = possiblePaths.ToString()
            };
        }

        internal class FindAssemblyRelativeFileResult
        {
            public bool Found { get; set; }

            public string Path { get; set; }

            public string PossiblePaths { get; set; }
        }
    }
}
