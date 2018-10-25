// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    public class CabinetResolver
    {
        public CabinetResolver(string cabCachePath, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions)
        {
            this.CabCachePath = cabCachePath;

            this.BackendExtensions = backendExtensions;
        }

        private string CabCachePath { get; }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        public ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<FileFacade> fileFacades)
        {
            var filesWithPath = fileFacades.Select(f => new BindFileWithPath() { Id = f.File.File, Path = f.WixFile.Source.Path }).ToList();

            ResolvedCabinet resolved = null;

            foreach (var extension in this.BackendExtensions)
            {
                resolved = extension.ResolveCabinet(cabinetPath, filesWithPath);

                if (null != resolved)
                {
                    return resolved;
                }
            }

            // By default cabinet should be built and moved to the suggested location.
            resolved = new ResolvedCabinet() { BuildOption = CabinetBuildOption.BuildAndMove, Path = cabinetPath };

            // If a cabinet cache path was provided, change the location for the cabinet
            // to be built to and check if there is a cabinet that can be reused.
            if (!String.IsNullOrEmpty(this.CabCachePath))
            {
                var cabinetName = Path.GetFileName(cabinetPath);
                resolved.Path = Path.Combine(this.CabCachePath, cabinetName);

                if (CheckFileExists(resolved.Path))
                {
                    // Assume that none of the following are true:
                    // 1. any files are added or removed
                    // 2. order of files changed or names changed
                    // 3. modified time changed
                    var cabinetValid = true;

                    var cabinet = new Cabinet(resolved.Path);
                    var fileList = cabinet.Enumerate();

                    if (filesWithPath.Count() != fileList.Count)
                    {
                        cabinetValid = false;
                    }
                    else
                    {
                        var i = 0;
                        foreach (var file in filesWithPath)
                        {
                            // First check that the file identifiers match because that is quick and easy.
                            var cabFileInfo = fileList[i];
                            cabinetValid = (cabFileInfo.FileId == file.Id);
                            if (cabinetValid)
                            {
                                // Still valid so ensure the file sizes are the same.
                                var fileInfo = new FileInfo(file.Path);
                                cabinetValid = (cabFileInfo.Size == fileInfo.Length);
                                if (cabinetValid)
                                {
                                    // Still valid so ensure the source time stamp hasn't changed.
                                    cabinetValid = cabFileInfo.SameAsDateTime(fileInfo.LastWriteTime);
                                }
                            }

                            if (!cabinetValid)
                            {
                                break;
                            }

                            i++;
                        }
                    }

                    resolved.BuildOption = cabinetValid ? CabinetBuildOption.Copy : CabinetBuildOption.BuildAndCopy;
                }
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
