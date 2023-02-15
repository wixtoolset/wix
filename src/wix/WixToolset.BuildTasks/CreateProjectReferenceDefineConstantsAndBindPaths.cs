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
    /// MSBuild task to create a list of preprocessor defines and bind paths from resolved
    /// project references.
    /// </summary>
    public sealed class CreateProjectReferenceDefineConstantsAndBindPaths : Task
    {
        private static readonly string DirectorySeparatorString = Path.DirectorySeparatorChar.ToString();

        [Required]
        public ITaskItem[] ResolvedProjectReferences { get; set; }

        public ITaskItem[] ProjectConfigurations { get; set; }

        [Output]
        public ITaskItem[] BindPaths { get; private set; }

        [Output]
        public ITaskItem[] DefineConstants { get; private set; }

        public override bool Execute()
        {
            var bindPaths = new Dictionary<string, List<ITaskItem>>(StringComparer.OrdinalIgnoreCase);
            var defineConstants = new SortedDictionary<string, string>();

            foreach (var resolvedReference in this.ResolvedProjectReferences)
            {
                this.AddBindPathsForResolvedReference(bindPaths, resolvedReference);

                this.AddDefineConstantsForResolvedReference(defineConstants, resolvedReference);
            }

            this.BindPaths = bindPaths.Values.SelectMany(bp => bp).ToArray();
            this.DefineConstants = defineConstants.Select(define => new TaskItem(define.Key + "=" + define.Value)).ToArray();

            return true;
        }

        private void AddBindPathsForResolvedReference(IDictionary<string, List<ITaskItem>> bindPathByPaths, ITaskItem resolvedReference)
        {
            // If the BindName was not explicitly provided, try to use the source project's filename
            // as the bind name.
            var name = resolvedReference.GetMetadata("BindName");
            if (String.IsNullOrWhiteSpace(name))
            {
                var projectPath = resolvedReference.GetMetadata("MSBuildSourceProjectFile");
                name = String.IsNullOrWhiteSpace(projectPath) ? String.Empty : Path.GetFileNameWithoutExtension(projectPath);
            }

            var path = resolvedReference.GetMetadata("BindPath");
            if (String.IsNullOrWhiteSpace(path))
            {
                path = Path.GetDirectoryName(resolvedReference.GetMetadata("FullPath"));
            }

            if (!bindPathByPaths.TryGetValue(path, out var bindPathsForPath) ||
                !bindPathsForPath.Any(bp => bp.GetMetadata("BindName").Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                if (bindPathsForPath == null)
                {
                    bindPathsForPath = new List<ITaskItem>
                    {
                        new TaskItem(path)
                    };

                    bindPathByPaths.Add(path, bindPathsForPath);
                }

                if (!String.IsNullOrWhiteSpace(name))
                {
                    var metadata = new Dictionary<string, string> { ["BindName"] = name };
                    bindPathsForPath.Add(new TaskItem(path, metadata));
                }
            }
        }

        private void AddDefineConstantsForResolvedReference(IDictionary<string, string> defineConstants, ITaskItem resolvedReference)
        {
            var configuration = resolvedReference.GetMetadata("Configuration");
            var fullConfiguration = resolvedReference.GetMetadata("FullConfiguration");
            var platform = resolvedReference.GetMetadata("Platform");

            var projectPath = resolvedReference.GetMetadata("MSBuildSourceProjectFile");
            var projectDir = Path.GetDirectoryName(projectPath) + Path.DirectorySeparatorChar;
            var projectExt = Path.GetExtension(projectPath);
            var projectFileName = Path.GetFileName(projectPath);
            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            var referenceName = ToolsCommon.CreateIdentifierFromValue(ToolsCommon.GetMetadataOrDefault(resolvedReference, "Name", projectName));

            var targetPath = resolvedReference.GetMetadata("FullPath");
            var targetDir = Path.GetDirectoryName(targetPath) + Path.DirectorySeparatorChar;
            var targetExt = Path.GetExtension(targetPath);
            var targetFileName = Path.GetFileName(targetPath);
            var targetName = Path.GetFileNameWithoutExtension(targetPath);

            // If there is no configuration metadata on the project reference task item,
            // check for any additional configuration data provided in the optional task property.
            if (String.IsNullOrWhiteSpace(fullConfiguration))
            {
                fullConfiguration = this.FindProjectConfiguration(projectName);
                if (!String.IsNullOrWhiteSpace(fullConfiguration))
                {
                    var typeAndPlatform = fullConfiguration.Split('|');
                    configuration = typeAndPlatform[0];
                    platform = (typeAndPlatform.Length > 1 ? typeAndPlatform[1] : String.Empty);
                }
            }

            // write out the platform/configuration defines
            defineConstants[referenceName + ".Configuration"] = configuration;
            defineConstants[referenceName + ".FullConfiguration"] = fullConfiguration;
            defineConstants[referenceName + ".Platform"] = platform;

            // write out the ProjectX defines
            defineConstants[referenceName + ".ProjectDir"] = projectDir;
            defineConstants[referenceName + ".ProjectExt"] = projectExt;
            defineConstants[referenceName + ".ProjectFileName"] = projectFileName;
            defineConstants[referenceName + ".ProjectName"] = projectName;
            defineConstants[referenceName + ".ProjectPath"] = projectPath;

            // write out the TargetX defines
            var targetDirDefine = referenceName + ".TargetDir";
            if (defineConstants.ContainsKey(targetDirDefine))
            {
                //if target dir was already defined, redefine it as the common root shared by multiple references from the same project
                var commonDir = FindCommonRoot(targetDir, defineConstants[targetDirDefine]);
                if (!String.IsNullOrEmpty(commonDir))
                {
                    targetDir = commonDir;
                }
            }
            defineConstants[targetDirDefine] = CreateProjectReferenceDefineConstantsAndBindPaths.EnsureEndsWithBackslash(targetDir);

            defineConstants[referenceName + ".TargetExt"] = targetExt;
            defineConstants[referenceName + ".TargetFileName"] = targetFileName;
            defineConstants[referenceName + ".TargetName"] = targetName;

            // If target path was already defined, append to it creating a list of multiple references from the same project
            var targetPathDefine = referenceName + ".TargetPath";
            if (defineConstants.TryGetValue(targetPathDefine, out var oldTargetPath))
            {
                if (!targetPath.Equals(oldTargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    defineConstants[targetPathDefine] += "%3B" + targetPath;
                }

                // If there was only one targetpath we need to create its culture specific define
                if (!oldTargetPath.Contains("%3B"))
                {
                    var oldSubFolder = FindSubfolder(oldTargetPath, targetDir, targetFileName);
                    if (!String.IsNullOrEmpty(oldSubFolder))
                    {
                        defineConstants[referenceName + "." + ToolsCommon.CreateIdentifierFromValue(oldSubFolder) + ".TargetPath"] = oldTargetPath;
                    }
                }

                // Create a culture specific define
                var subFolder = FindSubfolder(targetPath, targetDir, targetFileName);
                if (!String.IsNullOrEmpty(subFolder))
                {
                    defineConstants[referenceName + "." + ToolsCommon.CreateIdentifierFromValue(subFolder) + ".TargetPath"] = targetPath;
                }
            }
            else
            {
                defineConstants[targetPathDefine] = targetPath;
            }
        }

        /// <summary>
        /// Look through the configuration data in the ProjectConfigurations property
        /// to find the configuration for a project, if available.
        /// </summary>
        /// <param name="projectName">Name of the project that is being searched for.</param>
        /// <returns>Full configuration spec, for example "Release|Win32".</returns>
        private string FindProjectConfiguration(string projectName)
        {
            var configuration = String.Empty;

            if (this.ProjectConfigurations != null)
            {
                foreach (var configItem in this.ProjectConfigurations)
                {
                    var configProject = configItem.ItemSpec;
                    if (configProject.Length > projectName.Length &&
                        configProject.StartsWith(projectName) &&
                        configProject[projectName.Length] == '=')
                    {
                        configuration = configProject.Substring(projectName.Length + 1);
                        break;
                    }
                }
            }

            return configuration;
        }

        /// <summary>
        /// Finds the common root between two paths
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns>common root on success, empty string on failure</returns>
        private static string FindCommonRoot(string path1, string path2)
        {
            path1 = path1.TrimEnd(Path.DirectorySeparatorChar);
            path2 = path2.TrimEnd(Path.DirectorySeparatorChar);

            while (!String.IsNullOrEmpty(path1))
            {
                for (var searchPath = path2; !String.IsNullOrEmpty(searchPath); searchPath = Path.GetDirectoryName(searchPath))
                {
                    if (path1.Equals(searchPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return searchPath;
                    }
                }

                path1 = Path.GetDirectoryName(path1);
            }

            return path1;
        }

        /// <summary>
        /// Finds the subfolder of a path, excluding a root and filename.
        /// </summary>
        /// <param name="path">Path to examine</param>
        /// <param name="rootPath">Root that must be present </param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string FindSubfolder(string path, string rootPath, string fileName)
        {
            if (Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                path = Path.GetDirectoryName(path);
            }

            if (path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                // cut out the root and return the subpath
                return path.Substring(rootPath.Length).Trim(Path.DirectorySeparatorChar);
            }

            return String.Empty;
        }

        private static string EnsureEndsWithBackslash(string dir)
        {
            if (!dir.EndsWith(DirectorySeparatorString))
            {
                dir += Path.DirectorySeparatorChar;
            }

            return dir;
        }
    }
}
