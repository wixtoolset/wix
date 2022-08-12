// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.CommandLine
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class TransformSubcommand : WindowsInstallerSubcommandBase
    {
        public TransformSubcommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IExtensionManager ExtensionManager { get; }

        private string OutputPath { get; set; }

        private string TargetPath { get; set; }

        private string UpdatedPath { get; set; }

        private string ExportBasePath { get; set; }

        private string IntermediateFolder { get; set; }

        private bool IsAdminImage { get; set; }

        private bool PreserveUnchangedRows { get; set; }

        private bool ShowPedanticMessages { get; set; }

        private bool SuppressKeepingSpecialRows { get; set; }

        private bool OutputAsWixout { get; set; }

        private TransformFlags ValidationFlags { get; set; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Creates an MST transform file.", "msi transform [options] target.msi [updated.msi] -out output.mst")
            {
                Switches = new[]
                {
                    new CommandLineHelpSwitch("-a", "Admin image, generates source file information in the transform."),
                    new CommandLineHelpSwitch("-intermediateFolder", "Optional working folder. If not specified %TMP% will be used."),
                    new CommandLineHelpSwitch("-p", "Preserve unchanged rows."),
                    new CommandLineHelpSwitch("-pedantic", "Show pedantic messages."),
                    new CommandLineHelpSwitch("-serr <flags>", "Suppress error when applying transform; see Error flags below"),
                    new CommandLineHelpSwitch("-t <type>", "Use default validation flags for the transform type; see Transform types below"),
                    new CommandLineHelpSwitch("-val <flags>", "Validation flags for the transform; see Validation flags below"),
                    new CommandLineHelpSwitch("-x", "Folder to extract binaries."),
                    new CommandLineHelpSwitch("-xo", "Output transfrom as a WiX output instead of an MST file."),
                    new CommandLineHelpSwitch("-out", "-o", "Path to output the transform file."),
                },
                Notes = String.Join(Environment.NewLine,
                    "Error flags:",
                    "   a          Ignore errors when adding an existing row",
                    "   b          Ignore errors when deleting a missing row",
                    "   c          Ignore errors when adding an existing table",
                    "   d          Ignore errors when deleting a missing table",
                    "   e          Ignore errors when modifying a missing row",
                    "   f          Ignore errors when changing the code page",
                    String.Empty,
                    "Validation flags:",
                    "   g          UpgradeCode must match",
                    "   l          Language must match",
                    "   r          Product ID must match",
                    "   s          Check major version only",
                    "   t          Check major and minor versions",
                    "   u          Check major, minor, and upgrade versions",
                    "   v          Upgrade version < target version",
                    "   w          Upgrade version <= target version",
                    "   x          Upgrade version = target version",
                    "   y          Upgrade version > target version",
                    "   z          Upgrade version >= target version",
                    String.Empty,
                    "Transform types:",
                    "   language   Default flags for a language transform",
                    "   instance   Default flags for an instance transform",
                    "   patch      Default flags for a patch transform")
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(this.TargetPath))
            {
                Console.Error.WriteLine("Input file required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.OutputPath))
            {
                Console.Error.WriteLine("Output file required");
                return Task.FromResult(-1);
            }

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            var transform = this.LoadTransform();

            if (!this.Messaging.EncounteredError)
            {
                this.SaveTransform(transform);
            }

            return Task.FromResult(this.Messaging.EncounteredError ? 1 : 0);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument))
            {
                var parameter = argument.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                    case "a":
                        this.IsAdminImage = true;
                        return true;

                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "o":
                    case "out":
                        this.OutputPath = parser.GetNextArgumentAsFilePathOrError(argument);
                        return true;

                    case "p":
                        this.PreserveUnchangedRows = true;
                        return true;

                    case "pedantic":
                        this.ShowPedanticMessages = true;
                        return true;

                    case "serr":
                    {
                        var serr = parser.GetNextArgumentOrError(argument);

                        switch (serr.ToLowerInvariant())
                        {
                            case "a":
                                this.ValidationFlags |= TransformFlags.ErrorAddExistingRow;
                                return true;

                            case "b":
                                this.ValidationFlags |= TransformFlags.ErrorDeleteMissingRow;
                                return true;

                            case "c":
                                this.ValidationFlags |= TransformFlags.ErrorAddExistingTable;
                                return true;

                            case "d":
                                this.ValidationFlags |= TransformFlags.ErrorDeleteMissingTable;
                                return true;

                            case "e":
                                this.ValidationFlags |= TransformFlags.ErrorUpdateMissingRow;
                                return true;

                            case "f":
                                this.ValidationFlags |= TransformFlags.ErrorChangeCodePage;
                                return true;

                            default:
                                this.Messaging.Write(ErrorMessages.ExpectedArgument(serr));
                                return true;
                        }
                    }

                    case "val":
                    {
                        var val = parser.GetNextArgumentOrError(argument);

                        switch (val.ToLowerInvariant())
                        {
                            case "language":
                                this.ValidationFlags |= TransformFlags.LanguageTransformDefault;
                                return true;

                            case "instance":
                                this.ValidationFlags |= TransformFlags.InstanceTransformDefault;
                                return true;

                            case "patch":
                                this.ValidationFlags |= TransformFlags.PatchTransformDefault;
                                return true;

                            case "g":
                                this.ValidationFlags |= TransformFlags.ValidateUpgradeCode;
                                return true;

                            case "l":
                                this.ValidationFlags |= TransformFlags.ValidateLanguage;
                                return true;

                            case "r":
                                this.ValidationFlags |= TransformFlags.ValidateProduct;
                                return true;

                            case "s":
                                this.ValidationFlags |= TransformFlags.ValidateMajorVersion;
                                return true;

                            case "t":
                                this.ValidationFlags |= TransformFlags.ValidateMinorVersion;
                                return true;

                            case "u":
                                this.ValidationFlags |= TransformFlags.ValidateUpdateVersion;
                                return true;

                            case "v":
                                this.ValidationFlags |= TransformFlags.ValidateNewLessBaseVersion;
                                return true;

                            case "w":
                                this.ValidationFlags |= TransformFlags.ValidateNewLessEqualBaseVersion;
                                return true;

                            case "x":
                                this.ValidationFlags |= TransformFlags.ValidateNewEqualBaseVersion;
                                return true;

                            case "y":
                                this.ValidationFlags |= TransformFlags.ValidateNewGreaterEqualBaseVersion;
                                return true;

                            case "z":
                                this.ValidationFlags |= TransformFlags.ValidateNewGreaterBaseVersion;
                                return true;

                            default:
                                this.Messaging.Write(ErrorMessages.ExpectedArgument(val));
                                return true;
                        }
                    }

                    case "x":
                        this.ExportBasePath = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "xo":
                        this.OutputAsWixout = true;
                        return true;
                }
            }
            else if (String.IsNullOrEmpty(this.TargetPath))
            {
                this.TargetPath = argument;
                return true;
            }
            else if (String.IsNullOrEmpty(this.UpdatedPath))
            {
                this.UpdatedPath = argument;
                return true;
            }

            return false;
        }

        private WindowsInstallerData LoadTransform()
        {
            WindowsInstallerData transform;

            if (String.IsNullOrEmpty(this.UpdatedPath))
            {
                Exception exception;

                (transform, exception) = LoadWindowsInstallerDataSafely(this.TargetPath);

                if (transform?.Type != OutputType.Transform)
                {
                    this.Messaging.Write(WindowsInstallerBackendErrors.CannotLoadWixoutAsTransform(new SourceLineNumber(this.TargetPath), exception));
                }
            }
            else
            {
                transform = this.CreateTransform();

                if (null == transform.Tables || 0 >= transform.Tables.Count)
                {
                    this.Messaging.Write(ErrorMessages.NoDifferencesInTransform(new SourceLineNumber(this.OutputPath)));
                }
            }

            return transform;
        }

        private void SaveTransform(WindowsInstallerData transform)
        {
            if (this.OutputAsWixout)
            {
                using (var output = WixOutput.Create(this.OutputPath))
                {
                    transform.Save(output);
                }
            }
            else
            {
                var fileSystemExtensions = this.ExtensionManager.GetServices<IFileSystemExtension>();
                var fileSystemManager = new FileSystemManager(fileSystemExtensions);

                var tableDefinitions = this.GetTableDefinitions();

                var bindCommand = new BindTransformCommand(this.Messaging, this.BackendHelper, fileSystemManager, this.IntermediateFolder, transform, this.OutputPath, tableDefinitions);
                bindCommand.Execute();
            }
        }

        private WindowsInstallerData CreateTransform()
        {
            if (!TryLoadWindowsInstallerData(this.TargetPath, out var targetOutput))
            {
                var unbindCommand = new UnbindMsiOrMsmCommand(this.Messaging, this.BackendHelper, this.TargetPath, this.ExportBasePath, this.IntermediateFolder, this.IsAdminImage, suppressDemodularization: true, suppressExtractCabinets: true);
                targetOutput = unbindCommand.Execute();
            }

            if (!TryLoadWindowsInstallerData(this.TargetPath, out var updatedOutput))
            {
                var unbindCommand = new UnbindMsiOrMsmCommand(this.Messaging, this.BackendHelper, this.UpdatedPath, this.ExportBasePath, this.IntermediateFolder, this.IsAdminImage, suppressDemodularization: true, suppressExtractCabinets: true);
                updatedOutput = unbindCommand.Execute();
            }

            var differ = new Differ(this.Messaging)
            {
                PreserveUnchangedRows = this.PreserveUnchangedRows,
                ShowPedanticMessages = this.ShowPedanticMessages,
                SuppressKeepingSpecialRows = this.SuppressKeepingSpecialRows
            };

            return differ.Diff(targetOutput, updatedOutput, this.ValidationFlags);
        }

        private TableDefinitionCollection GetTableDefinitions()
        {
            var backendExtensions = this.ExtensionManager.GetServices<IWindowsInstallerBackendBinderExtension>();

            var loadTableDefinitions = new LoadTableDefinitionsCommand(this.Messaging, null, backendExtensions);
            return loadTableDefinitions.Execute();
        }

        private static bool TryLoadWindowsInstallerData(string path, out WindowsInstallerData data)
        {
            data = null;

            var extension = Path.GetExtension(path);

            // If the path is _not_ obviously a Windows Installer database, let's try opening it as
            // our own data file format.
            if (!extension.Equals(".msi", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".msm", StringComparison.OrdinalIgnoreCase))
            {
                (data, _) = LoadWindowsInstallerDataSafely(path);
            }

            return data != null;
        }

        private static (WindowsInstallerData, Exception) LoadWindowsInstallerDataSafely(string path)
        {
            WindowsInstallerData data = null;
            Exception exception = null;

            try
            {
                data = WindowsInstallerData.Load(path);
            }
            catch (Exception e)
            {
                exception = e;
            }

            return (data, exception);
        }
    }
}
