// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// Harvest WiX authoring for the outputs of a VS project.
    /// </summary>
    internal class VSProjectHarvester : BaseHarvesterExtension
    {
        // These format strings are used for generated element identifiers.
        //   {0} = project name
        //   {1} = POG name
        //   {2} = file name
        private const string DirectoryIdFormat = "{0}.{1}";
        private const string ComponentIdFormat = "{0}.{1}.{2}";
        private const string FileIdFormat = "{0}.{1}.{2}";
        private const string VariableFormat = "$(var.{0}.{1})";
        private const string WixVariableFormat = "!(wix.{0}.{1})";

        private const string ComponentPrefix = "cmp";
        private const string DirectoryPrefix = "dir";
        private const string FilePrefix = "fil";

        private string projectGUID;
        private string directoryIds;
        private string directoryRefSeed;
        private string projectName;
        private string configuration;
        private string platform;
        private bool setUniqueIdentifiers;
        private GenerateType generateType;
        private bool generateWixVars;


        private static readonly ProjectOutputGroup[] allOutputGroups = new ProjectOutputGroup[]
        {
            new ProjectOutputGroup("Binaries",   "BuiltProjectOutputGroup",         "TargetDir"),
            new ProjectOutputGroup("Symbols",    "DebugSymbolsProjectOutputGroup",  "TargetDir"),
            new ProjectOutputGroup("Documents",  "DocumentationProjectOutputGroup", "ProjectDir"),
            new ProjectOutputGroup("Satellites", "SatelliteDllsProjectOutputGroup", "TargetDir"),
            new ProjectOutputGroup("Sources",    "SourceFilesProjectOutputGroup",   "ProjectDir"),
            new ProjectOutputGroup("Content",    "ContentFilesProjectOutputGroup",  "ProjectDir"),
        };

        private string[] outputGroups;

        /// <summary>
        /// Instantiate a new VSProjectHarvester.
        /// </summary>
        /// <param name="outputGroups">List of project output groups to harvest.</param>
        public VSProjectHarvester(string[] outputGroups)
        {
            if (outputGroups == null)
            {
                throw new ArgumentNullException("outputGroups");
            }

            this.outputGroups = outputGroups;
        }

        /// <summary>
        /// Gets or sets the configuration to set when harvesting.
        /// </summary>
        /// <value>The configuration to set when harvesting.</value>
        public string Configuration
        {
            get { return this.configuration; }
            set { this.configuration = value; }
        }

        public string DirectoryIds
        {
            get { return this.directoryIds; }
            set { this.directoryIds = value; }
        }

        /// <summary>
        /// Gets or sets what type of elements are to be generated.
        /// </summary>
        /// <value>The type of elements being generated.</value>
        public GenerateType GenerateType
        {
            get { return this.generateType; }
            set { this.generateType = value; }
        }

        /// <summary>
        /// Gets or sets whether or not to use wix variables.
        /// </summary>
        /// <value>Whether or not to use wix variables.</value>
        public bool GenerateWixVars
        {
            get { return this.generateWixVars; }
            set { this.generateWixVars = value; }
        }

        /// <summary>
        /// Gets or sets the location to load MSBuild from.
        /// </summary>
        public string MsbuildBinPath { get; set; }

        /// <summary>
        /// Gets or sets the platform to set when harvesting.
        /// </summary>
        /// <value>The platform to set when harvesting.</value>
        public string Platform
        {
            get { return this.platform; }
            set { this.platform = value; }
        }

        /// <summary>
        /// Gets or sets the project name to use in wix variables.
        /// </summary>
        /// <value>The project name to use in wix variables.</value>
        public string ProjectName
        {
            get { return this.projectName; }
            set { this.projectName = value; }
        }

        /// <summary>
        /// Gets or sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Gets or sets whether to ignore MsbuildBinPath when the project file specifies a known MSBuild version.
        /// </summary>
        public bool UseToolsVersion { get; set; }

        /// <summary>
        /// Gets a list of friendly output group names that will be recognized on the command-line.
        /// </summary>
        /// <returns>Array of output group names.</returns>
        public static string[] GetOutputGroupNames()
        {
            string[] names = new string[VSProjectHarvester.allOutputGroups.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = VSProjectHarvester.allOutputGroups[i].Name;
            }
            return names;
        }

        /// <summary>
        /// Harvest a VS project.
        /// </summary>
        /// <param name="argument">The path of the VS project file.</param>
        /// <returns>The harvested directory.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            if (!System.IO.File.Exists(argument))
            {
                throw new FileNotFoundException(argument);
            }

            // Match specified output group names to available POG structures
            // and collect list of build output groups to pass to MSBuild.
            ProjectOutputGroup[] pogs = new ProjectOutputGroup[this.outputGroups.Length];
            string[] buildOutputGroups = new string[this.outputGroups.Length];
            for (int i = 0; i < this.outputGroups.Length; i++)
            {
                foreach (ProjectOutputGroup pog in VSProjectHarvester.allOutputGroups)
                {
                    if (pog.Name == this.outputGroups[i])
                    {
                        pogs[i] = pog;
                        buildOutputGroups[i] = pog.BuildOutputGroup;
                    }
                }

                if (buildOutputGroups[i] == null)
                {
                    throw new WixException(HarvesterErrors.InvalidOutputGroup(this.outputGroups[i]));
                }
            }

            string projectFile = Path.GetFullPath(argument);

            IDictionary buildOutputs = this.GetProjectBuildOutputs(projectFile, buildOutputGroups);

            ArrayList fragmentList = new ArrayList();

            for (int i = 0; i < pogs.Length; i++)
            {
                this.HarvestProjectOutputGroup(projectFile, buildOutputs, pogs[i], fragmentList);
            }

            return (Wix.Fragment[]) fragmentList.ToArray(typeof(Wix.Fragment));
        }

        /// <summary>
        /// Runs MSBuild on a project file to get the list of filenames for the specified output groups.
        /// </summary>
        /// <param name="projectFile">VS MSBuild project file to load.</param>
        /// <param name="buildOutputGroups">List of MSBuild output group names.</param>
        /// <returns>Dictionary mapping output group names to lists of filenames in the group.</returns>
        private IDictionary GetProjectBuildOutputs(string projectFile, string[] buildOutputGroups)
        {
            MSBuildProject project = this.GetMsbuildProject(projectFile);

            project.Load(projectFile);

            IDictionary buildOutputs = new Hashtable();

            string originalDirectory = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(projectFile));
            bool buildSuccess = false;
            try
            {
                buildSuccess = project.Build(projectFile, buildOutputGroups, buildOutputs);
            }
            finally
            {
                System.IO.Directory.SetCurrentDirectory(originalDirectory);
            }

            if (!buildSuccess)
            {
                throw new WixException(HarvesterErrors.BuildFailed());
            }

            this.projectGUID = project.GetEvaluatedProperty("ProjectGuid");

            if (null == this.projectGUID)
            {
                throw new WixException(HarvesterErrors.BuildFailed());
            }

            IDictionary newDictionary = new Dictionary<object, object>();
            foreach (string buildOutput in buildOutputs.Keys)
            {
                IEnumerable buildOutputFiles = buildOutputs[buildOutput] as IEnumerable;

                bool hasFiles = false;

                foreach (object file in buildOutputFiles)
                {
                    hasFiles = true;
                    break;
                }

                // Try the item group if no outputs
                if (!hasFiles)
                {
                    IEnumerable itemFiles = project.GetEvaluatedItemsByName(String.Concat(buildOutput, "Output"));
                    List<object> itemFileList = new List<object>();

                    // Get each BuildItem and add the file path to our list
                    foreach (object itemFile in itemFiles)
                    {
                        itemFileList.Add(project.GetBuildItem(itemFile));
                    }

                    // Use our list for this build output
                    newDictionary.Add(buildOutput, itemFileList);
                }
                else
                {
                    newDictionary.Add(buildOutput, buildOutputFiles);
                }
            }

            return newDictionary;
        }

        /// <summary>
        /// Creates WiX fragments for files in one output group.
        /// </summary>
        /// <param name="projectFile">VS MSBuild project file.</param>
        /// <param name="buildOutputs">Dictionary of build outputs retrieved from an MSBuild run on the project file.</param>
        /// <param name="pog">Project output group parameters.</param>
        /// <param name="fragmentList">List to which generated fragments will be added.</param>
        /// <returns>Count of harvested files.</returns>
        private int HarvestProjectOutputGroup(string projectFile, IDictionary buildOutputs, ProjectOutputGroup pog, IList fragmentList)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectBaseDir = null;

            if (this.ProjectName != null)
            {
                projectName = this.ProjectName;
            }

            string sanitizedProjectName = this.Core.CreateIdentifierFromFilename(projectName);

            Wix.IParentElement harvestParent;

            if (this.GenerateType == GenerateType.Container)
            {
                Wix.Container container = new Wix.Container();
                harvestParent = container;

                container.Name = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else if (this.GenerateType == GenerateType.PayloadGroup)
            {
                Wix.PayloadGroup payloadGroup = new Wix.PayloadGroup();
                harvestParent = payloadGroup;

                payloadGroup.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else if (this.GenerateType == GenerateType.PackageGroup)
            {
                Wix.PackageGroup packageGroup = new Wix.PackageGroup();
                harvestParent = packageGroup;

                packageGroup.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else
            {
                Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
                harvestParent = directoryRef;

                if (!String.IsNullOrEmpty(this.directoryIds))
                {
                    directoryRef.Id = this.directoryIds;
                }
                else if (this.setUniqueIdentifiers)
                {
                    directoryRef.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
                }
                else
                {
                    directoryRef.Id = this.Core.CreateIdentifierFromFilename(String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name));
                }

                this.directoryRefSeed = this.Core.GenerateIdentifier(DirectoryPrefix, this.projectGUID, pog.Name);
            }

            IEnumerable pogFiles = buildOutputs[pog.BuildOutputGroup] as IEnumerable;
            if (pogFiles == null)
            {
                throw new WixException(HarvesterErrors.MissingProjectOutputGroup(
                    projectFile, pog.BuildOutputGroup));
            }

            if (pog.FileSource == "ProjectDir")
            {
                projectBaseDir = Path.GetDirectoryName(projectFile) + "\\";
            }

            int harvestCount = this.HarvestProjectOutputGroupFiles(projectBaseDir, projectName, pog.Name, pog.FileSource, pogFiles, harvestParent);

            if (this.GenerateType == GenerateType.Container)
            {
                // harvestParent must be a Container at this point
                Wix.Container container = harvestParent as Wix.Container;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(container);
                fragmentList.Add(fragment);
            }
            else if (this.GenerateType == GenerateType.PackageGroup)
            {
                // harvestParent must be a PackageGroup at this point
                Wix.PackageGroup packageGroup = harvestParent as Wix.PackageGroup;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(packageGroup);
                fragmentList.Add(fragment);
            }
            else if (this.GenerateType == GenerateType.PayloadGroup)
            {
                // harvestParent must be a Container at this point
                Wix.PayloadGroup payloadGroup = harvestParent as Wix.PayloadGroup;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(payloadGroup);
                fragmentList.Add(fragment);
            }
            else
            {
                // harvestParent must be a DirectoryRef at this point
                Wix.DirectoryRef directoryRef = harvestParent as Wix.DirectoryRef;

                if (harvestCount > 0)
                {
                    Wix.Fragment drf = new Wix.Fragment();
                    drf.AddChild(directoryRef);
                    fragmentList.Add(drf);
                }

                Wix.ComponentGroup cg = new Wix.ComponentGroup();

                if (this.setUniqueIdentifiers || !String.IsNullOrEmpty(this.directoryIds))
                {
                    cg.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
                }
                else
                {
                    cg.Id = directoryRef.Id;
                }

                if (harvestCount > 0)
                {
                    this.AddComponentsToComponentGroup(directoryRef, cg);
                }

                Wix.Fragment cgf = new Wix.Fragment();
                cgf.AddChild(cg);
                fragmentList.Add(cgf);
            }

            return harvestCount;
        }

        /// <summary>
        /// Add all Components in an element tree to a ComponentGroup.
        /// </summary>
        /// <param name="parent">Parent of an element tree that will be searched for Components.</param>
        /// <param name="cg">The ComponentGroup the Components will be added to.</param>
        private void AddComponentsToComponentGroup(Wix.IParentElement parent, Wix.ComponentGroup cg)
        {
            foreach (Wix.ISchemaElement childElement in parent.Children)
            {
                Wix.Component c = childElement as Wix.Component;
                if (c != null)
                {
                    Wix.ComponentRef cr = new Wix.ComponentRef();
                    cr.Id = c.Id;
                    cg.AddChild(cr);
                }
                else
                {
                    Wix.IParentElement p = childElement as Wix.IParentElement;
                    if (p != null)
                    {
                        this.AddComponentsToComponentGroup(p, cg);
                    }
                }
            }
        }

        /// <summary>
        /// Harvest files from one output group of a VS project.
        /// </summary>
        /// <param name="baseDir">The base directory of the files.</param>
        /// <param name="projectName">Name of the project, to be used as a prefix for generated identifiers.</param>
        /// <param name="pogName">Name of the project output group, used for generating identifiers for WiX elements.</param>
        /// <param name="pogFileSource">The ProjectOutputGroup file source.</param>
        /// <param name="outputGroupFiles">The files from one output group to harvest.</param>
        /// <param name="parent">The parent element that will contain the components of the harvested files.</param>
        /// <returns>The number of files harvested.</returns>
        private int HarvestProjectOutputGroupFiles(string baseDir, string projectName, string pogName, string pogFileSource, IEnumerable outputGroupFiles, Wix.IParentElement parent)
        {
            int fileCount = 0;

            Wix.ISchemaElement exeFile = null;
            Wix.ISchemaElement dllFile = null;
            Wix.ISchemaElement appConfigFile = null;

            // Keep track of files inserted
            // Files can have different absolute paths but get mapped to the same SourceFile
            // after the project variables have been used. For example, a WiX project that
            // is building multiple cultures will have many output MSIs/MSMs, but will all get
            // mapped to $(var.ProjName.TargetDir)\ProjName.msm. These duplicates would
            // prevent generated code from compiling.
            Dictionary<string, bool> seenList = new Dictionary<string,bool>();

            foreach (object output in outputGroupFiles)
            {
                string filePath = output.ToString();
                string fileName = Path.GetFileName(filePath);
                string fileDir = Path.GetDirectoryName(filePath);
                string link = null;

                MethodInfo getMetadataMethod = output.GetType().GetMethod("GetMetadata");
                if (getMetadataMethod != null)
                {
                    link = (string)getMetadataMethod.Invoke(output, new object[] { "Link" });
                    if (!String.IsNullOrEmpty(link))
                    {
                        fileDir = Path.GetDirectoryName(Path.Combine(baseDir, link));
                    }
                }

                Wix.IParentElement parentDir = parent;
                // Ignore Containers and PayloadGroups because they do not have a nested structure.
                if (baseDir != null && !String.Equals(Path.GetDirectoryName(baseDir), fileDir, StringComparison.OrdinalIgnoreCase)
                    && this.GenerateType != GenerateType.Container && this.GenerateType != GenerateType.PackageGroup && this.GenerateType != GenerateType.PayloadGroup)
                {
                    Uri baseUri = new Uri(baseDir);
                    Uri relativeUri = baseUri.MakeRelativeUri(new Uri(fileDir));
                    parentDir = this.GetSubDirElement(parentDir, relativeUri);
                }

                string parentDirId = null;

                if (parentDir is Wix.DirectoryRef)
                {
                    parentDirId = this.directoryRefSeed;
                }
                else if (parentDir is Wix.Directory)
                {
                    parentDirId = ((Wix.Directory)parentDir).Id;
                }

                if (this.GenerateType == GenerateType.Container || this.GenerateType == GenerateType.PayloadGroup)
                {
                    Wix.Payload payload = new Wix.Payload();

                    this.HarvestProjectOutputGroupPayloadFile(baseDir, projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, payload, seenList);
                }
                else if (this.GenerateType == GenerateType.PackageGroup)
                {
                    this.HarvestProjectOutputGroupPackage(projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, seenList);
                }
                else
                {
                    Wix.Component component = new Wix.Component();
                    Wix.File file = new Wix.File();

                    this.HarvestProjectOutputGroupFile(baseDir, projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, parentDirId, component, file, seenList);

                    if (String.Equals(Path.GetExtension(file.Source), ".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exeFile = file;
                    }
                    else if (String.Equals(Path.GetExtension(file.Source), ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        dllFile = file;
                    }
                    else if (file.Source.EndsWith("app.config", StringComparison.OrdinalIgnoreCase))
                    {
                        appConfigFile = file;
                    }
                }

                fileCount++;
            }

            // if there was no exe file found fallback on the dll file found
            if (exeFile == null && dllFile != null)
            {
                exeFile = dllFile;
            }

            // Special case for the app.config file in the Binaries POG...
            // The POG refers to the files in the OBJ directory, while the
            // generated WiX code references them in the bin directory.
            // The app.config file gets renamed to match the exe name.
            if ("Binaries" == pogName && null != exeFile && null != appConfigFile)
            {
                if (appConfigFile is Wix.File)
                {
                    Wix.File appConfigFileAsWixFile = appConfigFile as Wix.File;
                    Wix.File exeFileAsWixFile = exeFile as Wix.File;
                    // Case insensitive replace
                    appConfigFileAsWixFile.Source = Regex.Replace(appConfigFileAsWixFile.Source, @"app\.config", Path.GetFileName(exeFileAsWixFile.Source) + ".config", RegexOptions.IgnoreCase);
                }
            }

            return fileCount;
        }

        private void HarvestProjectOutputGroupFile(string baseDir, string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, string parentDirId, Wix.Component component, Wix.File file, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
            {
                Wix.Directory locDirectory = new Wix.Directory();

                locDirectory.Name = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                file.Source = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", locDirectory.Name, "\\", Path.GetFileName(filePath));

                if (!seenList.ContainsKey(file.Source))
                {
                    parentDir.AddChild(locDirectory);
                    locDirectory.AddChild(component);
                    component.AddChild(file);
                    seenList.Add(file.Source, true);

                    if (this.setUniqueIdentifiers)
                    {
                        locDirectory.Id = this.Core.GenerateIdentifier(DirectoryPrefix, parentDirId, locDirectory.Name);
                        file.Id = this.Core.GenerateIdentifier(FilePrefix, locDirectory.Id, fileName);
                        component.Id = this.Core.GenerateIdentifier(ComponentPrefix, locDirectory.Id, file.Id);
                    }
                    else
                    {
                        locDirectory.Id = this.Core.CreateIdentifierFromFilename(String.Format(DirectoryIdFormat, (parentDir is Wix.DirectoryRef) ? ((Wix.DirectoryRef)parentDir).Id : parentDirId, locDirectory.Name));
                        file.Id = this.Core.CreateIdentifierFromFilename(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.FileIdFormat, projectName, pogName, String.Concat(locDirectory.Name, ".", fileName)));
                        component.Id = this.Core.CreateIdentifierFromFilename(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.ComponentIdFormat, projectName, pogName, String.Concat(locDirectory.Name, ".", fileName)));
                    }
                }
            }
            else
            {
                file.Source = GenerateSourceFilePath(baseDir, projectName, pogFileSource, filePath, link, varFormat);

                if (!seenList.ContainsKey(file.Source))
                {
                    component.AddChild(file);
                    parentDir.AddChild(component);
                    seenList.Add(file.Source, true);

                    if (this.setUniqueIdentifiers)
                    {
                        file.Id = this.Core.GenerateIdentifier(FilePrefix, parentDirId, fileName);
                        component.Id = this.Core.GenerateIdentifier(ComponentPrefix, parentDirId, file.Id);
                    }
                    else
                    {
                        file.Id = this.Core.CreateIdentifierFromFilename(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.FileIdFormat, projectName, pogName, fileName));
                        component.Id = this.Core.CreateIdentifierFromFilename(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.ComponentIdFormat, projectName, pogName, fileName));
                    }
                }
            }
        }

        private void HarvestProjectOutputGroupPackage(string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Binaries", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Wix.ExePackage exePackage = new Wix.ExePackage();
                    exePackage.SourceFile =  String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
                    if (!seenList.ContainsKey(exePackage.SourceFile))
                    {
                        parentDir.AddChild(exePackage);
                        seenList.Add(exePackage.SourceFile, true);
                    }
                }
                else if (String.Equals(Path.GetExtension(filePath), ".msi", StringComparison.OrdinalIgnoreCase))
                {
                    Wix.MsiPackage msiPackage = new Wix.MsiPackage();
                    msiPackage.SourceFile = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
                    if (!seenList.ContainsKey(msiPackage.SourceFile))
                    {
                        parentDir.AddChild(msiPackage);
                        seenList.Add(msiPackage.SourceFile, true);
                    }
                }
            }
        }

        private void HarvestProjectOutputGroupPayloadFile(string baseDir, string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, Wix.Payload file, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
            {
                string locDirectoryName = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                file.SourceFile = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", locDirectoryName, "\\", Path.GetFileName(filePath));

                if (!seenList.ContainsKey(file.SourceFile))
                {
                    parentDir.AddChild(file);
                    seenList.Add(file.SourceFile, true);
                }
            }
            else
            {
                file.SourceFile = GenerateSourceFilePath(baseDir, projectName, pogFileSource, filePath, link, varFormat);

                if (!seenList.ContainsKey(file.SourceFile))
                {
                    parentDir.AddChild(file);
                    seenList.Add(file.SourceFile, true);
                }
            }
        }

        /// <summary>
        /// Helper function to generates a source file path when harvesting files.
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="projectName"></param>
        /// <param name="pogFileSource"></param>
        /// <param name="filePath"></param>
        /// <param name="link"></param>
        /// <param name="varFormat"></param>
        /// <returns></returns>
        private static string GenerateSourceFilePath(string baseDir, string projectName, string pogFileSource, string filePath, string link, string varFormat)
        {
            string ret;

            if (null == baseDir && !String.IsNullOrEmpty(link))
            {
                // This needs to be the absolute path as a link can be located anywhere.
                ret = filePath;
            }
            else if (null == baseDir)
            {
                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
            }
            else if (filePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", filePath.Substring(baseDir.Length));
            }
            else
            {
                // come up with a relative path to the file
                Uri sourcePathUri = new Uri(filePath);
                Uri baseDirUri = new Uri(baseDir);
                Uri sourceRelativeUri = baseDirUri.MakeRelativeUri(sourcePathUri);
                string relativePath = sourceRelativeUri.ToString().Replace('/', Path.DirectorySeparatorChar);
                if (!sourceRelativeUri.UserEscaped)
                {
                    relativePath = Uri.UnescapeDataString(relativePath);
                }

                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", relativePath);
            }

            return ret;
        }

        /// <summary>
        /// Gets a Directory element corresponding to a relative subdirectory within the project,
        /// either by locating a suitable existing Directory or creating a new one.
        /// </summary>
        /// <param name="parentDir">The parent element which the subdirectory is relative to.</param>
        /// <param name="relativeUri">Relative path of the subdirectory.</param>
        /// <returns>Directory element for the relative path.</returns>
        private Wix.IParentElement GetSubDirElement(Wix.IParentElement parentDir, Uri relativeUri)
        {
            string[] segments = relativeUri.ToString().Split('\\', '/');
            string firstSubDirName = Uri.UnescapeDataString(segments[0]);
            DirectoryAttributeAccessor subDir = null;

            if (String.Equals(firstSubDirName, "..", StringComparison.Ordinal))
            {
                return parentDir;
            }

            Type directoryType;
            Type directoryRefType;
            if (parentDir is Wix.Directory || parentDir is Wix.DirectoryRef)
            {
                directoryType = typeof(Wix.Directory);
                directoryRefType = typeof(Wix.DirectoryRef);
            }
            else
            {
                throw new ArgumentException("GetSubDirElement parentDir");
            }

            // Search for an existing directory element.
            foreach (Wix.ISchemaElement childElement in parentDir.Children)
            {
                if(VSProjectHarvester.AreTypesEquivalent(childElement.GetType(), directoryType))
                {
                    DirectoryAttributeAccessor childDir = new DirectoryAttributeAccessor(childElement);
                    if (String.Equals(childDir.Name, firstSubDirName, StringComparison.OrdinalIgnoreCase))
                    {
                        subDir = childDir;
                        break;
                    }
                }
            }

            if (subDir == null)
            {
                string parentId = null;
                DirectoryAttributeAccessor parentDirectory = null;
                DirectoryAttributeAccessor parentDirectoryRef = null;

                if (VSProjectHarvester.AreTypesEquivalent(parentDir.GetType(), directoryType))
                {
                    parentDirectory = new DirectoryAttributeAccessor((Wix.ISchemaElement)parentDir);
                }
                else if (VSProjectHarvester.AreTypesEquivalent(parentDir.GetType(), directoryRefType))
                {
                    parentDirectoryRef = new DirectoryAttributeAccessor((Wix.ISchemaElement)parentDir);
                }

                if (parentDirectory != null)
                {
                    parentId = parentDirectory.Id;
                }
                else if (parentDirectoryRef != null)
                {
                    if (this.setUniqueIdentifiers)
                    {
                        //Use the GUID of the project instead of the project name to help keep things stable.
                        parentId = this.directoryRefSeed;
                    }
                    else
                    {
                        parentId = parentDirectoryRef.Id;
                    }
                }

                Wix.ISchemaElement newDirectory = (Wix.ISchemaElement)directoryType.GetConstructor(new Type[] { }).Invoke(null);
                subDir = new DirectoryAttributeAccessor(newDirectory);

                if (this.setUniqueIdentifiers)
                {
                    subDir.Id = this.Core.GenerateIdentifier(DirectoryPrefix, parentId, firstSubDirName);
                }
                else
                {
                    subDir.Id = String.Format(DirectoryIdFormat, parentId, firstSubDirName);
                }

                subDir.Name = firstSubDirName;

                parentDir.AddChild(subDir.Element);
            }

            if (segments.Length == 1)
            {
                return subDir.ElementAsParent;
            }
            else
            {
                Uri nextRelativeUri = new Uri(Uri.UnescapeDataString(relativeUri.ToString()).Substring(firstSubDirName.Length + 1), UriKind.Relative);
                return this.GetSubDirElement(subDir.ElementAsParent, nextRelativeUri);
            }
        }

        private MSBuildProject GetMsbuildProject(string projectFile)
        {
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(projectFile);
            }
            catch (Exception e)
            {
                throw new WixException(HarvesterErrors.CannotLoadProject(projectFile, e.Message));
            }

            string version = null;

            if (this.UseToolsVersion)
            {
                foreach (XmlNode child in document.ChildNodes)
                {
                    if (String.Equals(child.Name, "Project", StringComparison.Ordinal) && child.Attributes != null)
                    {
                        XmlNode toolsVersionAttribute = child.Attributes["ToolsVersion"];
                        if (toolsVersionAttribute != null)
                        {
                            version = toolsVersionAttribute.Value;
                            this.Core.Messaging.Write(HarvesterVerboses.FoundToolsVersion(version));

                            break;
                        }
                    }
                }

                switch (version)
                {
                    case "4.0":
                        version = "4.0.0.0";
                        break;
                    case "12.0":
                        version = "12.0.0.0";
                        break;
                    case "14.0":
                        version = "14.0.0.0";
                        break;
                    default:
                        if (String.IsNullOrEmpty(this.MsbuildBinPath))
                        {
                            throw new WixException(HarvesterErrors.MsbuildBinPathRequired(version ?? "(none)"));
                        }

                        version = null;
                        break;
                }
            }

            var project = this.ConstructMsbuild40Project(version);
            return project;
        }

        private Assembly ResolveFromMsbuildBinPath(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            var assemblyPath = Path.Combine(this.MsbuildBinPath, $"{assemblyName.Name}.dll");
            if (!File.Exists(assemblyPath))
            {
                return null;
            }

            return Assembly.LoadFrom(assemblyPath);
        }

        private MSBuildProject ConstructMsbuild40Project(string loadVersion)
        {
            const string MSBuildEngineAssemblyName = "Microsoft.Build, Version={0}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            const string MSBuildFrameworkAssemblyName = "Microsoft.Build.Framework, Version={0}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            Assembly msbuildAssembly;
            Assembly msbuildFrameworkAssembly;

            if (loadVersion == null)
            {
                this.Core.Messaging.Write(HarvesterVerboses.LoadingProjectWithBinPath(this.MsbuildBinPath));
                AppDomain.CurrentDomain.AssemblyResolve += this.ResolveFromMsbuildBinPath;

                try
                {
                    msbuildAssembly = Assembly.Load("Microsoft.Build");
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotLoadMSBuildAssembly(e.Message));
                }

                try
                {
                    msbuildFrameworkAssembly = Assembly.Load("Microsoft.Build.Framework");
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotLoadMSBuildAssembly(e.Message));
                }
            }
            else
            {
                this.Core.Messaging.Write(HarvesterVerboses.LoadingProjectWithVersion(loadVersion));

                try
                {
                    msbuildAssembly = Assembly.Load(String.Format(MSBuildEngineAssemblyName, loadVersion));
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotLoadMSBuildAssembly(e.Message));
                }

                try
                {
                    msbuildFrameworkAssembly = Assembly.Load(String.Format(MSBuildFrameworkAssemblyName, loadVersion));
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotLoadMSBuildAssembly(e.Message));
                }
            }

            Type projectType;
            Type buildItemType;

            Type buildManagerType;
            Type buildParametersType;
            Type buildRequestDataFlagsType;
            Type buildRequestDataType;
            Type hostServicesType;
            Type projectCollectionType;
            Type projectInstanceType;

            Type writeHandlerType;
            Type colorSetterType;
            Type colorResetterType;
            Type loggerVerbosityType;
            Type consoleLoggerType;
            Type iLoggerType;

            try
            {
                buildItemType = msbuildAssembly.GetType("Microsoft.Build.Execution.ProjectItemInstance", true);
                projectType = msbuildAssembly.GetType("Microsoft.Build.Evaluation.Project", true);

                buildManagerType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildManager", true);
                buildParametersType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildParameters", true);
                buildRequestDataFlagsType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildRequestDataFlags", true);
                buildRequestDataType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildRequestData", true);
                hostServicesType = msbuildAssembly.GetType("Microsoft.Build.Execution.HostServices", true);
                projectCollectionType = msbuildAssembly.GetType("Microsoft.Build.Evaluation.ProjectCollection", true);
                projectInstanceType = msbuildAssembly.GetType("Microsoft.Build.Execution.ProjectInstance", true);

                writeHandlerType = msbuildAssembly.GetType("Microsoft.Build.Logging.WriteHandler", true);
                colorSetterType = msbuildAssembly.GetType("Microsoft.Build.Logging.ColorSetter", true);
                colorResetterType = msbuildAssembly.GetType("Microsoft.Build.Logging.ColorResetter", true);
                loggerVerbosityType = msbuildFrameworkAssembly.GetType("Microsoft.Build.Framework.LoggerVerbosity", true);
                consoleLoggerType = msbuildAssembly.GetType("Microsoft.Build.Logging.ConsoleLogger", true);
                iLoggerType = msbuildFrameworkAssembly.GetType("Microsoft.Build.Framework.ILogger", true);
            }
            catch (TargetInvocationException tie)
            {
                throw new WixException(HarvesterErrors.CannotLoadMSBuildEngine(tie.InnerException.Message));
            }
            catch (Exception e)
            {
                throw new WixException(HarvesterErrors.CannotLoadMSBuildEngine(e.Message));
            }

            MSBuild40Types types = new MSBuild40Types();
            types.buildManagerType = buildManagerType;
            types.buildParametersType = buildParametersType;
            types.buildRequestDataFlagsType = buildRequestDataFlagsType;
            types.buildRequestDataType = buildRequestDataType;
            types.hostServicesType = hostServicesType;
            types.projectCollectionType = projectCollectionType;
            types.projectInstanceType = projectInstanceType;
            types.writeHandlerType = writeHandlerType;
            types.colorSetterType = colorSetterType;
            types.colorResetterType = colorResetterType;
            types.loggerVerbosityType = loggerVerbosityType;
            types.consoleLoggerType = consoleLoggerType;
            types.iLoggerType = iLoggerType;
            return new MSBuild40Project(null, projectType, buildItemType, loadVersion, types, this.Core, this.configuration, this.platform);
        }

        private static bool AreTypesEquivalent(Type a, Type b)
        {
            return (a == b) || (a.IsAssignableFrom(b) && b.IsAssignableFrom(a));
        }

        private abstract class MSBuildProject
        {
            protected Type projectType;
            protected Type buildItemType;
            protected object project;
            private string loadVersion;

            public MSBuildProject(object project, Type projectType, Type buildItemType, string loadVersion)
            {
                this.project = project;
                this.projectType = projectType;
                this.buildItemType = buildItemType;
                this.loadVersion = loadVersion;
            }

            public string LoadVersion
            {
                get { return this.loadVersion; }
            }

            public abstract bool Build(string projectFileName, string[] targetNames, IDictionary targetOutputs);

            public abstract MSBuildProjectItemType GetBuildItem(object buildItem);

            public abstract IEnumerable GetEvaluatedItemsByName(string itemName);

            public abstract string GetEvaluatedProperty(string propertyName);

            public abstract void Load(string projectFileName);
        }

        private abstract class MSBuildProjectItemType
        {
            public MSBuildProjectItemType(object buildItem)
            {
                this.buildItem = buildItem;
            }

            public abstract override string ToString();

            public abstract string GetMetadata(string name);

            protected object buildItem;
        }


        private struct MSBuild40Types
        {
            public Type buildManagerType;
            public Type buildParametersType;
            public Type buildRequestDataFlagsType;
            public Type buildRequestDataType;
            public Type hostServicesType;
            public Type projectCollectionType;
            public Type projectInstanceType;
            public Type writeHandlerType;
            public Type colorSetterType;
            public Type colorResetterType;
            public Type loggerVerbosityType;
            public Type consoleLoggerType;
            public Type iLoggerType;
        }

        private class MSBuild40Project : MSBuildProject
        {
            private MSBuild40Types types;
            private object projectCollection;
            private object currentProjectInstance;
            private object buildManager;
            private object buildParameters;
            private IHarvesterCore harvesterCore;

            public MSBuild40Project(object project, Type projectType, Type buildItemType, string loadVersion, MSBuild40Types types, IHarvesterCore harvesterCore, string configuration, string platform)
                : base(project, projectType, buildItemType, loadVersion)
            {
                this.types = types;
                this.harvesterCore = harvesterCore;

                this.buildParameters = this.types.buildParametersType.GetConstructor(new Type[] { }).Invoke(null);

                try
                {
                    var loggers = this.CreateLoggers();

                    // this.buildParameters.Loggers = loggers;
                    this.types.buildParametersType.GetProperty("Loggers").SetValue(this.buildParameters, loggers, null);
                }
                catch (TargetInvocationException tie)
                {
                    if (this.harvesterCore != null)
                    {
                        this.harvesterCore.Messaging.Write(HarvesterWarnings.NoLogger(tie.InnerException.Message));
                    }
                }
                catch (Exception e)
                {
                    if (this.harvesterCore != null)
                    {
                        this.harvesterCore.Messaging.Write(HarvesterWarnings.NoLogger(e.Message));
                    }
                }

                this.buildManager = this.types.buildManagerType.GetConstructor(new Type[] { }).Invoke(null);

                if (configuration != null || platform != null)
                {
                    Dictionary<string, string> globalVariables = new Dictionary<string, string>();
                    if (configuration != null)
                    {
                        globalVariables.Add("Configuration", configuration);
                    }

                    if (platform != null)
                    {
                        globalVariables.Add("Platform", platform);
                    }

                    this.projectCollection = this.types.projectCollectionType.GetConstructor(new Type[] { typeof(IDictionary<string, string>) }).Invoke(new object[] { globalVariables });
                }
                else
                {
                    this.projectCollection = this.types.projectCollectionType.GetConstructor(new Type[] {}).Invoke(null);
                }
            }

            private object CreateLoggers()
            {
                var logger = new HarvestLogger(this.harvesterCore.Messaging);
                var loggerVerbosity = Enum.Parse(this.types.loggerVerbosityType, "Minimal");
                var writeHandler = Delegate.CreateDelegate(this.types.writeHandlerType, logger, nameof(logger.LogMessage));
                var colorSetter = Delegate.CreateDelegate(this.types.colorSetterType, logger, nameof(logger.SetColor));
                var colorResetter = Delegate.CreateDelegate(this.types.colorResetterType, logger, nameof(logger.ResetColor));

                var consoleLoggerCtor = this.types.consoleLoggerType.GetConstructor(new Type[] {
                    this.types.loggerVerbosityType,
                    this.types.writeHandlerType,
                    this.types.colorSetterType,
                    this.types.colorResetterType,
                });
                var consoleLogger = consoleLoggerCtor.Invoke(new object[] { loggerVerbosity, writeHandler, colorSetter, colorResetter });

                var loggers = Array.CreateInstance(this.types.iLoggerType, 1);
                loggers.SetValue(consoleLogger, 0);

                return loggers;
            }

            public override bool Build(string projectFileName, string[] targetNames, IDictionary targetOutputs)
            {
                try
                {
                    // this.buildManager.BeginBuild(this.buildParameters);
                    this.types.buildManagerType.GetMethod("BeginBuild", new Type[] { this.types.buildParametersType }).Invoke(this.buildManager, new object[] { this.buildParameters });

                    // buildRequestData = new BuildRequestData(this.currentProjectInstance, targetNames, null, BuildRequestData.BuildRequestDataFlags.ReplaceExistingProjectInstance);
                    ConstructorInfo buildRequestDataCtor = this.types.buildRequestDataType.GetConstructor(
                        new Type[]
                        {
                            this.types.projectInstanceType, typeof(string[]), this.types.hostServicesType, this.types.buildRequestDataFlagsType
                        });
                    object buildRequestDataFlags = this.types.buildRequestDataFlagsType.GetField("ReplaceExistingProjectInstance").GetRawConstantValue();
                    object buildRequestData = buildRequestDataCtor.Invoke(new object[] { this.currentProjectInstance, targetNames, null, buildRequestDataFlags });

                    // BuildSubmission submission  = this.buildManager.PendBuildRequest(buildRequestData);
                    object submission = this.types.buildManagerType.GetMethod("PendBuildRequest", new Type[] { this.types.buildRequestDataType })
                        .Invoke(this.buildManager, new object[] { buildRequestData });

                    // BuildResult buildResult = submission.Execute();
                    object buildResult = submission.GetType().GetMethod("Execute", new Type[] { }).Invoke(submission, null);

                    // bool buildSucceeded = buildResult.OverallResult == BuildResult.Success;
                    object overallResult = buildResult.GetType().GetProperty("OverallResult").GetValue(buildResult, null);
                    bool buildSucceeded = String.Equals(overallResult.ToString(), "Success", StringComparison.Ordinal);

                    // this.buildManager.EndBuild();
                    this.types.buildManagerType.GetMethod("EndBuild", new Type[] { }).Invoke(this.buildManager, null);

                    // fill in empty lists for each target so that heat will look at the item group later
                    foreach (string target in targetNames)
                    {
                        targetOutputs.Add(target, new List<object>());
                    }

                    return buildSucceeded;
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(HarvesterErrors.CannotBuildProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotBuildProject(projectFileName, e.Message));
                }
            }

            public override MSBuildProjectItemType GetBuildItem(object buildItem)
            {
                return new MSBuild40ProjectItemType(buildItem);
            }

            public override IEnumerable GetEvaluatedItemsByName(string itemName)
            {
                MethodInfo getEvaluatedItem = this.types.projectInstanceType.GetMethod("GetItems", new Type[] { typeof(string) });
                return (IEnumerable)getEvaluatedItem.Invoke(this.currentProjectInstance, new object[] { itemName });
            }

            public override string GetEvaluatedProperty(string propertyName)
            {
                MethodInfo getProperty = this.types.projectInstanceType.GetMethod("GetPropertyValue", new Type[] { typeof(string) });
                return (string)getProperty.Invoke(this.currentProjectInstance, new object[] { propertyName });
            }

            public override void Load(string projectFileName)
            {
                try
                {
                    //this.project = this.projectCollection.LoadProject(projectFileName);
                    this.project = this.types.projectCollectionType.GetMethod("LoadProject", new Type[] { typeof(string) }).Invoke(this.projectCollection, new object[] { projectFileName });

                    // this.currentProjectInstance = this.project.CreateProjectInstance();
                    MethodInfo createProjectInstanceMethod = this.projectType.GetMethod("CreateProjectInstance", new Type[] { });
                    this.currentProjectInstance = createProjectInstanceMethod.Invoke(this.project, null);
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(HarvesterErrors.CannotLoadProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(HarvesterErrors.CannotLoadProject(projectFileName, e.Message));
                }
            }
        }

        private class MSBuild40ProjectItemType : MSBuildProjectItemType
        {
            public MSBuild40ProjectItemType(object buildItem)
                : base(buildItem)
            {
            }

            public override string ToString()
            {
                PropertyInfo includeProperty = this.buildItem.GetType().GetProperty("EvaluatedInclude");
                return (string)includeProperty.GetValue(this.buildItem, null);
            }

            public override string GetMetadata(string name)
            {
                MethodInfo getMetadataMethod = this.buildItem.GetType().GetMethod("GetMetadataValue");
                if (null != getMetadataMethod)
                {
                    return (string)getMetadataMethod.Invoke(this.buildItem, new object[] { name });
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Used internally in the VSProjectHarvester class to encapsulate
        /// the settings for a particular MSBuild "project output group".
        /// </summary>
        private struct ProjectOutputGroup
        {
            public readonly string Name;
            public readonly string BuildOutputGroup;
            public readonly string FileSource;

            /// <summary>
            /// Creates a new project output group.
            /// </summary>
            /// <param name="name">Friendly name used by heat.</param>
            /// <param name="buildOutputGroup">MSBuild's name of the project output group.</param>
            /// <param name="fileSource">VS directory token containing the files of the POG.</param>
            public ProjectOutputGroup(string name, string buildOutputGroup, string fileSource)
            {
                this.Name = name;
                this.BuildOutputGroup = buildOutputGroup;
                this.FileSource = fileSource;
            }
        }

        /// <summary>
        /// Internal class for getting and setting common attrbiutes on
        /// directory elements.
        /// </summary>
        internal class DirectoryAttributeAccessor
        {
            public Wix.ISchemaElement directoryElement;

            public DirectoryAttributeAccessor(Wix.ISchemaElement directoryElement)
            {
                this.directoryElement = directoryElement;
            }

            /// <summary>
            /// Gets the element as a ISchemaElement.
            /// </summary>
            public Wix.ISchemaElement Element
            {
                get { return this.directoryElement; }
            }

            /// <summary>
            /// Gets the element as a IParentElement.
            /// </summary>
            public Wix.IParentElement ElementAsParent
            {
                get { return (Wix.IParentElement)this.directoryElement; }
            }

            /// <summary>
            /// Gets or sets the Id attrbiute.
            /// </summary>
            public string Id
            {
                get
                {
                    if (this.directoryElement is Wix.Directory wixDirectory)
                    {
                        return wixDirectory.Id;
                    }
                    else if (this.directoryElement is Wix.DirectoryRef wixDirectoryRef)
                    {
                        return wixDirectoryRef.Id;
                    }
                    else
                    {
                        throw new WixException(HarvesterErrors.DirectoryAttributeAccessorBadType("Id"));
                    }
                }
                set
                {
                    if (this.directoryElement is Wix.Directory wixDirectory)
                    {
                        wixDirectory.Id = value;
                    }
                    else if (this.directoryElement is Wix.DirectoryRef wixDirectoryRef)
                    {
                        wixDirectoryRef.Id = value;
                    }
                    else
                    {
                        throw new WixException(HarvesterErrors.DirectoryAttributeAccessorBadType("Id"));
                    }
                }
            }

            /// <summary>
            /// Gets or sets the Name attribute.
            /// </summary>
            public string Name
            {
                get
                {
                    if (this.directoryElement is Wix.Directory wixDirectory)
                    {
                        return wixDirectory.Name;
                    }
                    else
                    {
                        throw new WixException(HarvesterErrors.DirectoryAttributeAccessorBadType("Name"));
                    }
                }
                set
                {
                    if (this.directoryElement is Wix.Directory wixDirectory)
                    {
                        wixDirectory.Name = value;
                    }
                    else
                    {
                        throw new WixException(HarvesterErrors.DirectoryAttributeAccessorBadType("Name"));
                    }
                }
            }
        }

        internal class HarvestLogger
        {
            public HarvestLogger(IMessaging messaging)
            {
                this.Color = ConsoleColor.Black;
                this.Messaging = messaging;
            }

            private ConsoleColor Color { get; set; }
            private IMessaging Messaging { get; }

            public void LogMessage(string message)
            {
                if (this.Color == ConsoleColor.Red)
                {
                    this.Messaging.Write(HarvesterErrors.BuildErrorDuringHarvesting(message));
                }
            }

            public void SetColor(ConsoleColor color)
            {
                this.Color = color;
            }

            public void ResetColor()
            {
                this.Color = ConsoleColor.Black;
            }
        }
    }
}
