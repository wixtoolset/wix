// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class CreateWindowsInstallerDataFromIRCommand
    {
        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public CreateWindowsInstallerDataFromIRCommand(IMessaging messaging, IntermediateSection section, TableDefinitionCollection tableDefinitions, int codepage, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions, IWindowsInstallerBackendHelper backendHelper)
        {
            this.Messaging = messaging;
            this.Section = section;
            this.TableDefinitions = tableDefinitions;
            this.Codepage = codepage;
            this.BackendExtensions = backendExtensions;
            this.BackendHelper = backendHelper;
            this.GeneratedShortNames = new Dictionary<string, List<FileSymbol>>();
        }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private IWindowsInstallerBackendHelper BackendHelper { get; }

        private IMessaging Messaging { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private int Codepage { get; }

        private IntermediateSection Section { get; }

        private Dictionary<string, List<FileSymbol>> GeneratedShortNames { get; }

        public WindowsInstallerData Data { get; private set; }

        public WindowsInstallerData Execute()
        {
            this.Data = new WindowsInstallerData(this.Section.Symbols.First().SourceLineNumbers)
            {
                Codepage = this.Codepage,
                Type = SectionTypeToOutputType(this.Section.Type)
            };

            this.AddSectionToData();

            return this.Data;
        }

        private void AddSectionToData()
        {
            var cellsByTableAndRowId = new Dictionary<string, List<WixCustomTableCellSymbol>>();

            foreach (var symbol in this.Section.Symbols)
            {
                var unknownSymbol = false;
                switch (symbol.Definition.Type)
                {
                    case SymbolDefinitionType.AppSearch:
                        this.AddSymbolDefaultly(symbol);
                        this.Data.EnsureTable(this.TableDefinitions["Signature"]);
                        break;

                    case SymbolDefinitionType.Assembly:
                        this.AddAssemblySymbol((AssemblySymbol)symbol);
                        break;

                    case SymbolDefinitionType.BBControl:
                        this.AddBBControlSymbol((BBControlSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Class:
                        this.AddClassSymbol((ClassSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Control:
                        this.AddControlSymbol((ControlSymbol)symbol);
                        break;

                    case SymbolDefinitionType.ControlEvent:
                        this.AddControlEventSymbol((ControlEventSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Component:
                        this.AddComponentSymbol((ComponentSymbol)symbol);
                        break;

                    case SymbolDefinitionType.CustomAction:
                        this.AddCustomActionSymbol((CustomActionSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Dialog:
                        this.AddDialogSymbol((DialogSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Directory:
                        this.AddDirectorySymbol((DirectorySymbol)symbol);
                        break;

                    case SymbolDefinitionType.DuplicateFile:
                        this.AddDuplicateFileSymbol((DuplicateFileSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Environment:
                        this.AddEnvironmentSymbol((EnvironmentSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Error:
                        this.AddErrorSymbol((ErrorSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Feature:
                        this.AddFeatureSymbol((FeatureSymbol)symbol);
                        break;

                    case SymbolDefinitionType.File:
                        this.AddFileSymbol((FileSymbol)symbol);
                        break;

                    case SymbolDefinitionType.IniFile:
                        this.AddIniFileSymbol((IniFileSymbol)symbol);
                        break;

                    case SymbolDefinitionType.IniLocator:
                        this.AddIniLocatorSymbol((IniLocatorSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Media:
                        this.AddMediaSymbol((MediaSymbol)symbol);
                        break;

                    case SymbolDefinitionType.ModuleConfiguration:
                        this.AddModuleConfigurationSymbol((ModuleConfigurationSymbol)symbol);
                        this.EnsureModuleIgnoredTable(symbol, "ModuleConfiguration");
                        break;

                    case SymbolDefinitionType.ModuleSubstitution:
                        this.AddSymbolDefaultly(symbol);
                        this.EnsureModuleIgnoredTable(symbol, "ModuleSubstitution");
                        break;

                    case SymbolDefinitionType.MsiEmbeddedUI:
                        this.AddMsiEmbeddedUISymbol((MsiEmbeddedUISymbol)symbol);
                        break;

                    case SymbolDefinitionType.MsiServiceConfig:
                        this.AddMsiServiceConfigSymbol((MsiServiceConfigSymbol)symbol);
                        break;

                    case SymbolDefinitionType.MsiServiceConfigFailureActions:
                        this.AddMsiServiceConfigFailureActionsSymbol((MsiServiceConfigFailureActionsSymbol)symbol);
                        break;

                    case SymbolDefinitionType.MoveFile:
                        this.AddMoveFileSymbol((MoveFileSymbol)symbol);
                        break;

                    case SymbolDefinitionType.ProgId:
                        this.AddSymbolDefaultly(symbol);
                        this.Data.EnsureTable(this.TableDefinitions["Extension"]);
                        break;

                    case SymbolDefinitionType.Property:
                        this.AddPropertySymbol((PropertySymbol)symbol);
                        break;

                    case SymbolDefinitionType.RemoveFile:
                        this.AddRemoveFileSymbol((RemoveFileSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Registry:
                        this.AddRegistrySymbol((RegistrySymbol)symbol);
                        break;

                    case SymbolDefinitionType.RegLocator:
                        this.AddRegLocatorSymbol((RegLocatorSymbol)symbol);
                        break;

                    case SymbolDefinitionType.RemoveRegistry:
                        this.AddRemoveRegistrySymbol((RemoveRegistrySymbol)symbol);
                        break;

                    case SymbolDefinitionType.ServiceControl:
                        this.AddServiceControlSymbol((ServiceControlSymbol)symbol);
                        break;

                    case SymbolDefinitionType.ServiceInstall:
                        this.AddServiceInstallSymbol((ServiceInstallSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Shortcut:
                        this.AddShortcutSymbol((ShortcutSymbol)symbol);
                        break;

                    case SymbolDefinitionType.TextStyle:
                        this.AddTextStyleSymbol((TextStyleSymbol)symbol);
                        break;

                    case SymbolDefinitionType.Upgrade:
                        this.AddUpgradeSymbol((UpgradeSymbol)symbol);
                        break;

                    case SymbolDefinitionType.WixAction:
                        this.AddWixActionSymbol((WixActionSymbol)symbol);
                        break;

                    case SymbolDefinitionType.WixCustomTableCell:
                        this.IndexCustomTableCellSymbol((WixCustomTableCellSymbol)symbol, cellsByTableAndRowId);
                        break;

                    case SymbolDefinitionType.WixEnsureTable:
                        this.AddWixEnsureTableSymbol((WixEnsureTableSymbol)symbol);
                        break;

                    case SymbolDefinitionType.WixModule:
                        this.AddWixModuleSymbol((WixModuleSymbol)symbol);
                        break;

                    case SymbolDefinitionType.WixPackage:
                        this.AddWixPackageSymbol((WixPackageSymbol)symbol);
                        break;

                    // Symbols used internally and are not added to the output.
                    case SymbolDefinitionType.WixBuildInfo:
                    case SymbolDefinitionType.WixBindUpdatedFiles:
                    case SymbolDefinitionType.WixComponentGroup:
                    case SymbolDefinitionType.WixComplexReference:
                    case SymbolDefinitionType.WixDeltaPatchFile:
                    case SymbolDefinitionType.WixDeltaPatchSymbolPaths:
                    case SymbolDefinitionType.WixFragment:
                    case SymbolDefinitionType.WixFeatureGroup:
                    case SymbolDefinitionType.WixInstanceComponent:
                    case SymbolDefinitionType.WixInstanceTransforms:
                    case SymbolDefinitionType.WixFeatureModules:
                    case SymbolDefinitionType.WixGroup:
                    case SymbolDefinitionType.WixMediaTemplate:
                    case SymbolDefinitionType.WixMerge:
                    case SymbolDefinitionType.WixOrdering:
                    case SymbolDefinitionType.WixPatchBaseline:
                    case SymbolDefinitionType.WixPatchFamilyGroup:
                    case SymbolDefinitionType.WixPatch:
                    case SymbolDefinitionType.WixPatchRef:
                    case SymbolDefinitionType.WixPatchTarget:
                    case SymbolDefinitionType.WixProperty:
                    case SymbolDefinitionType.WixPackageTag:
                    case SymbolDefinitionType.WixSimpleReference:
                    case SymbolDefinitionType.WixSuppressAction:
                    case SymbolDefinitionType.WixSuppressModularization:
                    case SymbolDefinitionType.WixUI:
                    case SymbolDefinitionType.WixVariable:
                        break;

                    // Already processed by LoadTableDefinitions.
                    case SymbolDefinitionType.WixCustomTable:
                    case SymbolDefinitionType.WixCustomTableColumn:
                        break;

                    case SymbolDefinitionType.MustBeFromAnExtension:
                        if (!this.AddSymbolFromExtension(symbol))
                        {
                            unknownSymbol = !this.AddSymbolDefaultly(symbol);
                        }
                        break;

                    default:
                        unknownSymbol = !this.AddSymbolDefaultly(symbol);
                        break;
                }

                if (unknownSymbol)
                {
                    this.Messaging.Write(WarningMessages.SymbolNotTranslatedToOutput(symbol));
                }
            }

            this.AddIndexedCellSymbols(cellsByTableAndRowId);
            this.EnsureRequiredTables();
            this.ReportGeneratedShortFileNameConflicts();
            this.ReportIllegalTables();
            this.ReportMismatchedModularizations();
            this.ReportWindowsInstallerDataInconsistencies();
        }

        private void AddAssemblySymbol(AssemblySymbol symbol)
        {
            var attributes = symbol.Type == AssemblyType.Win32Assembly ? 1 : (int?)null;

            var row = this.CreateRow(symbol, "MsiAssembly");
            row[0] = symbol.ComponentRef;
            row[1] = symbol.FeatureRef;
            row[2] = symbol.ManifestFileRef;
            row[3] = symbol.ApplicationFileRef;
            row[4] = attributes;
        }

        private void AddBBControlSymbol(BBControlSymbol symbol)
        {
            var attributes = symbol.Attributes;
            attributes |= symbol.Enabled ? WindowsInstallerConstants.MsidbControlAttributesEnabled : 0;
            attributes |= symbol.Indirect ? WindowsInstallerConstants.MsidbControlAttributesIndirect : 0;
            attributes |= symbol.Integer ? WindowsInstallerConstants.MsidbControlAttributesInteger : 0;
            attributes |= symbol.LeftScroll ? WindowsInstallerConstants.MsidbControlAttributesLeftScroll : 0;
            attributes |= symbol.RightAligned ? WindowsInstallerConstants.MsidbControlAttributesRightAligned : 0;
            attributes |= symbol.RightToLeft ? WindowsInstallerConstants.MsidbControlAttributesRTLRO : 0;
            attributes |= symbol.Sunken ? WindowsInstallerConstants.MsidbControlAttributesSunken : 0;
            attributes |= symbol.Visible ? WindowsInstallerConstants.MsidbControlAttributesVisible : 0;

            var row = this.CreateRow(symbol, "BBControl");
            row[0] = symbol.BillboardRef;
            row[1] = symbol.BBControl;
            row[2] = symbol.Type;
            row[3] = symbol.X;
            row[4] = symbol.Y;
            row[5] = symbol.Width;
            row[6] = symbol.Height;
            row[7] = attributes;
            row[8] = symbol.Text;
        }

        private void AddClassSymbol(ClassSymbol symbol)
        {
            var row = this.CreateRow(symbol, "Class");
            row[0] = symbol.CLSID;
            row[1] = symbol.Context;
            row[2] = symbol.ComponentRef;
            row[3] = symbol.DefaultProgIdRef;
            row[4] = symbol.Description;
            row[5] = symbol.AppIdRef;
            row[6] = symbol.FileTypeMask;
            row[7] = symbol.IconRef;
            row[8] = symbol.IconIndex;
            row[9] = symbol.DefInprocHandler;
            row[10] = symbol.Argument;
            row[11] = symbol.FeatureRef;
            row[12] = symbol.RelativePath ? (int?)1 : null;
        }

        private void AddControlSymbol(ControlSymbol symbol)
        {
            var text = symbol.Text;
            var attributes = symbol.Attributes;
            attributes |= symbol.Enabled ? WindowsInstallerConstants.MsidbControlAttributesEnabled : 0;
            attributes |= symbol.Indirect ? WindowsInstallerConstants.MsidbControlAttributesIndirect : 0;
            attributes |= symbol.Integer ? WindowsInstallerConstants.MsidbControlAttributesInteger : 0;
            attributes |= symbol.LeftScroll ? WindowsInstallerConstants.MsidbControlAttributesLeftScroll : 0;
            attributes |= symbol.RightAligned ? WindowsInstallerConstants.MsidbControlAttributesRightAligned : 0;
            attributes |= symbol.RightToLeft ? WindowsInstallerConstants.MsidbControlAttributesRTLRO : 0;
            attributes |= symbol.Sunken ? WindowsInstallerConstants.MsidbControlAttributesSunken : 0;
            attributes |= symbol.Visible ? WindowsInstallerConstants.MsidbControlAttributesVisible : 0;

            // If we're tracking disk space, and this is a non-FormatSize Text control,
            // and the text attribute starts with '[' and ends with ']', add a space.
            // It is not necessary for the whole string to be a property, just those
            // two characters matter.
            if (symbol.TrackDiskSpace &&
                "Text" == symbol.Type &&
                WindowsInstallerConstants.MsidbControlAttributesFormatSize != (attributes & WindowsInstallerConstants.MsidbControlAttributesFormatSize) &&
                null != text && text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
            {
                text = String.Concat(text, " ");
            }

            var row = this.CreateRow(symbol, "Control");
            row[0] = symbol.DialogRef;
            row[1] = symbol.Control;
            row[2] = symbol.Type;
            row[3] = symbol.X;
            row[4] = symbol.Y;
            row[5] = symbol.Width;
            row[6] = symbol.Height;
            row[7] = attributes;
            row[8] = symbol.Property;
            row[9] = text;
            row[10] = symbol.NextControlRef;
            row[11] = symbol.Help;
        }

        private void AddControlEventSymbol(ControlEventSymbol symbol)
        {
            var row = this.CreateRow(symbol, "ControlEvent");
            row[0] = symbol.DialogRef;
            row[1] = symbol.ControlRef;
            row[2] = symbol.Event;
            row[3] = symbol.Argument;
            row[4] = String.IsNullOrEmpty(symbol.Condition) ? "1" : symbol.Condition;
            row[5] = symbol.Ordering;
        }

        private void AddComponentSymbol(ComponentSymbol symbol)
        {
            var attributes = ComponentLocation.Either == symbol.Location ? WindowsInstallerConstants.MsidbComponentAttributesOptional : 0;
            attributes |= ComponentLocation.SourceOnly == symbol.Location ? WindowsInstallerConstants.MsidbComponentAttributesSourceOnly : 0;
            attributes |= ComponentKeyPathType.Registry == symbol.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath : 0;
            attributes |= ComponentKeyPathType.OdbcDataSource == symbol.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource : 0;
            attributes |= symbol.DisableRegistryReflection ? WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection : 0;
            attributes |= symbol.NeverOverwrite ? WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite : 0;
            attributes |= symbol.Permanent ? WindowsInstallerConstants.MsidbComponentAttributesPermanent : 0;
            attributes |= symbol.SharedDllRefCount ? WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount : 0;
            attributes |= symbol.Shared ? WindowsInstallerConstants.MsidbComponentAttributesShared : 0;
            attributes |= symbol.Transitive ? WindowsInstallerConstants.MsidbComponentAttributesTransitive : 0;
            attributes |= symbol.UninstallWhenSuperseded ? WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence : 0;
            attributes |= symbol.Win64 ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;

            var row = this.CreateRow(symbol, "Component");
            row[0] = symbol.Id.Id;
            row[1] = symbol.ComponentId;
            row[2] = symbol.DirectoryRef;
            row[3] = attributes;
            row[4] = symbol.Condition;
            row[5] = symbol.KeyPath;
        }

        private void AddCustomActionSymbol(CustomActionSymbol symbol)
        {
            var type = symbol.Win64 ? WindowsInstallerConstants.MsidbCustomActionType64BitScript : 0;
            type |= symbol.IgnoreResult ? WindowsInstallerConstants.MsidbCustomActionTypeContinue : 0;
            type |= symbol.Hidden ? WindowsInstallerConstants.MsidbCustomActionTypeHideTarget : 0;
            type |= symbol.Async ? WindowsInstallerConstants.MsidbCustomActionTypeAsync : 0;
            type |= CustomActionExecutionType.FirstSequence == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence : 0;
            type |= CustomActionExecutionType.OncePerProcess == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess : 0;
            type |= CustomActionExecutionType.ClientRepeat == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat : 0;
            type |= CustomActionExecutionType.Deferred == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript : 0;
            type |= CustomActionExecutionType.Rollback == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback : 0;
            type |= CustomActionExecutionType.Commit == symbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit : 0;
            type |= CustomActionSourceType.File == symbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeSourceFile : 0;
            type |= CustomActionSourceType.Directory == symbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeDirectory : 0;
            type |= CustomActionSourceType.Property == symbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeProperty : 0;
            type |= CustomActionTargetType.Dll == symbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeDll : 0;
            type |= CustomActionTargetType.Exe == symbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeExe : 0;
            type |= CustomActionTargetType.TextData == symbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeTextData : 0;
            type |= CustomActionTargetType.JScript == symbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeJScript : 0;
            type |= CustomActionTargetType.VBScript == symbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeVBScript : 0;

            if (WindowsInstallerConstants.MsidbCustomActionTypeInScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeInScript))
            {
                type |= symbol.Impersonate ? 0 : WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate;
                type |= symbol.TSAware ? WindowsInstallerConstants.MsidbCustomActionTypeTSAware : 0;
            }

            var row = this.CreateRow(symbol, "CustomAction");
            row[0] = symbol.Id.Id;
            row[1] = type;
            row[2] = symbol.Source;
            row[3] = symbol.Target;
            row[4] = symbol.PatchUninstall ? (int?)WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall : null;

            if (OutputType.Module == this.Data.Type)
            {
                this.Data.EnsureTable(this.TableDefinitions["AdminExecuteSequence"]);
                this.Data.EnsureTable(this.TableDefinitions["AdminUISequence"]);
                this.Data.EnsureTable(this.TableDefinitions["AdvtExecuteSequence"]);
                this.Data.EnsureTable(this.TableDefinitions["InstallExecuteSequence"]);
                this.Data.EnsureTable(this.TableDefinitions["InstallUISequence"]);
            }
        }

        private void AddDialogSymbol(DialogSymbol symbol)
        {
            var attributes = symbol.Visible ? WindowsInstallerConstants.MsidbDialogAttributesVisible : 0;
            attributes |= symbol.Modal ? WindowsInstallerConstants.MsidbDialogAttributesModal : 0;
            attributes |= symbol.Minimize ? WindowsInstallerConstants.MsidbDialogAttributesMinimize : 0;
            attributes |= symbol.CustomPalette ? WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette : 0;
            attributes |= symbol.ErrorDialog ? WindowsInstallerConstants.MsidbDialogAttributesError : 0;
            attributes |= symbol.LeftScroll ? WindowsInstallerConstants.MsidbDialogAttributesLeftScroll : 0;
            attributes |= symbol.KeepModeless ? WindowsInstallerConstants.MsidbDialogAttributesKeepModeless : 0;
            attributes |= symbol.RightAligned ? WindowsInstallerConstants.MsidbDialogAttributesRightAligned : 0;
            attributes |= symbol.RightToLeft ? WindowsInstallerConstants.MsidbDialogAttributesRTLRO : 0;
            attributes |= symbol.SystemModal ? WindowsInstallerConstants.MsidbDialogAttributesSysModal : 0;
            attributes |= symbol.TrackDiskSpace ? WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace : 0;

            var row = this.CreateRow(symbol, "Dialog");
            row[0] = symbol.Id.Id;
            row[1] = symbol.HCentering;
            row[2] = symbol.VCentering;
            row[3] = symbol.Width;
            row[4] = symbol.Height;
            row[5] = attributes;
            row[6] = symbol.Title;
            row[7] = symbol.FirstControlRef;
            row[8] = symbol.DefaultControlRef;
            row[9] = symbol.CancelControlRef;

            this.Data.EnsureTable(this.TableDefinitions["ListBox"]);
        }

        private void AddDirectorySymbol(DirectorySymbol symbol)
        {
            (var name, var parentDir) = this.AddDirectorySubdirectories(symbol);

            var shortName = symbol.ShortName;
            var sourceShortname = symbol.SourceShortName;

            if (String.IsNullOrEmpty(shortName) && name != null && name != "." && name != "SourceDir" && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                shortName = this.CreateShortName(name, false, "Directory", symbol.ParentDirectoryRef);
            }

            if (String.IsNullOrEmpty(sourceShortname) && !String.IsNullOrEmpty(symbol.SourceName) && symbol.SourceName != "." && !this.BackendHelper.IsValidShortFilename(symbol.SourceName, false))
            {
                sourceShortname = this.CreateShortName(symbol.SourceName, false, "Directory", symbol.ParentDirectoryRef);
            }

            var sourceName = CreateMsiFilename(sourceShortname, symbol.SourceName);
            var targetName = CreateMsiFilename(shortName, name);

            if (String.IsNullOrEmpty(targetName))
            {
                targetName = ".";
            }

            var defaultDir = String.IsNullOrEmpty(sourceName) || sourceName == targetName ? targetName : targetName + ":" + sourceName;

            var row = this.CreateRow(symbol, "Directory");
            row[0] = symbol.Id.Id;
            row[1] = parentDir;
            row[2] = defaultDir;

            if (OutputType.Module == this.Data.Type)
            {
                var directoryId = symbol.Id.Id;

                if (WindowsInstallerStandard.IsStandardDirectory(directoryId))
                {
                    // If the directory table contains references to standard windows folders
                    // mergemod.dll will add customactions to set the MSM directory to
                    // the same directory as the standard windows folder and will add references to
                    // custom action to all the standard sequence tables. A problem will occur
                    // if the MSI does not have these tables as mergemod.dll does not add these
                    // tables to the MSI if absent. This code adds the tables in case mergemod.dll
                    // needs them.
                    this.Data.EnsureTable(this.TableDefinitions["CustomAction"]);
                    this.Data.EnsureTable(this.TableDefinitions["AdminExecuteSequence"]);
                    this.Data.EnsureTable(this.TableDefinitions["AdminUISequence"]);
                    this.Data.EnsureTable(this.TableDefinitions["AdvtExecuteSequence"]);
                    this.Data.EnsureTable(this.TableDefinitions["InstallExecuteSequence"]);
                    this.Data.EnsureTable(this.TableDefinitions["InstallUISequence"]);
                }
                else
                {
                    foreach (var standardDirectory in WindowsInstallerStandard.StandardDirectories())
                    {
                        if (directoryId.StartsWith(standardDirectory.Id.Id, StringComparison.Ordinal))
                        {
                            this.Messaging.Write(WarningMessages.StandardDirectoryConflictInMergeModule(symbol.SourceLineNumbers, directoryId, standardDirectory.Id.Id));
                        }
                    }
                }
            }
        }

        private void AddDuplicateFileSymbol(DuplicateFileSymbol symbol)
        {
            var name = symbol.DestinationName;
            if (null == symbol.DestinationShortName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.DestinationShortName = this.CreateShortName(name, true, "CopyFile", symbol.ComponentRef, symbol.FileRef);
            }

            var row = this.CreateRow(symbol, "DuplicateFile");
            row[0] = symbol.Id.Id;
            row[1] = symbol.ComponentRef;
            row[2] = symbol.FileRef;
            row[3] = CreateMsiFilename(symbol.DestinationShortName, symbol.DestinationName);
            row[4] = symbol.DestinationFolder;
        }

        private void AddEnvironmentSymbol(EnvironmentSymbol symbol)
        {
            var action = String.Empty;
            var system = symbol.System ? "*" : String.Empty;
            var uninstall = symbol.Permanent ? String.Empty : "-";
            var value = symbol.Value;

            switch (symbol.Action)
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

            switch (symbol.Part)
            {
                case EnvironmentPartType.First:
                    value = String.Concat(value, symbol.Separator, "[~]");
                    break;
                case EnvironmentPartType.Last:
                    value = String.Concat("[~]", symbol.Separator, value);
                    break;
            }

            var row = this.CreateRow(symbol, "Environment");
            row[0] = symbol.Id.Id;
            row[1] = String.Concat(action, uninstall, system, symbol.Name);
            row[2] = value;
            row[3] = symbol.ComponentRef;
        }

        private void AddErrorSymbol(ErrorSymbol symbol)
        {
            var row = this.CreateRow(symbol, "Error");
            row[0] = Convert.ToInt32(symbol.Id.Id);
            row[1] = symbol.Message;
        }

        private void AddFeatureSymbol(FeatureSymbol symbol)
        {
            var attributes = symbol.DisallowAbsent ? WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent : 0;
            attributes |= symbol.DisallowAdvertise ? WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise : 0;
            attributes |= FeatureInstallDefault.FollowParent == symbol.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFollowParent : 0;
            attributes |= FeatureInstallDefault.Source == symbol.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorSource : 0;
            attributes |= FeatureTypicalDefault.Advertise == symbol.TypicalDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise : 0;

            var row = this.CreateRow(symbol, "Feature");
            row[0] = symbol.Id.Id;
            row[1] = symbol.ParentFeatureRef;
            row[2] = symbol.Title;
            row[3] = symbol.Description;
            row[4] = symbol.Display;
            row[5] = symbol.Level;
            row[6] = symbol.DirectoryRef;
            row[7] = attributes;
        }

        private void AddFileSymbol(FileSymbol symbol)
        {
            var name = symbol.Name;
            if (null == symbol.ShortName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.ShortName = this.CreateShortName(name, true, "File", symbol.DirectoryRef);

                if (!this.GeneratedShortNames.TryGetValue(symbol.ShortName, out var potentialConflicts))
                {
                    potentialConflicts = new List<FileSymbol>();
                    this.GeneratedShortNames.Add(symbol.ShortName, potentialConflicts);
                }

                potentialConflicts.Add(symbol);
            }

            var row = (FileRow)this.CreateRow(symbol, "File");
            row.File = symbol.Id.Id;
            row.Component = symbol.ComponentRef;
            row.FileName = CreateMsiFilename(symbol.ShortName, name);
            row.FileSize = symbol.FileSize;
            row.Version = symbol.Version;
            row.Language = symbol.Language;
            row.Sequence = symbol.Sequence;
            row.DiskId = symbol.DiskId ?? throw new InvalidDataException("FileSymbol.DiskId should have been initialized before creating WindowsInstallerData from IntermediateRepresentation.");
            row.Source = symbol.Source.Path;

            var previousSourceField = symbol.Fields[(int)FileSymbolFields.Source]?.PreviousValue;
            row.PreviousSource = previousSourceField?.AsPath().Path;

            var attributes = (symbol.Attributes & FileSymbolAttributes.Checksum) == FileSymbolAttributes.Checksum ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.Compressed) == FileSymbolAttributes.Compressed ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.Uncompressed) == FileSymbolAttributes.Uncompressed ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.Hidden) == FileSymbolAttributes.Hidden ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.ReadOnly) == FileSymbolAttributes.ReadOnly ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.System) == FileSymbolAttributes.System ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0;
            attributes |= (symbol.Attributes & FileSymbolAttributes.Vital) == FileSymbolAttributes.Vital ? WindowsInstallerConstants.MsidbFileAttributesVital : 0;
            row.Attributes = attributes;

            // Note that TrueType fonts are denoted by the empty string in the FontTitle
            // field. So, non-null means a font is present.
            if (symbol.FontTitle != null)
            {
                var fontRow = this.CreateRow(symbol, "Font");
                fontRow[0] = symbol.Id.Id;
                fontRow[1] = symbol.FontTitle;
            }

            if (symbol.SelfRegCost.HasValue)
            {
                var selfRegRow = this.CreateRow(symbol, "SelfReg");
                selfRegRow[0] = symbol.Id.Id;
                selfRegRow[1] = symbol.SelfRegCost.Value;
            }
        }

        private void AddIniFileSymbol(IniFileSymbol symbol)
        {
            var tableName = (IniFileActionType.AddLine == symbol.Action || IniFileActionType.AddTag == symbol.Action || IniFileActionType.CreateLine == symbol.Action) ? "IniFile" : "RemoveIniFile";

            var name = symbol.FileName;
            if (null == symbol.ShortFileName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.ShortFileName = this.CreateShortName(name, true, "IniFile", symbol.ComponentRef);
            }

            var row = this.CreateRow(symbol, tableName);
            row[0] = symbol.Id.Id;
            row[1] = CreateMsiFilename(symbol.ShortFileName, name);
            row[2] = symbol.DirProperty;
            row[3] = symbol.Section;
            row[4] = symbol.Key;
            row[5] = symbol.Value;
            row[6] = symbol.Action;
            row[7] = symbol.ComponentRef;
        }

        private void AddIniLocatorSymbol(IniLocatorSymbol symbol)
        {
            var name = symbol.FileName;
            if (null == symbol.ShortFileName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.ShortFileName = this.CreateShortName(name, true, "IniFileSearch");
            }

            var row = this.CreateRow(symbol, "IniLocator");
            row[0] = symbol.Id.Id;
            row[1] = CreateMsiFilename(symbol.ShortFileName, name);
            row[2] = symbol.Section;
            row[3] = symbol.Key;
            row[4] = symbol.Field;
            row[5] = symbol.Type;
        }

        private void AddMediaSymbol(MediaSymbol symbol)
        {
            if (this.Section.Type != SectionType.Module)
            {
                var row = (MediaRow)this.CreateRow(symbol, "Media");
                row.DiskId = symbol.DiskId;
                row.LastSequence = symbol.LastSequence ?? 0;
                row.DiskPrompt = symbol.DiskPrompt;
                row.Cabinet = symbol.Cabinet;
                row.VolumeLabel = symbol.VolumeLabel;
                row.Source = symbol.Source;
            }
        }

        private void AddModuleConfigurationSymbol(ModuleConfigurationSymbol symbol)
        {
            var row = this.CreateRow(symbol, "ModuleConfiguration");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Format;
            row[2] = symbol.Type;
            row[3] = symbol.ContextData;
            row[4] = symbol.DefaultValue;
            row[5] = (symbol.KeyNoOrphan ? WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan : 0) |
                     (symbol.NonNullable ? WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable : 0);
            row[6] = symbol.DisplayName;
            row[7] = symbol.Description;
            row[8] = symbol.HelpLocation;
            row[9] = symbol.HelpKeyword;
        }

        private void AddMsiEmbeddedUISymbol(MsiEmbeddedUISymbol symbol)
        {
            var attributes = symbol.EntryPoint ? WindowsInstallerConstants.MsidbEmbeddedUI : 0;
            attributes |= symbol.SupportsBasicUI ? WindowsInstallerConstants.MsidbEmbeddedHandlesBasic : 0;

            var row = this.CreateRow(symbol, "MsiEmbeddedUI");
            row[0] = symbol.Id.Id;
            row[1] = symbol.FileName;
            row[2] = attributes;
            row[3] = symbol.MessageFilter;
            row[4] = symbol.Source;
        }

        private void AddMsiServiceConfigSymbol(MsiServiceConfigSymbol symbol)
        {
            var events = symbol.OnInstall ? WindowsInstallerConstants.MsidbServiceConfigEventInstall : 0;
            events |= symbol.OnReinstall ? WindowsInstallerConstants.MsidbServiceConfigEventReinstall : 0;
            events |= symbol.OnUninstall ? WindowsInstallerConstants.MsidbServiceConfigEventUninstall : 0;

            var row = this.CreateRow(symbol, "MsiServiceConfig");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Name;
            row[2] = events;
            row[3] = symbol.ConfigType;
            row[4] = symbol.Argument;
            row[5] = symbol.ComponentRef;
        }

        private void AddMsiServiceConfigFailureActionsSymbol(MsiServiceConfigFailureActionsSymbol symbol)
        {
            var events = symbol.OnInstall ? WindowsInstallerConstants.MsidbServiceConfigEventInstall : 0;
            events |= symbol.OnReinstall ? WindowsInstallerConstants.MsidbServiceConfigEventReinstall : 0;
            events |= symbol.OnUninstall ? WindowsInstallerConstants.MsidbServiceConfigEventUninstall : 0;

            var row = this.CreateRow(symbol, "MsiServiceConfigFailureActions");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Name;
            row[2] = events;
            row[3] = symbol.ResetPeriod.HasValue ? symbol.ResetPeriod : null;
            row[4] = symbol.RebootMessage ?? "[~]";
            row[5] = symbol.Command ?? "[~]";
            row[6] = symbol.Actions;
            row[7] = symbol.DelayActions;
            row[8] = symbol.ComponentRef;
        }

        private void AddMoveFileSymbol(MoveFileSymbol symbol)
        {
            var name = symbol.DestinationName;
            if (null == symbol.DestinationShortName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.DestinationShortName = this.CreateShortName(name, true, "MoveFile", symbol.ComponentRef);
            }

            var row = this.CreateRow(symbol, "MoveFile");
            row[0] = symbol.Id.Id;
            row[1] = symbol.ComponentRef;
            row[2] = symbol.SourceName;
            row[3] = CreateMsiFilename(symbol.DestinationShortName, symbol.DestinationName);
            row[4] = symbol.SourceFolder;
            row[5] = symbol.DestFolder;
            row[6] = symbol.Delete ? WindowsInstallerConstants.MsidbMoveFileOptionsMove : 0;
        }

        private void AddPropertySymbol(PropertySymbol symbol)
        {
            if (String.IsNullOrEmpty(symbol.Value))
            {
                return;
            }

            var row = (PropertyRow)this.CreateRow(symbol, "Property");
            row.Property = symbol.Id.Id;
            row.Value = symbol.Value;
        }

        private void AddRemoveFileSymbol(RemoveFileSymbol symbol)
        {
            var name = symbol.FileName;
            if (null == symbol.ShortFileName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.ShortFileName = this.CreateShortName(name, true, "RemoveFile", symbol.ComponentRef);
            }

            var installMode = symbol.OnInstall == true ? WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall : 0;
            installMode |= symbol.OnUninstall == true ? WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove : 0;

            var row = this.CreateRow(symbol, "RemoveFile");
            row[0] = symbol.Id.Id;
            row[1] = symbol.ComponentRef;
            row[2] = CreateMsiFilename(symbol.ShortFileName, symbol.FileName);
            row[3] = symbol.DirPropertyRef;
            row[4] = installMode;
        }

        private void AddRegistrySymbol(RegistrySymbol symbol)
        {
            var value = symbol.Value;

            switch (symbol.ValueType)
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
                    switch (symbol.ValueAction)
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

            var row = this.CreateRow(symbol, "Registry");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Root;
            row[2] = symbol.Key;
            row[3] = symbol.Name;
            row[4] = value;
            row[5] = symbol.ComponentRef;
        }

        private void AddRegLocatorSymbol(RegLocatorSymbol symbol)
        {
            var type = (int)symbol.Type;
            type |= symbol.Win64 ? WindowsInstallerConstants.MsidbLocatorType64bit : 0;

            var row = this.CreateRow(symbol, "RegLocator");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Root;
            row[2] = symbol.Key;
            row[3] = symbol.Name;
            row[4] = type;
        }

        private void AddRemoveRegistrySymbol(RemoveRegistrySymbol symbol)
        {
            if (symbol.Action == RemoveRegistryActionType.RemoveOnInstall)
            {
                var row = this.CreateRow(symbol, "RemoveRegistry");
                row[0] = symbol.Id.Id;
                row[1] = symbol.Root;
                row[2] = symbol.Key;
                row[3] = symbol.Name;
                row[4] = symbol.ComponentRef;
            }
            else // Registry table is used to remove registry keys on uninstall.
            {
                var row = this.CreateRow(symbol, "Registry");
                row[0] = symbol.Id.Id;
                row[1] = symbol.Root;
                row[2] = symbol.Key;
                row[3] = symbol.Name;
                row[5] = symbol.ComponentRef;
            }
        }

        private void AddServiceControlSymbol(ServiceControlSymbol symbol)
        {
            var events = symbol.InstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventDelete : 0;
            events |= symbol.UninstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete : 0;
            events |= symbol.InstallStart ? WindowsInstallerConstants.MsidbServiceControlEventStart : 0;
            events |= symbol.UninstallStart ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStart : 0;
            events |= symbol.InstallStop ? WindowsInstallerConstants.MsidbServiceControlEventStop : 0;
            events |= symbol.UninstallStop ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStop : 0;

            var row = this.CreateRow(symbol, "ServiceControl");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Name;
            row[2] = events;
            row[3] = symbol.Arguments;
            if (symbol.Wait.HasValue)
            {
                row[4] = symbol.Wait.Value ? 1 : 0;
            }
            row[5] = symbol.ComponentRef;
        }

        private void AddServiceInstallSymbol(ServiceInstallSymbol symbol)
        {
            var errorControl = (int)symbol.ErrorControl;
            errorControl |= symbol.Vital ? WindowsInstallerConstants.MsidbServiceInstallErrorControlVital : 0;

            var serviceType = (int)symbol.ServiceType;
            serviceType |= symbol.Interactive ? WindowsInstallerConstants.MsidbServiceInstallInteractive : 0;

            var row = this.CreateRow(symbol, "ServiceInstall");
            row[0] = symbol.Id.Id;
            row[1] = symbol.Name;
            row[2] = symbol.DisplayName;
            row[3] = serviceType;
            row[4] = (int)symbol.StartType;
            row[5] = errorControl;
            row[6] = symbol.LoadOrderGroup;
            row[7] = symbol.Dependencies;
            row[8] = symbol.StartName;
            row[9] = symbol.Password;
            row[10] = symbol.Arguments;
            row[11] = symbol.ComponentRef;
            row[12] = symbol.Description;
        }

        private void AddShortcutSymbol(ShortcutSymbol symbol)
        {
            var name = symbol.Name;
            if (null == symbol.ShortName && null != name && !this.BackendHelper.IsValidShortFilename(name, false))
            {
                symbol.ShortName = this.CreateShortName(name, true, "Shortcut", symbol.ComponentRef, symbol.DirectoryRef);
            }

            var row = this.CreateRow(symbol, "Shortcut");
            row[0] = symbol.Id.Id;
            row[1] = symbol.DirectoryRef;
            row[2] = CreateMsiFilename(symbol.ShortName, name);
            row[3] = symbol.ComponentRef;
            row[4] = symbol.Target;
            row[5] = symbol.Arguments;
            row[6] = symbol.Description;
            row[7] = symbol.Hotkey;
            row[8] = symbol.IconRef;
            row[9] = symbol.IconIndex;
            row[10] = (int?)symbol.Show;
            row[11] = symbol.WorkingDirectory;
            row[12] = symbol.DisplayResourceDll;
            row[13] = symbol.DisplayResourceId;
            row[14] = symbol.DescriptionResourceDll;
            row[15] = symbol.DescriptionResourceId;
        }

        private void AddTextStyleSymbol(TextStyleSymbol symbol)
        {
            var styleBits = symbol.Bold ? WindowsInstallerConstants.MsidbTextStyleStyleBitsBold : 0;
            styleBits |= symbol.Italic ? WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic : 0;
            styleBits |= symbol.Strike ? WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike : 0;
            styleBits |= symbol.Underline ? WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline : 0;

            long? color = null;

            if (symbol.Red.HasValue || symbol.Green.HasValue || symbol.Blue.HasValue)
            {
                color = symbol.Red ?? 0;
                color += (long)(symbol.Green ?? 0) * 256;
                color += (long)(symbol.Blue ?? 0) * 65536;
            }

            var row = this.CreateRow(symbol, "TextStyle");
            row[0] = symbol.Id.Id;
            row[1] = symbol.FaceName;
            row[2] = symbol.Size;
            row[3] = color;
            row[4] = styleBits == 0 ? null : (int?)styleBits;
        }

        private void AddUpgradeSymbol(UpgradeSymbol symbol)
        {
            if (this.CheckUpgradeVersion(symbol, symbol.VersionMin, out var changedVersion))
            {
                symbol.VersionMin = changedVersion;
            }

            if (this.CheckUpgradeVersion(symbol, symbol.VersionMax, out changedVersion))
            {
                symbol.VersionMax = changedVersion;
            }

            var row = (UpgradeRow)this.CreateRow(symbol, "Upgrade");
            row.UpgradeCode = symbol.UpgradeCode;
            row.VersionMin = symbol.VersionMin;
            row.VersionMax = symbol.VersionMax;
            row.Language = symbol.Language;
            row.Remove = symbol.Remove;
            row.ActionProperty = symbol.ActionProperty;

            var attributes = symbol.MigrateFeatures ? WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures : 0;
            attributes |= symbol.OnlyDetect ? WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect : 0;
            attributes |= symbol.IgnoreRemoveFailures ? WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure : 0;
            attributes |= symbol.VersionMinInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive : 0;
            attributes |= symbol.VersionMaxInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive : 0;
            attributes |= symbol.ExcludeLanguages ? WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive : 0;
            row.Attributes = attributes;
        }

        private void AddWixActionSymbol(WixActionSymbol symbol)
        {
            // Get the table definition for the action (and ensure the proper table exists for a module).
            string sequenceTableName = null;
            switch (symbol.SequenceTable)
            {
                case SequenceTable.AdminExecuteSequence:
                    if (OutputType.Module == this.Data.Type)
                    {
                        this.Data.EnsureTable(this.TableDefinitions["AdminExecuteSequence"]);
                        sequenceTableName = "ModuleAdminExecuteSequence";
                    }
                    else
                    {
                        sequenceTableName = "AdminExecuteSequence";
                    }
                    break;
                case SequenceTable.AdminUISequence:
                    if (OutputType.Module == this.Data.Type)
                    {
                        this.Data.EnsureTable(this.TableDefinitions["AdminUISequence"]);
                        sequenceTableName = "ModuleAdminUISequence";
                    }
                    else
                    {
                        sequenceTableName = "AdminUISequence";
                    }
                    break;
                case SequenceTable.AdvertiseExecuteSequence:
                    if (OutputType.Module == this.Data.Type)
                    {
                        this.Data.EnsureTable(this.TableDefinitions["AdvtExecuteSequence"]);
                        sequenceTableName = "ModuleAdvtExecuteSequence";
                    }
                    else
                    {
                        sequenceTableName = "AdvtExecuteSequence";
                    }
                    break;
                case SequenceTable.InstallExecuteSequence:
                    if (OutputType.Module == this.Data.Type)
                    {
                        this.Data.EnsureTable(this.TableDefinitions["InstallExecuteSequence"]);
                        sequenceTableName = "ModuleInstallExecuteSequence";
                    }
                    else
                    {
                        sequenceTableName = "InstallExecuteSequence";
                    }
                    break;
                case SequenceTable.InstallUISequence:
                    if (OutputType.Module == this.Data.Type)
                    {
                        this.Data.EnsureTable(this.TableDefinitions["InstallUISequence"]);
                        sequenceTableName = "ModuleInstallUISequence";
                    }
                    else
                    {
                        sequenceTableName = "InstallUISequence";
                    }
                    break;
            }

            // create the action sequence row in the output
            var row = this.CreateRow(symbol, sequenceTableName);

            if (SectionType.Module == this.Section.Type)
            {
                row[0] = symbol.Action;
                if (symbol.Sequence.HasValue && symbol.Sequence.Value != 0)
                {
                    row[1] = symbol.Sequence;
                }
                else
                {
                    var after = (null == symbol.Before);
                    row[2] = after ? symbol.After : symbol.Before;
                    row[3] = after ? 1 : 0;
                }
                row[4] = symbol.Condition;
            }
            else
            {
                row[0] = symbol.Action;
                row[1] = symbol.Condition;
                row[2] = symbol.Sequence;
            }
        }

        private void IndexCustomTableCellSymbol(WixCustomTableCellSymbol wixCustomTableCellSymbol, Dictionary<string, List<WixCustomTableCellSymbol>> cellsByTableAndRowId)
        {
            var tableAndRowId = wixCustomTableCellSymbol.TableRef + "/" + wixCustomTableCellSymbol.RowId;
            if (!cellsByTableAndRowId.TryGetValue(tableAndRowId, out var cells))
            {
                cells = new List<WixCustomTableCellSymbol>();
                cellsByTableAndRowId.Add(tableAndRowId, cells);
            }

            cells.Add(wixCustomTableCellSymbol);
        }

        private void AddIndexedCellSymbols(Dictionary<string, List<WixCustomTableCellSymbol>> cellsByTableAndRowId)
        {
            foreach (var rowOfCells in cellsByTableAndRowId.Values)
            {
                var firstCellSymbol = rowOfCells[0];
                var customTableDefinition = this.TableDefinitions[firstCellSymbol.TableRef];

                if (customTableDefinition.Unreal)
                {
                    continue;
                }

                var customRow = this.CreateRow(firstCellSymbol, customTableDefinition);
                var customRowFieldsByColumnName = customRow.Fields.ToDictionary(f => f.Column.Name);

                foreach (var cell in rowOfCells)
                {
                    var data = cell.Data;

                    if (customRowFieldsByColumnName.TryGetValue(cell.ColumnRef, out var rowField))
                    {
                        if (!String.IsNullOrEmpty(data))
                        {
                            if (rowField.Column.Type == ColumnType.Number)
                            {
                                try
                                {
                                    rowField.Data = Convert.ToInt32(data, CultureInfo.InvariantCulture);
                                }
                                catch (FormatException)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIntegerValue(cell.SourceLineNumbers, rowField.Column.Name, customTableDefinition.Name, data));
                                }
                                catch (OverflowException)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIntegerValue(cell.SourceLineNumbers, rowField.Column.Name, customTableDefinition.Name, data));
                                }
                            }
                            else if (rowField.Column.Category == ColumnCategory.Identifier)
                            {
                                if (this.BackendHelper.IsValidIdentifier(data) || this.BackendHelper.IsValidBinderVariable(data) || ColumnCategory.Formatted == rowField.Column.Category)
                                {
                                    rowField.Data = data;
                                }
                                else
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalIdentifier(cell.SourceLineNumbers, "Data", data));
                                }
                            }
                            else
                            {
                                rowField.Data = data;
                            }
                        }
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.UnexpectedCustomTableColumn(cell.SourceLineNumbers, cell.ColumnRef));
                    }
                }

                for (var i = 0; i < customTableDefinition.Columns.Length; ++i)
                {
                    if (!customTableDefinition.Columns[i].Nullable && (null == customRow.Fields[i].Data || 0 == customRow.Fields[i].Data.ToString().Length))
                    {
                        this.Messaging.Write(ErrorMessages.NoDataForColumn(firstCellSymbol.SourceLineNumbers, customTableDefinition.Columns[i].Name, customTableDefinition.Name));
                    }
                }
            }
        }

        private void AddWixEnsureTableSymbol(WixEnsureTableSymbol symbol)
        {
            try
            {
                var tableDefinition = this.TableDefinitions[symbol.Table];
                this.Data.EnsureTable(tableDefinition);
            }
            catch (WixMissingTableDefinitionException e)
            {
                this.Messaging.Write(e.Error);
            }
        }

        private void AddWixModuleSymbol(WixModuleSymbol symbol)
        {
            if (!String.IsNullOrEmpty(symbol.Version) && this.BackendHelper.TryParseFourPartVersion(symbol.Version, out var version))
            {
                var row = this.CreateRow(symbol, "ModuleSignature");
                row[0] = symbol.ModuleId;
                row[1] = symbol.Language;
                row[2] = version;
            }
            else
            {
                this.Messaging.Write(WindowsInstallerBackendErrors.InvalidModuleVersion(symbol.SourceLineNumbers, symbol.Version));
            }
        }

        private void AddWixPackageSymbol(WixPackageSymbol symbol)
        {
            // TODO: Remove the following from the compiler and do it here instead.
            //this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "Manufacturer"), manufacturer, false, false, false, true);
            //this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "ProductCode"), productCode, false, false, false, true);
            //this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "ProductLanguage"), productLanguage, false, false, false, true);
            //this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "ProductName"), this.activeName, false, false, false, true);
            //this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "ProductVersion"), version, false, false, false, true);
            //if (null != upgradeCode)
            //{
            //    this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "UpgradeCode"), upgradeCode, false, false, false, true);
            //}

            //if (isPerMachine)
            //{
            //    this.AddProperty(sourceLineNumbers, new Identifier(AccessModifier.Global, "ALLUSERS"), "1", false, false, false, false);
            //}
        }

        private bool AddSymbolFromExtension(IntermediateSymbol symbol)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryProcessSymbol(this.Section, symbol, this.Data, this.TableDefinitions))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AddSymbolDefaultly(IntermediateSymbol symbol)
        {
            return this.BackendHelper.TryAddSymbolToMatchingTableDefinitions(this.Section, symbol, this.Data, this.TableDefinitions);
        }

        private void EnsureModuleIgnoredTable(IntermediateSymbol symbol, string ignoredTable)
        {
            var tableDefinition = this.TableDefinitions["ModuleIgnoreTable"];
            var table = this.Data.EnsureTable(tableDefinition);
            if (!table.Rows.Any(r => r.FieldAsString(0) == ignoredTable))
            {
                var row = this.CreateRow(symbol, tableDefinition);
                row[0] = ignoredTable;
            }
        }

        private (string, string) AddDirectorySubdirectories(DirectorySymbol symbol)
        {
            var directory = symbol.Name.Trim(PathSeparatorChars);
            var parentDir = symbol.ParentDirectoryRef ?? (symbol.Id.Id == "TARGETDIR" ? null : "TARGETDIR");
            var directoryRows = this.Data.TryGetTable("Directory", out var table) ? table.Rows.ToDictionary(row => row.FieldAsString(0)) : new Dictionary<string, Row>();

            var start = 0;
            var end = directory.IndexOfAny(PathSeparatorChars);
            var path = String.Empty;

            while (start <= end)
            {
                var subdirectoryName = directory.Substring(start, end - start);

                if (!String.IsNullOrEmpty(subdirectoryName))
                {
                    path = Path.Combine(path, subdirectoryName);

                    var id = this.BackendHelper.GenerateIdentifier("d", symbol.ParentDirectoryRef, path);
                    var shortnameSubdirectory = this.BackendHelper.IsValidShortFilename(subdirectoryName, false) ? null : this.CreateShortName(subdirectoryName, false, "Directory", symbol.ParentDirectoryRef);

                    if (!directoryRows.ContainsKey(id))
                    {
                        var subdirectoryRow = this.CreateRow(symbol, "Directory");
                        subdirectoryRow[0] = id;
                        subdirectoryRow[1] = parentDir;
                        subdirectoryRow[2] = CreateMsiFilename(shortnameSubdirectory, subdirectoryName);

                        directoryRows.Add(id, subdirectoryRow);
                    }

                    parentDir = id;
                }

                start = end + 1;
                end = symbol.Name.IndexOfAny(PathSeparatorChars, start);
            }

            var name = (start == 0) ? directory : directory.Substring(start);

            return (name, parentDir);
        }

        private void EnsureRequiredTables()
        {
            // check for missing table and add them or display an error as appropriate
            switch (this.Data.Type)
            {
                case OutputType.Module:
                    this.Data.EnsureTable(this.TableDefinitions["Component"]);
                    this.Data.EnsureTable(this.TableDefinitions["Directory"]);
                    this.Data.EnsureTable(this.TableDefinitions["FeatureComponents"]);
                    this.Data.EnsureTable(this.TableDefinitions["File"]);
                    this.Data.EnsureTable(this.TableDefinitions["ModuleComponents"]);
                    this.Data.EnsureTable(this.TableDefinitions["ModuleSignature"]);
                    break;

                case OutputType.PatchCreation:
                    var imageFamiliesCount = this.Data.Tables["ImageFamilies"]?.Rows.Count ?? 0;
                    var targetImagesCount = this.Data.Tables["TargetImages"]?.Rows.Count ?? 0;
                    var upgradedImagesCount = this.Data.Tables["UpgradedImages"]?.Rows.Count ?? 0;

                    if (imageFamiliesCount < 1)
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedRowInPatchCreationPackage("ImageFamilies"));
                    }

                    if (targetImagesCount < 1)
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedRowInPatchCreationPackage("TargetImages"));
                    }

                    if (upgradedImagesCount < 1)
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedRowInPatchCreationPackage("UpgradedImages"));
                    }

                    this.Data.EnsureTable(this.TableDefinitions["Properties"]);
                    break;

                case OutputType.Package:
                    this.Data.EnsureTable(this.TableDefinitions["File"]);
                    this.Data.EnsureTable(this.TableDefinitions["Media"]);
                    break;
            }
        }

        private void ReportGeneratedShortFileNameConflicts()
        {
            foreach (var conflicts in this.GeneratedShortNames.Values.Where(l => l.Count > 1))
            {
                this.Messaging.Write(WarningMessages.GeneratedShortFileNameConflict(conflicts[0].SourceLineNumbers, conflicts[0].ShortName));
                for (var i = 1; i < conflicts.Count; ++i)
                {
                    this.Messaging.Write(WarningMessages.GeneratedShortFileNameConflict2(conflicts[i].SourceLineNumbers));
                }
            }
        }

        private void ReportIllegalTables()
        {
            foreach (var table in this.Data.Tables)
            {
                switch (this.Data.Type)
                {
                    case OutputType.Module:
                        if ("BBControl" == table.Name ||
                            "Billboard" == table.Name ||
                            "CCPSearch" == table.Name ||
                            "Feature" == table.Name ||
                            "LaunchCondition" == table.Name ||
                            "Media" == table.Name ||
                            "Patch" == table.Name ||
                            "Upgrade" == table.Name ||
                            "WixMerge" == table.Name)
                        {
                            foreach (Row row in table.Rows)
                            {
                                this.Messaging.Write(ErrorMessages.UnexpectedTableInMergeModule(row.SourceLineNumbers, table.Name));
                            }
                        }
                        else if ("Error" == table.Name)
                        {
                            foreach (var row in table.Rows)
                            {
                                this.Messaging.Write(WarningMessages.DangerousTableInMergeModule(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;

                    case OutputType.PatchCreation:
                        if (!table.Definition.Unreal &&
                            "_SummaryInformation" != table.Name &&
                            "ExternalFiles" != table.Name &&
                            "FamilyFileRanges" != table.Name &&
                            "ImageFamilies" != table.Name &&
                            "PatchMetadata" != table.Name &&
                            "PatchSequence" != table.Name &&
                            "Properties" != table.Name &&
                            "TargetFiles_OptionalData" != table.Name &&
                            "TargetImages" != table.Name &&
                            "UpgradedFiles_OptionalData" != table.Name &&
                            "UpgradedFilesToIgnore" != table.Name &&
                            "UpgradedImages" != table.Name)
                        {
                            foreach (var row in table.Rows)
                            {
                                this.Messaging.Write(ErrorMessages.UnexpectedTableInPatchCreationPackage(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;

                    case OutputType.Patch:
                        if (!table.Definition.Unreal &&
                            "_SummaryInformation" != table.Name &&
                            "Media" != table.Name &&
                            "MsiFileHash" != table.Name &&
                            "MsiPatchMetadata" != table.Name &&
                            "MsiPatchSequence" != table.Name)
                        {
                            foreach (var row in table.Rows)
                            {
                                this.Messaging.Write(ErrorMessages.UnexpectedTableInPatch(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;

                    case OutputType.Package:
                        if ("ModuleAdminExecuteSequence" == table.Name ||
                            "ModuleAdminUISequence" == table.Name ||
                            "ModuleAdvtExecuteSequence" == table.Name ||
                            "ModuleAdvtUISequence" == table.Name ||
                            "ModuleComponents" == table.Name ||
                            "ModuleConfiguration" == table.Name ||
                            "ModuleDependency" == table.Name ||
                            "ModuleExclusion" == table.Name ||
                            "ModuleIgnoreTable" == table.Name ||
                            "ModuleInstallExecuteSequence" == table.Name ||
                            "ModuleInstallUISequence" == table.Name ||
                            "ModuleSignature" == table.Name ||
                            "ModuleSubstitution" == table.Name)
                        {
                            foreach (var row in table.Rows)
                            {
                                this.Messaging.Write(WarningMessages.UnexpectedTableInProduct(row.SourceLineNumbers, table.Name));
                            }
                        }
                        break;
                }
            }
        }

        private void ReportMismatchedModularizations()
        {
            // verify that modularization types match for foreign key relationships
            foreach (var tableDefinition in this.TableDefinitions)
            {
                foreach (var columnDefinition in tableDefinition.Columns)
                {
                    if (null != columnDefinition.KeyTable && 0 > columnDefinition.KeyTable.IndexOf(';') && columnDefinition.KeyColumn.HasValue)
                    {
                        if (this.TableDefinitions.TryGet(columnDefinition.KeyTable, out var keyTableDefinition))
                        {
                            var keyColumnIndex = columnDefinition.KeyColumn ?? -1;

                            if (keyColumnIndex <= 0 || keyColumnIndex > keyTableDefinition.Columns.Length)
                            {
                                this.Messaging.Write(ErrorMessages.InvalidKeyColumn(tableDefinition.Name, columnDefinition.Name, columnDefinition.KeyTable, keyColumnIndex));
                            }
                            else if (keyTableDefinition.Columns[keyColumnIndex - 1].ModularizeType != columnDefinition.ModularizeType && ColumnModularizeType.CompanionFile != columnDefinition.ModularizeType)
                            {
                                this.Messaging.Write(WarningMessages.CollidingModularizationTypes(tableDefinition.Name, columnDefinition.Name, columnDefinition.KeyTable, keyColumnIndex, columnDefinition.ModularizeType.ToString(), keyTableDefinition.Columns[keyColumnIndex - 1].ModularizeType.ToString()));
                            }
                        }
                        // else - ignore missing table definitions as that error is caught in other places
                    }
                }
            }
        }

        private void ReportWindowsInstallerDataInconsistencies()
        {
            // Get the output's minimum installer version
            var outputInstallerVersion = Int32.MaxValue;

            if (this.Data.Tables.TryGetTable("_SummaryInformation", out var summaryInformationTable))
            {
                outputInstallerVersion = summaryInformationTable.Rows.FirstOrDefault(r => 14 == r.FieldAsInteger(0))?.FieldAsInteger(1) ?? Int32.MaxValue;
            }

            // Ensure the Error table exists if output is marked for MSI 1.0 or below (see ICE40).
            if (outputInstallerVersion <= 100 && OutputType.Package == this.Data.Type)
            {
                this.Data.EnsureTable(this.TableDefinitions["Error"]);
            }

            // Check for the presence of tables/rows/columns that require MSI 1.1 or later.
            if (outputInstallerVersion < 110)
            {
                if (this.Data.Tables.TryGetTable("IsolatedComponent", out var isolatedComponentTable))
                {
                    foreach (var row in isolatedComponentTable.Rows)
                    {
                        this.Messaging.Write(WarningMessages.TableIncompatibleWithInstallerVersion(row.SourceLineNumbers, "IsolatedComponent", outputInstallerVersion));
                    }
                }
            }

            // Check for the presence of tables/rows/columns that require MSI 4.0 or later
            if (outputInstallerVersion < 400)
            {
                if (this.Data.Tables.TryGetTable("Shortcut", out var shortcutTable))
                {
                    foreach (var row in shortcutTable.Rows)
                    {
                        if (null != row[12] || null != row[13] || null != row[14] || null != row[15])
                        {
                            this.Messaging.Write(WarningMessages.ColumnsIncompatibleWithInstallerVersion(row.SourceLineNumbers, "Shortcut", outputInstallerVersion));
                        }
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
                case SectionType.Package:
                    return OutputType.Package;
                case SectionType.PatchCreation:
                    return OutputType.PatchCreation;
                case SectionType.Patch:
                    return OutputType.Patch;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private Row CreateRow(IntermediateSymbol symbol, string tableDefinitionName)
        {
            return this.CreateRow(symbol, this.TableDefinitions[tableDefinitionName]);
        }

        private Row CreateRow(IntermediateSymbol symbol, TableDefinition tableDefinition)
        {
            return this.BackendHelper.CreateRow(this.Section, symbol, this.Data, tableDefinition);
        }

        private bool CheckUpgradeVersion(UpgradeSymbol symbol, string version, out string changedVersion)
        {
            if (String.IsNullOrEmpty(version))
            {
                // Null is allowed.
            }
            else if (this.BackendHelper.TryParseMsiProductVersion(version, strict: false, out var parsedVersionMin))
            {
                // If the strictly parsed value is different, update the symbol.
                if (version != parsedVersionMin)
                {
                    changedVersion = parsedVersionMin;
                    return true;
                }
            }
            else
            {
                this.Messaging.Write(WarningMessages.InvalidMsiProductVersion(symbol.SourceLineNumbers, version));
            }

            changedVersion = null;
            return false;
        }

        private string CreateShortName(string longName, bool keepExtension, params string[] args)
        {
            longName = longName.ToLowerInvariant();

            // collect all the data
            var strings = new List<string>(1 + args.Length);
            strings.Add(longName);
            strings.AddRange(args);

            // prepare for hashing
            var stringData = String.Join("|", strings);
            var data = Encoding.UTF8.GetBytes(stringData);

            // hash the data
            byte[] hash;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                hash = sha1.ComputeHash(data);
            }

            // generate the short file/directory name without an extension
            var shortName = new StringBuilder(Convert.ToBase64String(hash));
            shortName.Length = 8;
            shortName.Replace('+', '-').Replace('/', '_');

            if (keepExtension)
            {
                var extension = Path.GetExtension(longName);

                if (4 < extension.Length)
                {
                    extension = extension.Substring(0, 4);
                }

                shortName.Append(extension);

                // check the generated short name to ensure its still legal (the extension may not be legal)
                if (!this.BackendHelper.IsValidShortFilename(shortName.ToString(), false))
                {
                    // remove the extension (by truncating the generated file name back to the generated characters)
                    shortName.Length -= extension.Length;
                }
            }

            return shortName.ToString().ToLowerInvariant();
        }

        private static string CreateMsiFilename(string shortName, string longName)
        {
            if (String.IsNullOrEmpty(shortName) || String.Equals(shortName, longName, StringComparison.OrdinalIgnoreCase))
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
