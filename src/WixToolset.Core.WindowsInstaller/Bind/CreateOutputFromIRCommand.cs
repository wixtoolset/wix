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

    internal class CreateOutputFromIRCommand
    {
        private const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB
        private const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)

        public CreateOutputFromIRCommand(IntermediateSection section, TableDefinitionCollection tableDefinitions, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions)
        {
            this.Section = section;
            this.TableDefinitions = tableDefinitions;
            this.BackendExtensions = backendExtensions;
        }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private IntermediateSection Section { get; }

        public Output Output { get; private set; }

        public void Execute()
        {
            var output = new Output(this.Section.Tuples.First().SourceLineNumbers);
            output.Codepage = this.Section.Codepage;
            output.Type = SectionTypeToOutputType(this.Section.Type);

            this.AddSectionToOutput(this.Section, output);

            this.Output = output;
        }

        private void AddSectionToOutput(IntermediateSection section, Output output)
        {
            foreach (var tuple in section.Tuples)
            {
                switch (tuple.Definition.Type)
                {
                case TupleDefinitionType.Binary:
                    this.AddTupleDefaultly(tuple, output, true);
                    break;

                case TupleDefinitionType.BBControl:
                    this.AddBBControlTuple((BBControlTuple)tuple, output);
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

                case TupleDefinitionType.MsiServiceConfig:
                    this.AddMsiServiceConfigTuple((MsiServiceConfigTuple)tuple, output);
                    break;

                case TupleDefinitionType.MsiServiceConfigFailureActions:
                    this.AddMsiServiceConfigFailureActionsTuple((MsiServiceConfigFailureActionsTuple)tuple, output);
                    break;

                case TupleDefinitionType.MoveFile:
                    this.AddMoveFileTuple((MoveFileTuple)tuple, output);
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

                case TupleDefinitionType.ServiceControl:
                    this.AddServiceControlTuple((ServiceControlTuple)tuple, output);
                    break;

                case TupleDefinitionType.ServiceInstall:
                    this.AddServiceInstallTuple((ServiceInstallTuple)tuple, output);
                    break;

                case TupleDefinitionType.Shortcut:
                    this.AddTupleDefaultly(tuple, output, true);
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

                // ignored.
                case TupleDefinitionType.WixFile:
                case TupleDefinitionType.WixComponentGroup:
                case TupleDefinitionType.WixDeltaPatchFile:
                    break;

                default:
                    this.AddTupleDefaultly(tuple, output);
                    break;
                }
            }
        }

        private void AddBBControlTuple(BBControlTuple tuple, Output output)
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

        private void AddControlTuple(ControlTuple tuple, Output output)
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

        private void AddComponentTuple(ComponentTuple tuple, Output output)
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

        private void AddCustomActionTuple(CustomActionTuple tuple, Output output)
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
            row[4] = tuple.PatchUninstall ? WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall : 0;
        }

        private void AddDialogTuple(DialogTuple tuple, Output output)
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
        }

        private void AddDirectoryTuple(DirectoryTuple tuple, Output output)
        {
            var table = output.EnsureTable(this.TableDefinitions["Directory"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.ParentDirectoryRef;
            row[2] = tuple.DefaultDir;
        }

        private void AddEnvironmentTuple(EnvironmentTuple tuple, Output output)
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

        private void AddFeatureTuple(FeatureTuple tuple, Output output)
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

        private void AddFileTuple(FileTuple tuple, Output output)
        {
            var table = output.EnsureTable(this.TableDefinitions["File"]);
            var row = (FileRow)table.CreateRow(tuple.SourceLineNumbers);
            row.File = tuple.Id.Id;
            row.Component = tuple.ComponentRef;
            row.FileName = GetMsiFilenameValue(tuple.ShortName, tuple.Name);
            row.FileSize = tuple.FileSize;
            row.Version = tuple.Version;
            row.Language = tuple.Language;

            var attributes = tuple.Checksum ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0;
            attributes |= (tuple.Compressed.HasValue && tuple.Compressed.Value) ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0;
            attributes |= (tuple.Compressed.HasValue && !tuple.Compressed.Value) ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0;
            attributes |= tuple.Hidden ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0;
            attributes |= tuple.ReadOnly ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0;
            attributes |= tuple.System ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0;
            attributes |= tuple.Vital ? WindowsInstallerConstants.MsidbFileAttributesVital : 0;
            row.Attributes = attributes;
        }

        private void AddIniFileTuple(IniFileTuple tuple, Output output)
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

        private void AddMediaTuple(MediaTuple tuple, Output output)
        {
            if (this.Section.Type != SectionType.Module)
            {
                var table = output.EnsureTable(this.TableDefinitions["Media"]);
                var row = (MediaRow)table.CreateRow(tuple.SourceLineNumbers);
                row.DiskId = tuple.DiskId;
                row.LastSequence = tuple.LastSequence;
                row.DiskPrompt = tuple.DiskPrompt;
                row.Cabinet = tuple.Cabinet;
                row.VolumeLabel = tuple.VolumeLabel;
                row.Source = tuple.Source;
            }
        }

        private void AddModuleConfigurationTuple(ModuleConfigurationTuple tuple, Output output)
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

        private void AddMsiEmbeddedUITuple(MsiEmbeddedUITuple tuple, Output output)
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

        private void AddMsiServiceConfigTuple(MsiServiceConfigTuple tuple, Output output)
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

        private void AddMsiServiceConfigFailureActionsTuple(MsiServiceConfigFailureActionsTuple tuple, Output output)
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

        private void AddMoveFileTuple(MoveFileTuple tuple, Output output)
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

        private void AddPropertyTuple(PropertyTuple tuple, Output output)
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

        private void AddRemoveFileTuple(RemoveFileTuple tuple, Output output)
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

        private void AddRegistryTuple(RegistryTuple tuple, Output output)
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

        private void AddRegLocatorTuple(RegLocatorTuple tuple, Output output)
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

        private void AddRemoveRegistryTuple(RemoveRegistryTuple tuple, Output output)
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

        private void AddServiceControlTuple(ServiceControlTuple tuple, Output output)
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

        private void AddServiceInstallTuple(ServiceInstallTuple tuple, Output output)
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

        private void AddTextStyleTuple(TextStyleTuple tuple, Output output)
        {
            var styleBits = tuple.Bold ? WindowsInstallerConstants.MsidbTextStyleStyleBitsBold : 0;
            styleBits |= tuple.Italic ? WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic : 0;
            styleBits |= tuple.Strike ? WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike : 0;
            styleBits |= tuple.Underline ? WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline : 0;

            var table = output.EnsureTable(this.TableDefinitions["TextStyle"]);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            row[0] = tuple.Id.Id;
            row[1] = tuple.FaceName;
            row[2] = tuple.Size;
            row[3] = tuple.Color;
            row[4] = styleBits;
        }

        private void AddUpgradeTuple(UpgradeTuple tuple, Output output)
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

        private void AddWixActionTuple(WixActionTuple tuple, Output output)
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

        private void AddWixMediaTemplateTuple(WixMediaTemplateTuple tuple, Output output)
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

        private void AddTupleFromExtension(IntermediateTuple tuple, Output output)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryAddTupleToOutput(tuple, output))
                {
                    break;
                }
            }
        }

        private void AddTupleDefaultly(IntermediateTuple tuple, Output output, bool idIsPrimaryKey = false)
        {
            if (!this.TableDefinitions.TryGet(tuple.Definition.Name, out var tableDefinition))
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
                    var column = tableDefinition.Columns[i];

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
