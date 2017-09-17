// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Msi;

    // TODO: (4.0) Refactor so that these don't need to be copied.
    // Copied verbatim from ext\UtilExtension\wixext\UtilCompiler.cs
    [Flags]
    internal enum WixFileSearchAttributes
    {
        Default = 0x001,
        MinVersionInclusive = 0x002,
        MaxVersionInclusive = 0x004,
        MinSizeInclusive = 0x008,
        MaxSizeInclusive = 0x010,
        MinDateInclusive = 0x020,
        MaxDateInclusive = 0x040,
        WantVersion = 0x080,
        WantExists = 0x100,
        IsDirectory = 0x200,
    }

    [Flags]
    internal enum WixRegistrySearchAttributes
    {
        Raw = 0x01,
        Compatible = 0x02,
        ExpandEnvironmentVariables = 0x04,
        WantValue = 0x08,
        WantExists = 0x10,
        Win64 = 0x20,
    }

    internal enum WixComponentSearchAttributes
    {
        KeyPath = 0x1,
        State = 0x2,
        WantDirectory = 0x4,
    }

    [Flags]
    internal enum WixProductSearchAttributes
    {
        Version = 0x1,
        Language = 0x2,
        State = 0x4,
        Assignment = 0x8,
        UpgradeCode = 0x10,
    }

    /// <summary>
    /// Binder of the WiX toolset.
    /// </summary>
    public sealed class Binder
    {
        private BinderCore core;
        private BinderFileManagerCore fileManagerCore;
        private List<IBinderExtension> extensions;
        private List<IBinderFileManager> fileManagers;
        private List<InspectorExtension> inspectorExtensions;

        public Binder()
        {
            this.DefaultCompressionLevel = CompressionLevel.High;

            this.BindPaths = new List<BindPath>();
            this.TargetBindPaths = new List<BindPath>();
            this.UpdatedBindPaths = new List<BindPath>();

            this.extensions = new List<IBinderExtension>();
            this.fileManagers = new List<IBinderFileManager>();
            this.inspectorExtensions = new List<InspectorExtension>();

            this.Ices = new List<string>();
            this.SuppressIces = new List<string>();
        }

        public string ContentsFile { private get; set; }

        public string OutputsFile { private get; set; }

        public string BuiltOutputsFile { private get; set; }

        public string WixprojectFile { private get; set; }

        /// <summary>
        /// Gets the list of bindpaths.
        /// </summary>
        public List<BindPath> BindPaths { get; private set; }

        /// <summary>
        /// Gets the list of target bindpaths.
        /// </summary>
        public List<BindPath> TargetBindPaths { get; private set; }

        /// <summary>
        /// Gets the list of updated bindpaths.
        /// </summary>
        public List<BindPath> UpdatedBindPaths { get; private set; }

        /// <summary>
        /// Gets or sets the option to enable building binary delta patches.
        /// </summary>
        /// <value>The option to enable building binary delta patches.</value>
        public bool DeltaBinaryPatch { get; set; }

        /// <summary>
        /// Gets or sets the cabinet cache location.
        /// </summary>
        public string CabCachePath { get; set; }

        /// <summary>
        /// Gets or sets the number of threads to use for cabinet creation.
        /// </summary>
        /// <value>The number of threads to use for cabinet creation.</value>
        public int CabbingThreadCount { get; set; }

        /// <summary>
        /// Gets or sets the default compression level to use for cabinets
        /// that don't have their compression level explicitly set.
        /// </summary>
        public CompressionLevel DefaultCompressionLevel { get; set; }

        /// <summary>
        /// Gets and sets the location to save the WixPdb.
        /// </summary>
        /// <value>The location in which to save the WixPdb. Null if the the WixPdb should not be output.</value>
        public string PdbFile { get; set; }

        public List<string> Ices { get; private set; }

        public List<string> SuppressIces { get; private set; }

        /// <summary>
        /// Gets and sets the option to suppress resetting ACLs by the binder.
        /// </summary>
        /// <value>The option to suppress resetting ACLs by the binder.</value>
        public bool SuppressAclReset { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress creating an image for MSI/MSM.
        /// </summary>
        /// <value>The option to suppress creating an image for MSI/MSM.</value>
        public bool SuppressLayout { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress MSI/MSM validation.
        /// </summary>
        /// <value>The option to suppress MSI/MSM validation.</value>
        /// <remarks>This must be set before calling Bind.</remarks>
        public bool SuppressValidation { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress adding _Validation table rows.
        /// </summary>
        public bool SuppressAddingValidationRows { get; set; }

        /// <summary>
        /// Gets or sets the localizer.
        /// </summary>
        /// <value>The localizer.</value>
        public Localizer Localizer { get; set; }

        /// <summary>
        /// Gets or sets the temporary path for the Binder.  If left null, the binder
        /// will use %TEMP% environment variable.
        /// </summary>
        /// <value>Path to temp files.</value>
        public string TempFilesLocation { get; set; }

        /// <summary>
        /// Gets or sets the Wix variable resolver.
        /// </summary>
        /// <value>The Wix variable resolver.</value>
        public WixVariableResolver WixVariableResolver { get; set; }

        /// <summary>
        /// Add a binder extension.
        /// </summary>
        /// <param name="extension">New extension.</param>
        public void AddExtension(IBinderExtension extension)
        {
            this.extensions.Add(extension);
        }

        /// <summary>
        /// Add a file manager extension.
        /// </summary>
        /// <param name="extension">New file manager.</param>
        public void AddExtension(IBinderFileManager extension)
        {
            this.fileManagers.Add(extension);
        }

        /// <summary>
        /// Binds an output.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="file">The Windows Installer file to create.</param>
        /// <remarks>The Binder.DeleteTempFiles method should be called after calling this method.</remarks>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        public bool Bind(Output output, string file)
        {
            // Ensure the cabinet cache path exists if we are going to use it.
            if (!String.IsNullOrEmpty(this.CabCachePath))
            {
                Directory.CreateDirectory(this.CabCachePath);
            }

            this.fileManagerCore = new BinderFileManagerCore();
            this.fileManagerCore.CabCachePath = this.CabCachePath;
            this.fileManagerCore.Output = output;
            this.fileManagerCore.TempFilesLocation = this.TempFilesLocation;
            this.fileManagerCore.AddBindPaths(this.BindPaths, BindStage.Normal);
            this.fileManagerCore.AddBindPaths(this.TargetBindPaths, BindStage.Target);
            this.fileManagerCore.AddBindPaths(this.UpdatedBindPaths, BindStage.Updated);
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                fileManager.Core = this.fileManagerCore;
            }

            this.core = new BinderCore();
            this.core.FileManagerCore = this.fileManagerCore;

            this.WriteBuildInfoTable(output, file);

            // Initialize extensions.
            foreach (IBinderExtension extension in this.extensions)
            {
                extension.Core = this.core;

                extension.Initialize(output);
            }

            // Gather all the wix variables.
            Table wixVariableTable = output.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    this.WixVariableResolver.AddVariable(wixVariableRow);
                }
            }

            IEnumerable<FileTransfer> fileTransfers = null;
            IEnumerable<string> contentPaths = null;

            switch (output.Type)
            {
                case OutputType.Bundle:
                    this.BindBundle(output, file, out fileTransfers, out contentPaths);
                    break;

                case OutputType.Transform:
                    this.BindTransform(output, file);
                    break;

                default:
                    this.BindDatabase(output, file, out fileTransfers, out contentPaths);
                    break;
            }


            // Layout media
            try
            {
                this.LayoutMedia(fileTransfers);
            }
            finally
            {
                if (!String.IsNullOrEmpty(this.ContentsFile) && contentPaths != null)
                {
                    this.CreateContentsFile(this.ContentsFile, contentPaths);
                }

                if (!String.IsNullOrEmpty(this.OutputsFile) && fileTransfers != null)
                {
                    this.CreateOutputsFile(this.OutputsFile, fileTransfers, this.PdbFile);
                }

                if (!String.IsNullOrEmpty(this.BuiltOutputsFile) && fileTransfers != null)
                {
                    this.CreateBuiltOutputsFile(this.BuiltOutputsFile, fileTransfers, this.PdbFile);
                }
            }

            this.core = null;

            return Messaging.Instance.EncounteredError;
        }

        /// <summary>
        /// Does any housekeeping after Bind.
        /// </summary>
        /// <param name="tidy">Whether or not any actual tidying should be done.</param>
        public void Cleanup(bool tidy)
        {
            if (tidy)
            {
                if (!this.DeleteTempFiles())
                {
                    this.core.OnMessage(WixWarnings.FailedToDeleteTempDir(this.TempFilesLocation));
                }
            }
            else
            {
                this.core.OnMessage(WixVerboses.BinderTempDirLocatedAt(this.TempFilesLocation));
            }
        }

        /// <summary>
        /// Cleans up the temp files used by the Binder.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        private bool DeleteTempFiles()
        {
            bool deleted = Common.DeleteTempFiles(this.TempFilesLocation, this.core);
            return deleted;
        }

        /// <summary>
        /// Populates the WixBuildInfo table in an output.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="databaseFile">The output file if OutputFile not set.</param>
        private void WriteBuildInfoTable(Output output, string outputFile)
        {
            Table buildInfoTable = output.EnsureTable(this.core.TableDefinitions["WixBuildInfo"]);
            Row buildInfoRow = buildInfoTable.CreateRow(null);

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
            buildInfoRow[0] = fileVersion.FileVersion;
            buildInfoRow[1] = outputFile;

            if (!String.IsNullOrEmpty(this.WixprojectFile))
            {
                buildInfoRow[2] = this.WixprojectFile;
            }

            if (!String.IsNullOrEmpty(this.PdbFile))
            {
                buildInfoRow[3] = this.PdbFile;
            }
        }

        /// <summary>
        /// Binds a bundle.
        /// </summary>
        /// <param name="bundle">The bundle to bind.</param>
        /// <param name="bundleFile">The bundle to create.</param>
        private void BindBundle(Output bundle, string bundleFile, out IEnumerable<FileTransfer> fileTransfers, out IEnumerable<string> contentPaths)
        {
            BindBundleCommand command = new BindBundleCommand();
            command.DefaultCompressionLevel = this.DefaultCompressionLevel;
            command.Extensions = this.extensions;
            command.FileManagerCore = this.fileManagerCore;
            command.FileManagers = this.fileManagers;
            command.Output = bundle;
            command.OutputPath = bundleFile;
            command.PdbFile = this.PdbFile;
            command.TableDefinitions = this.core.TableDefinitions;
            command.TempFilesLocation = this.TempFilesLocation;
            command.WixVariableResolver = this.WixVariableResolver;
            command.Execute();

            fileTransfers = command.FileTransfers;
            contentPaths = command.ContentFilePaths;
        }

        /// <summary>
        /// Binds a databse.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="databaseFile">The database file to create.</param>
        private void BindDatabase(Output output, string databaseFile, out IEnumerable<FileTransfer> fileTransfers, out IEnumerable<string> contentPaths)
        {
            Validator validator = null;

            // tell the binder about the validator if validation isn't suppressed
            if (!this.SuppressValidation && (OutputType.Module == output.Type || OutputType.Product == output.Type))
            {
                validator = new Validator();
                validator.TempFilesLocation = Path.Combine(this.TempFilesLocation, "validate");

                // set the default cube file
                string lightDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cubePath = (OutputType.Module == output.Type) ? Path.Combine(lightDirectory, "mergemod.cub") : Path.Combine(lightDirectory, "darice.cub");
                validator.AddCubeFile(cubePath);

                // by default, disable ICEs that have equivalent-or-better checks in WiX
                this.SuppressIces.Add("ICE08");
                this.SuppressIces.Add("ICE33");
                this.SuppressIces.Add("ICE47");
                this.SuppressIces.Add("ICE66");

                // set the ICEs
                validator.ICEs = this.Ices.ToArray();

                // set the suppressed ICEs
                validator.SuppressedICEs = this.SuppressIces.ToArray();
            }

            BindDatabaseCommand command = new BindDatabaseCommand();
            command.CabbingThreadCount = this.CabbingThreadCount;
            command.Codepage = this.Localizer == null ? -1 : this.Localizer.Codepage;
            command.DefaultCompressionLevel = this.DefaultCompressionLevel;
            command.Extensions = this.extensions;
            command.FileManagerCore = this.fileManagerCore;
            command.FileManagers = this.fileManagers;
            command.InspectorExtensions = this.inspectorExtensions;
            command.Localizer = this.Localizer;
            command.PdbFile = this.PdbFile;
            command.Output = output;
            command.OutputPath = databaseFile;
            command.SuppressAddingValidationRows = this.SuppressAddingValidationRows;
            command.SuppressLayout = this.SuppressLayout;
            command.TableDefinitions = this.core.TableDefinitions;
            command.TempFilesLocation = this.TempFilesLocation;
            command.Validator = validator;
            command.WixVariableResolver = this.WixVariableResolver;
            command.Execute();

            fileTransfers = command.FileTransfers;
            contentPaths = command.ContentFilePaths;
        }

        /// <summary>
        /// Binds a transform.
        /// </summary>
        /// <param name="transform">The transform to bind.</param>
        /// <param name="outputPath">The transform to create.</param>
        private void BindTransform(Output transform, string outputPath)
        {
            BindTransformCommand command = new BindTransformCommand();
            command.Extensions = this.extensions;
            command.FileManagers = this.fileManagers;
            command.TableDefinitions = this.core.TableDefinitions;
            command.TempFilesLocation = this.TempFilesLocation;
            command.Transform = transform;
            command.OutputPath = outputPath;
            command.Execute();
        }

        /// <summary>
        /// Final step in binding that transfers (moves/copies) all files generated into the appropriate
        /// location in the source image
        /// </summary>
        /// <param name="fileTransfers">List of files to transfer.</param>
        private void LayoutMedia(IEnumerable<FileTransfer> transfers)
        {
            if (null != transfers && transfers.Any())
            {
                this.core.OnMessage(WixVerboses.LayingOutMedia());

                TransferFilesCommand command = new TransferFilesCommand();
                command.FileManagers = this.fileManagers;
                command.FileTransfers = transfers;
                command.SuppressAclReset = this.SuppressAclReset;
                command.Execute();
            }
        }

        /// <summary>
        /// Get the source path of a directory.
        /// </summary>
        /// <param name="directories">All cached directories.</param>
        /// <param name="componentIdGenSeeds">Hash table of Component GUID generation seeds indexed by directory id.</param>
        /// <param name="directory">Directory identifier.</param>
        /// <param name="canonicalize">Canonicalize the path for standard directories.</param>
        /// <returns>Source path of a directory.</returns>
        internal static string GetDirectoryPath(Hashtable directories, Hashtable componentIdGenSeeds, string directory, bool canonicalize)
        {
            if (!directories.Contains(directory))
            {
                throw new WixException(WixErrors.ExpectedDirectory(directory));
            }

            ResolvedDirectory resolvedDirectory = (ResolvedDirectory)directories[directory];

            if (null == resolvedDirectory.Path)
            {
                if (null != componentIdGenSeeds && componentIdGenSeeds.Contains(directory))
                {
                    resolvedDirectory.Path = (string)componentIdGenSeeds[directory];
                }
                else if (canonicalize && WindowsInstallerStandard.IsStandardDirectory(directory))
                {
                    // when canonicalization is on, standard directories are treated equally
                    resolvedDirectory.Path = directory;
                }
                else
                {
                    string name = resolvedDirectory.Name;

                    if (canonicalize && null != name)
                    {
                        name = name.ToLower(CultureInfo.InvariantCulture);
                    }

                    if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                    {
                        resolvedDirectory.Path = name;
                    }
                    else
                    {
                        string parentPath = GetDirectoryPath(directories, componentIdGenSeeds, resolvedDirectory.DirectoryParent, canonicalize);

                        if (null != resolvedDirectory.Name)
                        {
                            resolvedDirectory.Path = Path.Combine(parentPath, name);
                        }
                        else
                        {
                            resolvedDirectory.Path = parentPath;
                        }
                    }
                }
            }

            return resolvedDirectory.Path;
        }

        /// <summary>
        /// Gets the source path of a file.
        /// </summary>
        /// <param name="directories">All cached directories in <see cref="ResolvedDirectory"/>.</param>
        /// <param name="directoryId">Parent directory identifier.</param>
        /// <param name="fileName">File name (in long|source format).</param>
        /// <param name="compressed">Specifies the package is compressed.</param>
        /// <param name="useLongName">Specifies the package uses long file names.</param>
        /// <returns>Source path of file relative to package directory.</returns>
        internal static string GetFileSourcePath(Hashtable directories, string directoryId, string fileName, bool compressed, bool useLongName)
        {
            string fileSourcePath = Installer.GetName(fileName, true, useLongName);

            if (compressed)
            {
                // Use just the file name of the file since all uncompressed files must appear
                // in the root of the image in a compressed package.
            }
            else
            {
                // Get the relative path of where we want the file to be layed out as specified
                // in the Directory table.
                string directoryPath = Binder.GetDirectoryPath(directories, null, directoryId, false);
                fileSourcePath = Path.Combine(directoryPath, fileSourcePath);
            }

            // Strip off "SourceDir" if it's still on there.
            if (fileSourcePath.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                fileSourcePath = fileSourcePath.Substring(10);
            }

            return fileSourcePath;
        }

        /// <summary>
        /// Writes the paths to the content files included in the package to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="contentFilePaths">Collection of paths to content files that will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<string> contentFilePaths)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (string contentPath in contentFilePaths)
                {
                    contents.WriteLine(contentPath);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the content files included in the bundle to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="payloads">Collection of payloads whose source will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<WixBundlePayloadRow> payloads)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (WixBundlePayloadRow payload in payloads)
                {
                    if (payload.ContentFile)
                    {
                        contents.WriteLine(payload.FullFileName);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the paths to the output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateOutputsFile(string path, IEnumerable<FileTransfer> fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Don't list files where the source is the same as the destination since
                    // that might be the only place the file exists. The outputs file is often
                    // used to delete stuff and losing the original source would be bad.
                    if (!fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Writes the paths to the built output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateBuiltOutputsFile(string path, IEnumerable<FileTransfer> fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Only write the built file transfers. Also, skip redundant
                    // files for the same reason spelled out in this.CreateOutputsFile().
                    if (fileTransfer.Built && !fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }
    }
}
