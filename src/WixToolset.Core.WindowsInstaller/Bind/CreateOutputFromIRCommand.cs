// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CreateOutputFromIRCommand
    {
        private const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB
        private const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)

        private static readonly char[] ColonCharacter = new[] { ':' };

        public CreateOutputFromIRCommand(IMessaging messaging, IntermediateSection section, TableDefinitionCollection tableDefinitions, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.TableDefinitions = tableDefinitions;
            this.BackendExtensions = backendExtensions;
        }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private IMessaging Messaging { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private IntermediateSection Section { get; }

        public WindowsInstallerData Output { get; private set; }

        public void Execute()
        {
            var output = new WindowsInstallerData(this.Section.Tuples.First().SourceLineNumbers);
            output.Codepage = this.Section.Codepage;
            output.Type = SectionTypeToOutputType(this.Section.Type);

            this.AddSectionToOutput(this.Section, output);

            this.Output = output;
        }

        private void AddSectionToOutput(IntermediateSection section, WindowsInstallerData output)
        {
            foreach (var tuple in section.Tuples)
            {
                switch (tuple.Definition.Type)
                {
                case TupleDefinitionType.AppSearch:
                    this.AddTupleDefaultly(tuple, output);
                    output.EnsureTable(this.TableDefinitions["Signature"]);
                    break;

                case TupleDefinitionType.Assembly:
                    this.AddAssemblyTuple((AssemblyTuple)tuple, output);
                    break;

                case TupleDefinitionType.Binary:
                    this.AddTupleDefaultly(tuple, output, idIsPrimaryKey: true);
                    break;

                case TupleDefinitionType.BBControl:
                    this.AddBBControlTuple((BBControlTuple)tuple, output);
                    break;

                case TupleDefinitionType.Class:
                    this.AddClassTuple((ClassTuple)tuple, output);
                    break;

                case TupleDefinitionType.Control:
                    this.AddControlTuple((ControlTuple)tuple, output);
                    break;

                case TupleDefinitionType.Component:
                    this.AddComponentTuple((ComponentTuple)tuple, output);
                    break;

                case TupleDefinitionType.CustomAction:
                    this.AddCustomActionTuple((CustomActionTuple)tuple, output);
                    break;

                case TupleDefinitionType.Dialog:
                    this.AddDialogTuple((DialogTuple)tuple, output);
                    break;

                case TupleDefinitionType.Directory:
                    this.AddDirectoryTuple((DirectoryTuple)tuple, output);
                    break;

                case TupleDefinitionType.Environment:
                    this.AddEnvironmentTuple((EnvironmentTuple)tuple, output);
                    break;

                case TupleDefinitionType.Feature:
                    this.AddFeatureTuple((FeatureTuple)tuple, output);
                    break;

                case TupleDefinitionType.File:
                    this.AddFileTuple((FileTuple)tuple, output);
                    break;

                case TupleDefinitionType.Icon:
                    this.AddTupleDefaultly(tuple, output, idIsPrimaryKey: true);
                    break;

                case TupleDefinitionType.IniFile:
                    this.AddIniFileTuple((IniFileTuple)tuple, output);
                    break;

                case TupleDefinitionType.Media:
                    this.AddMediaTuple((MediaTuple)tuple, output);
                    break;

                case TupleDefinitionType.ModuleConfiguration:
                    this.AddModuleConfigurationTuple((ModuleConfigurationTuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiEmbeddedUI:
                    this.AddMsiEmbeddedUITuple((MsiEmbeddedUITuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiFileHash:
                    this.AddMsiFileHashTuple((MsiFileHashTuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiServiceConfig:
                    this.AddMsiServiceConfigTuple((MsiServiceConfigTuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiServiceConfigFailureActions:
                    this.AddMsiServiceConfigFailureActionsTuple((MsiServiceConfigFailureActionsTuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiShortcutProperty:
                    this.AddTupleDefaultly(tuple, output, idIsPrimaryKey: true);
                    break;

                case TupleDefinitionType.MoveFile:
                    this.AddMoveFileTuple((MoveFileTuple)tuple, output);
                    break;

                case TupleDefinitionType.ProgId:
                    this.AddTupleDefaultly(tuple, output);
                    output.EnsureTable(this.TableDefinitions["Extension"]);
                    break;

                case TupleDefinitionType.Property:
                    this.AddPropertyTuple((PropertyTuple)tuple, output);
                    break;

                case TupleDefinitionType.RemoveFile:
                    this.AddRemoveFileTuple((RemoveFileTuple)tuple, output);
                    break;

                case TupleDefinitionType.Registry:
                    this.AddRegistryTuple((RegistryTuple)tuple, output);
                    break;

                case TupleDefinitionType.RegLocator:
                    this.AddRegLocatorTuple((RegLocatorTuple)tuple, output);
                    break;

                case TupleDefinitionType.RemoveRegistry:
                    this.AddRemoveRegistryTuple((RemoveRegistryTuple)tuple, output);
                    break;

                case TupleDefinitionType.ReserveCost:
                    this.AddTupleDefaultly(tuple, output, idIsPrimaryKey: true);
                    break;

                case TupleDefinitionType.ServiceControl:
                    this.AddServiceControlTuple((ServiceControlTuple)tuple, output);
                    break;

                case TupleDefinitionType.ServiceInstall:
                    this.AddServiceInstallTuple((ServiceInstallTuple)tuple, output);
                    break;

                case TupleDefinitionType.Shortcut:
                    this.AddShortcutTuple((ShortcutTuple)tuple, output);
                    break;
                        
                case TupleDefinitionType.Signature:
                    this.AddTupleDefaultly(tuple, output, idIsPrimaryKey: true);
                    break;

                case TupleDefinitionType.SummaryInformation:
                    this.AddTupleDefaultly(tuple, output, tableName: "_SummaryInformation");
                    break;

                case TupleDefinitionType.TextStyle:
                    this.AddTextStyleTuple((TextStyleTuple)tuple, output);
                    break;

                case TupleDefinitionType.Upgrade:
                    this.AddUpgradeTuple((UpgradeTuple)tuple, output);
                    break;

                case TupleDefinitionType.WixAction:
                    this.AddWixActionTuple((WixActionTuple)tuple, output);
                    break;

                case TupleDefinitionType.WixMediaTemplate:
                    this.AddWixMediaTemplateTuple((WixMediaTemplateTuple)tuple, output);
                    break;

                case TupleDefinitionType.MustBeFromAnExtension:
                    this.AddTupleFromExtension(tuple, output);
                    break;

                case TupleDefinitionType.WixCustomRow:
                    this.AddWixCustomRowTuple((WixCustomRowTuple)tuple, output);
                    break;

                case TupleDefinitionType.WixEnsureTable:
                    this.AddWixEnsureTableTuple((WixEnsureTableTuple)tuple, output);
                    break;

                // ignored.
                case TupleDefinitionType.WixFile:
                case TupleDefinitionType.WixComponentGroup:
                case TupleDefinitionType.WixDeltaPatchFile:
                case TupleDefinitionType.WixFeatureGroup:
                    break;

                // Already processed.
                case TupleDefinitionType.WixCustomTable:
                    break;

                default:
                    this.AddTupleDefaultly(tuple, output);
                    break;
                }
            }
        }

        private void AddAssemblyTuple(AssemblyTuple tuple, WindowsInstallerData output)
        {
            var attributes = tuple.Type == AssemblyType.Win32Assembly ? 1 : (int?)null;

            var table = output.EnsureTable(this.TableDefinitions["MsiAssembly"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.ComponentRef;
            row[1] = tuple.FeatureRef;
            row[2] = tuple.ManifestFileRef;
            row[3] = tuple.ApplicationFileRef;
            row[4] = attributes;
        }

        private void AddBBControlTuple(BBControlTuple tuple, WindowsInstallerData output)
        {
            var attributes = tuple.Attributes;
            attributes |= tuple.Enabled ? WindowsInstallerConstants.MsidbControlAttributesEnabled : 0;
            attributes |= tuple.Indirect ? WindowsInstallerConstants.MsidbControlAttributesIndirect : 0;
            attributes |= tuple.Integer ? WindowsInstallerConstants.MsidbControlAttributesInteger : 0;
            attributes |= tuple.LeftScroll ? WindowsInstallerConstants.MsidbControlAttributesLeftScroll : 0;
            attributes |= tuple.RightAligned ? WindowsInstallerConstants.MsidbControlAttributesRightAligned : 0;
            attributes |= tuple.RightToLeft ? WindowsInstallerConstants.MsidbControlAttributesRTLRO : 0;
            attributes |= tuple.Sunken ? WindowsInstallerConstants.MsidbControlAttributesSunken : 0;
            attributes |= tuple.Visible ? WindowsInstallerConstants.MsidbControlAttributesVisible : 0;

            var table = output.EnsureTable(this.TableDefinitions["BBControl"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.BillboardRef;
            row[1] = tuple.BBControl;
            row[2] = tuple.Type;
            row[3] = tuple.X;
            row[4] = tuple.Y;
            row[5] = tuple.Width;
            row[6] = tuple.Height;
            row[7] = attributes;
            row[8] = tuple.Text;
        }

        private void AddClassTuple(ClassTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["Class"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.CLSID;
            row[1] = tuple.Context;
            row[2] = tuple.ComponentRef;
            row[3] = tuple.DefaultProgIdRef;
            row[4] = tuple.Description;
            row[5] = tuple.AppIdRef;
            row[6] = tuple.FileTypeMask;
            row[7] = tuple.IconRef;
            row[8] = tuple.IconIndex;
            row[9] = tuple.DefInprocHandler;
            row[10] = tuple.Argument;
            row[11] = tuple.FeatureRef;
            row[12] = tuple.RelativePath ? (int?)1 : null;
        }

        private void AddControlTuple(ControlTuple tuple, WindowsInstallerData output)
        {
            var text = tuple.Text;
            var attributes = tuple.Attributes;
            attributes |= tuple.Enabled ? WindowsInstallerConstants.MsidbControlAttributesEnabled : 0;
            attributes |= tuple.Indirect ? WindowsInstallerConstants.MsidbControlAttributesIndirect : 0;
            attributes |= tuple.Integer ? WindowsInstallerConstants.MsidbControlAttributesInteger : 0;
            attributes |= tuple.LeftScroll ? WindowsInstallerConstants.MsidbControlAttributesLeftScroll : 0;
            attributes |= tuple.RightAligned ? WindowsInstallerConstants.MsidbControlAttributesRightAligned : 0;
            attributes |= tuple.RightToLeft ? WindowsInstallerConstants.MsidbControlAttributesRTLRO : 0;
            attributes |= tuple.Sunken ? WindowsInstallerConstants.MsidbControlAttributesSunken : 0;
            attributes |= tuple.Visible ? WindowsInstallerConstants.MsidbControlAttributesVisible : 0;

            // If we're tracking disk space, and this is a non-FormatSize Text control,
            // and the text attribute starts with '[' and ends with ']', add a space.
            // It is not necessary for the whole string to be a property, just those
            // two characters matter.
            if (tuple.TrackDiskSpace &&
                "Text" == tuple.Type &&
                WindowsInstallerConstants.MsidbControlAttributesFormatSize != (attributes & WindowsInstallerConstants.MsidbControlAttributesFormatSize) &&
                null != text && text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
            {
                text = String.Concat(text, " ");
            }

            var table = output.EnsureTable(this.TableDefinitions["Control"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.DialogRef;
            row[1] = tuple.Control;
            row[2] = tuple.Type;
            row[3] = tuple.X;
            row[4] = tuple.Y;
            row[5] = tuple.Width;
            row[6] = tuple.Height;
            row[7] = attributes;
            row[8] = text;
            row[9] = tuple.NextControlRef;
            row[10] = tuple.Help;
        }

        private void AddComponentTuple(ComponentTuple tuple, WindowsInstallerData output)
        {
            var attributes = ComponentLocation.Either == tuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesOptional : 0;
            attributes |= ComponentLocation.SourceOnly == tuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesSourceOnly : 0;
            attributes |= ComponentKeyPathType.Registry == tuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath : 0;
            attributes |= ComponentKeyPathType.OdbcDataSource == tuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource : 0;
            attributes |= tuple.DisableRegistryReflection ? WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection : 0;
            attributes |= tuple.NeverOverwrite ? WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite : 0;
            attributes |= tuple.Permanent ? WindowsInstallerConstants.MsidbComponentAttributesPermanent : 0;
            attributes |= tuple.SharedDllRefCount ? WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount : 0;
            attributes |= tuple.Shared ? WindowsInstallerConstants.MsidbComponentAttributesShared : 0;
            attributes |= tuple.Transitive ? WindowsInstallerConstants.MsidbComponentAttributesTransitive : 0;
            attributes |= tuple.UninstallWhenSuperseded ? WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence : 0;
            attributes |= tuple.Win64 ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;

            var table = output.EnsureTable(this.TableDefinitions["Component"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ComponentId;
            row[2] = tuple.DirectoryRef;
            row[3] = attributes;
            row[4] = tuple.Condition;
            row[5] = tuple.KeyPath;
        }

        private void AddCustomActionTuple(CustomActionTuple tuple, WindowsInstallerData output)
        {
            var type = tuple.Win64 ? WindowsInstallerConstants.MsidbCustomActionType64BitScript : 0;
            type |= tuple.IgnoreResult ? WindowsInstallerConstants.MsidbCustomActionTypeContinue : 0;
            type |= tuple.Hidden ? WindowsInstallerConstants.MsidbCustomActionTypeHideTarget : 0;
            type |= tuple.Async ? WindowsInstallerConstants.MsidbCustomActionTypeAsync : 0;
            type |= CustomActionExecutionType.FirstSequence == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence : 0;
            type |= CustomActionExecutionType.OncePerProcess == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess : 0;
            type |= CustomActionExecutionType.ClientRepeat == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat : 0;
            type |= CustomActionExecutionType.Deferred == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript : 0;
            type |= CustomActionExecutionType.Rollback == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback : 0;
            type |= CustomActionExecutionType.Commit == tuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit : 0;
            type |= CustomActionSourceType.File == tuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeSourceFile : 0;
            type |= CustomActionSourceType.Directory == tuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeDirectory : 0;
            type |= CustomActionSourceType.Property == tuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeProperty : 0;
            type |= CustomActionTargetType.Dll == tuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeDll : 0;
            type |= CustomActionTargetType.Exe == tuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeExe : 0;
            type |= CustomActionTargetType.TextData == tuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeTextData : 0;
            type |= CustomActionTargetType.JScript == tuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeJScript : 0;
            type |= CustomActionTargetType.VBScript == tuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeVBScript : 0;

            if (WindowsInstallerConstants.MsidbCustomActionTypeInScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeInScript))
            {
                type |= tuple.Impersonate ? 0 : WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate;
                type |= tuple.TSAware ? WindowsInstallerConstants.MsidbCustomActionTypeTSAware : 0;
            }

            var table = output.EnsureTable(this.TableDefinitions["CustomAction"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = type;
            row[2] = tuple.Source;
            row[3] = tuple.Target;
            row[4] = tuple.PatchUninstall ? (int?)WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall : null;
        }

        private void AddDialogTuple(DialogTuple tuple, WindowsInstallerData output)
        {
            var attributes = tuple.Visible ? WindowsInstallerConstants.MsidbDialogAttributesVisible : 0;
            attributes|= tuple.Modal ? WindowsInstallerConstants.MsidbDialogAttributesModal : 0;
            attributes|= tuple.Minimize ? WindowsInstallerConstants.MsidbDialogAttributesMinimize : 0;
            attributes|= tuple.CustomPalette ? WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette: 0;
            attributes|= tuple.ErrorDialog ? WindowsInstallerConstants.MsidbDialogAttributesError : 0;
            attributes|= tuple.LeftScroll ? WindowsInstallerConstants.MsidbDialogAttributesLeftScroll : 0;
            attributes|= tuple.KeepModeless ? WindowsInstallerConstants.MsidbDialogAttributesKeepModeless : 0;
            attributes|= tuple.RightAligned ? WindowsInstallerConstants.MsidbDialogAttributesRightAligned : 0;
            attributes|= tuple.RightToLeft ? WindowsInstallerConstants.MsidbDialogAttributesRTLRO : 0;
            attributes|= tuple.SystemModal ? WindowsInstallerConstants.MsidbDialogAttributesSysModal : 0;
            attributes|= tuple.TrackDiskSpace ? WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace : 0;

            var table = output.EnsureTable(this.TableDefinitions["Dialog"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.HCentering;
            row[2] = tuple.VCentering;
            row[3] = tuple.Width;
            row[4] = tuple.Height;
            row[5] = attributes;
            row[6] = tuple.Title;
            row[7] = tuple.FirstControlRef;
            row[8] = tuple.DefaultControlRef;
            row[9] = tuple.CancelControlRef;

            output.EnsureTable(this.TableDefinitions["ListBox"]);
        }

        private void AddDirectoryTuple(DirectoryTuple tuple, WindowsInstallerData output)
        {
            var sourceName = GetMsiFilenameValue(tuple.SourceShortName, tuple.SourceName);
            var targetName = GetMsiFilenameValue(tuple.ShortName, tuple.Name);

            if (String.IsNullOrEmpty(targetName))
            {
                targetName = ".";
            }

            var defaultDir = String.IsNullOrEmpty(sourceName) ? targetName :  targetName + ":" + sourceName ;

            var table = output.EnsureTable(this.TableDefinitions["Directory"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ParentDirectoryRef;
            row[2] = defaultDir;
        }

        private void AddEnvironmentTuple(EnvironmentTuple tuple, WindowsInstallerData output)
        {
            var action = String.Empty;
            var system = tuple.System ? "*" : String.Empty;
            var uninstall = tuple.Permanent ? String.Empty : "-";
            var value = tuple.Value;

            switch (tuple.Action)
            {
            case EnvironmentActionType.Create:
                action = "+";
                break;
            case EnvironmentActionType.Set:
                action = "=";
                break;
            case EnvironmentActionType.Remove:
                action = "!";
                break;
            }

            switch (tuple.Part)
            {
            case EnvironmentPartType.First:
                value = String.Concat(value, tuple.Separator, "[~]");
                break;
            case EnvironmentPartType.Last:
                value = String.Concat("[~]", tuple.Separator, value);
                break;
            }

            var table = output.EnsureTable(this.TableDefinitions["Environment"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = String.Concat(action, uninstall, system, tuple.Name);
            row[2] = value;
            row[3] = tuple.ComponentRef;
        }

        private void AddFeatureTuple(FeatureTuple tuple, WindowsInstallerData output)
        {
            var attributes = tuple.DisallowAbsent ? WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent : 0;
            attributes |= tuple.DisallowAdvertise ? WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise : 0;
            attributes |= FeatureInstallDefault.FollowParent == tuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFollowParent : 0;
            attributes |= FeatureInstallDefault.Source == tuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorSource : 0;
            attributes |= FeatureTypicalDefault.Advertise == tuple.TypicalDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise : 0;

            var table = output.EnsureTable(this.TableDefinitions["Feature"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ParentFeatureRef;
            row[2] = tuple.Title;
            row[3] = tuple.Description;
            row[4] = tuple.Display;
            row[5] = tuple.Level;
            row[6] = tuple.DirectoryRef;
            row[7] = attributes;
        }

        private void AddFileTuple(FileTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["File"]);
            var row = (FileRow)table.CreateRow(tuple.SourceLineNumbers);
            row.File = tuple.Id.Id;
            row.Component = tuple.ComponentRef;
            row.FileName = GetMsiFilenameValue(tuple.ShortName, tuple.Name);
            row.FileSize = tuple.FileSize;
            row.Version = tuple.Version;
            row.Language = tuple.Language;

            var attributes = (tuple.Attributes & FileTupleAttributes.Checksum) == FileTupleAttributes.Checksum ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.Compressed) == FileTupleAttributes.Compressed ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.Uncompressed) == FileTupleAttributes.Uncompressed ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.Hidden) == FileTupleAttributes.Hidden ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.ReadOnly) == FileTupleAttributes.ReadOnly ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.System) == FileTupleAttributes.System ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0;
            attributes |= (tuple.Attributes & FileTupleAttributes.Vital) == FileTupleAttributes.Vital ? WindowsInstallerConstants.MsidbFileAttributesVital : 0;
            row.Attributes = attributes;

            if (!String.IsNullOrEmpty(tuple.FontTitle))
            {
                var fontTable = output.EnsureTable(this.TableDefinitions["Font"]);
                var fontRow = fontTable.CreateRow(tuple.SourceLineNumbers);
                fontRow[0] = tuple.Id.Id;
                fontRow[1] = tuple.FontTitle;
            }
        }

        private void AddIniFileTuple(IniFileTuple tuple, WindowsInstallerData output)
        {
            var tableName = (InifFileActionType.AddLine == tuple.Action || InifFileActionType.AddTag == tuple.Action || InifFileActionType.CreateLine == tuple.Action) ? "IniFile" : "RemoveIniFile";

            var table = output.EnsureTable(this.TableDefinitions[tableName]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.FileName;
            row[2] = tuple.DirProperty;
            row[3] = tuple.Section;
            row[4] = tuple.Key;
            row[5] = tuple.Value;
            row[6] = tuple.Action;
            row[7] = tuple.ComponentRef;
        }

        private void AddMediaTuple(MediaTuple tuple, WindowsInstallerData output)
        {
            if (this.Section.Type != SectionType.Module)
            {
                var table = output.EnsureTable(this.TableDefinitions["Media"]);
                var row = (MediaRow)table.CreateRow(tuple.SourceLineNumbers);
                row.DiskId = tuple.DiskId;
                row.LastSequence = tuple.LastSequence ?? 0;
                row.DiskPrompt = tuple.DiskPrompt;
                row.Cabinet = tuple.Cabinet;
                row.VolumeLabel = tuple.VolumeLabel;
                row.Source = tuple.Source;
            }
        }

        private void AddModuleConfigurationTuple(ModuleConfigurationTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["ModuleConfiguration"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Format;
            row[2] = tuple.Type;
            row[3] = tuple.ContextData;
            row[4] = tuple.DefaultValue;
            row[5] = (tuple.KeyNoOrphan ? WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan : 0) |
                     (tuple.NonNullable ? WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable : 0);
            row[6] = tuple.DisplayName;
            row[7] = tuple.Description;
            row[8] = tuple.HelpLocation;
            row[9] = tuple.HelpKeyword;
        }

        private void AddMsiEmbeddedUITuple(MsiEmbeddedUITuple tuple, WindowsInstallerData output)
        {
            var attributes = tuple.EntryPoint ? WindowsInstallerConstants.MsidbEmbeddedUI : 0;
            attributes |= tuple.SupportsBasicUI ? WindowsInstallerConstants.MsidbEmbeddedHandlesBasic : 0;

            var table = output.EnsureTable(this.TableDefinitions["MsiEmbeddedUI"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.FileName;
            row[2] = attributes;
            row[3] = tuple.MessageFilter;
            row[4] = tuple.Source;
        }

        private void AddMsiFileHashTuple(MsiFileHashTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["MsiFileHash"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Options;
            row[2] = tuple.HashPart1;
            row[3] = tuple.HashPart2;
            row[4] = tuple.HashPart3;
            row[5] = tuple.HashPart4;
        }

        private void AddMsiServiceConfigTuple(MsiServiceConfigTuple tuple, WindowsInstallerData output)
        {
            var events = tuple.OnInstall ? WindowsInstallerConstants.MsidbServiceConfigEventInstall : 0;
            events |= tuple.OnReinstall ? WindowsInstallerConstants.MsidbServiceConfigEventReinstall : 0;
            events |= tuple.OnUninstall ? WindowsInstallerConstants.MsidbServiceConfigEventUninstall : 0;

            var table = output.EnsureTable(this.TableDefinitions["MsiServiceConfigFailureActions"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Name;
            row[2] = events;
            row[3] = tuple.ConfigType;
            row[4] = tuple.Argument;
            row[5] = tuple.ComponentRef;
        }

        private void AddMsiServiceConfigFailureActionsTuple(MsiServiceConfigFailureActionsTuple tuple, WindowsInstallerData output)
        {
            var events = tuple.OnInstall ? WindowsInstallerConstants.MsidbServiceConfigEventInstall : 0;
            events |= tuple.OnReinstall ? WindowsInstallerConstants.MsidbServiceConfigEventReinstall : 0;
            events |= tuple.OnUninstall ? WindowsInstallerConstants.MsidbServiceConfigEventUninstall : 0;

            var table = output.EnsureTable(this.TableDefinitions["MsiServiceConfig"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Name;
            row[2] = events;
            row[3] = tuple.ResetPeriod.HasValue ? tuple.ResetPeriod : null;
            row[4] = tuple.RebootMessage ?? "[~]";
            row[5] = tuple.Command ?? "[~]";
            row[6] = tuple.Actions;
            row[7] = tuple.DelayActions;
            row[8] = tuple.ComponentRef;
        }

        private void AddMoveFileTuple(MoveFileTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["MoveFile"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ComponentRef;
            row[2] = tuple.SourceName;
            row[3] = tuple.DestName;
            row[4] = tuple.SourceFolder;
            row[5] = tuple.DestFolder;
            row[6] = tuple.Delete ? WindowsInstallerConstants.MsidbMoveFileOptionsMove : 0;
        }

        private void AddPropertyTuple(PropertyTuple tuple, WindowsInstallerData output)
        {
            if (String.IsNullOrEmpty(tuple.Value))
            {
                return;
            }

            var table = output.EnsureTable(this.TableDefinitions["Property"]);
            var row = (PropertyRow)table.CreateRow(tuple.SourceLineNumbers);
            row.Property = tuple.Id.Id;
            row.Value = tuple.Value;
        }

        private void AddRemoveFileTuple(RemoveFileTuple tuple, WindowsInstallerData output)
        {
            var installMode = tuple.OnInstall == true ? WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall : 0;
            installMode |= tuple.OnUninstall == true ? WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove : 0;

            var table = output.EnsureTable(this.TableDefinitions["RemoveFile"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ComponentRef;
            row[2] = tuple.FileName;
            row[3] = tuple.DirProperty;
            row[4] = installMode;
        }

        private void AddRegistryTuple(RegistryTuple tuple, WindowsInstallerData output)
        {
            var value = tuple.Value;

            switch (tuple.ValueType)
            {
            case RegistryValueType.Binary:
                value = String.Concat("#x", value);
                break;
            case RegistryValueType.Expandable:
                value = String.Concat("#%", value);
                break;
            case RegistryValueType.Integer:
                value = String.Concat("#", value);
                break;
            case RegistryValueType.MultiString:
                switch (tuple.ValueAction)
                {
                case RegistryValueActionType.Append:
                    value = String.Concat("[~]", value);
                    break;
                case RegistryValueActionType.Prepend:
                    value = String.Concat(value, "[~]");
                    break;
                case RegistryValueActionType.Write:
                default:
                    if (null != value && -1 == value.IndexOf("[~]", StringComparison.Ordinal))
                    {
                        value = String.Format(CultureInfo.InvariantCulture, "[~]{0}[~]", value);
                    }
                    break;
                }
                break;
            case RegistryValueType.String:
                // escape the leading '#' character for string registry keys
                if (null != value && value.StartsWith("#", StringComparison.Ordinal))
                {
                    value = String.Concat("#", value);
                }
                break;
            }

            var table = output.EnsureTable(this.TableDefinitions["Registry"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Root;
            row[2] = tuple.Key;
            row[3] = tuple.Name;
            row[4] = value;
            row[5] = tuple.ComponentRef;
        }

        private void AddRegLocatorTuple(RegLocatorTuple tuple, WindowsInstallerData output)
        {
            var type = (int)tuple.Type;
            type |= tuple.Win64 ? WindowsInstallerConstants.MsidbLocatorType64bit : 0;

            var table = output.EnsureTable(this.TableDefinitions["RegLocator"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Root;
            row[2] = tuple.Key;
            row[3] = tuple.Name;
            row[4] = type;
        }

        private void AddRemoveRegistryTuple(RemoveRegistryTuple tuple, WindowsInstallerData output)
        {
            if (tuple.Action == RemoveRegistryActionType.RemoveOnInstall)
            {
                var table = output.EnsureTable(this.TableDefinitions["RemoveRegistry"]);
                var row = table.CreateRow(tuple.SourceLineNumbers);
                row[0] = tuple.Id.Id;
                row[1] = tuple.Root;
                row[2] = tuple.Key;
                row[3] = tuple.Name;
                row[4] = tuple.ComponentRef;
            }
            else // Registry table is used to remove registry keys on uninstall.
            {
                var table = output.EnsureTable(this.TableDefinitions["Registry"]);
                var row = table.CreateRow(tuple.SourceLineNumbers);
                row[0] = tuple.Id.Id;
                row[1] = tuple.Root;
                row[2] = tuple.Key;
                row[3] = tuple.Name;
                row[5] = tuple.ComponentRef;
            }
        }

        private void AddServiceControlTuple(ServiceControlTuple tuple, WindowsInstallerData output)
        {
            var events = tuple.InstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventDelete : 0;
            events |= tuple.UninstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete : 0;
            events |= tuple.InstallStart ? WindowsInstallerConstants.MsidbServiceControlEventStart : 0;
            events |= tuple.UninstallStart ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStart : 0;
            events |= tuple.InstallStop ? WindowsInstallerConstants.MsidbServiceControlEventStop : 0;
            events |= tuple.UninstallStop ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStop : 0;

            var table = output.EnsureTable(this.TableDefinitions["ServiceControl"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Name;
            row[2] = events;
            row[3] = tuple.Arguments;
            row[4] = tuple.Wait;
            row[5] = tuple.ComponentRef;
        }

        private void AddServiceInstallTuple(ServiceInstallTuple tuple, WindowsInstallerData output)
        {
            var errorControl = (int)tuple.ErrorControl;
            errorControl |= tuple.Vital ? WindowsInstallerConstants.MsidbServiceInstallErrorControlVital : 0;

            var serviceType = (int)tuple.ServiceType;
            serviceType |= tuple.Interactive ? WindowsInstallerConstants.MsidbServiceInstallInteractive : 0;

            var table = output.EnsureTable(this.TableDefinitions["ServiceInstall"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.Name;
            row[2] = tuple.DisplayName;
            row[3] = serviceType;
            row[4] = (int)tuple.StartType;
            row[5] = errorControl;
            row[6] = tuple.LoadOrderGroup;
            row[7] = tuple.Dependencies;
            row[8] = tuple.StartName;
            row[9] = tuple.Password;
            row[10] = tuple.Arguments;
            row[11] = tuple.ComponentRef;
            row[12] = tuple.Description;
        }

        private void AddShortcutTuple(ShortcutTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["Shortcut"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.DirectoryRef;
            row[2] = GetMsiFilenameValue(tuple.ShortName, tuple.Name);
            row[3] = tuple.ComponentRef;
            row[4] = tuple.Target;
            row[5] = tuple.Arguments;
            row[6] = tuple.Description;
            row[7] = tuple.Hotkey;
            row[8] = tuple.IconRef;
            row[9] = tuple.IconIndex;
            row[10] = (int?)tuple.Show;
            row[11] = tuple.WorkingDirectory;
            row[12] = tuple.DisplayResourceDll;
            row[13] = tuple.DisplayResourceId;
            row[14] = tuple.DescriptionResourceDll;
            row[15] = tuple.DescriptionResourceId;
        }

        private void AddTextStyleTuple(TextStyleTuple tuple, WindowsInstallerData output)
        {
            var styleBits = tuple.Bold ? WindowsInstallerConstants.MsidbTextStyleStyleBitsBold : 0;
            styleBits |= tuple.Italic ? WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic : 0;
            styleBits |= tuple.Strike ? WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike : 0;
            styleBits |= tuple.Underline ? WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline : 0;

            long? color = null;

            if (tuple.Red.HasValue || tuple.Green.HasValue || tuple.Blue.HasValue)
            {
                color = tuple.Red ?? 0;
                color += (long)(tuple.Green ?? 0) * 256;
                color += (long)(tuple.Blue ?? 0) * 65536;
            }

            var table = output.EnsureTable(this.TableDefinitions["TextStyle"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.FaceName;
            row[2] = tuple.Size;
            row[3] = color;
            row[4] = styleBits == 0 ? null : (int?)styleBits;
        }

        private void AddUpgradeTuple(UpgradeTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["Upgrade"]);
            var row = (UpgradeRow)table.CreateRow(tuple.SourceLineNumbers);
            row.UpgradeCode = tuple.UpgradeCode;
            row.VersionMin = tuple.VersionMin;
            row.VersionMax = tuple.VersionMax;
            row.Language = tuple.Language;
            row.Remove = tuple.Remove;
            row.ActionProperty = tuple.ActionProperty;

            var attributes = tuple.MigrateFeatures ? WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures : 0;
            attributes |= tuple.OnlyDetect ? WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect : 0;
            attributes |= tuple.IgnoreRemoveFailures ? WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure : 0;
            attributes |= tuple.VersionMinInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive : 0;
            attributes |= tuple.VersionMaxInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive : 0;
            attributes |= tuple.ExcludeLanguages ? WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive : 0;
            row.Attributes = attributes;
        }

        private void AddWixActionTuple(WixActionTuple tuple, WindowsInstallerData output)
        {
            // Get the table definition for the action (and ensure the proper table exists for a module).
            TableDefinition sequenceTableDefinition = null;
            switch (tuple.SequenceTable)
            {
                case SequenceTable.AdminExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdminExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdminExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdminExecuteSequence"];
                    }
                    break;
                case SequenceTable.AdminUISequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdminUISequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdminUISequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdminUISequence"];
                    }
                    break;
                case SequenceTable.AdvertiseExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdvtExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdvtExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdvtExecuteSequence"];
                    }
                    break;
                case SequenceTable.InstallExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["InstallExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleInstallExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["InstallExecuteSequence"];
                    }
                    break;
                case SequenceTable.InstallUISequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["InstallUISequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleInstallUISequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["InstallUISequence"];
                    }
                    break;
            }

            // create the action sequence row in the output
            var sequenceTable = output.EnsureTable(sequenceTableDefinition);
            var row = sequenceTable.CreateRow(tuple.SourceLineNumbers);

            if (SectionType.Module == this.Section.Type)
            {
                row[0] = tuple.Action;
                if (0 != tuple.Sequence)
                {
                    row[1] = tuple.Sequence;
                }
                else
                {
                    bool after = (null == tuple.Before);
                    row[2] = after ? tuple.After : tuple.Before;
                    row[3] = after ? 1 : 0;
                }
                row[4] = tuple.Condition;
            }
            else
            {
                row[0] = tuple.Action;
                row[1] = tuple.Condition;
                row[2] = tuple.Sequence;
            }
        }
        
        private void AddWixCustomRowTuple(WixCustomRowTuple tuple, WindowsInstallerData output)
        {
            var customTableDefinition = this.TableDefinitions[tuple.Table];

            if (customTableDefinition.Unreal)
            {

                return;
            }

            var customTable = output.EnsureTable(customTableDefinition);
            var customRow = customTable.CreateRow(tuple.SourceLineNumbers);

#if TODO // SectionId seems like a good thing to preserve.
            customRow.SectionId = tuple.SectionId;
#endif

            var data = tuple.FieldDataSeparated;

            for (var i = 0; i < data.Length; ++i)
            {
                var foundColumn = false;
                var item = data[i].Split(ColonCharacter, 2);

                for (var j = 0; j < customRow.Fields.Length; ++j)
                {
                    if (customRow.Fields[j].Column.Name == item[0])
                    {
                        if (0 < item[1].Length)
                        {
                            if (ColumnType.Number == customRow.Fields[j].Column.Type)
                            {
                                try
                                {
                                    customRow.Fields[j].Data = Convert.ToInt32(item[1], CultureInfo.InvariantCulture);
                                }
                                catch (FormatException)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIntegerValue(tuple.SourceLineNumbers, customTableDefinition.Columns[i].Name, customTableDefinition.Name, item[1]));
                                }
                                catch (OverflowException)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIntegerValue(tuple.SourceLineNumbers, customTableDefinition.Columns[i].Name, customTableDefinition.Name, item[1]));
                                }
                            }
                            else if (ColumnCategory.Identifier == customRow.Fields[j].Column.Category)
                            {
                                if (Common.IsIdentifier(item[1]) || Common.IsValidBinderVariable(item[1]) || ColumnCategory.Formatted == customRow.Fields[j].Column.Category)
                                {
                                    customRow.Fields[j].Data = item[1];
                                }
                                else
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIdentifier(tuple.SourceLineNumbers, "Data", item[1]));
                                }
                            }
                            else
                            {
                                customRow.Fields[j].Data = item[1];
                            }
                        }
                        foundColumn = true;
                        break;
                    }
                }

                if (!foundColumn)
                {
                    this.Messaging.Write(ErrorMessages.UnexpectedCustomTableColumn(tuple.SourceLineNumbers, item[0]));
                }
            }

            for (var i = 0; i < customTableDefinition.Columns.Length; ++i)
            {
                if (!customTableDefinition.Columns[i].Nullable && (null == customRow.Fields[i].Data || 0 == customRow.Fields[i].Data.ToString().Length))
                {
                    this.Messaging.Write(ErrorMessages.NoDataForColumn(tuple.SourceLineNumbers, customTableDefinition.Columns[i].Name, customTableDefinition.Name));
                }
            }
        }

        private void AddWixEnsureTableTuple(WixEnsureTableTuple tuple, WindowsInstallerData output)
        {
            var tableDefinition = this.TableDefinitions[tuple.Table];
            output.EnsureTable(tableDefinition);
        }

        private void AddWixMediaTemplateTuple(WixMediaTemplateTuple tuple, WindowsInstallerData output)
        {
            var table = output.EnsureTable(this.TableDefinitions["WixMediaTemplate"]);
            var row = (WixMediaTemplateRow)table.CreateRow(tuple.SourceLineNumbers);
            row.CabinetTemplate = tuple.CabinetTemplate;
            row.CompressionLevel = tuple.CompressionLevel;
            row.DiskPrompt = tuple.DiskPrompt;
            row.VolumeLabel = tuple.VolumeLabel;
            row.MaximumUncompressedMediaSize = tuple.MaximumUncompressedMediaSize ?? DefaultMaximumUncompressedMediaSize;
            row.MaximumCabinetSizeForLargeFileSplitting = tuple.MaximumCabinetSizeForLargeFileSplitting ?? MaxValueOfMaxCabSizeForLargeFileSplitting;
        }

        private void AddTupleFromExtension(IntermediateTuple tuple, WindowsInstallerData output)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryAddTupleToOutput(tuple, output))
                {
                    break;
                }
            }
        }

        private void AddTupleDefaultly(IntermediateTuple tuple, WindowsInstallerData output, bool idIsPrimaryKey = false, string tableName = null)
        {
            if (!this.TableDefinitions.TryGet(tableName ?? tuple.Definition.Name, out var tableDefinition))
            {
                return;
            }

            var table = output.EnsureTable(tableDefinition);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            var rowOffset = 0;

            if (idIsPrimaryKey)
            {
                row[0] = tuple.Id.Id;
                rowOffset = 1;
            }

            for (var i = 0; i < tuple.Fields.Length; ++i)
            {
                if (i < tableDefinition.Columns.Length)
                {
                    var column = tableDefinition.Columns[i + rowOffset];

                    switch (column.Type)
                    {
                        case ColumnType.Number:
                            row[i + rowOffset] = column.Nullable ? tuple.AsNullableNumber(i) : tuple.AsNumber(i);
                            break;

                        default:
                            row[i + rowOffset] = tuple.AsString(i);
                            break;
                    }
                }
            }
        }

        private static OutputType SectionTypeToOutputType(SectionType type)
        {
            switch (type)
            {
                case SectionType.Bundle:
                    return OutputType.Bundle;
                case SectionType.Module:
                    return OutputType.Module;
                case SectionType.Product:
                    return OutputType.Product;
                case SectionType.PatchCreation:
                    return OutputType.PatchCreation;
                case SectionType.Patch:
                    return OutputType.Patch;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static string GetMsiFilenameValue(string shortName, string longName)
        {
            if (String.IsNullOrEmpty(shortName))
            {
                return longName;
            }
            else
            {
                return shortName + "|" + longName;
            }
        }
    }
}
