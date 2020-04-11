// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// Decompiles an msi database into WiX source.
    /// </summary>
    internal class Decompiler
    {
        private static readonly Regex NullSplitter = new Regex(@"\[~]");

        // NameToBit arrays
        private static readonly string[] TextControlAttributes = { "Transparent", "NoPrefix", "NoWrap", "FormatSize", "UserLanguage" };
        private static readonly string[] HyperlinkControlAttributes = { "Transparent" };
        private static readonly string[] EditControlAttributes = { "Multiline", null, null, null, null, "Password" };
        private static readonly string[] ProgressControlAttributes = { "ProgressBlocks" };
        private static readonly string[] VolumeControlAttributes = { "Removable", "Fixed", "Remote", "CDROM", "RAMDisk", "Floppy", "ShowRollbackCost" };
        private static readonly string[] ListboxControlAttributes = { "Sorted", null, null, null, "UserLanguage" };
        private static readonly string[] ListviewControlAttributes = { "Sorted", null, null, null, "FixedSize", "Icon16", "Icon32" };
        private static readonly string[] ComboboxControlAttributes = { "Sorted", "ComboList", null, null, "UserLanguage" };
        private static readonly string[] RadioControlAttributes = { "Image", "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", null, "HasBorder" };
        private static readonly string[] ButtonControlAttributes = { "Image", null, "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", "ElevationShield" };
        private static readonly string[] IconControlAttributes = { "Image", null, null, null, "FixedSize", "Icon16", "Icon32" };
        private static readonly string[] BitmapControlAttributes = { "Image", null, null, null, "FixedSize" };
        private static readonly string[] CheckboxControlAttributes = { null, "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32" };

        private bool compressed;
        private bool shortNames;
        private DecompilerCore core;
        private string modularizationGuid;
        private readonly Hashtable patchTargetFiles;
        private readonly Hashtable sequenceElements;
        private readonly TableDefinitionCollection tableDefinitions;

        /// <summary>
        /// Creates a new decompiler object with a default set of table definitions.
        /// </summary>
        public Decompiler(IMessaging messaging, IEnumerable<IWindowsInstallerBackendDecompilerExtension> extensions, string baseSourcePath, bool suppressCustomTables, bool suppressDroppingEmptyTables, bool suppressUI, bool treatProductAsModule)
        {
            this.Messaging = messaging;
            this.Extensions = extensions;
            this.BaseSourcePath = String.IsNullOrEmpty(baseSourcePath) ? "SourceDir" : baseSourcePath;
            this.SuppressCustomTables = suppressCustomTables;
            this.SuppressDroppingEmptyTables = suppressDroppingEmptyTables;
            this.SuppressUI = suppressUI;
            this.TreatProductAsModule = treatProductAsModule;

            this.ExtensionsByTableName = new Dictionary<string, IWindowsInstallerBackendDecompilerExtension>();
            this.StandardActions = WindowsInstallerStandard.StandardActions().ToDictionary(a => a.Id.Id);

            this.patchTargetFiles = new Hashtable();
            this.sequenceElements = new Hashtable();
            this.tableDefinitions = new TableDefinitionCollection();
        }

        private IMessaging Messaging { get; }

        private IEnumerable<IWindowsInstallerBackendDecompilerExtension> Extensions { get; }

        private Dictionary<string, IWindowsInstallerBackendDecompilerExtension> ExtensionsByTableName { get; }

        private string BaseSourcePath { get; }

        private bool SuppressCustomTables { get; }

        private bool SuppressDroppingEmptyTables { get; }

        private bool SuppressRelativeActionSequencing { get; }

        private bool SuppressUI { get; }

        private bool TreatProductAsModule { get; }

        private OutputType OutputType { get; set; }

        private Dictionary<string, WixActionTuple> StandardActions { get; }

        /// <summary>
        /// Decompile the database file.
        /// </summary>
        /// <param name="output">The output to decompile.</param>
        /// <returns>The serialized WiX source code.</returns>
        public XDocument Decompile(WindowsInstallerData output)
        {
            if (null == output)
            {
                throw new ArgumentNullException(nameof(output));
            }

            this.OutputType = output.Type;

            // collect the table definitions from the output
            this.tableDefinitions.Clear();
            foreach (var table in output.Tables)
            {
                this.tableDefinitions.Add(table.Definition);
            }

            // add any missing standard and wix-specific table definitions
            foreach (var tableDefinition in WindowsInstallerTableDefinitions.All)
            {
                if (!this.tableDefinitions.Contains(tableDefinition.Name))
                {
                    this.tableDefinitions.Add(tableDefinition);
                }
            }

            // add any missing extension table definitions
#if TODO_DECOMPILER_EXTENSIONS
            foreach (var extension in this.Extensions)
            {
                this.AddExtension(extension);
            }
#endif

            var wixElement = new Wix.Wix();
            Wix.IParentElement rootElement;

            switch (this.OutputType)
            {
            case OutputType.Module:
                rootElement = new Wix.Module();
                break;
            case OutputType.PatchCreation:
                rootElement = new Wix.PatchCreation();
                break;
            case OutputType.Product:
                rootElement = new Wix.Product();
                break;
            default:
                throw new InvalidOperationException("Unknown output type.");
            }
            wixElement.AddChild((Wix.ISchemaElement)rootElement);

            // try to decompile the database file
            try
            {
                this.core = new DecompilerCore(rootElement);

                // stop processing if an error previously occurred
                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                this.InitializeDecompile(output.Tables, output.Codepage);

                // stop processing if an error previously occurred
                if (this.Messaging.EncounteredError)
                {
                    return null;
                }

                // decompile the tables
                this.DecompileTables(output);

                // finalize the decompiler and its extensions
                this.FinalizeDecompile(output.Tables);
            }
            finally
            {
                this.core = null;
            }

            var document = new XDocument();
            using (var writer = document.CreateWriter())
            {
                wixElement.OutputXml(writer);
            }

            // return the XML document only if decompilation completed successfully
            return this.Messaging.EncounteredError ? null : document;
        }

#if TODO_DECOMPILER_EXTENSIONS
        private void AddExtension(IWindowsInstallerBackendDecompilerExtension extension)
        {
            if (null != extension.TableDefinitions)
            {
                foreach (TableDefinition tableDefinition in extension.TableDefinitions)
                {
                    if (!this.ExtensionsByTableName.ContainsKey(tableDefinition.Name))
                    {
                        this.ExtensionsByTableName.Add(tableDefinition.Name, extension);
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.DuplicateExtensionTable(extension.GetType().ToString(), tableDefinition.Name));
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Set the common control attributes in a control element.
        /// </summary>
        /// <param name="attributes">The control attributes.</param>
        /// <param name="control">The control element.</param>
        private static void SetControlAttributes(int attributes, Wix.Control control)
        {
            if (0 == (attributes & WindowsInstallerConstants.MsidbControlAttributesEnabled))
            {
                control.Disabled = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesIndirect == (attributes & WindowsInstallerConstants.MsidbControlAttributesIndirect))
            {
                control.Indirect = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesInteger == (attributes & WindowsInstallerConstants.MsidbControlAttributesInteger))
            {
                control.Integer = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesLeftScroll == (attributes & WindowsInstallerConstants.MsidbControlAttributesLeftScroll))
            {
                control.LeftScroll = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesRightAligned == (attributes & WindowsInstallerConstants.MsidbControlAttributesRightAligned))
            {
                control.RightAligned = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesRTLRO == (attributes & WindowsInstallerConstants.MsidbControlAttributesRTLRO))
            {
                control.RightToLeft = Wix.YesNoType.yes;
            }

            if (WindowsInstallerConstants.MsidbControlAttributesSunken == (attributes & WindowsInstallerConstants.MsidbControlAttributesSunken))
            {
                control.Sunken = Wix.YesNoType.yes;
            }

            if (0 == (attributes & WindowsInstallerConstants.MsidbControlAttributesVisible))
            {
                control.Hidden = Wix.YesNoType.yes;
            }
        }

        /// <summary>
        /// Creates an action element.
        /// </summary>
        /// <param name="actionRow">The action row from which the element should be created.</param>
        private void CreateActionElement(WixActionRow actionRow)
        {
            Wix.ISchemaElement actionElement = null;

            if (null != this.core.GetIndexedElement("CustomAction", actionRow.Action)) // custom action
            {
                var custom = new Wix.Custom();

                custom.Action = actionRow.Action;

                if (null != actionRow.Condition)
                {
                    custom.Content = actionRow.Condition;
                }

                switch (actionRow.Sequence)
                {
                case (-4):
                    custom.OnExit = Wix.ExitType.suspend;
                    break;
                case (-3):
                    custom.OnExit = Wix.ExitType.error;
                    break;
                case (-2):
                    custom.OnExit = Wix.ExitType.cancel;
                    break;
                case (-1):
                    custom.OnExit = Wix.ExitType.success;
                    break;
                default:
                    if (null != actionRow.Before)
                    {
                        custom.Before = actionRow.Before;
                    }
                    else if (null != actionRow.After)
                    {
                        custom.After = actionRow.After;
                    }
                    else if (0 < actionRow.Sequence)
                    {
                        custom.Sequence = actionRow.Sequence;
                    }
                    break;
                }

                actionElement = custom;
            }
            else if (null != this.core.GetIndexedElement("Dialog", actionRow.Action)) // dialog
            {
                var show = new Wix.Show();

                show.Dialog = actionRow.Action;

                if (null != actionRow.Condition)
                {
                    show.Content = actionRow.Condition;
                }

                switch (actionRow.Sequence)
                {
                case (-4):
                    show.OnExit = Wix.ExitType.suspend;
                    break;
                case (-3):
                    show.OnExit = Wix.ExitType.error;
                    break;
                case (-2):
                    show.OnExit = Wix.ExitType.cancel;
                    break;
                case (-1):
                    show.OnExit = Wix.ExitType.success;
                    break;
                default:
                    if (null != actionRow.Before)
                    {
                        show.Before = actionRow.Before;
                    }
                    else if (null != actionRow.After)
                    {
                        show.After = actionRow.After;
                    }
                    else if (0 < actionRow.Sequence)
                    {
                        show.Sequence = actionRow.Sequence;
                    }
                    break;
                }

                actionElement = show;
            }
            else // possibly a standard action without suggested sequence information
            {
                actionElement = this.CreateStandardActionElement(actionRow);
            }

            // add the action element to the appropriate sequence element
            if (null != actionElement)
            {
                var sequenceTable = actionRow.SequenceTable.ToString();
                var sequenceElement = (Wix.IParentElement)this.sequenceElements[sequenceTable];

                if (null == sequenceElement)
                {
                    switch (actionRow.SequenceTable)
                    {
                    case SequenceTable.AdminExecuteSequence:
                        sequenceElement = new Wix.AdminExecuteSequence();
                        break;
                    case SequenceTable.AdminUISequence:
                        sequenceElement = new Wix.AdminUISequence();
                        break;
                    case SequenceTable.AdvertiseExecuteSequence:
                        sequenceElement = new Wix.AdvertiseExecuteSequence();
                        break;
                    case SequenceTable.InstallExecuteSequence:
                        sequenceElement = new Wix.InstallExecuteSequence();
                        break;
                    case SequenceTable.InstallUISequence:
                        sequenceElement = new Wix.InstallUISequence();
                        break;
                    default:
                        throw new InvalidOperationException("Unknown sequence table.");
                    }

                    this.core.RootElement.AddChild((Wix.ISchemaElement)sequenceElement);
                    this.sequenceElements.Add(sequenceTable, sequenceElement);
                }

                try
                {
                    sequenceElement.AddChild(actionElement);
                }
                catch (System.ArgumentException) // action/dialog is not valid for this sequence
                {
                    this.Messaging.Write(WarningMessages.IllegalActionInSequence(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                }
            }
        }

        /// <summary>
        /// Creates a standard action element.
        /// </summary>
        /// <param name="actionRow">The action row from which the element should be created.</param>
        /// <returns>The created element.</returns>
        private Wix.ISchemaElement CreateStandardActionElement(WixActionRow actionRow)
        {
            Wix.ActionSequenceType actionElement = null;

            switch (actionRow.Action)
            {
            case "AllocateRegistrySpace":
                actionElement = new Wix.AllocateRegistrySpace();
                break;
            case "AppSearch":
                this.StandardActions.TryGetValue(actionRow.GetPrimaryKey(), out var appSearchActionRow);

                if (null != actionRow.Before || null != actionRow.After || (null != appSearchActionRow && actionRow.Sequence != appSearchActionRow.Sequence))
                {
                    var appSearch = new Wix.AppSearch();

                    if (null != actionRow.Condition)
                    {
                        appSearch.Content = actionRow.Condition;
                    }

                    if (null != actionRow.Before)
                    {
                        appSearch.Before = actionRow.Before;
                    }
                    else if (null != actionRow.After)
                    {
                        appSearch.After = actionRow.After;
                    }
                    else if (0 < actionRow.Sequence)
                    {
                        appSearch.Sequence = actionRow.Sequence;
                    }

                    return appSearch;
                }
                break;
            case "BindImage":
                actionElement = new Wix.BindImage();
                break;
            case "CCPSearch":
                var ccpSearch = new Wix.CCPSearch();
                Decompiler.SequenceRelativeAction(actionRow, ccpSearch);
                return ccpSearch;
            case "CostFinalize":
                actionElement = new Wix.CostFinalize();
                break;
            case "CostInitialize":
                actionElement = new Wix.CostInitialize();
                break;
            case "CreateFolders":
                actionElement = new Wix.CreateFolders();
                break;
            case "CreateShortcuts":
                actionElement = new Wix.CreateShortcuts();
                break;
            case "DeleteServices":
                actionElement = new Wix.DeleteServices();
                break;
            case "DisableRollback":
                var disableRollback = new Wix.DisableRollback();
                Decompiler.SequenceRelativeAction(actionRow, disableRollback);
                return disableRollback;
            case "DuplicateFiles":
                actionElement = new Wix.DuplicateFiles();
                break;
            case "ExecuteAction":
                actionElement = new Wix.ExecuteAction();
                break;
            case "FileCost":
                actionElement = new Wix.FileCost();
                break;
            case "FindRelatedProducts":
                var findRelatedProducts = new Wix.FindRelatedProducts();
                Decompiler.SequenceRelativeAction(actionRow, findRelatedProducts);
                return findRelatedProducts;
            case "ForceReboot":
                var forceReboot = new Wix.ForceReboot();
                Decompiler.SequenceRelativeAction(actionRow, forceReboot);
                return forceReboot;
            case "InstallAdminPackage":
                actionElement = new Wix.InstallAdminPackage();
                break;
            case "InstallExecute":
                var installExecute = new Wix.InstallExecute();
                Decompiler.SequenceRelativeAction(actionRow, installExecute);
                return installExecute;
            case "InstallExecuteAgain":
                var installExecuteAgain = new Wix.InstallExecuteAgain();
                Decompiler.SequenceRelativeAction(actionRow, installExecuteAgain);
                return installExecuteAgain;
            case "InstallFiles":
                actionElement = new Wix.InstallFiles();
                break;
            case "InstallFinalize":
                actionElement = new Wix.InstallFinalize();
                break;
            case "InstallInitialize":
                actionElement = new Wix.InstallInitialize();
                break;
            case "InstallODBC":
                actionElement = new Wix.InstallODBC();
                break;
            case "InstallServices":
                actionElement = new Wix.InstallServices();
                break;
            case "InstallValidate":
                actionElement = new Wix.InstallValidate();
                break;
            case "IsolateComponents":
                actionElement = new Wix.IsolateComponents();
                break;
            case "LaunchConditions":
                var launchConditions = new Wix.LaunchConditions();
                Decompiler.SequenceRelativeAction(actionRow, launchConditions);
                return launchConditions;
            case "MigrateFeatureStates":
                actionElement = new Wix.MigrateFeatureStates();
                break;
            case "MoveFiles":
                actionElement = new Wix.MoveFiles();
                break;
            case "MsiPublishAssemblies":
                actionElement = new Wix.MsiPublishAssemblies();
                break;
            case "MsiUnpublishAssemblies":
                actionElement = new Wix.MsiUnpublishAssemblies();
                break;
            case "PatchFiles":
                actionElement = new Wix.PatchFiles();
                break;
            case "ProcessComponents":
                actionElement = new Wix.ProcessComponents();
                break;
            case "PublishComponents":
                actionElement = new Wix.PublishComponents();
                break;
            case "PublishFeatures":
                actionElement = new Wix.PublishFeatures();
                break;
            case "PublishProduct":
                actionElement = new Wix.PublishProduct();
                break;
            case "RegisterClassInfo":
                actionElement = new Wix.RegisterClassInfo();
                break;
            case "RegisterComPlus":
                actionElement = new Wix.RegisterComPlus();
                break;
            case "RegisterExtensionInfo":
                actionElement = new Wix.RegisterExtensionInfo();
                break;
            case "RegisterFonts":
                actionElement = new Wix.RegisterFonts();
                break;
            case "RegisterMIMEInfo":
                actionElement = new Wix.RegisterMIMEInfo();
                break;
            case "RegisterProduct":
                actionElement = new Wix.RegisterProduct();
                break;
            case "RegisterProgIdInfo":
                actionElement = new Wix.RegisterProgIdInfo();
                break;
            case "RegisterTypeLibraries":
                actionElement = new Wix.RegisterTypeLibraries();
                break;
            case "RegisterUser":
                actionElement = new Wix.RegisterUser();
                break;
            case "RemoveDuplicateFiles":
                actionElement = new Wix.RemoveDuplicateFiles();
                break;
            case "RemoveEnvironmentStrings":
                actionElement = new Wix.RemoveEnvironmentStrings();
                break;
            case "RemoveExistingProducts":
                var removeExistingProducts = new Wix.RemoveExistingProducts();
                Decompiler.SequenceRelativeAction(actionRow, removeExistingProducts);
                return removeExistingProducts;
            case "RemoveFiles":
                actionElement = new Wix.RemoveFiles();
                break;
            case "RemoveFolders":
                actionElement = new Wix.RemoveFolders();
                break;
            case "RemoveIniValues":
                actionElement = new Wix.RemoveIniValues();
                break;
            case "RemoveODBC":
                actionElement = new Wix.RemoveODBC();
                break;
            case "RemoveRegistryValues":
                actionElement = new Wix.RemoveRegistryValues();
                break;
            case "RemoveShortcuts":
                actionElement = new Wix.RemoveShortcuts();
                break;
            case "ResolveSource":
                var resolveSource = new Wix.ResolveSource();
                Decompiler.SequenceRelativeAction(actionRow, resolveSource);
                return resolveSource;
            case "RMCCPSearch":
                var rmccpSearch = new Wix.RMCCPSearch();
                Decompiler.SequenceRelativeAction(actionRow, rmccpSearch);
                return rmccpSearch;
            case "ScheduleReboot":
                var scheduleReboot = new Wix.ScheduleReboot();
                Decompiler.SequenceRelativeAction(actionRow, scheduleReboot);
                return scheduleReboot;
            case "SelfRegModules":
                actionElement = new Wix.SelfRegModules();
                break;
            case "SelfUnregModules":
                actionElement = new Wix.SelfUnregModules();
                break;
            case "SetODBCFolders":
                actionElement = new Wix.SetODBCFolders();
                break;
            case "StartServices":
                actionElement = new Wix.StartServices();
                break;
            case "StopServices":
                actionElement = new Wix.StopServices();
                break;
            case "UnpublishComponents":
                actionElement = new Wix.UnpublishComponents();
                break;
            case "UnpublishFeatures":
                actionElement = new Wix.UnpublishFeatures();
                break;
            case "UnregisterClassInfo":
                actionElement = new Wix.UnregisterClassInfo();
                break;
            case "UnregisterComPlus":
                actionElement = new Wix.UnregisterComPlus();
                break;
            case "UnregisterExtensionInfo":
                actionElement = new Wix.UnregisterExtensionInfo();
                break;
            case "UnregisterFonts":
                actionElement = new Wix.UnregisterFonts();
                break;
            case "UnregisterMIMEInfo":
                actionElement = new Wix.UnregisterMIMEInfo();
                break;
            case "UnregisterProgIdInfo":
                actionElement = new Wix.UnregisterProgIdInfo();
                break;
            case "UnregisterTypeLibraries":
                actionElement = new Wix.UnregisterTypeLibraries();
                break;
            case "ValidateProductID":
                actionElement = new Wix.ValidateProductID();
                break;
            case "WriteEnvironmentStrings":
                actionElement = new Wix.WriteEnvironmentStrings();
                break;
            case "WriteIniValues":
                actionElement = new Wix.WriteIniValues();
                break;
            case "WriteRegistryValues":
                actionElement = new Wix.WriteRegistryValues();
                break;
            default:
                this.Messaging.Write(WarningMessages.UnknownAction(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                return null;
            }

            if (actionElement != null)
            {
                this.SequenceStandardAction(actionRow, actionElement);
            }

            return actionElement;
        }

        /// <summary>
        /// Applies the condition and sequence to a standard action element based on the action row data.
        /// </summary>
        /// <param name="actionRow">Action row data from the database.</param>
        /// <param name="actionElement">Element to be sequenced.</param>
        private void SequenceStandardAction(WixActionRow actionRow, Wix.ActionSequenceType actionElement)
        {
            if (null != actionRow.Condition)
            {
                actionElement.Content = actionRow.Condition;
            }

            if ((null != actionRow.Before || null != actionRow.After) && 0 == actionRow.Sequence)
            {
                this.Messaging.Write(WarningMessages.DecompiledStandardActionRelativelyScheduledInModule(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
            }
            else if (0 < actionRow.Sequence)
            {
                actionElement.Sequence = actionRow.Sequence;
            }
        }

        /// <summary>
        /// Applies the condition and relative sequence to an action element based on the action row data.
        /// </summary>
        /// <param name="actionRow">Action row data from the database.</param>
        /// <param name="actionElement">Element to be sequenced.</param>
        private static void SequenceRelativeAction(WixActionRow actionRow, Wix.ActionModuleSequenceType actionElement)
        {
            if (null != actionRow.Condition)
            {
                actionElement.Content = actionRow.Condition;
            }

            if (null != actionRow.Before)
            {
                actionElement.Before = actionRow.Before;
            }
            else if (null != actionRow.After)
            {
                actionElement.After = actionRow.After;
            }
            else if (0 < actionRow.Sequence)
            {
                actionElement.Sequence = actionRow.Sequence;
            }
        }

        /// <summary>
        /// Ensure that a particular property exists in the decompiled output.
        /// </summary>
        /// <param name="id">The identifier of the property.</param>
        /// <returns>The property element.</returns>
        private Wix.Property EnsureProperty(string id)
        {
            var property = (Wix.Property)this.core.GetIndexedElement("Property", id);

            if (null == property)
            {
                property = new Wix.Property();
                property.Id = id;

                // create a dummy row for indexing
                var row = new Row(null, this.tableDefinitions["Property"]);
                row[0] = id;

                this.core.RootElement.AddChild(property);
                this.core.IndexElement(row, property);
            }

            return property;
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizeDecompile(TableIndexedCollection tables)
        {
            if (OutputType.PatchCreation == this.OutputType)
            {
                this.FinalizeFamilyFileRangesTable(tables);
            }
            else
            {
                this.FinalizeCheckBoxTable(tables);
                this.FinalizeComponentTable(tables);
                this.FinalizeDialogTable(tables);
                this.FinalizeDuplicateMoveFileTables(tables);
                this.FinalizeFeatureComponentsTable(tables);
                this.FinalizeFileTable(tables);
                this.FinalizeMIMETable(tables);
                this.FinalizeMsiLockPermissionsExTable(tables);
                this.FinalizeLockPermissionsTable(tables);
                this.FinalizeProgIdTable(tables);
                this.FinalizePropertyTable(tables);
                this.FinalizeRemoveFileTable(tables);
                this.FinalizeSearchTables(tables);
                this.FinalizeShortcutTable(tables);
                this.FinalizeUpgradeTable(tables);
                this.FinalizeSequenceTables(tables);
                this.FinalizeVerbTable(tables);
            }
        }

        /// <summary>
        /// Finalize the CheckBox table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Enumerates through all the Control rows, looking for controls of type "CheckBox" with
        /// a value in the Property column.  This is then possibly matched up with a CheckBox row
        /// to retrieve a CheckBoxValue.  There is no foreign key from the Control to CheckBox table.
        /// </remarks>
        private void FinalizeCheckBoxTable(TableIndexedCollection tables)
        {
            // if the user has requested to suppress the UI elements, we have nothing to do
            if (this.SuppressUI)
            {
                return;
            }

            var checkBoxTable = tables["CheckBox"];
            var controlTable = tables["Control"];

            var checkBoxes = new Hashtable();
            var checkBoxProperties = new Hashtable();

            // index the CheckBox table
            if (null != checkBoxTable)
            {
                foreach (var row in checkBoxTable.Rows)
                {
                    checkBoxes.Add(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), row);
                    checkBoxProperties.Add(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), false);
                }
            }

            // enumerate through the Control table, adding CheckBox values where appropriate
            if (null != controlTable)
            {
                foreach (var row in controlTable.Rows)
                {
                    var control = (Wix.Control)this.core.GetIndexedElement(row);

                    if ("CheckBox" == Convert.ToString(row[2]) && null != row[8])
                    {
                        var checkBoxRow = (Row)checkBoxes[row[8]];

                        if (null == checkBoxRow)
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Control", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Property", Convert.ToString(row[8]), "CheckBox"));
                        }
                        else
                        {
                            // if we've seen this property already, create a reference to it
                            if (Convert.ToBoolean(checkBoxProperties[row[8]]))
                            {
                                control.CheckBoxPropertyRef = Convert.ToString(row[8]);
                            }
                            else
                            {
                                control.Property = Convert.ToString(row[8]);
                                checkBoxProperties[row[8]] = true;
                            }

                            if (null != checkBoxRow[1])
                            {
                                control.CheckBoxValue = Convert.ToString(checkBoxRow[1]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the Component table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Set the keypaths for each component.
        /// </remarks>
        private void FinalizeComponentTable(TableIndexedCollection tables)
        {
            var componentTable = tables["Component"];
            var fileTable = tables["File"];
            var odbcDataSourceTable = tables["ODBCDataSource"];
            var registryTable = tables["Registry"];

            // set the component keypaths
            if (null != componentTable)
            {
                foreach (var row in componentTable.Rows)
                {
                    var attributes = Convert.ToInt32(row[3]);

                    if (null == row[5])
                    {
                        var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[0]));

                        component.KeyPath = Wix.YesNoType.yes;
                    }
                    else if (WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath == (attributes & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath))
                    {
                        object registryObject = this.core.GetIndexedElement("Registry", Convert.ToString(row[5]));

                        if (null != registryObject)
                        {
                            var registryValue = registryObject as Wix.RegistryValue;

                            if (null != registryValue)
                            {
                                registryValue.KeyPath = Wix.YesNoType.yes;
                            }
                            else
                            {
                                this.Messaging.Write(WarningMessages.IllegalRegistryKeyPath(row.SourceLineNumbers, "Component", Convert.ToString(row[5])));
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", Convert.ToString(row[5]), "Registry"));
                        }
                    }
                    else if (WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource == (attributes & WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource))
                    {
                        var odbcDataSource = (Wix.ODBCDataSource)this.core.GetIndexedElement("ODBCDataSource", Convert.ToString(row[5]));

                        if (null != odbcDataSource)
                        {
                            odbcDataSource.KeyPath = Wix.YesNoType.yes;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", Convert.ToString(row[5]), "ODBCDataSource"));
                        }
                    }
                    else
                    {
                        var file = (Wix.File)this.core.GetIndexedElement("File", Convert.ToString(row[5]));

                        if (null != file)
                        {
                            file.KeyPath = Wix.YesNoType.yes;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", Convert.ToString(row[5]), "File"));
                        }
                    }
                }
            }

            // add the File children elements
            if (null != fileTable)
            {
                foreach (FileRow fileRow in fileTable.Rows)
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", fileRow.Component);
                    var file = (Wix.File)this.core.GetIndexedElement(fileRow);

                    if (null != component)
                    {
                        component.AddChild(file);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(fileRow.SourceLineNumbers, "File", fileRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", fileRow.Component, "Component"));
                    }
                }
            }

            // add the ODBCDataSource children elements
            if (null != odbcDataSourceTable)
            {
                foreach (var row in odbcDataSourceTable.Rows)
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                    var odbcDataSource = (Wix.ODBCDataSource)this.core.GetIndexedElement(row);

                    if (null != component)
                    {
                        component.AddChild(odbcDataSource);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "ODBCDataSource", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                    }
                }
            }

            // add the Registry children elements
            if (null != registryTable)
            {
                foreach (var row in registryTable.Rows)
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[5]));
                    var registryElement = this.core.GetIndexedElement(row);

                    if (null != component)
                    {
                        component.AddChild(registryElement);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Registry", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[5]), "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the Dialog table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Sets the first, default, and cancel control for each dialog and adds all child control
        /// elements to the dialog.
        /// </remarks>
        private void FinalizeDialogTable(TableIndexedCollection tables)
        {
            // if the user has requested to suppress the UI elements, we have nothing to do
            if (this.SuppressUI)
            {
                return;
            }

            var controlTable = tables["Control"];
            var dialogTable = tables["Dialog"];

            var addedControls = new Hashtable();
            var controlRows = new Hashtable();

            // index the rows in the control rows (because we need the Control_Next value)
            if (null != controlTable)
            {
                foreach (var row in controlTable.Rows)
                {
                    controlRows.Add(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), row);
                }
            }

            if (null != dialogTable)
            {
                foreach (var row in dialogTable.Rows)
                {
                    var dialog = (Wix.Dialog)this.core.GetIndexedElement(row);
                    var dialogId = Convert.ToString(row[0]);

                    var control = (Wix.Control)this.core.GetIndexedElement("Control", dialogId, Convert.ToString(row[7]));
                    if (null == control)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Dialog", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_First", Convert.ToString(row[7]), "Control"));
                    }

                    // add tabbable controls
                    while (null != control)
                    {
                        var controlRow = (Row)controlRows[String.Concat(dialogId, DecompilerConstants.PrimaryKeyDelimiter, control.Id)];

                        control.TabSkip = Wix.YesNoType.no;
                        dialog.AddChild(control);
                        addedControls.Add(control, null);

                        if (null != controlRow[10])
                        {
                            control = (Wix.Control)this.core.GetIndexedElement("Control", dialogId, Convert.ToString(controlRow[10]));
                            if (null != control)
                            {
                                // looped back to the first control in the dialog
                                if (addedControls.Contains(control))
                                {
                                    control = null;
                                }
                            }
                            else
                            {
                                this.Messaging.Write(WarningMessages.ExpectedForeignRow(controlRow.SourceLineNumbers, "Control", controlRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", dialogId, "Control_Next", Convert.ToString(controlRow[10]), "Control"));
                            }
                        }
                        else
                        {
                            control = null;
                        }
                    }

                    // set default control
                    if (null != row[8])
                    {
                        var defaultControl = (Wix.Control)this.core.GetIndexedElement("Control", dialogId, Convert.ToString(row[8]));

                        if (null != defaultControl)
                        {
                            defaultControl.Default = Wix.YesNoType.yes;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Dialog", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_Default", Convert.ToString(row[8]), "Control"));
                        }
                    }

                    // set cancel control
                    if (null != row[9])
                    {
                        var cancelControl = (Wix.Control)this.core.GetIndexedElement("Control", dialogId, Convert.ToString(row[9]));

                        if (null != cancelControl)
                        {
                            cancelControl.Cancel = Wix.YesNoType.yes;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Dialog", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_Cancel", Convert.ToString(row[9]), "Control"));
                        }
                    }
                }
            }

            // add the non-tabbable controls to the dialog
            if (null != controlTable)
            {
                foreach (var row in controlTable.Rows)
                {
                    var control = (Wix.Control)this.core.GetIndexedElement(row);
                    var dialog = (Wix.Dialog)this.core.GetIndexedElement("Dialog", Convert.ToString(row[0]));

                    if (null == dialog)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Control", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", Convert.ToString(row[0]), "Dialog"));
                        continue;
                    }

                    if (!addedControls.Contains(control))
                    {
                        control.TabSkip = Wix.YesNoType.yes;
                        dialog.AddChild(control);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the DuplicateFile and MoveFile tables.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Sets the source/destination property/directory for each DuplicateFile or
        /// MoveFile row.
        /// </remarks>
        private void FinalizeDuplicateMoveFileTables(TableIndexedCollection tables)
        {
            var duplicateFileTable = tables["DuplicateFile"];
            var moveFileTable = tables["MoveFile"];

            if (null != duplicateFileTable)
            {
                foreach (var row in duplicateFileTable.Rows)
                {
                    var copyFile = (Wix.CopyFile)this.core.GetIndexedElement(row);

                    if (null != row[4])
                    {
                        if (null != this.core.GetIndexedElement("Directory", Convert.ToString(row[4])))
                        {
                            copyFile.DestinationDirectory = Convert.ToString(row[4]);
                        }
                        else
                        {
                            copyFile.DestinationProperty = Convert.ToString(row[4]);
                        }
                    }
                }
            }

            if (null != moveFileTable)
            {
                foreach (var row in moveFileTable.Rows)
                {
                    var copyFile = (Wix.CopyFile)this.core.GetIndexedElement(row);

                    if (null != row[4])
                    {
                        if (null != this.core.GetIndexedElement("Directory", Convert.ToString(row[4])))
                        {
                            copyFile.SourceDirectory = Convert.ToString(row[4]);
                        }
                        else
                        {
                            copyFile.SourceProperty = Convert.ToString(row[4]);
                        }
                    }

                    if (null != this.core.GetIndexedElement("Directory", Convert.ToString(row[5])))
                    {
                        copyFile.DestinationDirectory = Convert.ToString(row[5]);
                    }
                    else
                    {
                        copyFile.DestinationProperty = Convert.ToString(row[5]);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the FamilyFileRanges table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizeFamilyFileRangesTable(TableIndexedCollection tables)
        {
            var externalFilesTable = tables["ExternalFiles"];
            var familyFileRangesTable = tables["FamilyFileRanges"];
            var targetFiles_OptionalDataTable = tables["TargetFiles_OptionalData"];

            var usedProtectRanges = new Hashtable();

            if (null != familyFileRangesTable)
            {
                foreach (var row in familyFileRangesTable.Rows)
                {
                    var protectRange = new Wix.ProtectRange();

                    if (null != row[2] && null != row[3])
                    {
                        var retainOffsets = (Convert.ToString(row[2])).Split(',');
                        var retainLengths = (Convert.ToString(row[3])).Split(',');

                        if (retainOffsets.Length == retainLengths.Length)
                        {
                            for (var i = 0; i < retainOffsets.Length; i++)
                            {
                                if (retainOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                                {
                                    protectRange.Offset = Convert.ToInt32(retainOffsets[i].Substring(2), 16);
                                }
                                else
                                {
                                    protectRange.Offset = Convert.ToInt32(retainOffsets[i], CultureInfo.InvariantCulture);
                                }

                                if (retainLengths[i].StartsWith("0x", StringComparison.Ordinal))
                                {
                                    protectRange.Length = Convert.ToInt32(retainLengths[i].Substring(2), 16);
                                }
                                else
                                {
                                    protectRange.Length = Convert.ToInt32(retainLengths[i], CultureInfo.InvariantCulture);
                                }
                            }
                        }
                        else
                        {
                            // TODO: warn
                        }
                    }
                    else if (null != row[2] || null != row[3])
                    {
                        // TODO: warn about mismatch between columns
                    }

                    this.core.IndexElement(row, protectRange);
                }
            }

            if (null != externalFilesTable)
            {
                foreach (var row in externalFilesTable.Rows)
                {
                    var externalFile = (Wix.ExternalFile)this.core.GetIndexedElement(row);

                    var protectRange = (Wix.ProtectRange)this.core.GetIndexedElement("FamilyFileRanges", Convert.ToString(row[0]), Convert.ToString(row[1]));
                    if (null != protectRange)
                    {
                        externalFile.AddChild(protectRange);
                        usedProtectRanges[protectRange] = null;
                    }
                }
            }

            if (null != targetFiles_OptionalDataTable)
            {
                var targetImagesTable = tables["TargetImages"];
                var upgradedImagesTable = tables["UpgradedImages"];

                var targetImageRows = new Hashtable();
                var upgradedImagesRows = new Hashtable();

                // index the TargetImages table
                if (null != targetImagesTable)
                {
                    foreach (var row in targetImagesTable.Rows)
                    {
                        targetImageRows.Add(row[0], row);
                    }
                }

                // index the UpgradedImages table
                if (null != upgradedImagesTable)
                {
                    foreach (var row in upgradedImagesTable.Rows)
                    {
                        upgradedImagesRows.Add(row[0], row);
                    }
                }

                foreach (var row in targetFiles_OptionalDataTable.Rows)
                {
                    var targetFile = (Wix.TargetFile)this.patchTargetFiles[row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter)];

                    var targetImageRow = (Row)targetImageRows[row[0]];
                    if (null == targetImageRow)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, targetFiles_OptionalDataTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Target", Convert.ToString(row[0]), "TargetImages"));
                        continue;
                    }

                    var upgradedImagesRow = (Row)upgradedImagesRows[targetImageRow[3]];
                    if (null == upgradedImagesRow)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(targetImageRow.SourceLineNumbers, targetImageRow.Table.Name, targetImageRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Upgraded", Convert.ToString(row[3]), "UpgradedImages"));
                        continue;
                    }

                    var protectRange = (Wix.ProtectRange)this.core.GetIndexedElement("FamilyFileRanges", Convert.ToString(upgradedImagesRow[4]), Convert.ToString(row[1]));
                    if (null != protectRange)
                    {
                        targetFile.AddChild(protectRange);
                        usedProtectRanges[protectRange] = null;
                    }
                }
            }

            if (null != familyFileRangesTable)
            {
                foreach (var row in familyFileRangesTable.Rows)
                {
                    var protectRange = (Wix.ProtectRange)this.core.GetIndexedElement(row);

                    if (!usedProtectRanges.Contains(protectRange))
                    {
                        var protectFile = new Wix.ProtectFile();

                        protectFile.File = Convert.ToString(row[1]);

                        protectFile.AddChild(protectRange);

                        var family = (Wix.Family)this.core.GetIndexedElement("ImageFamilies", Convert.ToString(row[0]));
                        if (null != family)
                        {
                            family.AddChild(protectFile);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, familyFileRangesTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Family", Convert.ToString(row[0]), "ImageFamilies"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the FeatureComponents table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since tables specifying references to the FeatureComponents table have references to
        /// the Feature and Component table separately, but not the FeatureComponents table specifically,
        /// the FeatureComponents table and primary features must be decompiled during finalization.
        /// </remarks>
        private void FinalizeFeatureComponentsTable(TableIndexedCollection tables)
        {
            var classTable = tables["Class"];
            var extensionTable = tables["Extension"];
            var msiAssemblyTable = tables["MsiAssembly"];
            var publishComponentTable = tables["PublishComponent"];
            var typeLibTable = tables["TypeLib"];

            if (null != classTable)
            {
                foreach (var row in classTable.Rows)
                {
                    this.SetPrimaryFeature(row, 11, 2);
                }
            }

            if (null != extensionTable)
            {
                foreach (var row in extensionTable.Rows)
                {
                    this.SetPrimaryFeature(row, 4, 1);
                }
            }

            if (null != msiAssemblyTable)
            {
                foreach (var row in msiAssemblyTable.Rows)
                {
                    this.SetPrimaryFeature(row, 1, 0);
                }
            }

            if (null != publishComponentTable)
            {
                foreach (var row in publishComponentTable.Rows)
                {
                    this.SetPrimaryFeature(row, 4, 2);
                }
            }

            if (null != typeLibTable)
            {
                foreach (var row in typeLibTable.Rows)
                {
                    this.SetPrimaryFeature(row, 6, 2);
                }
            }
        }

        /// <summary>
        /// Finalize the File table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Sets the source, diskId, and assembly information for each file.
        /// </remarks>
        private void FinalizeFileTable(TableIndexedCollection tables)
        {
            var fileTable = tables["File"];
            var mediaTable = tables["Media"];
            var msiAssemblyTable = tables["MsiAssembly"];
            var typeLibTable = tables["TypeLib"];

            // index the media table by media id
            RowDictionary<MediaRow> mediaRows;
            if (null != mediaTable)
            {
                mediaRows = new RowDictionary<MediaRow>(mediaTable);
            }

            // set the disk identifiers and sources for files
            if (null != fileTable)
            {
                foreach (FileRow fileRow in fileTable.Rows)
                {
                    var file = (Wix.File)this.core.GetIndexedElement("File", fileRow.File);

                    // Don't bother processing files that are orphaned (and won't show up in the output anyway)
                    if (null != file.ParentElement)
                    {
                        // set the diskid
                        if (null != mediaTable)
                        {
                            foreach (MediaRow mediaRow in mediaTable.Rows)
                            {
                                if (fileRow.Sequence <= mediaRow.LastSequence && mediaRow.DiskId != 1)
                                {
                                    file.DiskId = Convert.ToString(mediaRow.DiskId);
                                    break;
                                }
                            }
                        }

                        // set the source (done here because it requires information from the Directory table)
                        if (OutputType.Module == this.OutputType)
                        {
                            file.Source = String.Concat(this.BaseSourcePath, Path.DirectorySeparatorChar, "File", Path.DirectorySeparatorChar, file.Id, '.', this.modularizationGuid.Substring(1, 36).Replace('-', '_'));
                        }
                        else if (Wix.YesNoDefaultType.yes == file.Compressed || (Wix.YesNoDefaultType.no != file.Compressed && this.compressed) || (OutputType.Product == this.OutputType && this.TreatProductAsModule))
                        {
                            file.Source = String.Concat(this.BaseSourcePath, Path.DirectorySeparatorChar, "File", Path.DirectorySeparatorChar, file.Id);
                        }
                        else // uncompressed
                        {
                            var fileName = (null != file.ShortName ? file.ShortName : file.Name);

                            if (!this.shortNames && null != file.Name)
                            {
                                fileName = file.Name;
                            }

                            if (this.compressed) // uncompressed at the root of the source image
                            {
                                file.Source = String.Concat("SourceDir", Path.DirectorySeparatorChar, fileName);
                            }
                            else
                            {
                                var sourcePath = this.GetSourcePath(file);

                                file.Source = Path.Combine(sourcePath, fileName);
                            }
                        }
                    }
                }
            }

            // set the file assemblies and manifests
            if (null != msiAssemblyTable)
            {
                foreach (var row in msiAssemblyTable.Rows)
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[0]));

                    if (null == component)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MsiAssembly", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[0]), "Component"));
                    }
                    else
                    {
                        foreach (Wix.ISchemaElement element in component.Children)
                        {
                            var file = element as Wix.File;

                            if (null != file && Wix.YesNoType.yes == file.KeyPath)
                            {
                                if (null != row[2])
                                {
                                    file.AssemblyManifest = Convert.ToString(row[2]);
                                }

                                if (null != row[3])
                                {
                                    file.AssemblyApplication = Convert.ToString(row[3]);
                                }

                                if (null == row[4] || 0 == Convert.ToInt32(row[4]))
                                {
                                    file.Assembly = Wix.File.AssemblyType.net;
                                }
                                else
                                {
                                    file.Assembly = Wix.File.AssemblyType.win32;
                                }
                            }
                        }
                    }
                }
            }

            // nest the TypeLib elements
            if (null != typeLibTable)
            {
                foreach (var row in typeLibTable.Rows)
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[2]));
                    var typeLib = (Wix.TypeLib)this.core.GetIndexedElement(row);

                    foreach (Wix.ISchemaElement element in component.Children)
                    {
                        if (element is Wix.File file && Wix.YesNoType.yes == file.KeyPath)
                        {
                            file.AddChild(typeLib);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the MIME table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// There is a foreign key shared between the MIME and Extension
        /// tables so either one would be valid to be decompiled first, so
        /// the only safe way to nest the MIME elements is to do it during finalize.
        /// </remarks>
        private void FinalizeMIMETable(TableIndexedCollection tables)
        {
            var extensionTable = tables["Extension"];
            var mimeTable = tables["MIME"];

            var comExtensions = new Hashtable();

            if (null != extensionTable)
            {
                foreach (var row in extensionTable.Rows)
                {
                    var extension = (Wix.Extension)this.core.GetIndexedElement(row);

                    // index the extension
                    if (!comExtensions.Contains(row[0]))
                    {
                        comExtensions.Add(row[0], new ArrayList());
                    }
                    ((ArrayList)comExtensions[row[0]]).Add(extension);

                    // set the default MIME element for this extension
                    if (null != row[3])
                    {
                        var mime = (Wix.MIME)this.core.GetIndexedElement("MIME", Convert.ToString(row[3]));

                        if (null != mime)
                        {
                            mime.Default = Wix.YesNoType.yes;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Extension", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "MIME_", Convert.ToString(row[3]), "MIME"));
                        }
                    }
                }
            }

            if (null != mimeTable)
            {
                foreach (var row in mimeTable.Rows)
                {
                    var mime = (Wix.MIME)this.core.GetIndexedElement(row);

                    if (comExtensions.Contains(row[1]))
                    {
                        var extensionElements = (ArrayList)comExtensions[row[1]];

                        foreach (Wix.Extension extension in extensionElements)
                        {
                            extension.AddChild(mime);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MIME", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Extension_", Convert.ToString(row[1]), "Extension"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the ProgId table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Enumerates through all the Class rows, looking for child ProgIds (these are the
        /// default ProgIds for a given Class).  Then go through the ProgId table and add any
        /// remaining ProgIds for each Class.  This happens during finalize because there is
        /// a circular dependency between the Class and ProgId tables.
        /// </remarks>
        private void FinalizeProgIdTable(TableIndexedCollection tables)
        {
            var classTable = tables["Class"];
            var progIdTable = tables["ProgId"];
            var extensionTable = tables["Extension"];
            var componentTable = tables["Component"];

            var addedProgIds = new Hashtable();
            var classes = new Hashtable();
            var components = new Hashtable();

            // add the default ProgIds for each class (and index the class table)
            if (null != classTable)
            {
                foreach (var row in classTable.Rows)
                {
                    var wixClass = (Wix.Class)this.core.GetIndexedElement(row);

                    if (null != row[3])
                    {
                        var progId = (Wix.ProgId)this.core.GetIndexedElement("ProgId", Convert.ToString(row[3]));

                        if (null != progId)
                        {
                            if (addedProgIds.Contains(progId))
                            {
                                this.Messaging.Write(WarningMessages.TooManyProgIds(row.SourceLineNumbers, Convert.ToString(row[0]), Convert.ToString(row[3]), Convert.ToString(addedProgIds[progId])));
                            }
                            else
                            {
                                wixClass.AddChild(progId);
                                addedProgIds.Add(progId, wixClass.Id);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Class", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ProgId_Default", Convert.ToString(row[3]), "ProgId"));
                        }
                    }

                    // index the Class elements for nesting of ProgId elements (which don't use the full Class primary key)
                    if (!classes.Contains(wixClass.Id))
                    {
                        classes.Add(wixClass.Id, new ArrayList());
                    }
                    ((ArrayList)classes[wixClass.Id]).Add(wixClass);
                }
            }

            // add the remaining non-default ProgId entries for each class
            if (null != progIdTable)
            {
                foreach (var row in progIdTable.Rows)
                {
                    var progId = (Wix.ProgId)this.core.GetIndexedElement(row);

                    if (!addedProgIds.Contains(progId) && null != row[2] && null == progId.ParentElement)
                    {
                        var classElements = (ArrayList)classes[row[2]];

                        if (null != classElements)
                        {
                            foreach (Wix.Class wixClass in classElements)
                            {
                                wixClass.AddChild(progId);
                                addedProgIds.Add(progId, wixClass.Id);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "ProgId", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Class_", Convert.ToString(row[2]), "Class"));
                        }
                    }
                }
            }

            if (null != componentTable)
            {
                foreach (var row in componentTable.Rows)
                {
                    var wixComponent = (Wix.Component)this.core.GetIndexedElement(row);

                    // index the Class elements for nesting of ProgId elements (which don't use the full Class primary key)
                    if (!components.Contains(wixComponent.Id))
                    {
                        components.Add(wixComponent.Id, new ArrayList());
                    }
                    ((ArrayList)components[wixComponent.Id]).Add(wixComponent);
                }
            }

            // Check for any progIds that are not hooked up to a class and hook them up to the component specified by the extension
            if (null != extensionTable)
            {
                foreach (var row in extensionTable.Rows)
                {
                    // ignore the extension if it isn't associated with a progId
                    if (null == row[2])
                    {
                        continue;
                    }

                    var progId = (Wix.ProgId)this.core.GetIndexedElement("ProgId", Convert.ToString(row[2]));

                    // Haven't added the progId yet and it doesn't have a parent progId
                    if (!addedProgIds.Contains(progId) && null == progId.ParentElement)
                    {
                        var componentElements = (ArrayList)components[row[1]];

                        if (null != componentElements)
                        {
                            foreach (Wix.Component wixComponent in componentElements)
                            {
                                wixComponent.AddChild(progId);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Extension", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the Property table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Removes properties that are generated from other entries.
        /// </remarks>
        private void FinalizePropertyTable(TableIndexedCollection tables)
        {
            var propertyTable = tables["Property"];
            var customActionTable = tables["CustomAction"];

            if (null != propertyTable && null != customActionTable)
            {
                foreach (var row in customActionTable.Rows)
                {
                    var bits = Convert.ToInt32(row[1]);
                    if (WindowsInstallerConstants.MsidbCustomActionTypeHideTarget == (bits & WindowsInstallerConstants.MsidbCustomActionTypeHideTarget) &&
                        WindowsInstallerConstants.MsidbCustomActionTypeInScript == (bits & WindowsInstallerConstants.MsidbCustomActionTypeInScript))
                    {
                        var property = (Wix.Property)this.core.GetIndexedElement("Property", Convert.ToString(row[0]));

                        // If no other fields on the property are set we must have created it during link
                        if (null != property && null == property.Value && Wix.YesNoType.yes != property.Secure && Wix.YesNoType.yes != property.SuppressModularization)
                        {
                            this.core.RootElement.RemoveChild(property);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the RemoveFile table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Sets the directory/property for each RemoveFile row.
        /// </remarks>
        private void FinalizeRemoveFileTable(TableIndexedCollection tables)
        {
            var removeFileTable = tables["RemoveFile"];

            if (null != removeFileTable)
            {
                foreach (var row in removeFileTable.Rows)
                {
                    var isDirectory = false;
                    var property = Convert.ToString(row[3]);

                    // determine if the property is actually authored as a directory
                    if (null != this.core.GetIndexedElement("Directory", property))
                    {
                        isDirectory = true;
                    }

                    var element = this.core.GetIndexedElement(row);

                    var removeFile = element as Wix.RemoveFile;
                    if (null != removeFile)
                    {
                        if (isDirectory)
                        {
                            removeFile.Directory = property;
                        }
                        else
                        {
                            removeFile.Property = property;
                        }
                    }
                    else
                    {
                        var removeFolder = (Wix.RemoveFolder)element;

                        if (isDirectory)
                        {
                            removeFolder.Directory = property;
                        }
                        else
                        {
                            removeFolder.Property = property;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the LockPermissions table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Nests the Permission elements below their parent elements.  There are no declared foreign
        /// keys for the parents of the LockPermissions table.
        /// </remarks>
        private void FinalizeLockPermissionsTable(TableIndexedCollection tables)
        {
            var createFolderTable = tables["CreateFolder"];
            var lockPermissionsTable = tables["LockPermissions"];

            var createFolders = new Hashtable();

            // index the CreateFolder table because the foreign key to this table from the
            // LockPermissions table is only part of the primary key of this table
            if (null != createFolderTable)
            {
                foreach (var row in createFolderTable.Rows)
                {
                    var createFolder = (Wix.CreateFolder)this.core.GetIndexedElement(row);
                    var directoryId = Convert.ToString(row[0]);

                    if (!createFolders.Contains(directoryId))
                    {
                        createFolders.Add(directoryId, new ArrayList());
                    }
                    ((ArrayList)createFolders[directoryId]).Add(createFolder);
                }
            }

            if (null != lockPermissionsTable)
            {
                foreach (var row in lockPermissionsTable.Rows)
                {
                    var id = Convert.ToString(row[0]);
                    var table = Convert.ToString(row[1]);

                    var permission = (Wix.Permission)this.core.GetIndexedElement(row);

                    if ("CreateFolder" == table)
                    {
                        var createFolderElements = (ArrayList)createFolders[id];

                        if (null != createFolderElements)
                        {
                            foreach (Wix.CreateFolder createFolder in createFolderElements)
                            {
                                createFolder.AddChild(permission);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "LockPermissions", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                    else
                    {
                        var parentElement = (Wix.IParentElement)this.core.GetIndexedElement(table, id);

                        if (null != parentElement)
                        {
                            parentElement.AddChild(permission);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "LockPermissions", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the MsiLockPermissionsEx table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Nests the PermissionEx elements below their parent elements.  There are no declared foreign
        /// keys for the parents of the MsiLockPermissionsEx table.
        /// </remarks>
        private void FinalizeMsiLockPermissionsExTable(TableIndexedCollection tables)
        {
            var createFolderTable = tables["CreateFolder"];
            var msiLockPermissionsExTable = tables["MsiLockPermissionsEx"];

            var createFolders = new Hashtable();

            // index the CreateFolder table because the foreign key to this table from the
            // MsiLockPermissionsEx table is only part of the primary key of this table
            if (null != createFolderTable)
            {
                foreach (var row in createFolderTable.Rows)
                {
                    var createFolder = (Wix.CreateFolder)this.core.GetIndexedElement(row);
                    var directoryId = Convert.ToString(row[0]);

                    if (!createFolders.Contains(directoryId))
                    {
                        createFolders.Add(directoryId, new ArrayList());
                    }
                    ((ArrayList)createFolders[directoryId]).Add(createFolder);
                }
            }

            if (null != msiLockPermissionsExTable)
            {
                foreach (var row in msiLockPermissionsExTable.Rows)
                {
                    var id = Convert.ToString(row[1]);
                    var table = Convert.ToString(row[2]);

                    var permissionEx = (Wix.PermissionEx)this.core.GetIndexedElement(row);

                    if ("CreateFolder" == table)
                    {
                        var createFolderElements = (ArrayList)createFolders[id];

                        if (null != createFolderElements)
                        {
                            foreach (Wix.CreateFolder createFolder in createFolderElements)
                            {
                                createFolder.AddChild(permissionEx);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MsiLockPermissionsEx", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                    else
                    {
                        var parentElement = (Wix.IParentElement)this.core.GetIndexedElement(table, id);

                        if (null != parentElement)
                        {
                            parentElement.AddChild(permissionEx);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MsiLockPermissionsEx", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the search tables.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>Does all the complex linking required for the search tables.</remarks>
        private void FinalizeSearchTables(TableIndexedCollection tables)
        {
            var appSearchTable = tables["AppSearch"];
            var ccpSearchTable = tables["CCPSearch"];
            var drLocatorTable = tables["DrLocator"];

            var appSearches = new Hashtable();
            var ccpSearches = new Hashtable();
            var drLocators = new Hashtable();
            var locators = new Hashtable();
            var usedSearchElements = new Hashtable();
            var unusedSearchElements = new Dictionary<string, Wix.IParentElement>();

            Wix.ComplianceCheck complianceCheck = null;

            // index the AppSearch table by signatures
            if (null != appSearchTable)
            {
                foreach (var row in appSearchTable.Rows)
                {
                    var property = Convert.ToString(row[0]);
                    var signature = Convert.ToString(row[1]);

                    if (!appSearches.Contains(signature))
                    {
                        appSearches.Add(signature, new StringCollection());
                    }

                    ((StringCollection)appSearches[signature]).Add(property);
                }
            }

            // index the CCPSearch table by signatures
            if (null != ccpSearchTable)
            {
                foreach (var row in ccpSearchTable.Rows)
                {
                    var signature = Convert.ToString(row[0]);

                    if (!ccpSearches.Contains(signature))
                    {
                        ccpSearches.Add(signature, new StringCollection());
                    }

                    ((StringCollection)ccpSearches[signature]).Add(null);

                    if (null == complianceCheck && !appSearches.Contains(signature))
                    {
                        complianceCheck = new Wix.ComplianceCheck();
                        this.core.RootElement.AddChild(complianceCheck);
                    }
                }
            }

            // index the directory searches by their search elements (to get back the original row)
            if (null != drLocatorTable)
            {
                foreach (var row in drLocatorTable.Rows)
                {
                    drLocators.Add(this.core.GetIndexedElement(row), row);
                }
            }

            // index the locator tables by their signatures
            var locatorTableNames = new string[] { "CompLocator", "RegLocator", "IniLocator", "DrLocator", "Signature" };
            foreach (var locatorTableName in locatorTableNames)
            {
                var locatorTable = tables[locatorTableName];

                if (null != locatorTable)
                {
                    foreach (var row in locatorTable.Rows)
                    {
                        var signature = Convert.ToString(row[0]);

                        if (!locators.Contains(signature))
                        {
                            locators.Add(signature, new ArrayList());
                        }

                        ((ArrayList)locators[signature]).Add(row);
                    }
                }
            }

            // move the DrLocator rows with a parent of CCP_DRIVE first to ensure they get FileSearch children (not FileSearchRef)
            foreach (ArrayList locatorRows in locators.Values)
            {
                var firstDrLocator = -1;

                for (var i = 0; i < locatorRows.Count; i++)
                {
                    var locatorRow = (Row)locatorRows[i];

                    if ("DrLocator" == locatorRow.TableDefinition.Name)
                    {
                        if (-1 == firstDrLocator)
                        {
                            firstDrLocator = i;
                        }

                        if ("CCP_DRIVE" == Convert.ToString(locatorRow[1]))
                        {
                            locatorRows.RemoveAt(i);
                            locatorRows.Insert(firstDrLocator, locatorRow);
                            break;
                        }
                    }
                }
            }

            foreach (string signature in locators.Keys)
            {
                var locatorRows = (ArrayList)locators[signature];
                var signatureSearchElements = new ArrayList();

                foreach (Row locatorRow in locatorRows)
                {
                    var used = true;
                    var searchElement = this.core.GetIndexedElement(locatorRow);

                    if ("Signature" == locatorRow.TableDefinition.Name && 0 < signatureSearchElements.Count)
                    {
                        foreach (Wix.IParentElement searchParentElement in signatureSearchElements)
                        {
                            if (!usedSearchElements.Contains(searchElement))
                            {
                                searchParentElement.AddChild(searchElement);
                                usedSearchElements[searchElement] = null;
                            }
                            else
                            {
                                var fileSearchRef = new Wix.FileSearchRef();

                                fileSearchRef.Id = signature;

                                searchParentElement.AddChild(fileSearchRef);
                            }
                        }
                    }
                    else if ("DrLocator" == locatorRow.TableDefinition.Name && null != locatorRow[1])
                    {
                        var drSearchElement = (Wix.DirectorySearch)searchElement;
                        var parentSignature = Convert.ToString(locatorRow[1]);

                        if ("CCP_DRIVE" == parentSignature)
                        {
                            if (appSearches.Contains(signature))
                            {
                                var appSearchPropertyIds = (StringCollection)appSearches[signature];

                                foreach (var propertyId in appSearchPropertyIds)
                                {
                                    var property = this.EnsureProperty(propertyId);
                                    Wix.ComplianceDrive complianceDrive = null;

                                    if (ccpSearches.Contains(signature))
                                    {
                                        property.ComplianceCheck = Wix.YesNoType.yes;
                                    }

                                    foreach (Wix.ISchemaElement element in property.Children)
                                    {
                                        complianceDrive = element as Wix.ComplianceDrive;
                                        if (null != complianceDrive)
                                        {
                                            break;
                                        }
                                    }

                                    if (null == complianceDrive)
                                    {
                                        complianceDrive = new Wix.ComplianceDrive();
                                        property.AddChild(complianceDrive);
                                    }

                                    if (!usedSearchElements.Contains(searchElement))
                                    {
                                        complianceDrive.AddChild(searchElement);
                                        usedSearchElements[searchElement] = null;
                                    }
                                    else
                                    {
                                        var directorySearchRef = new Wix.DirectorySearchRef();

                                        directorySearchRef.Id = signature;

                                        if (null != locatorRow[1])
                                        {
                                            directorySearchRef.Parent = Convert.ToString(locatorRow[1]);
                                        }

                                        if (null != locatorRow[2])
                                        {
                                            directorySearchRef.Path = Convert.ToString(locatorRow[2]);
                                        }

                                        complianceDrive.AddChild(directorySearchRef);
                                        signatureSearchElements.Add(directorySearchRef);
                                    }
                                }
                            }
                            else if (ccpSearches.Contains(signature))
                            {
                                Wix.ComplianceDrive complianceDrive = null;

                                foreach (Wix.ISchemaElement element in complianceCheck.Children)
                                {
                                    complianceDrive = element as Wix.ComplianceDrive;
                                    if (null != complianceDrive)
                                    {
                                        break;
                                    }
                                }

                                if (null == complianceDrive)
                                {
                                    complianceDrive = new Wix.ComplianceDrive();
                                    complianceCheck.AddChild(complianceDrive);
                                }

                                if (!usedSearchElements.Contains(searchElement))
                                {
                                    complianceDrive.AddChild(searchElement);
                                    usedSearchElements[searchElement] = null;
                                }
                                else
                                {
                                    var directorySearchRef = new Wix.DirectorySearchRef();

                                    directorySearchRef.Id = signature;

                                    if (null != locatorRow[1])
                                    {
                                        directorySearchRef.Parent = Convert.ToString(locatorRow[1]);
                                    }

                                    if (null != locatorRow[2])
                                    {
                                        directorySearchRef.Path = Convert.ToString(locatorRow[2]);
                                    }

                                    complianceDrive.AddChild(directorySearchRef);
                                    signatureSearchElements.Add(directorySearchRef);
                                }
                            }
                        }
                        else
                        {
                            var usedDrLocator = false;
                            var parentLocatorRows = (ArrayList)locators[parentSignature];

                            if (null != parentLocatorRows)
                            {
                                foreach (Row parentLocatorRow in parentLocatorRows)
                                {
                                    if ("DrLocator" == parentLocatorRow.TableDefinition.Name)
                                    {
                                        var parentSearchElement = (Wix.IParentElement)this.core.GetIndexedElement(parentLocatorRow);

                                        if (parentSearchElement.Children.GetEnumerator().MoveNext())
                                        {
                                            var parentDrLocatorRow = (Row)drLocators[parentSearchElement];
                                            var directorySeachRef = new Wix.DirectorySearchRef();

                                            directorySeachRef.Id = parentSignature;

                                            if (null != parentDrLocatorRow[1])
                                            {
                                                directorySeachRef.Parent = Convert.ToString(parentDrLocatorRow[1]);
                                            }

                                            if (null != parentDrLocatorRow[2])
                                            {
                                                directorySeachRef.Path = Convert.ToString(parentDrLocatorRow[2]);
                                            }

                                            parentSearchElement = directorySeachRef;
                                            unusedSearchElements.Add(directorySeachRef.Id, directorySeachRef);
                                        }

                                        if (!usedSearchElements.Contains(searchElement))
                                        {
                                            parentSearchElement.AddChild(searchElement);
                                            usedSearchElements[searchElement] = null;
                                            usedDrLocator = true;
                                        }
                                        else
                                        {
                                            var directorySearchRef = new Wix.DirectorySearchRef();

                                            directorySearchRef.Id = signature;

                                            directorySearchRef.Parent = parentSignature;

                                            if (null != locatorRow[2])
                                            {
                                                directorySearchRef.Path = Convert.ToString(locatorRow[2]);
                                            }

                                            parentSearchElement.AddChild(searchElement);
                                            usedDrLocator = true;
                                        }
                                    }
                                    else if ("RegLocator" == parentLocatorRow.TableDefinition.Name)
                                    {
                                        var parentSearchElement = (Wix.IParentElement)this.core.GetIndexedElement(parentLocatorRow);

                                        parentSearchElement.AddChild(searchElement);
                                        usedSearchElements[searchElement] = null;
                                        usedDrLocator = true;
                                    }
                                }

                                // keep track of unused DrLocator rows
                                if (!usedDrLocator)
                                {
                                    unusedSearchElements.Add(drSearchElement.Id, drSearchElement);
                                }
                            }
                            else
                            {
                                // TODO: warn
                            }
                        }
                    }
                    else if (appSearches.Contains(signature))
                    {
                        var appSearchPropertyIds = (StringCollection)appSearches[signature];

                        foreach (var propertyId in appSearchPropertyIds)
                        {
                            var property = this.EnsureProperty(propertyId);

                            if (ccpSearches.Contains(signature))
                            {
                                property.ComplianceCheck = Wix.YesNoType.yes;
                            }

                            if (!usedSearchElements.Contains(searchElement))
                            {
                                property.AddChild(searchElement);
                                usedSearchElements[searchElement] = null;
                            }
                            else if ("RegLocator" == locatorRow.TableDefinition.Name)
                            {
                                var registrySearchRef = new Wix.RegistrySearchRef();

                                registrySearchRef.Id = signature;

                                property.AddChild(registrySearchRef);
                                signatureSearchElements.Add(registrySearchRef);
                            }
                            else
                            {
                                // TODO: warn about unavailable Ref element
                            }
                        }
                    }
                    else if (ccpSearches.Contains(signature))
                    {
                        if (!usedSearchElements.Contains(searchElement))
                        {
                            complianceCheck.AddChild(searchElement);
                            usedSearchElements[searchElement] = null;
                        }
                        else if ("RegLocator" == locatorRow.TableDefinition.Name)
                        {
                            var registrySearchRef = new Wix.RegistrySearchRef();

                            registrySearchRef.Id = signature;

                            complianceCheck.AddChild(registrySearchRef);
                            signatureSearchElements.Add(registrySearchRef);
                        }
                        else
                        {
                            // TODO: warn about unavailable Ref element
                        }
                    }
                    else
                    {
                        if (searchElement is Wix.DirectorySearch directorySearch)
                        {
                            unusedSearchElements.Add(directorySearch.Id, directorySearch);
                        }
                        else if (searchElement is Wix.RegistrySearch registrySearch)
                        {
                            unusedSearchElements.Add(registrySearch.Id, registrySearch);
                        }
                        else
                        {
                            // TODO: warn
                            used = false;
                        }
                    }

                    // keep track of the search elements for this signature so that nested searches go in the proper parents
                    if (used)
                    {
                        signatureSearchElements.Add(searchElement);
                    }
                }
            }

            // Iterate through the unused elements through a sorted list of their ids so the output is deterministic.
            var unusedSearchElementKeys = unusedSearchElements.Keys.ToList();
            unusedSearchElementKeys.Sort();
            foreach (var unusedSearchElementKey in unusedSearchElementKeys)
            {
                var unusedSearchElement = unusedSearchElements[unusedSearchElementKey];
                var used = false;

                Wix.DirectorySearch leafDirectorySearch = null;
                var parentElement = unusedSearchElement;
                var updatedLeaf = true;
                while (updatedLeaf)
                {
                    updatedLeaf = false;
                    foreach (var schemaElement in parentElement.Children)
                    {
                        if (schemaElement is Wix.DirectorySearch directorySearch)
                        {
                            parentElement = leafDirectorySearch = directorySearch;
                            updatedLeaf = true;
                            break;
                        }
                    }
                }

                if (leafDirectorySearch != null)
                {
                    var appSearchProperties = (StringCollection)appSearches[leafDirectorySearch.Id];

                    var unusedSearchSchemaElement = unusedSearchElement as Wix.ISchemaElement;
                    if (null != appSearchProperties)
                    {
                        var property = this.EnsureProperty(appSearchProperties[0]);

                        property.AddChild(unusedSearchSchemaElement);
                        used = true;
                    }
                    else if (ccpSearches.Contains(leafDirectorySearch.Id))
                    {
                        complianceCheck.AddChild(unusedSearchSchemaElement);
                        used = true;
                    }
                    else
                    {
                        // TODO: warn
                    }
                }

                if (!used)
                {
                    // TODO: warn
                }
            }
        }

        /// <summary>
        /// Finalize the Shortcut table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Sets Advertise to yes if Target points to a Feature.
        /// Occurs during finalization because it has to check against every feature row.
        /// </remarks>
        private void FinalizeShortcutTable(TableIndexedCollection tables)
        {
            var shortcutTable = tables["Shortcut"];
            if (null == shortcutTable)
            {
                return;
            }

            foreach (var row in shortcutTable.Rows)
            {
                var shortcut = (Wix.Shortcut)this.core.GetIndexedElement(row);
                var target = Convert.ToString(row[4]);
                var feature = this.core.GetIndexedElement("Feature", target);
                if (feature == null)
                {
                    // TODO: use this value to do a "more-correct" nesting under the indicated File or CreateDirectory element
                    shortcut.Target = target;
                }
                else
                {
                    shortcut.Advertise = Wix.YesNoType.yes;
                    this.SetPrimaryFeature(row, 4, 3);
                }
            }
        }

        /// <summary>
        /// Finalize the sequence tables.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Creates the sequence elements.  Occurs during finalization because its
        /// not known if sequences refer to custom actions or dialogs during decompilation.
        /// </remarks>
        private void FinalizeSequenceTables(TableIndexedCollection tables)
        {
            // finalize the normal sequence tables
            if (OutputType.Product == this.OutputType && !this.TreatProductAsModule)
            {
                foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
                {
                    var sequenceTableName = GetSequenceTableName(sequenceTable);

                    // if suppressing UI elements, skip UI-related sequence tables
                    if (this.SuppressUI && ("AdminUISequence" == sequenceTableName || "InstallUISequence" == sequenceTableName))
                    {
                        continue;
                    }

                    var actionsTable = new Table(this.tableDefinitions["WixAction"]);
                    var table = tables[sequenceTableName];

                    if (null != table)
                    {
                        var actionRows = new List<WixActionRow>();
                        var needAbsoluteScheduling = this.SuppressRelativeActionSequencing;
                        var nonSequencedActionRows = new Dictionary<string, WixActionRow>();
                        var suppressedRelativeActionRows = new Dictionary<string, WixActionRow>();

                        // create a sorted array of actions in this table
                        foreach (var row in table.Rows)
                        {
                            var actionRow = (WixActionRow)actionsTable.CreateRow(null);

                            actionRow.Action = Convert.ToString(row[0]);

                            if (null != row[1])
                            {
                                actionRow.Condition = Convert.ToString(row[1]);
                            }

                            actionRow.Sequence = Convert.ToInt32(row[2]);

                            actionRow.SequenceTable = sequenceTable;

                            actionRows.Add(actionRow);
                        }
                        actionRows.Sort();

                        for (var i = 0; i < actionRows.Count && !needAbsoluteScheduling; i++)
                        {
                            var actionRow = actionRows[i];
                            this.StandardActions.TryGetValue(actionRow.GetPrimaryKey(), out var standardActionRow);

                            // create actions for custom actions, dialogs, AppSearch when its moved, and standard actions with non-standard conditions
                            if ("AppSearch" == actionRow.Action || null == standardActionRow || actionRow.Condition != standardActionRow.Condition)
                            {
                                WixActionRow previousActionRow = null;
                                WixActionRow nextActionRow = null;

                                // find the previous action row if there is one
                                if (0 <= i - 1)
                                {
                                    previousActionRow = actionRows[i - 1];
                                }

                                // find the next action row if there is one
                                if (actionRows.Count > i + 1)
                                {
                                    nextActionRow = actionRows[i + 1];
                                }

                                // the logic for setting the before or after attribute for an action:
                                // 1. If more than one action shares the same sequence number, everything must be absolutely sequenced.
                                // 2. If the next action is a standard action and is 1 sequence number higher, this action occurs before it.
                                // 3. If the previous action is a standard action and is 1 sequence number lower, this action occurs after it.
                                // 4. If this action is not standard and the previous action is 1 sequence number lower and does not occur before this action, this action occurs after it.
                                // 5. If this action is not standard and the previous action does not have the same sequence number and the next action is 1 sequence number higher, this action occurs before it.
                                // 6. If this action is AppSearch and has all standard information, ignore it.
                                // 7. If this action is standard and has a non-standard condition, create the action without any scheduling information.
                                // 8. Everything must be absolutely sequenced.
                                if ((null != previousActionRow && actionRow.Sequence == previousActionRow.Sequence) || (null != nextActionRow && actionRow.Sequence == nextActionRow.Sequence))
                                {
                                    needAbsoluteScheduling = true;
                                }
                                else if (null != nextActionRow && this.StandardActions.ContainsKey(nextActionRow.GetPrimaryKey()) && actionRow.Sequence + 1 == nextActionRow.Sequence)
                                {
                                    actionRow.Before = nextActionRow.Action;
                                }
                                else if (null != previousActionRow && this.StandardActions.ContainsKey(previousActionRow.GetPrimaryKey()) && actionRow.Sequence - 1 == previousActionRow.Sequence)
                                {
                                    actionRow.After = previousActionRow.Action;
                                }
                                else if (null == standardActionRow && null != previousActionRow && actionRow.Sequence - 1 == previousActionRow.Sequence && previousActionRow.Before != actionRow.Action)
                                {
                                    actionRow.After = previousActionRow.Action;
                                }
                                else if (null == standardActionRow && null != previousActionRow && actionRow.Sequence != previousActionRow.Sequence && null != nextActionRow && actionRow.Sequence + 1 == nextActionRow.Sequence)
                                {
                                    actionRow.Before = nextActionRow.Action;
                                }
                                else if ("AppSearch" == actionRow.Action && null != standardActionRow && actionRow.Sequence == standardActionRow.Sequence && actionRow.Condition == standardActionRow.Condition)
                                {
                                    // ignore an AppSearch row which has the WiX standard sequence and a standard condition
                                }
                                else if (null != standardActionRow && actionRow.Condition != standardActionRow.Condition) // standard actions get their standard sequence numbers
                                {
                                    nonSequencedActionRows.Add(actionRow.GetPrimaryKey(), actionRow);
                                }
                                else if (0 < actionRow.Sequence)
                                {
                                    needAbsoluteScheduling = true;
                                }
                            }
                            else
                            {
                                suppressedRelativeActionRows.Add(actionRow.GetPrimaryKey(), actionRow);
                            }
                        }

                        // create the actions now that we know if they must be absolutely or relatively scheduled
                        foreach (var actionRow in actionRows)
                        {
                            var key = actionRow.GetPrimaryKey();

                            if (needAbsoluteScheduling)
                            {
                                // remove any before/after information to ensure this is absolutely sequenced
                                actionRow.Before = null;
                                actionRow.After = null;
                            }
                            else if (nonSequencedActionRows.ContainsKey(key))
                            {
                                // clear the sequence attribute to ensure this action is scheduled without a sequence number (or before/after)
                                actionRow.Sequence = 0;
                            }
                            else if (suppressedRelativeActionRows.ContainsKey(key))
                            {
                                // skip the suppressed relatively scheduled action rows
                                continue;
                            }

                            // create the action element
                            this.CreateActionElement(actionRow);
                        }
                    }
                }
            }
            else if (OutputType.Module == this.OutputType || this.TreatProductAsModule) // finalize the Module sequence tables
            {
                foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
                {
                    var sequenceTableName = GetSequenceTableName(sequenceTable);

                    // if suppressing UI elements, skip UI-related sequence tables
                    if (this.SuppressUI && ("AdminUISequence" == sequenceTableName || "InstallUISequence" == sequenceTableName))
                    {
                        continue;
                    }

                    var actionsTable = new Table(this.tableDefinitions["WixAction"]);
                    var table = tables[String.Concat("Module", sequenceTableName)];

                    if (null != table)
                    {
                        foreach (var row in table.Rows)
                        {
                            var actionRow = (WixActionRow)actionsTable.CreateRow(null);

                            actionRow.Action = Convert.ToString(row[0]);

                            if (null != row[1])
                            {
                                actionRow.Sequence = Convert.ToInt32(row[1]);
                            }

                            if (null != row[2] && null != row[3])
                            {
                                switch (Convert.ToInt32(row[3]))
                                {
                                case 0:
                                    actionRow.Before = Convert.ToString(row[2]);
                                    break;
                                case 1:
                                    actionRow.After = Convert.ToString(row[2]);
                                    break;
                                default:
                                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[3].Column.Name, row[3]));
                                    break;
                                }
                            }

                            if (null != row[4])
                            {
                                actionRow.Condition = Convert.ToString(row[4]);
                            }

                            actionRow.SequenceTable = sequenceTable;

                            // create action elements for non-standard actions
                            if (!this.StandardActions.ContainsKey(actionRow.GetPrimaryKey()) || null != actionRow.After || null != actionRow.Before)
                            {
                                this.CreateActionElement(actionRow);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the Upgrade table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Decompile the rows from the Upgrade and LaunchCondition tables
        /// created by the MajorUpgrade element.
        /// </remarks>
        private void FinalizeUpgradeTable(TableIndexedCollection tables)
        {
            var launchConditionTable = tables["LaunchCondition"];
            var upgradeTable = tables["Upgrade"];
            string downgradeErrorMessage = null;
            string disallowUpgradeErrorMessage = null;
            var majorUpgrade = new Wix.MajorUpgrade();

            // find the DowngradePreventedCondition launch condition message
            if (null != launchConditionTable && 0 < launchConditionTable.Rows.Count)
            {
                foreach (var launchRow in launchConditionTable.Rows)
                {
                    if (Common.DowngradePreventedCondition == Convert.ToString(launchRow[0]))
                    {
                        downgradeErrorMessage = Convert.ToString(launchRow[1]);
                    }
                    else if (Common.UpgradePreventedCondition == Convert.ToString(launchRow[0]))
                    {
                        disallowUpgradeErrorMessage = Convert.ToString(launchRow[1]);
                    }
                }
            }

            if (null != upgradeTable && 0 < upgradeTable.Rows.Count)
            {
                var hasMajorUpgrade = false;

                foreach (var row in upgradeTable.Rows)
                {
                    var upgradeRow = (UpgradeRow)row;

                    if (Common.UpgradeDetectedProperty == upgradeRow.ActionProperty)
                    {
                        hasMajorUpgrade = true;
                        var attr = upgradeRow.Attributes;
                        var removeFeatures = upgradeRow.Remove;

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive == (attr & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive))
                        {
                            majorUpgrade.AllowSameVersionUpgrades = Wix.YesNoType.yes;
                        }

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures != (attr & WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures))
                        {
                            majorUpgrade.MigrateFeatures = Wix.YesNoType.no;
                        }

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure == (attr & WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure))
                        {
                            majorUpgrade.IgnoreRemoveFailure = Wix.YesNoType.yes;
                        }

                        if (!String.IsNullOrEmpty(removeFeatures))
                        {
                            majorUpgrade.RemoveFeatures = removeFeatures;
                        }
                    }
                    else if (Common.DowngradeDetectedProperty == upgradeRow.ActionProperty)
                    {
                        hasMajorUpgrade = true;
                        majorUpgrade.DowngradeErrorMessage = downgradeErrorMessage;
                    }
                }

                if (hasMajorUpgrade)
                {
                    if (String.IsNullOrEmpty(downgradeErrorMessage))
                    {
                        majorUpgrade.AllowDowngrades = Wix.YesNoType.yes;
                    }

                    if (!String.IsNullOrEmpty(disallowUpgradeErrorMessage))
                    {
                        majorUpgrade.Disallow = Wix.YesNoType.yes;
                        majorUpgrade.DisallowUpgradeErrorMessage = disallowUpgradeErrorMessage;
                    }

                    var scheduledType = DetermineMajorUpgradeScheduling(tables);
                    if (Wix.MajorUpgrade.ScheduleType.afterInstallValidate != scheduledType)
                    {
                        majorUpgrade.Schedule = scheduledType;
                    }

                    this.core.RootElement.AddChild(majorUpgrade);
                }
            }
        }

        /// <summary>
        /// Finalize the Verb table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The Extension table is a foreign table for the Verb table, but the
        /// foreign key is only part of the primary key of the Extension table,
        /// so it needs special logic to be nested properly.
        /// </remarks>
        private void FinalizeVerbTable(TableIndexedCollection tables)
        {
            var extensionTable = tables["Extension"];
            var verbTable = tables["Verb"];

            var extensionElements = new Hashtable();

            if (null != extensionTable)
            {
                foreach (var row in extensionTable.Rows)
                {
                    var extension = (Wix.Extension)this.core.GetIndexedElement(row);

                    if (!extensionElements.Contains(row[0]))
                    {
                        extensionElements.Add(row[0], new ArrayList());
                    }

                    ((ArrayList)extensionElements[row[0]]).Add(extension);
                }
            }

            if (null != verbTable)
            {
                foreach (var row in verbTable.Rows)
                {
                    var verb = (Wix.Verb)this.core.GetIndexedElement(row);

                    var extensionsArray = (ArrayList)extensionElements[row[0]];
                    if (null != extensionsArray)
                    {
                        foreach (Wix.Extension extension in extensionsArray)
                        {
                            extension.AddChild(verb);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, verbTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Extension_", Convert.ToString(row[0]), "Extension"));
                    }
                }
            }
        }

        private static string GetSequenceTableName(SequenceTable sequenceTable)
        {
            switch (sequenceTable)
            {
                case SequenceTable.AdvertiseExecuteSequence:
                    return "AdvtExecuteSequence";
                default:
                    return sequenceTable.ToString();
            }
        }

        /// <summary>
        /// Get the path to a file in the source image.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>The path to the file in the source image.</returns>
        private string GetSourcePath(Wix.File file)
        {
            var sourcePath = new StringBuilder();

            var component = (Wix.Component)file.ParentElement;

            for (var directory = (Wix.Directory)component.ParentElement; null != directory; directory = directory.ParentElement as Wix.Directory)
            {
                string name;

                if (!this.shortNames && null != directory.SourceName)
                {
                    name = directory.SourceName;
                }
                else if (null != directory.ShortSourceName)
                {
                    name = directory.ShortSourceName;
                }
                else if (!this.shortNames || null == directory.ShortName)
                {
                    name = directory.Name;
                }
                else
                {
                    name = directory.ShortName;
                }

                if (0 == sourcePath.Length)
                {
                    sourcePath.Append(name);
                }
                else
                {
                    sourcePath.Insert(0, Path.DirectorySeparatorChar);
                    sourcePath.Insert(0, name);
                }
            }

            return sourcePath.ToString();
        }

        /// <summary>
        /// Resolve the dependencies for a table (this is a helper method for GetSortedTableNames).
        /// </summary>
        /// <param name="tableName">The name of the table to resolve.</param>
        /// <param name="unsortedTableNames">The unsorted table names.</param>
        /// <param name="sortedTableNames">The sorted table names.</param>
        private void ResolveTableDependencies(string tableName, SortedList unsortedTableNames, StringCollection sortedTableNames)
        {
            unsortedTableNames.Remove(tableName);

            foreach (var columnDefinition in this.tableDefinitions[tableName].Columns)
            {
                // no dependency to resolve because this column doesn't reference another table
                if (null == columnDefinition.KeyTable)
                {
                    continue;
                }

                foreach (var keyTable in columnDefinition.KeyTable.Split(';'))
                {
                    if (tableName == keyTable)
                    {
                        continue; // self-referencing dependency
                    }
                    else if (sortedTableNames.Contains(keyTable))
                    {
                        continue; // dependent table has already been sorted
                    }
                    else if (!this.tableDefinitions.Contains(keyTable))
                    {
                        this.Messaging.Write(ErrorMessages.MissingTableDefinition(keyTable));
                    }
                    else if (unsortedTableNames.Contains(keyTable))
                    {
                        this.ResolveTableDependencies(keyTable, unsortedTableNames, sortedTableNames);
                    }
                    else
                    {
                        // found a circular dependency, so ignore it (this assumes that the tables will
                        // use a finalize method to nest their elements since the ordering will not be
                        // deterministic
                    }
                }
            }

            sortedTableNames.Add(tableName);
        }

        /// <summary>
        /// Get the names of the tables to process in the order they should be processed, according to their dependencies.
        /// </summary>
        /// <returns>A StringCollection containing the ordered table names.</returns>
        private StringCollection GetSortedTableNames()
        {
            var sortedTableNames = new StringCollection();
            var unsortedTableNames = new SortedList();

            // index the table names
            foreach (var tableDefinition in this.tableDefinitions)
            {
                unsortedTableNames.Add(tableDefinition.Name, tableDefinition.Name);
            }

            // resolve the dependencies for each table
            while (0 < unsortedTableNames.Count)
            {
                this.ResolveTableDependencies(Convert.ToString(unsortedTableNames.GetByIndex(0)), unsortedTableNames, sortedTableNames);
            }

            return sortedTableNames;
        }

        /// <summary>
        /// Initialize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void InitializeDecompile(TableIndexedCollection tables, int codepage)
        {
            // reset all the state information
            this.compressed = false;
            this.patchTargetFiles.Clear();
            this.sequenceElements.Clear();
            this.shortNames = false;

            // set the codepage if its not neutral (0)
            if (0 != codepage)
            {
                switch (this.OutputType)
                {
                case OutputType.Module:
                    ((Wix.Module)this.core.RootElement).Codepage = codepage.ToString(CultureInfo.InvariantCulture);
                    break;
                case OutputType.PatchCreation:
                    ((Wix.PatchCreation)this.core.RootElement).Codepage = codepage.ToString(CultureInfo.InvariantCulture);
                    break;
                case OutputType.Product:
                    ((Wix.Product)this.core.RootElement).Codepage = codepage.ToString(CultureInfo.InvariantCulture);
                    break;
                }
            }

            // index the rows from the extension libraries
            var indexedExtensionTables = new Dictionary<string, HashSet<string>>();
#if TODO_DECOMPILER_EXTENSIONS
            foreach (IDecompilerExtension extension in this.extensions)
            {
                // Get the optional library from the extension with the rows to be removed.
                Library library = extension.GetLibraryToRemove(this.tableDefinitions);
                if (null != library)
                {
                    foreach (var section in library.Sections)
                    {
                        foreach (Table table in section.Tables)
                        {
                            foreach (Row row in table.Rows)
                            {
                                string primaryKey;
                                string tableName;

                                // the Actions table needs to be handled specially
                                if ("WixAction" == table.Name)
                                {
                                    primaryKey = Convert.ToString(row[1]);

                                    if (OutputType.Module == this.outputType)
                                    {
                                        tableName = String.Concat("Module", Convert.ToString(row[0]));
                                    }
                                    else
                                    {
                                        tableName = Convert.ToString(row[0]);
                                    }
                                }
                                else
                                {
                                    primaryKey = row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter);
                                    tableName = table.Name;
                                }

                                if (null != primaryKey)
                                {
                                    HashSet<string> indexedExtensionRows;
                                    if (!indexedExtensionTables.TryGetValue(tableName, out indexedExtensionRows))
                                    {
                                        indexedExtensionRows = new HashSet<string>();
                                        indexedExtensionTables.Add(tableName, indexedExtensionRows);
                                    }

                                    indexedExtensionRows.Add(primaryKey);
                                }
                            }
                        }
                    }
                }
            }
#endif

            // remove the rows from the extension libraries (to allow full round-tripping)
            foreach (var kvp in indexedExtensionTables)
            {
                var tableName = kvp.Key;
                var indexedExtensionRows = kvp.Value;

                var table = tables[tableName];
                if (null != table)
                {
                    var originalRows = new RowDictionary<Row>(table);

                    // remove the original rows so that they can be added back if they should remain
                    table.Rows.Clear();

                    foreach (var row in originalRows.Values)
                    {
                        if (!indexedExtensionRows.Contains(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter)))
                        {
                            table.Rows.Add(row);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the tables.
        /// </summary>
        /// <param name="output">The output being decompiled.</param>
        private void DecompileTables(WindowsInstallerData output)
        {
            var sortedTableNames = this.GetSortedTableNames();

            foreach (var tableName in sortedTableNames)
            {
                var table = output.Tables[tableName];

                // table does not exist in this database or should not be decompiled
                if (null == table || !this.DecompilableTable(output, tableName))
                {
                    continue;
                }

                this.Messaging.Write(VerboseMessages.DecompilingTable(table.Name));

                // empty tables may be kept with EnsureTable if the user set the proper option
                if (0 == table.Rows.Count && this.SuppressDroppingEmptyTables)
                {
                    var ensureTable = new Wix.EnsureTable();
                    ensureTable.Id = table.Name;
                    this.core.RootElement.AddChild(ensureTable);
                }

                switch (table.Name)
                {
                case "_SummaryInformation":
                    this.Decompile_SummaryInformationTable(table);
                    break;
                case "AdminExecuteSequence":
                case "AdminUISequence":
                case "AdvtExecuteSequence":
                case "InstallExecuteSequence":
                case "InstallUISequence":
                case "ModuleAdminExecuteSequence":
                case "ModuleAdminUISequence":
                case "ModuleAdvtExecuteSequence":
                case "ModuleInstallExecuteSequence":
                case "ModuleInstallUISequence":
                    // handled in FinalizeSequenceTables
                    break;
                case "ActionText":
                    this.DecompileActionTextTable(table);
                    break;
                case "AdvtUISequence":
                    this.Messaging.Write(WarningMessages.DeprecatedTable(table.Name));
                    break;
                case "AppId":
                    this.DecompileAppIdTable(table);
                    break;
                case "AppSearch":
                    // handled in FinalizeSearchTables
                    break;
                case "BBControl":
                    this.DecompileBBControlTable(table);
                    break;
                case "Billboard":
                    this.DecompileBillboardTable(table);
                    break;
                case "Binary":
                    this.DecompileBinaryTable(table);
                    break;
                case "BindImage":
                    this.DecompileBindImageTable(table);
                    break;
                case "CCPSearch":
                    // handled in FinalizeSearchTables
                    break;
                case "CheckBox":
                    // handled in FinalizeCheckBoxTable
                    break;
                case "Class":
                    this.DecompileClassTable(table);
                    break;
                case "ComboBox":
                    this.DecompileComboBoxTable(table);
                    break;
                case "Control":
                    this.DecompileControlTable(table);
                    break;
                case "ControlCondition":
                    this.DecompileControlConditionTable(table);
                    break;
                case "ControlEvent":
                    this.DecompileControlEventTable(table);
                    break;
                case "CreateFolder":
                    this.DecompileCreateFolderTable(table);
                    break;
                case "CustomAction":
                    this.DecompileCustomActionTable(table);
                    break;
                case "CompLocator":
                    this.DecompileCompLocatorTable(table);
                    break;
                case "Complus":
                    this.DecompileComplusTable(table);
                    break;
                case "Component":
                    this.DecompileComponentTable(table);
                    break;
                case "Condition":
                    this.DecompileConditionTable(table);
                    break;
                case "Dialog":
                    this.DecompileDialogTable(table);
                    break;
                case "Directory":
                    this.DecompileDirectoryTable(table);
                    break;
                case "DrLocator":
                    this.DecompileDrLocatorTable(table);
                    break;
                case "DuplicateFile":
                    this.DecompileDuplicateFileTable(table);
                    break;
                case "Environment":
                    this.DecompileEnvironmentTable(table);
                    break;
                case "Error":
                    this.DecompileErrorTable(table);
                    break;
                case "EventMapping":
                    this.DecompileEventMappingTable(table);
                    break;
                case "Extension":
                    this.DecompileExtensionTable(table);
                    break;
                case "ExternalFiles":
                    this.DecompileExternalFilesTable(table);
                    break;
                case "FamilyFileRanges":
                    // handled in FinalizeFamilyFileRangesTable
                    break;
                case "Feature":
                    this.DecompileFeatureTable(table);
                    break;
                case "FeatureComponents":
                    this.DecompileFeatureComponentsTable(table);
                    break;
                case "File":
                    this.DecompileFileTable(table);
                    break;
                case "FileSFPCatalog":
                    this.DecompileFileSFPCatalogTable(table);
                    break;
                case "Font":
                    this.DecompileFontTable(table);
                    break;
                case "Icon":
                    this.DecompileIconTable(table);
                    break;
                case "ImageFamilies":
                    this.DecompileImageFamiliesTable(table);
                    break;
                case "IniFile":
                    this.DecompileIniFileTable(table);
                    break;
                case "IniLocator":
                    this.DecompileIniLocatorTable(table);
                    break;
                case "IsolatedComponent":
                    this.DecompileIsolatedComponentTable(table);
                    break;
                case "LaunchCondition":
                    this.DecompileLaunchConditionTable(table);
                    break;
                case "ListBox":
                    this.DecompileListBoxTable(table);
                    break;
                case "ListView":
                    this.DecompileListViewTable(table);
                    break;
                case "LockPermissions":
                    this.DecompileLockPermissionsTable(table);
                    break;
                case "Media":
                    this.DecompileMediaTable(table);
                    break;
                case "MIME":
                    this.DecompileMIMETable(table);
                    break;
                case "ModuleAdvtUISequence":
                    this.Messaging.Write(WarningMessages.DeprecatedTable(table.Name));
                    break;
                case "ModuleComponents":
                    // handled by DecompileComponentTable (since the ModuleComponents table
                    // rows are created by nesting components under the Module element)
                    break;
                case "ModuleConfiguration":
                    this.DecompileModuleConfigurationTable(table);
                    break;
                case "ModuleDependency":
                    this.DecompileModuleDependencyTable(table);
                    break;
                case "ModuleExclusion":
                    this.DecompileModuleExclusionTable(table);
                    break;
                case "ModuleIgnoreTable":
                    this.DecompileModuleIgnoreTableTable(table);
                    break;
                case "ModuleSignature":
                    this.DecompileModuleSignatureTable(table);
                    break;
                case "ModuleSubstitution":
                    this.DecompileModuleSubstitutionTable(table);
                    break;
                case "MoveFile":
                    this.DecompileMoveFileTable(table);
                    break;
                case "MsiAssembly":
                    // handled in FinalizeFileTable
                    break;
                case "MsiDigitalCertificate":
                    this.DecompileMsiDigitalCertificateTable(table);
                    break;
                case "MsiDigitalSignature":
                    this.DecompileMsiDigitalSignatureTable(table);
                    break;
                case "MsiEmbeddedChainer":
                    this.DecompileMsiEmbeddedChainerTable(table);
                    break;
                case "MsiEmbeddedUI":
                    this.DecompileMsiEmbeddedUITable(table);
                    break;
                case "MsiLockPermissionsEx":
                    this.DecompileMsiLockPermissionsExTable(table);
                    break;
                case "MsiPackageCertificate":
                    this.DecompileMsiPackageCertificateTable(table);
                    break;
                case "MsiPatchCertificate":
                    this.DecompileMsiPatchCertificateTable(table);
                    break;
                case "MsiShortcutProperty":
                    this.DecompileMsiShortcutPropertyTable(table);
                    break;
                case "ODBCAttribute":
                    this.DecompileODBCAttributeTable(table);
                    break;
                case "ODBCDataSource":
                    this.DecompileODBCDataSourceTable(table);
                    break;
                case "ODBCDriver":
                    this.DecompileODBCDriverTable(table);
                    break;
                case "ODBCSourceAttribute":
                    this.DecompileODBCSourceAttributeTable(table);
                    break;
                case "ODBCTranslator":
                    this.DecompileODBCTranslatorTable(table);
                    break;
                case "PatchMetadata":
                    this.DecompilePatchMetadataTable(table);
                    break;
                case "PatchSequence":
                    this.DecompilePatchSequenceTable(table);
                    break;
                case "ProgId":
                    this.DecompileProgIdTable(table);
                    break;
                case "Properties":
                    this.DecompilePropertiesTable(table);
                    break;
                case "Property":
                    this.DecompilePropertyTable(table);
                    break;
                case "PublishComponent":
                    this.DecompilePublishComponentTable(table);
                    break;
                case "RadioButton":
                    this.DecompileRadioButtonTable(table);
                    break;
                case "Registry":
                    this.DecompileRegistryTable(table);
                    break;
                case "RegLocator":
                    this.DecompileRegLocatorTable(table);
                    break;
                case "RemoveFile":
                    this.DecompileRemoveFileTable(table);
                    break;
                case "RemoveIniFile":
                    this.DecompileRemoveIniFileTable(table);
                    break;
                case "RemoveRegistry":
                    this.DecompileRemoveRegistryTable(table);
                    break;
                case "ReserveCost":
                    this.DecompileReserveCostTable(table);
                    break;
                case "SelfReg":
                    this.DecompileSelfRegTable(table);
                    break;
                case "ServiceControl":
                    this.DecompileServiceControlTable(table);
                    break;
                case "ServiceInstall":
                    this.DecompileServiceInstallTable(table);
                    break;
                case "SFPCatalog":
                    this.DecompileSFPCatalogTable(table);
                    break;
                case "Shortcut":
                    this.DecompileShortcutTable(table);
                    break;
                case "Signature":
                    this.DecompileSignatureTable(table);
                    break;
                case "TargetFiles_OptionalData":
                    this.DecompileTargetFiles_OptionalDataTable(table);
                    break;
                case "TargetImages":
                    this.DecompileTargetImagesTable(table);
                    break;
                case "TextStyle":
                    this.DecompileTextStyleTable(table);
                    break;
                case "TypeLib":
                    this.DecompileTypeLibTable(table);
                    break;
                case "Upgrade":
                    this.DecompileUpgradeTable(table);
                    break;
                case "UpgradedFiles_OptionalData":
                    this.DecompileUpgradedFiles_OptionalDataTable(table);
                    break;
                case "UpgradedFilesToIgnore":
                    this.DecompileUpgradedFilesToIgnoreTable(table);
                    break;
                case "UpgradedImages":
                    this.DecompileUpgradedImagesTable(table);
                    break;
                case "UIText":
                    this.DecompileUITextTable(table);
                    break;
                case "Verb":
                    this.DecompileVerbTable(table);
                    break;

                default:
#if TODO_DECOMPILER_EXTENSIONS
                    if (this.ExtensionsByTableName.TryGetValue(table.Name, out var extension)
                    {
                        extension.DecompileTable(table);
                    }
                    else
#endif
                    if (!this.SuppressCustomTables)
                    {
                        this.DecompileCustomTable(table);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Determine if a particular table should be decompiled with the current settings.
        /// </summary>
        /// <param name="output">The output being decompiled.</param>
        /// <param name="tableName">The name of a table.</param>
        /// <returns>true if the table should be decompiled; false otherwise.</returns>
        private bool DecompilableTable(WindowsInstallerData output, string tableName)
        {
            switch (tableName)
            {
            case "ActionText":
            case "BBControl":
            case "Billboard":
            case "CheckBox":
            case "Control":
            case "ControlCondition":
            case "ControlEvent":
            case "Dialog":
            case "Error":
            case "EventMapping":
            case "RadioButton":
            case "TextStyle":
            case "UIText":
                return !this.SuppressUI;
            case "ModuleAdminExecuteSequence":
            case "ModuleAdminUISequence":
            case "ModuleAdvtExecuteSequence":
            case "ModuleAdvtUISequence":
            case "ModuleComponents":
            case "ModuleConfiguration":
            case "ModuleDependency":
            case "ModuleIgnoreTable":
            case "ModuleInstallExecuteSequence":
            case "ModuleInstallUISequence":
            case "ModuleExclusion":
            case "ModuleSignature":
            case "ModuleSubstitution":
                if (OutputType.Module != output.Type)
                {
                    this.Messaging.Write(WarningMessages.SkippingMergeModuleTable(output.SourceLineNumbers, tableName));
                    return false;
                }
                else
                {
                    return true;
                }
            case "ExternalFiles":
            case "FamilyFileRanges":
            case "ImageFamilies":
            case "PatchMetadata":
            case "PatchSequence":
            case "Properties":
            case "TargetFiles_OptionalData":
            case "TargetImages":
            case "UpgradedFiles_OptionalData":
            case "UpgradedFilesToIgnore":
            case "UpgradedImages":
                if (OutputType.PatchCreation != output.Type)
                {
                    this.Messaging.Write(WarningMessages.SkippingPatchCreationTable(output.SourceLineNumbers, tableName));
                    return false;
                }
                else
                {
                    return true;
                }
            case "MsiPatchHeaders":
            case "MsiPatchMetadata":
            case "MsiPatchOldAssemblyName":
            case "MsiPatchOldAssemblyFile":
            case "MsiPatchSequence":
            case "Patch":
            case "PatchPackage":
                this.Messaging.Write(WarningMessages.PatchTable(output.SourceLineNumbers, tableName));
                return false;
            case "_SummaryInformation":
                return true;
            case "_Validation":
            case "MsiAssemblyName":
            case "MsiFileHash":
                return false;
            default: // all other tables are allowed in any output except for a patch creation package
                if (OutputType.PatchCreation == output.Type)
                {
                    this.Messaging.Write(WarningMessages.IllegalPatchCreationTable(output.SourceLineNumbers, tableName));
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Decompile the _SummaryInformation table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void Decompile_SummaryInformationTable(Table table)
        {
            if (OutputType.Module == this.OutputType || OutputType.Product == this.OutputType)
            {
                var package = new Wix.Package();

                foreach (var row in table.Rows)
                {
                    var value = Convert.ToString(row[1]);

                    if (null != value && 0 < value.Length)
                    {
                        switch (Convert.ToInt32(row[0]))
                        {
                        case 1:
                            if ("1252" != value)
                            {
                                package.SummaryCodepage = value;
                            }
                            break;
                        case 3:
                            package.Description = value;
                            break;
                        case 4:
                            package.Manufacturer = value;
                            break;
                        case 5:
                            if ("Installer" != value)
                            {
                                package.Keywords = value;
                            }
                            break;
                        case 6:
                            if (!value.StartsWith("This installer database contains the logic and data required to install "))
                            {
                                package.Comments = value;
                            }
                            break;
                        case 7:
                            var template = value.Split(';');
                            if (0 < template.Length && 0 < template[template.Length - 1].Length)
                            {
                                package.Languages = template[template.Length - 1];
                            }

                            if (1 < template.Length && null != template[0] && 0 < template[0].Length)
                            {
                                switch (template[0])
                                {
                                case "Intel":
                                    package.Platform = WixToolset.Data.Serialize.Package.PlatformType.x86;
                                    break;
                                case "Intel64":
                                    package.Platform = WixToolset.Data.Serialize.Package.PlatformType.ia64;
                                    break;
                                case "x64":
                                    package.Platform = WixToolset.Data.Serialize.Package.PlatformType.x64;
                                    break;
                                }
                            }
                            break;
                        case 9:
                            if (OutputType.Module == this.OutputType)
                            {
                                this.modularizationGuid = value;
                                package.Id = value;
                            }
                            break;
                        case 14:
                            package.InstallerVersion = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                            break;
                        case 15:
                            var wordCount = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                            if (0x1 == (wordCount & 0x1))
                            {
                                this.shortNames = true;
                                package.ShortNames = Wix.YesNoType.yes;
                            }

                            if (0x2 == (wordCount & 0x2))
                            {
                                this.compressed = true;

                                if (OutputType.Product == this.OutputType)
                                {
                                    package.Compressed = Wix.YesNoType.yes;
                                }
                            }

                            if (0x4 == (wordCount & 0x4))
                            {
                                package.AdminImage = Wix.YesNoType.yes;
                            }

                            if (0x8 == (wordCount & 0x8))
                            {
                                package.InstallPrivileges = Wix.Package.InstallPrivilegesType.limited;
                            }

                            break;
                        case 19:
                            var security = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                            switch (security)
                            {
                            case 0:
                                package.ReadOnly = Wix.YesNoDefaultType.no;
                                break;
                            case 4:
                                package.ReadOnly = Wix.YesNoDefaultType.yes;
                                break;
                            }
                            break;
                        }
                    }
                }

                this.core.RootElement.AddChild(package);
            }
            else
            {
                var patchInformation = new Wix.PatchInformation();

                foreach (var row in table.Rows)
                {
                    var propertyId = Convert.ToInt32(row[0]);
                    var value = Convert.ToString(row[1]);

                    if (null != row[1] && 0 < value.Length)
                    {
                        switch (propertyId)
                        {
                        case 1:
                            if ("1252" != value)
                            {
                                patchInformation.SummaryCodepage = value;
                            }
                            break;
                        case 3:
                            patchInformation.Description = value;
                            break;
                        case 4:
                            patchInformation.Manufacturer = value;
                            break;
                        case 5:
                            if ("Installer,Patching,PCP,Database" != value)
                            {
                                patchInformation.Keywords = value;
                            }
                            break;
                        case 6:
                            patchInformation.Comments = value;
                            break;
                        case 7:
                            var template = value.Split(';');
                            if (0 < template.Length && 0 < template[template.Length - 1].Length)
                            {
                                patchInformation.Languages = template[template.Length - 1];
                            }

                            if (1 < template.Length && null != template[0] && 0 < template[0].Length)
                            {
                                patchInformation.Platforms = template[0];
                            }
                            break;
                        case 15:
                            var wordCount = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            if (0x1 == (wordCount & 0x1))
                            {
                                patchInformation.ShortNames = Wix.YesNoType.yes;
                            }

                            if (0x2 == (wordCount & 0x2))
                            {
                                patchInformation.Compressed = Wix.YesNoType.yes;
                            }

                            if (0x4 == (wordCount & 0x4))
                            {
                                patchInformation.AdminImage = Wix.YesNoType.yes;
                            }
                            break;
                        case 19:
                            var security = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            switch (security)
                            {
                            case 0:
                                patchInformation.ReadOnly = Wix.YesNoDefaultType.no;
                                break;
                            case 4:
                                patchInformation.ReadOnly = Wix.YesNoDefaultType.yes;
                                break;
                            }
                            break;
                        }
                    }
                }

                this.core.RootElement.AddChild(patchInformation);
            }
        }

        /// <summary>
        /// Decompile the ActionText table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileActionTextTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var progressText = new Wix.ProgressText();

                progressText.Action = Convert.ToString(row[0]);

                if (null != row[1])
                {
                    progressText.Content = Convert.ToString(row[1]);
                }

                if (null != row[2])
                {
                    progressText.Template = Convert.ToString(row[2]);
                }

                this.core.UIElement.AddChild(progressText);
            }
        }

        /// <summary>
        /// Decompile the AppId table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileAppIdTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var appId = new Wix.AppId();

                appId.Advertise = Wix.YesNoType.yes;

                appId.Id = Convert.ToString(row[0]);

                if (null != row[1])
                {
                    appId.RemoteServerName = Convert.ToString(row[1]);
                }

                if (null != row[2])
                {
                    appId.LocalService = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    appId.ServiceParameters = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    appId.DllSurrogate = Convert.ToString(row[4]);
                }

                if (null != row[5] && Int32.Equals(row[5], 1))
                {
                    appId.ActivateAtStorage = Wix.YesNoType.yes;
                }

                if (null != row[6] && Int32.Equals(row[6], 1))
                {
                    appId.RunAsInteractiveUser = Wix.YesNoType.yes;
                }

                this.core.RootElement.AddChild(appId);
                this.core.IndexElement(row, appId);
            }
        }

        /// <summary>
        /// Decompile the BBControl table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileBBControlTable(Table table)
        {
            foreach (BBControlRow bbControlRow in table.Rows)
            {
                var control = new Wix.Control();

                control.Id = bbControlRow.BBControl;

                control.Type = bbControlRow.Type;

                control.X = bbControlRow.X;

                control.Y = bbControlRow.Y;

                control.Width = bbControlRow.Width;

                control.Height = bbControlRow.Height;

                if (null != bbControlRow[7])
                {
                    SetControlAttributes(bbControlRow.Attributes, control);
                }

                if (null != bbControlRow.Text)
                {
                    control.Text = bbControlRow.Text;
                }

                var billboard = (Wix.Billboard)this.core.GetIndexedElement("Billboard", bbControlRow.Billboard);
                if (null != billboard)
                {
                    billboard.AddChild(control);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(bbControlRow.SourceLineNumbers, table.Name, bbControlRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Billboard_", bbControlRow.Billboard, "Billboard"));
                }
            }
        }

        /// <summary>
        /// Decompile the Billboard table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileBillboardTable(Table table)
        {
            var billboardActions = new Hashtable();
            var billboards = new SortedList();

            foreach (var row in table.Rows)
            {
                var billboard = new Wix.Billboard();

                billboard.Id = Convert.ToString(row[0]);

                billboard.Feature = Convert.ToString(row[1]);

                this.core.IndexElement(row, billboard);
                billboards.Add(String.Format(CultureInfo.InvariantCulture, "{0}|{1:0000000000}", row[0], row[3]), row);
            }

            foreach (Row row in billboards.Values)
            {
                var billboard = (Wix.Billboard)this.core.GetIndexedElement(row);
                var billboardAction = (Wix.BillboardAction)billboardActions[row[2]];

                if (null == billboardAction)
                {
                    billboardAction = new Wix.BillboardAction();

                    billboardAction.Id = Convert.ToString(row[2]);

                    this.core.UIElement.AddChild(billboardAction);
                    billboardActions.Add(row[2], billboardAction);
                }

                billboardAction.AddChild(billboard);
            }
        }

        /// <summary>
        /// Decompile the Binary table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileBinaryTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var binary = new Wix.Binary();

                binary.Id = Convert.ToString(row[0]);

                binary.SourceFile = Convert.ToString(row[1]);

                this.core.RootElement.AddChild(binary);
            }
        }

        /// <summary>
        /// Decompile the BindImage table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileBindImageTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var file = (Wix.File)this.core.GetIndexedElement("File", Convert.ToString(row[0]));

                if (null != file)
                {
                    file.BindPath = Convert.ToString(row[1]);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", Convert.ToString(row[0]), "File"));
                }
            }
        }

        /// <summary>
        /// Decompile the Class table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileClassTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var wixClass = new Wix.Class();

                wixClass.Advertise = Wix.YesNoType.yes;

                wixClass.Id = Convert.ToString(row[0]);

                switch (Convert.ToString(row[1]))
                {
                case "LocalServer":
                    wixClass.Context = Wix.Class.ContextType.LocalServer;
                    break;
                case "LocalServer32":
                    wixClass.Context = Wix.Class.ContextType.LocalServer32;
                    break;
                case "InprocServer":
                    wixClass.Context = Wix.Class.ContextType.InprocServer;
                    break;
                case "InprocServer32":
                    wixClass.Context = Wix.Class.ContextType.InprocServer32;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                // ProgId children are handled in FinalizeProgIdTable

                if (null != row[4])
                {
                    wixClass.Description = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    wixClass.AppId = Convert.ToString(row[5]);
                }

                if (null != row[6])
                {
                    var fileTypeMaskStrings = (Convert.ToString(row[6])).Split(';');

                    try
                    {
                        foreach (var fileTypeMaskString in fileTypeMaskStrings)
                        {
                            var fileTypeMaskParts = fileTypeMaskString.Split(',');

                            if (4 == fileTypeMaskParts.Length)
                            {
                                var fileTypeMask = new Wix.FileTypeMask();

                                fileTypeMask.Offset = Convert.ToInt32(fileTypeMaskParts[0], CultureInfo.InvariantCulture);

                                fileTypeMask.Mask = fileTypeMaskParts[2];

                                fileTypeMask.Value = fileTypeMaskParts[3];

                                wixClass.AddChild(fileTypeMask);
                            }
                            else
                            {
                                // TODO: warn
                            }
                        }
                    }
                    catch (FormatException)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                    }
                    catch (OverflowException)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                    }
                }

                if (null != row[7])
                {
                    wixClass.Icon = Convert.ToString(row[7]);
                }

                if (null != row[8])
                {
                    wixClass.IconIndex = Convert.ToInt32(row[8]);
                }

                if (null != row[9])
                {
                    wixClass.Handler = Convert.ToString(row[9]);
                }

                if (null != row[10])
                {
                    wixClass.Argument = Convert.ToString(row[10]);
                }

                if (null != row[12])
                {
                    if (1 == Convert.ToInt32(row[12]))
                    {
                        wixClass.RelativePath = Wix.YesNoType.yes;
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[12].Column.Name, row[12]));
                    }
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[2]));
                if (null != component)
                {
                    component.AddChild(wixClass);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[2]), "Component"));
                }

                this.core.IndexElement(row, wixClass);
            }
        }

        /// <summary>
        /// Decompile the ComboBox table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComboBoxTable(Table table)
        {
            Wix.ComboBox comboBox = null;
            var comboBoxRows = new SortedList();

            // sort the combo boxes by their property and order
            foreach (var row in table.Rows)
            {
                comboBoxRows.Add(String.Concat("{0}|{1:0000000000}", row[0], row[1]), row);
            }

            foreach (Row row in comboBoxRows.Values)
            {
                if (null == comboBox || Convert.ToString(row[0]) != comboBox.Property)
                {
                    comboBox = new Wix.ComboBox();

                    comboBox.Property = Convert.ToString(row[0]);

                    this.core.UIElement.AddChild(comboBox);
                }

                var listItem = new Wix.ListItem();

                listItem.Value = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    listItem.Text = Convert.ToString(row[3]);
                }

                comboBox.AddChild(listItem);
            }
        }

        /// <summary>
        /// Decompile the Control table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileControlTable(Table table)
        {
            foreach (ControlRow controlRow in table.Rows)
            {
                var control = new Wix.Control();

                control.Id = controlRow.Control;

                control.Type = controlRow.Type;

                control.X = controlRow.X;

                control.Y = controlRow.Y;

                control.Width = controlRow.Width;

                control.Height = controlRow.Height;

                if (null != controlRow[7])
                {
                    string[] specialAttributes;

                    // sets various common attributes like Disabled, Indirect, Integer, ...
                    SetControlAttributes(controlRow.Attributes, control);

                    switch (control.Type)
                    {
                    case "Bitmap":
                        specialAttributes = BitmapControlAttributes;
                        break;
                    case "CheckBox":
                        specialAttributes = CheckboxControlAttributes;
                        break;
                    case "ComboBox":
                        specialAttributes = ComboboxControlAttributes;
                        break;
                    case "DirectoryCombo":
                        specialAttributes = VolumeControlAttributes;
                        break;
                    case "Edit":
                        specialAttributes = EditControlAttributes;
                        break;
                    case "Icon":
                        specialAttributes = IconControlAttributes;
                        break;
                    case "ListBox":
                        specialAttributes = ListboxControlAttributes;
                        break;
                    case "ListView":
                        specialAttributes = ListviewControlAttributes;
                        break;
                    case "MaskedEdit":
                        specialAttributes = EditControlAttributes;
                        break;
                    case "PathEdit":
                        specialAttributes = EditControlAttributes;
                        break;
                    case "ProgressBar":
                        specialAttributes = ProgressControlAttributes;
                        break;
                    case "PushButton":
                        specialAttributes = ButtonControlAttributes;
                        break;
                    case "RadioButtonGroup":
                        specialAttributes = RadioControlAttributes;
                        break;
                    case "Text":
                        specialAttributes = TextControlAttributes;
                        break;
                    case "VolumeCostList":
                        specialAttributes = VolumeControlAttributes;
                        break;
                    case "VolumeSelectCombo":
                        specialAttributes = VolumeControlAttributes;
                        break;
                    default:
                        specialAttributes = null;
                        break;
                    }

                    if (null != specialAttributes)
                    {
                        var iconSizeSet = false;

                        for (var i = 16; 32 > i; i++)
                        {
                            if (1 == ((controlRow.Attributes >> i) & 1))
                            {
                                string attribute = null;

                                if (specialAttributes.Length > (i - 16))
                                {
                                    attribute = specialAttributes[i - 16];
                                }

                                // unknown attribute
                                if (null == attribute)
                                {
                                    this.Messaging.Write(WarningMessages.IllegalColumnValue(controlRow.SourceLineNumbers, table.Name, controlRow.Fields[7].Column.Name, controlRow.Attributes));
                                    continue;
                                }

                                switch (attribute)
                                {
                                case "Bitmap":
                                    control.Bitmap = Wix.YesNoType.yes;
                                    break;
                                case "CDROM":
                                    control.CDROM = Wix.YesNoType.yes;
                                    break;
                                case "ComboList":
                                    control.ComboList = Wix.YesNoType.yes;
                                    break;
                                case "ElevationShield":
                                    control.ElevationShield = Wix.YesNoType.yes;
                                    break;
                                case "Fixed":
                                    control.Fixed = Wix.YesNoType.yes;
                                    break;
                                case "FixedSize":
                                    control.FixedSize = Wix.YesNoType.yes;
                                    break;
                                case "Floppy":
                                    control.Floppy = Wix.YesNoType.yes;
                                    break;
                                case "FormatSize":
                                    control.FormatSize = Wix.YesNoType.yes;
                                    break;
                                case "HasBorder":
                                    control.HasBorder = Wix.YesNoType.yes;
                                    break;
                                case "Icon":
                                    control.Icon = Wix.YesNoType.yes;
                                    break;
                                case "Icon16":
                                    if (iconSizeSet)
                                    {
                                        control.IconSize = Wix.Control.IconSizeType.Item48;
                                    }
                                    else
                                    {
                                        iconSizeSet = true;
                                        control.IconSize = Wix.Control.IconSizeType.Item16;
                                    }
                                    break;
                                case "Icon32":
                                    if (iconSizeSet)
                                    {
                                        control.IconSize = Wix.Control.IconSizeType.Item48;
                                    }
                                    else
                                    {
                                        iconSizeSet = true;
                                        control.IconSize = Wix.Control.IconSizeType.Item32;
                                    }
                                    break;
                                case "Image":
                                    control.Image = Wix.YesNoType.yes;
                                    break;
                                case "Multiline":
                                    control.Multiline = Wix.YesNoType.yes;
                                    break;
                                case "NoPrefix":
                                    control.NoPrefix = Wix.YesNoType.yes;
                                    break;
                                case "NoWrap":
                                    control.NoWrap = Wix.YesNoType.yes;
                                    break;
                                case "Password":
                                    control.Password = Wix.YesNoType.yes;
                                    break;
                                case "ProgressBlocks":
                                    control.ProgressBlocks = Wix.YesNoType.yes;
                                    break;
                                case "PushLike":
                                    control.PushLike = Wix.YesNoType.yes;
                                    break;
                                case "RAMDisk":
                                    control.RAMDisk = Wix.YesNoType.yes;
                                    break;
                                case "Remote":
                                    control.Remote = Wix.YesNoType.yes;
                                    break;
                                case "Removable":
                                    control.Removable = Wix.YesNoType.yes;
                                    break;
                                case "ShowRollbackCost":
                                    control.ShowRollbackCost = Wix.YesNoType.yes;
                                    break;
                                case "Sorted":
                                    control.Sorted = Wix.YesNoType.yes;
                                    break;
                                case "Transparent":
                                    control.Transparent = Wix.YesNoType.yes;
                                    break;
                                case "UserLanguage":
                                    control.UserLanguage = Wix.YesNoType.yes;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown control attribute: '{attribute}'.");
                                }
                            }
                        }
                    }
                    else if (0 < (controlRow.Attributes & 0xFFFF0000))
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(controlRow.SourceLineNumbers, table.Name, controlRow.Fields[7].Column.Name, controlRow.Attributes));
                    }
                }

                // FinalizeCheckBoxTable adds Control/@Property|@CheckBoxPropertyRef
                if (null != controlRow.Property && 0 != String.CompareOrdinal("CheckBox", control.Type))
                {
                    control.Property = controlRow.Property;
                }

                if (null != controlRow.Text)
                {
                    control.Text = controlRow.Text;
                }

                if (null != controlRow.Help)
                {
                    var help = controlRow.Help.Split('|');

                    if (2 == help.Length)
                    {
                        if (0 < help[0].Length)
                        {
                            control.ToolTip = help[0];
                        }

                        if (0 < help[1].Length)
                        {
                            control.Help = help[1];
                        }
                    }
                }

                this.core.IndexElement(controlRow, control);
            }
        }

        /// <summary>
        /// Decompile the ControlCondition table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileControlConditionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var condition = new Wix.Condition();

                switch (Convert.ToString(row[2]))
                {
                case "Default":
                    condition.Action = Wix.Condition.ActionType.@default;
                    break;
                case "Disable":
                    condition.Action = Wix.Condition.ActionType.disable;
                    break;
                case "Enable":
                    condition.Action = Wix.Condition.ActionType.enable;
                    break;
                case "Hide":
                    condition.Action = Wix.Condition.ActionType.hide;
                    break;
                case "Show":
                    condition.Action = Wix.Condition.ActionType.show;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                    break;
                }

                condition.Content = Convert.ToString(row[3]);

                var control = (Wix.Control)this.core.GetIndexedElement("Control", Convert.ToString(row[0]), Convert.ToString(row[1]));
                if (null != control)
                {
                    control.AddChild(condition);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", Convert.ToString(row[0]), "Control_", Convert.ToString(row[1]), "Control"));
                }
            }
        }

        /// <summary>
        /// Decompile the ControlEvent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileControlEventTable(Table table)
        {
            var controlEvents = new SortedList();

            foreach (var row in table.Rows)
            {
                var publish = new Wix.Publish();

                var publishEvent = Convert.ToString(row[2]);
                if (publishEvent.StartsWith("[", StringComparison.Ordinal) && publishEvent.EndsWith("]", StringComparison.Ordinal))
                {
                    publish.Property = publishEvent.Substring(1, publishEvent.Length - 2);

                    if ("{}" != Convert.ToString(row[3]))
                    {
                        publish.Value = Convert.ToString(row[3]);
                    }
                }
                else
                {
                    publish.Event = publishEvent;
                    publish.Value = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    publish.Content = Convert.ToString(row[4]);
                }

                controlEvents.Add(String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2:0000000000}|{3}|{4}|{5}", row[0], row[1], (null == row[5] ? 0 : Convert.ToInt32(row[5])), row[2], row[3], row[4]), row);

                this.core.IndexElement(row, publish);
            }

            foreach (Row row in controlEvents.Values)
            {
                var control = (Wix.Control)this.core.GetIndexedElement("Control", Convert.ToString(row[0]), Convert.ToString(row[1]));
                var publish = (Wix.Publish)this.core.GetIndexedElement(row);

                if (null != control)
                {
                    control.AddChild(publish);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", Convert.ToString(row[0]), "Control_", Convert.ToString(row[1]), "Control"));
                }
            }
        }

        /// <summary>
        /// Decompile a custom table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCustomTable(Table table)
        {
            if (0 < table.Rows.Count || this.SuppressDroppingEmptyTables)
            {
                var customTable = new Wix.CustomTable();

                this.Messaging.Write(WarningMessages.DecompilingAsCustomTable(table.Rows[0].SourceLineNumbers, table.Name));

                customTable.Id = table.Name;

                foreach (var columnDefinition in table.Definition.Columns)
                {
                    var column = new Wix.Column();

                    column.Id = columnDefinition.Name;

                    if (ColumnCategory.Unknown != columnDefinition.Category)
                    {
                        switch (columnDefinition.Category)
                        {
                        case ColumnCategory.Text:
                            column.Category = Wix.Column.CategoryType.Text;
                            break;
                        case ColumnCategory.UpperCase:
                            column.Category = Wix.Column.CategoryType.UpperCase;
                            break;
                        case ColumnCategory.LowerCase:
                            column.Category = Wix.Column.CategoryType.LowerCase;
                            break;
                        case ColumnCategory.Integer:
                            column.Category = Wix.Column.CategoryType.Integer;
                            break;
                        case ColumnCategory.DoubleInteger:
                            column.Category = Wix.Column.CategoryType.DoubleInteger;
                            break;
                        case ColumnCategory.TimeDate:
                            column.Category = Wix.Column.CategoryType.TimeDate;
                            break;
                        case ColumnCategory.Identifier:
                            column.Category = Wix.Column.CategoryType.Identifier;
                            break;
                        case ColumnCategory.Property:
                            column.Category = Wix.Column.CategoryType.Property;
                            break;
                        case ColumnCategory.Filename:
                            column.Category = Wix.Column.CategoryType.Filename;
                            break;
                        case ColumnCategory.WildCardFilename:
                            column.Category = Wix.Column.CategoryType.WildCardFilename;
                            break;
                        case ColumnCategory.Path:
                            column.Category = Wix.Column.CategoryType.Path;
                            break;
                        case ColumnCategory.Paths:
                            column.Category = Wix.Column.CategoryType.Paths;
                            break;
                        case ColumnCategory.AnyPath:
                            column.Category = Wix.Column.CategoryType.AnyPath;
                            break;
                        case ColumnCategory.DefaultDir:
                            column.Category = Wix.Column.CategoryType.DefaultDir;
                            break;
                        case ColumnCategory.RegPath:
                            column.Category = Wix.Column.CategoryType.RegPath;
                            break;
                        case ColumnCategory.Formatted:
                            column.Category = Wix.Column.CategoryType.Formatted;
                            break;
                        case ColumnCategory.FormattedSDDLText:
                            column.Category = Wix.Column.CategoryType.FormattedSddl;
                            break;
                        case ColumnCategory.Template:
                            column.Category = Wix.Column.CategoryType.Template;
                            break;
                        case ColumnCategory.Condition:
                            column.Category = Wix.Column.CategoryType.Condition;
                            break;
                        case ColumnCategory.Guid:
                            column.Category = Wix.Column.CategoryType.Guid;
                            break;
                        case ColumnCategory.Version:
                            column.Category = Wix.Column.CategoryType.Version;
                            break;
                        case ColumnCategory.Language:
                            column.Category = Wix.Column.CategoryType.Language;
                            break;
                        case ColumnCategory.Binary:
                            column.Category = Wix.Column.CategoryType.Binary;
                            break;
                        case ColumnCategory.CustomSource:
                            column.Category = Wix.Column.CategoryType.CustomSource;
                            break;
                        case ColumnCategory.Cabinet:
                            column.Category = Wix.Column.CategoryType.Cabinet;
                            break;
                        case ColumnCategory.Shortcut:
                            column.Category = Wix.Column.CategoryType.Shortcut;
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown custom column category '{columnDefinition.Category.ToString()}'.");
                        }
                    }

                    if (null != columnDefinition.Description)
                    {
                        column.Description = columnDefinition.Description;
                    }

                    if (columnDefinition.KeyColumn.HasValue)
                    {
                        column.KeyColumn = columnDefinition.KeyColumn.Value;
                    }

                    if (null != columnDefinition.KeyTable)
                    {
                        column.KeyTable = columnDefinition.KeyTable;
                    }

                    if (columnDefinition.IsLocalizable)
                    {
                        column.Localizable = Wix.YesNoType.yes;
                    }

                    if (columnDefinition.MaxValue.HasValue)
                    {
                        column.MaxValue = columnDefinition.MaxValue.Value;
                    }

                    if (columnDefinition.MinValue.HasValue)
                    {
                        column.MinValue = columnDefinition.MinValue.Value;
                    }

                    if (ColumnModularizeType.None != columnDefinition.ModularizeType)
                    {
                        switch (columnDefinition.ModularizeType)
                        {
                        case ColumnModularizeType.Column:
                            column.Modularize = Wix.Column.ModularizeType.Column;
                            break;
                        case ColumnModularizeType.Condition:
                            column.Modularize = Wix.Column.ModularizeType.Condition;
                            break;
                        case ColumnModularizeType.Icon:
                            column.Modularize = Wix.Column.ModularizeType.Icon;
                            break;
                        case ColumnModularizeType.Property:
                            column.Modularize = Wix.Column.ModularizeType.Property;
                            break;
                        case ColumnModularizeType.SemicolonDelimited:
                            column.Modularize = Wix.Column.ModularizeType.SemicolonDelimited;
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown custom column modularization type '{columnDefinition.ModularizeType.ToString()}'.");
                        }
                    }

                    if (columnDefinition.Nullable)
                    {
                        column.Nullable = Wix.YesNoType.yes;
                    }

                    if (columnDefinition.PrimaryKey)
                    {
                        column.PrimaryKey = Wix.YesNoType.yes;
                    }

                    if (null != columnDefinition.Possibilities)
                    {
                        column.Set = columnDefinition.Possibilities;
                    }

                    if (ColumnType.Unknown != columnDefinition.Type)
                    {
                        switch (columnDefinition.Type)
                        {
                        case ColumnType.Localized:
                            column.Localizable = Wix.YesNoType.yes;
                            column.Type = Wix.Column.TypeType.@string;
                            break;
                        case ColumnType.Number:
                            column.Type = Wix.Column.TypeType.@int;
                            break;
                        case ColumnType.Object:
                            column.Type = Wix.Column.TypeType.binary;
                            break;
                        case ColumnType.Preserved:
                        case ColumnType.String:
                            column.Type = Wix.Column.TypeType.@string;
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown custom column type '{columnDefinition.Type.ToString()}'.");
                        }
                    }

                    column.Width = columnDefinition.Length;

                    customTable.AddChild(column);
                }

                foreach (var row in table.Rows)
                {
                    var wixRow = new Wix.Row();

                    foreach (var field in row.Fields)
                    {
                        var data = new Wix.Data();

                        data.Column = field.Column.Name;

                        data.Content = Convert.ToString(field.Data, CultureInfo.InvariantCulture);

                        wixRow.AddChild(data);
                    }

                    customTable.AddChild(wixRow);
                }

                this.core.RootElement.AddChild(customTable);
            }
        }

        /// <summary>
        /// Decompile the CreateFolder table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCreateFolderTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var createFolder = new Wix.CreateFolder();

                createFolder.Directory = Convert.ToString(row[0]);

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(createFolder);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
                this.core.IndexElement(row, createFolder);
            }
        }

        /// <summary>
        /// Decompile the CustomAction table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCustomActionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var customAction = new Wix.CustomAction();

                customAction.Id = Convert.ToString(row[0]);

                var type = Convert.ToInt32(row[1]);

                if (WindowsInstallerConstants.MsidbCustomActionTypeHideTarget == (type & WindowsInstallerConstants.MsidbCustomActionTypeHideTarget))
                {
                    customAction.HideTarget = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate == (type & WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate))
                {
                    customAction.Impersonate = Wix.YesNoType.no;
                }

                if (WindowsInstallerConstants.MsidbCustomActionTypeTSAware == (type & WindowsInstallerConstants.MsidbCustomActionTypeTSAware))
                {
                    customAction.TerminalServerAware = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbCustomActionType64BitScript == (type & WindowsInstallerConstants.MsidbCustomActionType64BitScript))
                {
                    customAction.Win64 = Wix.YesNoType.yes;
                }
                else if (WindowsInstallerConstants.MsidbCustomActionTypeVBScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeVBScript) ||
                    WindowsInstallerConstants.MsidbCustomActionTypeJScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeJScript))
                {
                    customAction.Win64 = Wix.YesNoType.no;
                }

                switch (type & WindowsInstallerConstants.MsidbCustomActionTypeExecuteBits)
                {
                case 0:
                    // this is the default value
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence:
                    customAction.Execute = Wix.CustomAction.ExecuteType.firstSequence;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess:
                    customAction.Execute = Wix.CustomAction.ExecuteType.oncePerProcess;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat:
                    customAction.Execute = Wix.CustomAction.ExecuteType.secondSequence;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeInScript:
                    customAction.Execute = Wix.CustomAction.ExecuteType.deferred;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeInScript + WindowsInstallerConstants.MsidbCustomActionTypeRollback:
                    customAction.Execute = Wix.CustomAction.ExecuteType.rollback;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeInScript + WindowsInstallerConstants.MsidbCustomActionTypeCommit:
                    customAction.Execute = Wix.CustomAction.ExecuteType.commit;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                switch (type & WindowsInstallerConstants.MsidbCustomActionTypeReturnBits)
                {
                case 0:
                    // this is the default value
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeContinue:
                    customAction.Return = Wix.CustomAction.ReturnType.ignore;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeAsync:
                    customAction.Return = Wix.CustomAction.ReturnType.asyncWait;
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeAsync + WindowsInstallerConstants.MsidbCustomActionTypeContinue:
                    customAction.Return = Wix.CustomAction.ReturnType.asyncNoWait;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                var source = type & WindowsInstallerConstants.MsidbCustomActionTypeSourceBits;
                switch (source)
                {
                case WindowsInstallerConstants.MsidbCustomActionTypeBinaryData:
                    customAction.BinaryKey = Convert.ToString(row[2]);
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeSourceFile:
                    if (null != row[2])
                    {
                        customAction.FileKey = Convert.ToString(row[2]);
                    }
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeDirectory:
                    if (null != row[2])
                    {
                        customAction.Directory = Convert.ToString(row[2]);
                    }
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeProperty:
                    customAction.Property = Convert.ToString(row[2]);
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                switch (type & WindowsInstallerConstants.MsidbCustomActionTypeTargetBits)
                {
                case WindowsInstallerConstants.MsidbCustomActionTypeDll:
                    customAction.DllEntry = Convert.ToString(row[3]);
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeExe:
                    customAction.ExeCommand = Convert.ToString(row[3]);
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeTextData:
                    if (WindowsInstallerConstants.MsidbCustomActionTypeSourceFile == source)
                    {
                        customAction.Error = Convert.ToString(row[3]);
                    }
                    else
                    {
                        customAction.Value = Convert.ToString(row[3]);
                    }
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeJScript:
                    if (WindowsInstallerConstants.MsidbCustomActionTypeDirectory == source)
                    {
                        customAction.Script = Wix.CustomAction.ScriptType.jscript;
                        customAction.Content = Convert.ToString(row[3]);
                    }
                    else
                    {
                        customAction.JScriptCall = Convert.ToString(row[3]);
                    }
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeVBScript:
                    if (WindowsInstallerConstants.MsidbCustomActionTypeDirectory == source)
                    {
                        customAction.Script = Wix.CustomAction.ScriptType.vbscript;
                        customAction.Content = Convert.ToString(row[3]);
                    }
                    else
                    {
                        customAction.VBScriptCall = Convert.ToString(row[3]);
                    }
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeInstall:
                    this.Messaging.Write(WarningMessages.NestedInstall(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    continue;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                var extype = 4 < row.Fields.Length && null != row[4] ? Convert.ToInt32(row[4]) : 0;
                if (WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall == (extype & WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall))
                {
                    customAction.PatchUninstall = Wix.YesNoType.yes;
                }

                this.core.RootElement.AddChild(customAction);
                this.core.IndexElement(row, customAction);
            }
        }

        /// <summary>
        /// Decompile the CompLocator table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCompLocatorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var componentSearch = new Wix.ComponentSearch();

                componentSearch.Id = Convert.ToString(row[0]);

                componentSearch.Guid = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    switch (Convert.ToInt32(row[2]))
                    {
                    case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                        componentSearch.Type = Wix.ComponentSearch.TypeType.directory;
                        break;
                    case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                        // this is the default value
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                        break;
                    }
                }

                this.core.IndexElement(row, componentSearch);
            }
        }

        /// <summary>
        /// Decompile the Complus table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComplusTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if (null != row[1])
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[0]));

                    if (null != component)
                    {
                        component.ComPlusFlags = Convert.ToInt32(row[1]);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[0]), "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the Component table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComponentTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var component = new Wix.Component();

                component.Id = Convert.ToString(row[0]);

                component.Guid = Convert.ToString(row[1]);

                var attributes = Convert.ToInt32(row[3]);

                if (WindowsInstallerConstants.MsidbComponentAttributesSourceOnly == (attributes & WindowsInstallerConstants.MsidbComponentAttributesSourceOnly))
                {
                    component.Location = Wix.Component.LocationType.source;
                }
                else if (WindowsInstallerConstants.MsidbComponentAttributesOptional == (attributes & WindowsInstallerConstants.MsidbComponentAttributesOptional))
                {
                    component.Location = Wix.Component.LocationType.either;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount == (attributes & WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount))
                {
                    component.SharedDllRefCount = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesPermanent == (attributes & WindowsInstallerConstants.MsidbComponentAttributesPermanent))
                {
                    component.Permanent = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesTransitive == (attributes & WindowsInstallerConstants.MsidbComponentAttributesTransitive))
                {
                    component.Transitive = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite == (attributes & WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite))
                {
                    component.NeverOverwrite = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributes64bit == (attributes & WindowsInstallerConstants.MsidbComponentAttributes64bit))
                {
                    component.Win64 = Wix.YesNoType.yes;
                }
                else
                {
                    component.Win64 = Wix.YesNoType.no;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection == (attributes & WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection))
                {
                    component.DisableRegistryReflection = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence == (attributes & WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence))
                {
                    component.UninstallWhenSuperseded = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesShared == (attributes & WindowsInstallerConstants.MsidbComponentAttributesShared))
                {
                    component.Shared = Wix.YesNoType.yes;
                }

                if (null != row[4])
                {
                    var condition = new Wix.Condition();

                    condition.Content = Convert.ToString(row[4]);

                    component.AddChild(condition);
                }

                var directory = (Wix.Directory)this.core.GetIndexedElement("Directory", Convert.ToString(row[2]));
                if (null != directory)
                {
                    directory.AddChild(component);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Directory_", Convert.ToString(row[2]), "Directory"));
                }
                this.core.IndexElement(row, component);
            }
        }

        /// <summary>
        /// Decompile the Condition table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileConditionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var condition = new Wix.Condition();

                condition.Level = Convert.ToInt32(row[1]);

                if (null != row[2])
                {
                    condition.Content = Convert.ToString(row[2]);
                }

                var feature = (Wix.Feature)this.core.GetIndexedElement("Feature", Convert.ToString(row[0]));
                if (null != feature)
                {
                    feature.AddChild(condition);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Feature_", Convert.ToString(row[0]), "Feature"));
                }
            }
        }

        /// <summary>
        /// Decompile the Dialog table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileDialogTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var dialog = new Wix.Dialog();

                dialog.Id = Convert.ToString(row[0]);

                dialog.X = Convert.ToInt32(row[1]);

                dialog.Y = Convert.ToInt32(row[2]);

                dialog.Width = Convert.ToInt32(row[3]);

                dialog.Height = Convert.ToInt32(row[4]);

                if (null != row[5])
                {
                    var attributes = Convert.ToInt32(row[5]);

                    if (0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesVisible))
                    {
                        dialog.Hidden = Wix.YesNoType.yes;
                    }

                    if (0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesModal))
                    {
                        dialog.Modeless = Wix.YesNoType.yes;
                    }

                    if (0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesMinimize))
                    {
                        dialog.NoMinimize = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesSysModal == (attributes & WindowsInstallerConstants.MsidbDialogAttributesSysModal))
                    {
                        dialog.SystemModal = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesKeepModeless == (attributes & WindowsInstallerConstants.MsidbDialogAttributesKeepModeless))
                    {
                        dialog.KeepModeless = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace == (attributes & WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace))
                    {
                        dialog.TrackDiskSpace = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette == (attributes & WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette))
                    {
                        dialog.CustomPalette = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesRTLRO == (attributes & WindowsInstallerConstants.MsidbDialogAttributesRTLRO))
                    {
                        dialog.RightToLeft = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesRightAligned == (attributes & WindowsInstallerConstants.MsidbDialogAttributesRightAligned))
                    {
                        dialog.RightAligned = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesLeftScroll == (attributes & WindowsInstallerConstants.MsidbDialogAttributesLeftScroll))
                    {
                        dialog.LeftScroll = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbDialogAttributesError == (attributes & WindowsInstallerConstants.MsidbDialogAttributesError))
                    {
                        dialog.ErrorDialog = Wix.YesNoType.yes;
                    }
                }

                if (null != row[6])
                {
                    dialog.Title = Convert.ToString(row[6]);
                }

                this.core.UIElement.AddChild(dialog);
                this.core.IndexElement(row, dialog);
            }
        }

        /// <summary>
        /// Decompile the Directory table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileDirectoryTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var directory = new Wix.Directory();

                directory.Id = Convert.ToString(row[0]);

                var names = Common.GetNames(Convert.ToString(row[2]));

                if (String.Equals(directory.Id, "TARGETDIR", StringComparison.Ordinal) && !String.Equals(names[0], "SourceDir", StringComparison.Ordinal))
                {
                    this.Messaging.Write(WarningMessages.TargetDirCorrectedDefaultDir());
                    directory.Name = "SourceDir";
                }
                else
                {
                    if (null != names[0] && "." != names[0])
                    {
                        if (null != names[1])
                        {
                            directory.ShortName = names[0];
                        }
                        else
                        {
                            directory.Name = names[0];
                        }
                    }

                    if (null != names[1])
                    {
                        directory.Name = names[1];
                    }
                }

                if (null != names[2])
                {
                    if (null != names[3])
                    {
                        directory.ShortSourceName = names[2];
                    }
                    else
                    {
                        directory.SourceName = names[2];
                    }
                }

                if (null != names[3])
                {
                    directory.SourceName = names[3];
                }

                this.core.IndexElement(row, directory);
            }

            // nest the directories
            foreach (var row in table.Rows)
            {
                var directory = (Wix.Directory)this.core.GetIndexedElement(row);

                if (null == row[1])
                {
                    this.core.RootElement.AddChild(directory);
                }
                else
                {
                    var parentDirectory = (Wix.Directory)this.core.GetIndexedElement("Directory", Convert.ToString(row[1]));

                    if (null == parentDirectory)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Directory_Parent", Convert.ToString(row[1]), "Directory"));
                    }
                    else if (parentDirectory == directory) // another way to specify a root directory
                    {
                        this.core.RootElement.AddChild(directory);
                    }
                    else
                    {
                        parentDirectory.AddChild(directory);
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the DrLocator table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileDrLocatorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var directorySearch = new Wix.DirectorySearch();

                directorySearch.Id = Convert.ToString(row[0]);

                if (null != row[2])
                {
                    directorySearch.Path = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    directorySearch.Depth = Convert.ToInt32(row[3]);
                }

                this.core.IndexElement(row, directorySearch);
            }
        }

        /// <summary>
        /// Decompile the DuplicateFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileDuplicateFileTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var copyFile = new Wix.CopyFile();

                copyFile.Id = Convert.ToString(row[0]);

                copyFile.FileId = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    var names = Common.GetNames(Convert.ToString(row[3]));
                    if (null != names[0] && null != names[1])
                    {
                        copyFile.DestinationShortName = names[0];
                        copyFile.DestinationName = names[1];
                    }
                    else if (null != names[0])
                    {
                        copyFile.DestinationName = names[0];
                    }
                }

                // destination directory/property is set in FinalizeDuplicateMoveFileTables

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(copyFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
                this.core.IndexElement(row, copyFile);
            }
        }

        /// <summary>
        /// Decompile the Environment table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileEnvironmentTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var environment = new Wix.Environment();

                environment.Id = Convert.ToString(row[0]);

                var done = false;
                var permanent = true;
                var name = Convert.ToString(row[1]);
                for (var i = 0; i < name.Length && !done; i++)
                {
                    switch (name[i])
                    {
                    case '=':
                        environment.Action = Wix.Environment.ActionType.set;
                        break;
                    case '+':
                        environment.Action = Wix.Environment.ActionType.create;
                        break;
                    case '-':
                        permanent = false;
                        break;
                    case '!':
                        environment.Action = Wix.Environment.ActionType.remove;
                        break;
                    case '*':
                        environment.System = Wix.YesNoType.yes;
                        break;
                    default:
                        environment.Name = name.Substring(i);
                        done = true;
                        break;
                    }
                }

                if (permanent)
                {
                    environment.Permanent = Wix.YesNoType.yes;
                }

                if (null != row[2])
                {
                    var value = Convert.ToString(row[2]);

                    if (value.StartsWith("[~]", StringComparison.Ordinal))
                    {
                        environment.Part = Wix.Environment.PartType.last;

                        if (3 < value.Length)
                        {
                            environment.Separator = value.Substring(3, 1);
                            environment.Value = value.Substring(4);
                        }
                    }
                    else if (value.EndsWith("[~]", StringComparison.Ordinal))
                    {
                        environment.Part = Wix.Environment.PartType.first;

                        if (3 < value.Length)
                        {
                            environment.Separator = value.Substring(value.Length - 4, 1);
                            environment.Value = value.Substring(0, value.Length - 4);
                        }
                    }
                    else
                    {
                        environment.Value = value;
                    }
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[3]));
                if (null != component)
                {
                    component.AddChild(environment);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[3]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the Error table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileErrorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var error = new Wix.Error();

                error.Id = Convert.ToInt32(row[0]);

                error.Content = Convert.ToString(row[1]);

                this.core.UIElement.AddChild(error);
            }
        }

        /// <summary>
        /// Decompile the EventMapping table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileEventMappingTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var subscribe = new Wix.Subscribe();

                subscribe.Event = Convert.ToString(row[2]);

                subscribe.Attribute = Convert.ToString(row[3]);

                var control = (Wix.Control)this.core.GetIndexedElement("Control", Convert.ToString(row[0]), Convert.ToString(row[1]));
                if (null != control)
                {
                    control.AddChild(subscribe);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", Convert.ToString(row[0]), "Control_", Convert.ToString(row[1]), "Control"));
                }
            }
        }

        /// <summary>
        /// Decompile the Extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileExtensionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var extension = new Wix.Extension();

                extension.Advertise = Wix.YesNoType.yes;

                extension.Id = Convert.ToString(row[0]);

                if (null != row[3])
                {
                    var mime = (Wix.MIME)this.core.GetIndexedElement("MIME", Convert.ToString(row[3]));

                    if (null != mime)
                    {
                        mime.Default = Wix.YesNoType.yes;
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "MIME_", Convert.ToString(row[3]), "MIME"));
                    }
                }

                if (null != row[2])
                {
                    var progId = (Wix.ProgId)this.core.GetIndexedElement("ProgId", Convert.ToString(row[2]));

                    if (null != progId)
                    {
                        progId.AddChild(extension);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ProgId_", Convert.ToString(row[2]), "ProgId"));
                    }
                }
                else
                {
                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));

                    if (null != component)
                    {
                        component.AddChild(extension);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                    }
                }

                this.core.IndexElement(row, extension);
            }
        }

        /// <summary>
        /// Decompile the ExternalFiles table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileExternalFilesTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var externalFile = new Wix.ExternalFile();

                externalFile.File = Convert.ToString(row[1]);

                externalFile.Source = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    var symbolPaths = (Convert.ToString(row[3])).Split(';');

                    foreach (var symbolPathString in symbolPaths)
                    {
                        var symbolPath = new Wix.SymbolPath();

                        symbolPath.Path = symbolPathString;

                        externalFile.AddChild(symbolPath);
                    }
                }

                if (null != row[4] && null != row[5])
                {
                    var ignoreOffsets = (Convert.ToString(row[4])).Split(',');
                    var ignoreLengths = (Convert.ToString(row[5])).Split(',');

                    if (ignoreOffsets.Length == ignoreLengths.Length)
                    {
                        for (var i = 0; i < ignoreOffsets.Length; i++)
                        {
                            var ignoreRange = new Wix.IgnoreRange();

                            if (ignoreOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                ignoreRange.Offset = Convert.ToInt32(ignoreOffsets[i].Substring(2), 16);
                            }
                            else
                            {
                                ignoreRange.Offset = Convert.ToInt32(ignoreOffsets[i], CultureInfo.InvariantCulture);
                            }

                            if (ignoreLengths[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                ignoreRange.Length = Convert.ToInt32(ignoreLengths[i].Substring(2), 16);
                            }
                            else
                            {
                                ignoreRange.Length = Convert.ToInt32(ignoreLengths[i], CultureInfo.InvariantCulture);
                            }

                            externalFile.AddChild(ignoreRange);
                        }
                    }
                    else
                    {
                        // TODO: warn
                    }
                }
                else if (null != row[4] || null != row[5])
                {
                    // TODO: warn about mismatch between columns
                }

                // the RetainOffsets column is handled in FinalizeFamilyFileRangesTable

                if (null != row[7])
                {
                    externalFile.Order = Convert.ToInt32(row[7]);
                }

                var family = (Wix.Family)this.core.GetIndexedElement("ImageFamilies", Convert.ToString(row[0]));
                if (null != family)
                {
                    family.AddChild(externalFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Family", Convert.ToString(row[0]), "ImageFamilies"));
                }
                this.core.IndexElement(row, externalFile);
            }
        }

        /// <summary>
        /// Decompile the Feature table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFeatureTable(Table table)
        {
            var sortedFeatures = new SortedList();

            foreach (var row in table.Rows)
            {
                var feature = new Wix.Feature();

                feature.Id = Convert.ToString(row[0]);

                if (null != row[2])
                {
                    feature.Title = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    feature.Description = Convert.ToString(row[3]);
                }

                if (null == row[4])
                {
                    feature.Display = "hidden";
                }
                else
                {
                    var display = Convert.ToInt32(row[4]);

                    if (0 == display)
                    {
                        feature.Display = "hidden";
                    }
                    else if (1 == display % 2)
                    {
                        feature.Display = "expand";
                    }
                }

                feature.Level = Convert.ToInt32(row[5]);

                if (null != row[6])
                {
                    feature.ConfigurableDirectory = Convert.ToString(row[6]);
                }

                var attributes = Convert.ToInt32(row[7]);

                if (WindowsInstallerConstants.MsidbFeatureAttributesFavorSource == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorSource) && WindowsInstallerConstants.MsidbFeatureAttributesFollowParent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFollowParent))
                {
                    // TODO: display a warning for setting favor local and follow parent together
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesFavorSource == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorSource))
                {
                    feature.InstallDefault = Wix.Feature.InstallDefaultType.source;
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesFollowParent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFollowParent))
                {
                    feature.InstallDefault = Wix.Feature.InstallDefaultType.followParent;
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise))
                {
                    feature.TypicalDefault = Wix.Feature.TypicalDefaultType.advertise;
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise) &&
                    WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise))
                {
                    this.Messaging.Write(WarningMessages.InvalidAttributeCombination(row.SourceLineNumbers, "msidbFeatureAttributesDisallowAdvertise", "msidbFeatureAttributesNoUnsupportedAdvertise", "Feature.AllowAdvertiseType", "no"));
                    feature.AllowAdvertise = Wix.Feature.AllowAdvertiseType.no;
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise))
                {
                    feature.AllowAdvertise = Wix.Feature.AllowAdvertiseType.no;
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise))
                {
                    feature.AllowAdvertise = Wix.Feature.AllowAdvertiseType.system;
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent))
                {
                    feature.Absent = Wix.Feature.AbsentType.disallow;
                }

                this.core.IndexElement(row, feature);

                // sort the features by their display column (and append the identifier to ensure unique keys)
                sortedFeatures.Add(String.Format(CultureInfo.InvariantCulture, "{0:00000}|{1}", Convert.ToInt32(row[4], CultureInfo.InvariantCulture), row[0]), row);
            }

            // nest the features
            foreach (Row row in sortedFeatures.Values)
            {
                var feature = (Wix.Feature)this.core.GetIndexedElement("Feature", Convert.ToString(row[0]));

                if (null == row[1])
                {
                    this.core.RootElement.AddChild(feature);
                }
                else
                {
                    var parentFeature = (Wix.Feature)this.core.GetIndexedElement("Feature", Convert.ToString(row[1]));

                    if (null == parentFeature)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Feature_Parent", Convert.ToString(row[1]), "Feature"));
                    }
                    else if (parentFeature == feature)
                    {
                        // TODO: display a warning about self-nesting
                    }
                    else
                    {
                        parentFeature.AddChild(feature);
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the FeatureComponents table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFeatureComponentsTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var componentRef = new Wix.ComponentRef();

                componentRef.Id = Convert.ToString(row[1]);

                var parentFeature = (Wix.Feature)this.core.GetIndexedElement("Feature", Convert.ToString(row[0]));
                if (null != parentFeature)
                {
                    parentFeature.AddChild(componentRef);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Feature_", Convert.ToString(row[0]), "Feature"));
                }
                this.core.IndexElement(row, componentRef);
            }
        }

        /// <summary>
        /// Decompile the File table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileTable(Table table)
        {
            foreach (FileRow fileRow in table.Rows)
            {
                var file = new Wix.File();

                file.Id = fileRow.File;

                var names = Common.GetNames(fileRow.FileName);
                if (null != names[0] && null != names[1])
                {
                    file.ShortName = names[0];
                    file.Name = names[1];
                }
                else if (null != names[0])
                {
                    file.Name = names[0];
                }

                if (null != fileRow.Version && 0 < fileRow.Version.Length)
                {
                    if (!Char.IsDigit(fileRow.Version[0]))
                    {
                        file.CompanionFile = fileRow.Version;
                    }
                }

                if (WindowsInstallerConstants.MsidbFileAttributesReadOnly == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesReadOnly))
                {
                    file.ReadOnly = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbFileAttributesHidden == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesHidden))
                {
                    file.Hidden = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbFileAttributesSystem == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesSystem))
                {
                    file.System = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbFileAttributesVital != (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesVital))
                {
                    file.Vital = Wix.YesNoType.no;
                }

                if (WindowsInstallerConstants.MsidbFileAttributesChecksum == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesChecksum))
                {
                    file.Checksum = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbFileAttributesNoncompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) &&
                    WindowsInstallerConstants.MsidbFileAttributesCompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed))
                {
                    // TODO: error
                }
                else if (WindowsInstallerConstants.MsidbFileAttributesNoncompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed))
                {
                    file.Compressed = Wix.YesNoDefaultType.no;
                }
                else if (WindowsInstallerConstants.MsidbFileAttributesCompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed))
                {
                    file.Compressed = Wix.YesNoDefaultType.yes;
                }

                this.core.IndexElement(fileRow, file);
            }
        }

        /// <summary>
        /// Decompile the FileSFPCatalog table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileSFPCatalogTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var sfpFile = new Wix.SFPFile();

                sfpFile.Id = Convert.ToString(row[0]);

                var sfpCatalog = (Wix.SFPCatalog)this.core.GetIndexedElement("SFPCatalog", Convert.ToString(row[1]));
                if (null != sfpCatalog)
                {
                    sfpCatalog.AddChild(sfpFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "SFPCatalog_", Convert.ToString(row[1]), "SFPCatalog"));
                }
            }
        }

        /// <summary>
        /// Decompile the Font table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFontTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var file = (Wix.File)this.core.GetIndexedElement("File", Convert.ToString(row[0]));

                if (null != file)
                {
                    if (null != row[1])
                    {
                        file.FontTitle = Convert.ToString(row[1]);
                    }
                    else
                    {
                        file.TrueType = Wix.YesNoType.yes;
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", Convert.ToString(row[0]), "File"));
                }
            }
        }

        /// <summary>
        /// Decompile the Icon table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIconTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var icon = new Wix.Icon();

                icon.Id = Convert.ToString(row[0]);

                icon.SourceFile = Convert.ToString(row[1]);

                this.core.RootElement.AddChild(icon);
            }
        }

        /// <summary>
        /// Decompile the ImageFamilies table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileImageFamiliesTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var family = new Wix.Family();

                family.Name = Convert.ToString(row[0]);

                if (null != row[1])
                {
                    family.MediaSrcProp = Convert.ToString(row[1]);
                }

                if (null != row[2])
                {
                    family.DiskId = Convert.ToString(Convert.ToInt32(row[2]));
                }

                if (null != row[3])
                {
                    family.SequenceStart = Convert.ToInt32(row[3]);
                }

                if (null != row[4])
                {
                    family.DiskPrompt = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    family.VolumeLabel = Convert.ToString(row[5]);
                }

                this.core.RootElement.AddChild(family);
                this.core.IndexElement(row, family);
            }
        }

        /// <summary>
        /// Decompile the IniFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIniFileTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var iniFile = new Wix.IniFile();

                iniFile.Id = Convert.ToString(row[0]);

                var names = Common.GetNames(Convert.ToString(row[1]));

                if (null != names[0])
                {
                    if (null == names[1])
                    {
                        iniFile.Name = names[0];
                    }
                    else
                    {
                        iniFile.ShortName = names[0];
                    }
                }

                if (null != names[1])
                {
                    iniFile.Name = names[1];
                }

                if (null != row[2])
                {
                    iniFile.Directory = Convert.ToString(row[2]);
                }

                iniFile.Section = Convert.ToString(row[3]);

                iniFile.Key = Convert.ToString(row[4]);

                iniFile.Value = Convert.ToString(row[5]);

                switch (Convert.ToInt32(row[6]))
                {
                case WindowsInstallerConstants.MsidbIniFileActionAddLine:
                    iniFile.Action = Wix.IniFile.ActionType.addLine;
                    break;
                case WindowsInstallerConstants.MsidbIniFileActionCreateLine:
                    iniFile.Action = Wix.IniFile.ActionType.createLine;
                    break;
                case WindowsInstallerConstants.MsidbIniFileActionAddTag:
                    iniFile.Action = Wix.IniFile.ActionType.addTag;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                    break;
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[7]));
                if (null != component)
                {
                    component.AddChild(iniFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[7]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IniLocator table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIniLocatorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var iniFileSearch = new Wix.IniFileSearch();

                iniFileSearch.Id = Convert.ToString(row[0]);

                var names = Common.GetNames(Convert.ToString(row[1]));
                if (null != names[0] && null != names[1])
                {
                    iniFileSearch.ShortName = names[0];
                    iniFileSearch.Name = names[1];
                }
                else if (null != names[0])
                {
                    iniFileSearch.Name = names[0];
                }

                iniFileSearch.Section = Convert.ToString(row[2]);

                iniFileSearch.Key = Convert.ToString(row[3]);

                if (null != row[4])
                {
                    var field = Convert.ToInt32(row[4]);

                    if (0 != field)
                    {
                        iniFileSearch.Field = field;
                    }
                }

                if (null != row[5])
                {
                    switch (Convert.ToInt32(row[5]))
                    {
                    case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                        iniFileSearch.Type = Wix.IniFileSearch.TypeType.directory;
                        break;
                    case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                        // this is the default value
                        break;
                    case WindowsInstallerConstants.MsidbLocatorTypeRawValue:
                        iniFileSearch.Type = Wix.IniFileSearch.TypeType.raw;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[5].Column.Name, row[5]));
                        break;
                    }
                }

                this.core.IndexElement(row, iniFileSearch);
            }
        }

        /// <summary>
        /// Decompile the IsolatedComponent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIsolatedComponentTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var isolateComponent = new Wix.IsolateComponent();

                isolateComponent.Shared = Convert.ToString(row[0]);

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(isolateComponent);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the LaunchCondition table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileLaunchConditionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if (Common.DowngradePreventedCondition == Convert.ToString(row[0]) || Common.UpgradePreventedCondition == Convert.ToString(row[0]))
                {
                    continue; // MajorUpgrade rows processed in FinalizeUpgradeTable
                }

                var condition = new Wix.Condition();

                condition.Content = Convert.ToString(row[0]);

                condition.Message = Convert.ToString(row[1]);

                this.core.RootElement.AddChild(condition);
            }
        }

        /// <summary>
        /// Decompile the ListBox table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileListBoxTable(Table table)
        {
            Wix.ListBox listBox = null;
            var listBoxRows = new SortedList();

            // sort the list boxes by their property and order
            foreach (var row in table.Rows)
            {
                listBoxRows.Add(String.Concat("{0}|{1:0000000000}", row[0], row[1]), row);
            }

            foreach (Row row in listBoxRows.Values)
            {
                if (null == listBox || Convert.ToString(row[0]) != listBox.Property)
                {
                    listBox = new Wix.ListBox();

                    listBox.Property = Convert.ToString(row[0]);

                    this.core.UIElement.AddChild(listBox);
                }

                var listItem = new Wix.ListItem();

                listItem.Value = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    listItem.Text = Convert.ToString(row[3]);
                }

                listBox.AddChild(listItem);
            }
        }

        /// <summary>
        /// Decompile the ListView table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileListViewTable(Table table)
        {
            Wix.ListView listView = null;
            var listViewRows = new SortedList();

            // sort the list views by their property and order
            foreach (var row in table.Rows)
            {
                listViewRows.Add(String.Concat("{0}|{1:0000000000}", row[0], row[1]), row);
            }

            foreach (Row row in listViewRows.Values)
            {
                if (null == listView || Convert.ToString(row[0]) != listView.Property)
                {
                    listView = new Wix.ListView();

                    listView.Property = Convert.ToString(row[0]);

                    this.core.UIElement.AddChild(listView);
                }

                var listItem = new Wix.ListItem();

                listItem.Value = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    listItem.Text = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    listItem.Icon = Convert.ToString(row[4]);
                }

                listView.AddChild(listItem);
            }
        }

        /// <summary>
        /// Decompile the LockPermissions table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileLockPermissionsTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var permission = new Wix.Permission();
                string[] specialPermissions;

                switch (Convert.ToString(row[1]))
                {
                case "CreateFolder":
                    specialPermissions = Common.FolderPermissions;
                    break;
                case "File":
                    specialPermissions = Common.FilePermissions;
                    break;
                case "Registry":
                    specialPermissions = Common.RegistryPermissions;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, row.Table.Name, row.Fields[1].Column.Name, row[1]));
                    return;
                }

                var permissionBits = Convert.ToInt32(row[4]);
                for (var i = 0; i < 32; i++)
                {
                    if (0 != ((permissionBits >> i) & 1))
                    {
                        string name = null;

                        if (specialPermissions.Length > i)
                        {
                            name = specialPermissions[i];
                        }
                        else if (16 > i && specialPermissions.Length <= i)
                        {
                            name = "SpecificRightsAll";
                        }
                        else if (28 > i && Common.StandardPermissions.Length > (i - 16))
                        {
                            name = Common.StandardPermissions[i - 16];
                        }
                        else if (0 <= (i - 28) && Common.GenericPermissions.Length > (i - 28))
                        {
                            name = Common.GenericPermissions[i - 28];
                        }

                        if (null == name)
                        {
                            this.Messaging.Write(WarningMessages.UnknownPermission(row.SourceLineNumbers, row.Table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), i));
                        }
                        else
                        {
                            switch (name)
                            {
                            case "Append":
                                permission.Append = Wix.YesNoType.yes;
                                break;
                            case "ChangePermission":
                                permission.ChangePermission = Wix.YesNoType.yes;
                                break;
                            case "CreateChild":
                                permission.CreateChild = Wix.YesNoType.yes;
                                break;
                            case "CreateFile":
                                permission.CreateFile = Wix.YesNoType.yes;
                                break;
                            case "CreateLink":
                                permission.CreateLink = Wix.YesNoType.yes;
                                break;
                            case "CreateSubkeys":
                                permission.CreateSubkeys = Wix.YesNoType.yes;
                                break;
                            case "Delete":
                                permission.Delete = Wix.YesNoType.yes;
                                break;
                            case "DeleteChild":
                                permission.DeleteChild = Wix.YesNoType.yes;
                                break;
                            case "EnumerateSubkeys":
                                permission.EnumerateSubkeys = Wix.YesNoType.yes;
                                break;
                            case "Execute":
                                permission.Execute = Wix.YesNoType.yes;
                                break;
                            case "FileAllRights":
                                permission.FileAllRights = Wix.YesNoType.yes;
                                break;
                            case "GenericAll":
                                permission.GenericAll = Wix.YesNoType.yes;
                                break;
                            case "GenericExecute":
                                permission.GenericExecute = Wix.YesNoType.yes;
                                break;
                            case "GenericRead":
                                permission.GenericRead = Wix.YesNoType.yes;
                                break;
                            case "GenericWrite":
                                permission.GenericWrite = Wix.YesNoType.yes;
                                break;
                            case "Notify":
                                permission.Notify = Wix.YesNoType.yes;
                                break;
                            case "Read":
                                permission.Read = Wix.YesNoType.yes;
                                break;
                            case "ReadAttributes":
                                permission.ReadAttributes = Wix.YesNoType.yes;
                                break;
                            case "ReadExtendedAttributes":
                                permission.ReadExtendedAttributes = Wix.YesNoType.yes;
                                break;
                            case "ReadPermission":
                                permission.ReadPermission = Wix.YesNoType.yes;
                                break;
                            case "SpecificRightsAll":
                                permission.SpecificRightsAll = Wix.YesNoType.yes;
                                break;
                            case "Synchronize":
                                permission.Synchronize = Wix.YesNoType.yes;
                                break;
                            case "TakeOwnership":
                                permission.TakeOwnership = Wix.YesNoType.yes;
                                break;
                            case "Traverse":
                                permission.Traverse = Wix.YesNoType.yes;
                                break;
                            case "Write":
                                permission.Write = Wix.YesNoType.yes;
                                break;
                            case "WriteAttributes":
                                permission.WriteAttributes = Wix.YesNoType.yes;
                                break;
                            case "WriteExtendedAttributes":
                                permission.WriteExtendedAttributes = Wix.YesNoType.yes;
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown permission attribute '{name}'.");
                            }
                        }
                    }
                }

                if (null != row[2])
                {
                    permission.Domain = Convert.ToString(row[2]);
                }

                permission.User = Convert.ToString(row[3]);

                this.core.IndexElement(row, permission);
            }
        }

        /// <summary>
        /// Decompile the Media table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMediaTable(Table table)
        {
            foreach (MediaRow mediaRow in table.Rows)
            {
                var media = new Wix.Media();

                media.Id = Convert.ToString(mediaRow.DiskId);

                if (null != mediaRow.DiskPrompt)
                {
                    media.DiskPrompt = mediaRow.DiskPrompt;
                }

                if (null != mediaRow.Cabinet)
                {
                    var cabinet = mediaRow.Cabinet;

                    if (cabinet.StartsWith("#", StringComparison.Ordinal))
                    {
                        media.EmbedCab = Wix.YesNoType.yes;
                        cabinet = cabinet.Substring(1);
                    }

                    media.Cabinet = cabinet;
                }

                if (null != mediaRow.VolumeLabel)
                {
                    media.VolumeLabel = mediaRow.VolumeLabel;
                }

                this.core.RootElement.AddChild(media);
                this.core.IndexElement(mediaRow, media);
            }
        }

        /// <summary>
        /// Decompile the MIME table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMIMETable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var mime = new Wix.MIME();

                mime.ContentType = Convert.ToString(row[0]);

                if (null != row[2])
                {
                    mime.Class = Convert.ToString(row[2]);
                }

                this.core.IndexElement(row, mime);
            }
        }

        /// <summary>
        /// Decompile the ModuleConfiguration table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleConfigurationTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var configuration = new Wix.Configuration();

                configuration.Name = Convert.ToString(row[0]);

                switch (Convert.ToInt32(row[1]))
                {
                case 0:
                    configuration.Format = Wix.Configuration.FormatType.Text;
                    break;
                case 1:
                    configuration.Format = Wix.Configuration.FormatType.Key;
                    break;
                case 2:
                    configuration.Format = Wix.Configuration.FormatType.Integer;
                    break;
                case 3:
                    configuration.Format = Wix.Configuration.FormatType.Bitfield;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                if (null != row[2])
                {
                    configuration.Type = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    configuration.ContextData = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    configuration.DefaultValue = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    var attributes = Convert.ToInt32(row[5]);

                    if (WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan == (attributes & WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan))
                    {
                        configuration.KeyNoOrphan = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable == (attributes & WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable))
                    {
                        configuration.NonNullable = Wix.YesNoType.yes;
                    }

                    if (3 < attributes)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[5].Column.Name, row[5]));
                    }
                }

                if (null != row[6])
                {
                    configuration.DisplayName = Convert.ToString(row[6]);
                }

                if (null != row[7])
                {
                    configuration.Description = Convert.ToString(row[7]);
                }

                if (null != row[8])
                {
                    configuration.HelpLocation = Convert.ToString(row[8]);
                }

                if (null != row[9])
                {
                    configuration.HelpKeyword = Convert.ToString(row[9]);
                }

                this.core.RootElement.AddChild(configuration);
            }
        }

        /// <summary>
        /// Decompile the ModuleDependency table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleDependencyTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var dependency = new Wix.Dependency();

                dependency.RequiredId = Convert.ToString(row[2]);

                dependency.RequiredLanguage = Convert.ToInt32(row[3], CultureInfo.InvariantCulture);

                if (null != row[4])
                {
                    dependency.RequiredVersion = Convert.ToString(row[4]);
                }

                this.core.RootElement.AddChild(dependency);
            }
        }

        /// <summary>
        /// Decompile the ModuleExclusion table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleExclusionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var exclusion = new Wix.Exclusion();

                exclusion.ExcludedId = Convert.ToString(row[2]);

                var excludedLanguage = Convert.ToInt32(Convert.ToString(row[3]), CultureInfo.InvariantCulture);
                if (0 < excludedLanguage)
                {
                    exclusion.ExcludeLanguage = excludedLanguage;
                }
                else if (0 > excludedLanguage)
                {
                    exclusion.ExcludeExceptLanguage = -excludedLanguage;
                }

                if (null != row[4])
                {
                    exclusion.ExcludedMinVersion = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    exclusion.ExcludedMinVersion = Convert.ToString(row[5]);
                }

                this.core.RootElement.AddChild(exclusion);
            }
        }

        /// <summary>
        /// Decompile the ModuleIgnoreTable table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleIgnoreTableTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var tableName = Convert.ToString(row[0]);

                // the linker automatically adds a ModuleIgnoreTable row for some tables
                if ("ModuleConfiguration" != tableName && "ModuleSubstitution" != tableName)
                {
                    var ignoreTable = new Wix.IgnoreTable();

                    ignoreTable.Id = tableName;

                    this.core.RootElement.AddChild(ignoreTable);
                }
            }
        }

        /// <summary>
        /// Decompile the ModuleSignature table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleSignatureTable(Table table)
        {
            if (1 == table.Rows.Count)
            {
                var row = table.Rows[0];

                var module = (Wix.Module)this.core.RootElement;

                module.Id = Convert.ToString(row[0]);

                // support Language columns that are treated as integers as well as strings (the WiX default, to support localizability)
                module.Language = Convert.ToString(row[1], CultureInfo.InvariantCulture);

                module.Version = Convert.ToString(row[2]);
            }
            else
            {
                // TODO: warn
            }
        }

        /// <summary>
        /// Decompile the ModuleSubstitution table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileModuleSubstitutionTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var substitution = new Wix.Substitution();

                substitution.Table = Convert.ToString(row[0]);

                substitution.Row = Convert.ToString(row[1]);

                substitution.Column = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    substitution.Value = Convert.ToString(row[3]);
                }

                this.core.RootElement.AddChild(substitution);
            }
        }

        /// <summary>
        /// Decompile the MoveFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMoveFileTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var copyFile = new Wix.CopyFile();

                copyFile.Id = Convert.ToString(row[0]);

                if (null != row[2])
                {
                    copyFile.SourceName = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    var names = Common.GetNames(Convert.ToString(row[3]));
                    if (null != names[0] && null != names[1])
                    {
                        copyFile.DestinationShortName = names[0];
                        copyFile.DestinationName = names[1];
                    }
                    else if (null != names[0])
                    {
                        copyFile.DestinationName = names[0];
                    }
                }

                // source/destination directory/property is set in FinalizeDuplicateMoveFileTables

                switch (Convert.ToInt32(row[6]))
                {
                case 0:
                    break;
                case WindowsInstallerConstants.MsidbMoveFileOptionsMove:
                    copyFile.Delete = Wix.YesNoType.yes;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                    break;
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(copyFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
                this.core.IndexElement(row, copyFile);
            }
        }

        /// <summary>
        /// Decompile the MsiDigitalCertificate table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiDigitalCertificateTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var digitalCertificate = new Wix.DigitalCertificate();

                digitalCertificate.Id = Convert.ToString(row[0]);

                digitalCertificate.SourceFile = Convert.ToString(row[1]);

                this.core.IndexElement(row, digitalCertificate);
            }
        }

        /// <summary>
        /// Decompile the MsiDigitalSignature table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiDigitalSignatureTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var digitalSignature = new Wix.DigitalSignature();

                if (null != row[3])
                {
                    digitalSignature.SourceFile = Convert.ToString(row[3]);
                }

                var digitalCertificate = (Wix.DigitalCertificate)this.core.GetIndexedElement("MsiDigitalCertificate", Convert.ToString(row[2]));
                if (null != digitalCertificate)
                {
                    digitalSignature.AddChild(digitalCertificate);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "DigitalCertificate_", Convert.ToString(row[2]), "MsiDigitalCertificate"));
                }

                var parentElement = (Wix.IParentElement)this.core.GetIndexedElement(Convert.ToString(row[0]), Convert.ToString(row[1]));
                if (null != parentElement)
                {
                    parentElement.AddChild(digitalSignature);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "SignObject", Convert.ToString(row[1]), Convert.ToString(row[0])));
                }
            }
        }

        /// <summary>
        /// Decompile the MsiEmbeddedChainer table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiEmbeddedChainerTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var embeddedChainer = new Wix.EmbeddedChainer();

                embeddedChainer.Id = Convert.ToString(row[0]);

                embeddedChainer.Content = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    embeddedChainer.CommandLine = Convert.ToString(row[2]);
                }

                switch (Convert.ToInt32(row[4]))
                {
                case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeBinaryData:
                    embeddedChainer.BinarySource = Convert.ToString(row[3]);
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeSourceFile:
                    embeddedChainer.FileSource = Convert.ToString(row[3]);
                    break;
                case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeProperty:
                    embeddedChainer.PropertySource = Convert.ToString(row[3]);
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                    break;
                }

                this.core.RootElement.AddChild(embeddedChainer);
            }
        }

        /// <summary>
        /// Decompile the MsiEmbeddedUI table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiEmbeddedUITable(Table table)
        {
            var embeddedUI = new Wix.EmbeddedUI();
            var foundEmbeddedUI = false;
            var foundEmbeddedResources = false;

            foreach (var row in table.Rows)
            {
                var attributes = Convert.ToInt32(row[2]);

                if (WindowsInstallerConstants.MsidbEmbeddedUI == (attributes & WindowsInstallerConstants.MsidbEmbeddedUI))
                {
                    if (foundEmbeddedUI)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                    }
                    else
                    {
                        embeddedUI.Id = Convert.ToString(row[0]);
                        embeddedUI.Name = Convert.ToString(row[1]);

                        var messageFilter = Convert.ToInt32(row[3]);
                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_FATALEXIT))
                        {
                            embeddedUI.IgnoreFatalExit = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ERROR))
                        {
                            embeddedUI.IgnoreError = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_WARNING))
                        {
                            embeddedUI.IgnoreWarning = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_USER))
                        {
                            embeddedUI.IgnoreUser = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INFO))
                        {
                            embeddedUI.IgnoreInfo = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_FILESINUSE))
                        {
                            embeddedUI.IgnoreFilesInUse = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_RESOLVESOURCE))
                        {
                            embeddedUI.IgnoreResolveSource = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_OUTOFDISKSPACE))
                        {
                            embeddedUI.IgnoreOutOfDiskSpace = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ACTIONSTART))
                        {
                            embeddedUI.IgnoreActionStart = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ACTIONDATA))
                        {
                            embeddedUI.IgnoreActionData = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_PROGRESS))
                        {
                            embeddedUI.IgnoreProgress = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_COMMONDATA))
                        {
                            embeddedUI.IgnoreCommonData = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INITIALIZE))
                        {
                            embeddedUI.IgnoreInitialize = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_TERMINATE))
                        {
                            embeddedUI.IgnoreTerminate = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_SHOWDIALOG))
                        {
                            embeddedUI.IgnoreShowDialog = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_RMFILESINUSE))
                        {
                            embeddedUI.IgnoreRMFilesInUse = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INSTALLSTART))
                        {
                            embeddedUI.IgnoreInstallStart = Wix.YesNoType.yes;
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INSTALLEND))
                        {
                            embeddedUI.IgnoreInstallEnd = Wix.YesNoType.yes;
                        }

                        if (WindowsInstallerConstants.MsidbEmbeddedHandlesBasic == (attributes & WindowsInstallerConstants.MsidbEmbeddedHandlesBasic))
                        {
                            embeddedUI.SupportBasicUI = Wix.YesNoType.yes;
                        }

                        embeddedUI.SourceFile = Convert.ToString(row[4]);

                        this.core.UIElement.AddChild(embeddedUI);
                        foundEmbeddedUI = true;
                    }
                }
                else
                {
                    var embeddedResource = new Wix.EmbeddedUIResource();

                    embeddedResource.Id = Convert.ToString(row[0]);
                    embeddedResource.Name = Convert.ToString(row[1]);
                    embeddedResource.SourceFile = Convert.ToString(row[4]);

                    embeddedUI.AddChild(embeddedResource);
                    foundEmbeddedResources = true;
                }
            }

            if (!foundEmbeddedUI && foundEmbeddedResources)
            {
                // TODO: warn
            }
        }

        /// <summary>
        /// Decompile the MsiLockPermissionsEx table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiLockPermissionsExTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var permissionEx = new Wix.PermissionEx();
                permissionEx.Id = Convert.ToString(row[0]);
                permissionEx.Sddl = Convert.ToString(row[3]);

                if (null != row[4])
                {
                    var condition = new Wix.Condition();
                    condition.Content = Convert.ToString(row[4]);
                    permissionEx.AddChild(condition);
                }

                switch (Convert.ToString(row[2]))
                {
                case "CreateFolder":
                case "File":
                case "Registry":
                case "ServiceInstall":
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, row.Table.Name, row.Fields[1].Column.Name, row[1]));
                    return;
                }

                this.core.IndexElement(row, permissionEx);
            }
        }

        /// <summary>
        /// Decompile the MsiPackageCertificate table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiPackageCertificateTable(Table table)
        {
            if (0 < table.Rows.Count)
            {
                var packageCertificates = new Wix.PackageCertificates();
                this.core.RootElement.AddChild(packageCertificates);
                this.AddCertificates(table, packageCertificates);
            }
        }

        /// <summary>
        /// Decompile the MsiPatchCertificate table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiPatchCertificateTable(Table table)
        {
            if (0 < table.Rows.Count)
            {
                var patchCertificates = new Wix.PatchCertificates();
                this.core.RootElement.AddChild(patchCertificates);
                this.AddCertificates(table, patchCertificates);
            }
        }

        /// <summary>
        /// Insert DigitalCertificate records associated with passed msiPackageCertificate or msiPatchCertificate table.
        /// </summary>
        /// <param name="table">The table being decompiled.</param>
        /// <param name="parent">DigitalCertificate parent</param>
        private void AddCertificates(Table table, Wix.IParentElement parent)
        {
            foreach (var row in table.Rows)
            {
                var digitalCertificate = (Wix.DigitalCertificate)this.core.GetIndexedElement("MsiDigitalCertificate", Convert.ToString(row[1]));

                if (null != digitalCertificate)
                {
                    parent.AddChild(digitalCertificate);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "DigitalCertificate_", Convert.ToString(row[1]), "MsiDigitalCertificate"));
                }
            }
        }

        /// <summary>
        /// Decompile the MsiShortcutProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiShortcutPropertyTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var property = new Wix.ShortcutProperty();
                property.Id = Convert.ToString(row[0]);
                property.Key = Convert.ToString(row[2]);
                property.Value = Convert.ToString(row[3]);

                var shortcut = (Wix.Shortcut)this.core.GetIndexedElement("Shortcut", Convert.ToString(row[1]));
                if (null != shortcut)
                {
                    shortcut.AddChild(property);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Shortcut_", Convert.ToString(row[1]), "Shortcut"));
                }
            }
        }

        /// <summary>
        /// Decompile the ODBCAttribute table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileODBCAttributeTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var property = new Wix.Property();

                property.Id = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    property.Value = Convert.ToString(row[2]);
                }

                var odbcDriver = (Wix.ODBCDriver)this.core.GetIndexedElement("ODBCDriver", Convert.ToString(row[0]));
                if (null != odbcDriver)
                {
                    odbcDriver.AddChild(property);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Driver_", Convert.ToString(row[0]), "ODBCDriver"));
                }
            }
        }

        /// <summary>
        /// Decompile the ODBCDataSource table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileODBCDataSourceTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var odbcDataSource = new Wix.ODBCDataSource();

                odbcDataSource.Id = Convert.ToString(row[0]);

                odbcDataSource.Name = Convert.ToString(row[2]);

                odbcDataSource.DriverName = Convert.ToString(row[3]);

                switch (Convert.ToInt32(row[4]))
                {
                case WindowsInstallerConstants.MsidbODBCDataSourceRegistrationPerMachine:
                    odbcDataSource.Registration = Wix.ODBCDataSource.RegistrationType.machine;
                    break;
                case WindowsInstallerConstants.MsidbODBCDataSourceRegistrationPerUser:
                    odbcDataSource.Registration = Wix.ODBCDataSource.RegistrationType.user;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                    break;
                }

                this.core.IndexElement(row, odbcDataSource);
            }
        }

        /// <summary>
        /// Decompile the ODBCDriver table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileODBCDriverTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var odbcDriver = new Wix.ODBCDriver();

                odbcDriver.Id = Convert.ToString(row[0]);

                odbcDriver.Name = Convert.ToString(row[2]);

                odbcDriver.File = Convert.ToString(row[3]);

                if (null != row[4])
                {
                    odbcDriver.SetupFile = Convert.ToString(row[4]);
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(odbcDriver);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
                this.core.IndexElement(row, odbcDriver);
            }
        }

        /// <summary>
        /// Decompile the ODBCSourceAttribute table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileODBCSourceAttributeTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var property = new Wix.Property();

                property.Id = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    property.Value = Convert.ToString(row[2]);
                }

                var odbcDataSource = (Wix.ODBCDataSource)this.core.GetIndexedElement("ODBCDataSource", Convert.ToString(row[0]));
                if (null != odbcDataSource)
                {
                    odbcDataSource.AddChild(property);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "DataSource_", Convert.ToString(row[0]), "ODBCDataSource"));
                }
            }
        }

        /// <summary>
        /// Decompile the ODBCTranslator table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileODBCTranslatorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var odbcTranslator = new Wix.ODBCTranslator();

                odbcTranslator.Id = Convert.ToString(row[0]);

                odbcTranslator.Name = Convert.ToString(row[2]);

                odbcTranslator.File = Convert.ToString(row[3]);

                if (null != row[4])
                {
                    odbcTranslator.SetupFile = Convert.ToString(row[4]);
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(odbcTranslator);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the PatchMetadata table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePatchMetadataTable(Table table)
        {
            if (0 < table.Rows.Count)
            {
                var patchMetadata = new Wix.PatchMetadata();

                foreach (var row in table.Rows)
                {
                    var value = Convert.ToString(row[2]);

                    switch (Convert.ToString(row[1]))
                    {
                    case "AllowRemoval":
                        if ("1" == value)
                        {
                            patchMetadata.AllowRemoval = Wix.YesNoType.yes;
                        }
                        break;
                    case "Classification":
                        if (null != value)
                        {
                            patchMetadata.Classification = value;
                        }
                        break;
                    case "CreationTimeUTC":
                        if (null != value)
                        {
                            patchMetadata.CreationTimeUTC = value;
                        }
                        break;
                    case "Description":
                        if (null != value)
                        {
                            patchMetadata.Description = value;
                        }
                        break;
                    case "DisplayName":
                        if (null != value)
                        {
                            patchMetadata.DisplayName = value;
                        }
                        break;
                    case "ManufacturerName":
                        if (null != value)
                        {
                            patchMetadata.ManufacturerName = value;
                        }
                        break;
                    case "MinorUpdateTargetRTM":
                        if (null != value)
                        {
                            patchMetadata.MinorUpdateTargetRTM = value;
                        }
                        break;
                    case "MoreInfoURL":
                        if (null != value)
                        {
                            patchMetadata.MoreInfoURL = value;
                        }
                        break;
                    case "OptimizeCA":
                        var optimizeCustomActions = new Wix.OptimizeCustomActions();
                        var optimizeCA = Int32.Parse(value, CultureInfo.InvariantCulture);
                        if (0 != (Convert.ToInt32(OptimizeCA.SkipAssignment) & optimizeCA))
                        {
                            optimizeCustomActions.SkipAssignment = Wix.YesNoType.yes;
                        }

                        if (0 != (Convert.ToInt32(OptimizeCA.SkipImmediate) & optimizeCA))
                        {
                            optimizeCustomActions.SkipImmediate = Wix.YesNoType.yes;
                        }

                        if (0 != (Convert.ToInt32(OptimizeCA.SkipDeferred) & optimizeCA))
                        {
                            optimizeCustomActions.SkipDeferred = Wix.YesNoType.yes;
                        }

                        patchMetadata.AddChild(optimizeCustomActions);
                        break;
                    case "OptimizedInstallMode":
                        if ("1" == value)
                        {
                            patchMetadata.OptimizedInstallMode = Wix.YesNoType.yes;
                        }
                        break;
                    case "TargetProductName":
                        if (null != value)
                        {
                            patchMetadata.TargetProductName = value;
                        }
                        break;
                    default:
                        var customProperty = new Wix.CustomProperty();

                        if (null != row[0])
                        {
                            customProperty.Company = Convert.ToString(row[0]);
                        }

                        customProperty.Property = Convert.ToString(row[1]);

                        if (null != row[2])
                        {
                            customProperty.Value = Convert.ToString(row[2]);
                        }

                        patchMetadata.AddChild(customProperty);
                        break;
                    }
                }

                this.core.RootElement.AddChild(patchMetadata);
            }
        }

        /// <summary>
        /// Decompile the PatchSequence table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePatchSequenceTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var patchSequence = new Wix.PatchSequence();

                patchSequence.PatchFamily = Convert.ToString(row[0]);

                if (null != row[1])
                {
                    try
                    {
                        var guid = new Guid(Convert.ToString(row[1]));

                        patchSequence.ProductCode = Convert.ToString(row[1]);
                    }
                    catch // non-guid value
                    {
                        patchSequence.TargetImage = Convert.ToString(row[1]);
                    }
                }

                if (null != row[2])
                {
                    patchSequence.Sequence = Convert.ToString(row[2]);
                }

                if (null != row[3] && 0x1 == Convert.ToInt32(row[3]))
                {
                    patchSequence.Supersede = Wix.YesNoType.yes;
                }

                this.core.RootElement.AddChild(patchSequence);
            }
        }

        /// <summary>
        /// Decompile the ProgId table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileProgIdTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var progId = new Wix.ProgId();

                progId.Advertise = Wix.YesNoType.yes;

                progId.Id = Convert.ToString(row[0]);

                if (null != row[3])
                {
                    progId.Description = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    progId.Icon = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    progId.IconIndex = Convert.ToInt32(row[5]);
                }

                this.core.IndexElement(row, progId);
            }

            // nest the ProgIds
            foreach (var row in table.Rows)
            {
                var progId = (Wix.ProgId)this.core.GetIndexedElement(row);

                if (null != row[1])
                {
                    var parentProgId = (Wix.ProgId)this.core.GetIndexedElement("ProgId", Convert.ToString(row[1]));

                    if (null != parentProgId)
                    {
                        parentProgId.AddChild(progId);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ProgId_Parent", Convert.ToString(row[1]), "ProgId"));
                    }
                }
                else if (null != row[2])
                {
                    // nesting is handled in FinalizeProgIdTable
                }
                else
                {
                    // TODO: warn for orphaned ProgId
                }
            }
        }

        /// <summary>
        /// Decompile the Properties table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePropertiesTable(Table table)
        {
            var patchCreation = (Wix.PatchCreation)this.core.RootElement;

            foreach (var row in table.Rows)
            {
                var name = Convert.ToString(row[0]);
                var value = Convert.ToString(row[1]);

                switch (name)
                {
                case "AllowProductCodeMismatches":
                    if ("1" == value)
                    {
                        patchCreation.AllowProductCodeMismatches = Wix.YesNoType.yes;
                    }
                    break;
                case "AllowProductVersionMajorMismatches":
                    if ("1" == value)
                    {
                        patchCreation.AllowMajorVersionMismatches = Wix.YesNoType.yes;
                    }
                    break;
                case "ApiPatchingSymbolFlags":
                    if (null != value)
                    {
                        try
                        {
                            // remove the leading "0x" if its present
                            if (value.StartsWith("0x", StringComparison.Ordinal))
                            {
                                value = value.Substring(2);
                            }

                            patchCreation.SymbolFlags = Convert.ToInt32(value, 16);
                        }
                        catch
                        {
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        }
                    }
                    break;
                case "DontRemoveTempFolderWhenFinished":
                    if ("1" == value)
                    {
                        patchCreation.CleanWorkingFolder = Wix.YesNoType.no;
                    }
                    break;
                case "IncludeWholeFilesOnly":
                    if ("1" == value)
                    {
                        patchCreation.WholeFilesOnly = Wix.YesNoType.yes;
                    }
                    break;
                case "ListOfPatchGUIDsToReplace":
                    if (null != value)
                    {
                        var guidRegex = new Regex(@"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}");
                        var guidMatches = guidRegex.Matches(value);

                        foreach (Match guidMatch in guidMatches)
                        {
                            var replacePatch = new Wix.ReplacePatch();

                            replacePatch.Id = guidMatch.Value;

                            this.core.RootElement.AddChild(replacePatch);
                        }
                    }
                    break;
                case "ListOfTargetProductCodes":
                    if (null != value)
                    {
                        var targetProductCodes = value.Split(';');

                        foreach (var targetProductCodeString in targetProductCodes)
                        {
                            var targetProductCode = new Wix.TargetProductCode();

                            targetProductCode.Id = targetProductCodeString;

                            this.core.RootElement.AddChild(targetProductCode);
                        }
                    }
                    break;
                case "PatchGUID":
                    patchCreation.Id = value;
                    break;
                case "PatchSourceList":
                    patchCreation.SourceList = value;
                    break;
                case "PatchOutputPath":
                    patchCreation.OutputPath = value;
                    break;
                default:
                    var patchProperty = new Wix.PatchProperty();

                    patchProperty.Name = name;

                    patchProperty.Value = value;

                    this.core.RootElement.AddChild(patchProperty);
                    break;
                }
            }
        }

        /// <summary>
        /// Decompile the Property table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePropertyTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var id = Convert.ToString(row[0]);
                var value = Convert.ToString(row[1]);

                if ("AdminProperties" == id || "MsiHiddenProperties" == id || "SecureCustomProperties" == id)
                {
                    if (0 < value.Length)
                    {
                        foreach (var propertyId in value.Split(';'))
                        {
                            if (Common.DowngradeDetectedProperty == propertyId || Common.UpgradeDetectedProperty == propertyId)
                            {
                                continue;
                            }

                            var property = propertyId;
                            var suppressModulularization = false;
                            if (OutputType.Module == this.OutputType)
                            {
                                if (propertyId.EndsWith(this.modularizationGuid.Substring(1, 36).Replace('-', '_'), StringComparison.Ordinal))
                                {
                                    property = propertyId.Substring(0, propertyId.Length - this.modularizationGuid.Length + 1);
                                }
                                else
                                {
                                    suppressModulularization = true;
                                }
                            }

                            var specialProperty = this.EnsureProperty(property);
                            if (suppressModulularization)
                            {
                                specialProperty.SuppressModularization = Wix.YesNoType.yes;
                            }

                            switch (id)
                            {
                            case "AdminProperties":
                                specialProperty.Admin = Wix.YesNoType.yes;
                                break;
                            case "MsiHiddenProperties":
                                specialProperty.Hidden = Wix.YesNoType.yes;
                                break;
                            case "SecureCustomProperties":
                                specialProperty.Secure = Wix.YesNoType.yes;
                                break;
                            }
                        }
                    }

                    continue;
                }
                else if (OutputType.Product == this.OutputType)
                {
                    var product = (Wix.Product)this.core.RootElement;

                    switch (id)
                    {
                    case "Manufacturer":
                        product.Manufacturer = value;
                        continue;
                    case "ProductCode":
                        product.Id = value.ToUpper(CultureInfo.InvariantCulture);
                        continue;
                    case "ProductLanguage":
                        product.Language = value;
                        continue;
                    case "ProductName":
                        product.Name = value;
                        continue;
                    case "ProductVersion":
                        product.Version = value;
                        continue;
                    case "UpgradeCode":
                        product.UpgradeCode = value;
                        continue;
                    }
                }

                if (!this.SuppressUI || "ErrorDialog" != id)
                {
                    var property = this.EnsureProperty(id);

                    property.Value = value;
                }
            }
        }

        /// <summary>
        /// Decompile the PublishComponent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePublishComponentTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var category = new Wix.Category();

                category.Id = Convert.ToString(row[0]);

                category.Qualifier = Convert.ToString(row[1]);

                if (null != row[3])
                {
                    category.AppData = Convert.ToString(row[3]);
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[2]));
                if (null != component)
                {
                    component.AddChild(category);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[2]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the RadioButton table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRadioButtonTable(Table table)
        {
            var radioButtons = new SortedList();
            var radioButtonGroups = new Hashtable();

            foreach (var row in table.Rows)
            {
                var radioButton = new Wix.RadioButton();

                radioButton.Value = Convert.ToString(row[2]);

                radioButton.X = Convert.ToString(row[3], CultureInfo.InvariantCulture);

                radioButton.Y = Convert.ToString(row[4], CultureInfo.InvariantCulture);

                radioButton.Width = Convert.ToString(row[5], CultureInfo.InvariantCulture);

                radioButton.Height = Convert.ToString(row[6], CultureInfo.InvariantCulture);

                if (null != row[7])
                {
                    radioButton.Text = Convert.ToString(row[7]);
                }

                if (null != row[8])
                {
                    var help = (Convert.ToString(row[8])).Split('|');

                    if (2 == help.Length)
                    {
                        if (0 < help[0].Length)
                        {
                            radioButton.ToolTip = help[0];
                        }

                        if (0 < help[1].Length)
                        {
                            radioButton.Help = help[1];
                        }
                    }
                }

                radioButtons.Add(String.Format(CultureInfo.InvariantCulture, "{0}|{1:0000000000}", row[0], row[1]), row);
                this.core.IndexElement(row, radioButton);
            }

            // nest the radio buttons
            foreach (Row row in radioButtons.Values)
            {
                var radioButton = (Wix.RadioButton)this.core.GetIndexedElement(row);
                var radioButtonGroup = (Wix.RadioButtonGroup)radioButtonGroups[Convert.ToString(row[0])];

                if (null == radioButtonGroup)
                {
                    radioButtonGroup = new Wix.RadioButtonGroup();

                    radioButtonGroup.Property = Convert.ToString(row[0]);

                    this.core.UIElement.AddChild(radioButtonGroup);
                    radioButtonGroups.Add(Convert.ToString(row[0]), radioButtonGroup);
                }

                radioButtonGroup.AddChild(radioButton);
            }
        }

        /// <summary>
        /// Decompile the Registry table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRegistryTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if (("-" == Convert.ToString(row[3]) || "+" == Convert.ToString(row[3]) || "*" == Convert.ToString(row[3])) && null == row[4])
                {
                    var registryKey = new Wix.RegistryKey();

                    registryKey.Id = Convert.ToString(row[0]);

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        registryKey.Root = registryRootType;
                    }

                    registryKey.Key = Convert.ToString(row[2]);

                    switch (Convert.ToString(row[3]))
                    {
                    case "+":
                        registryKey.ForceCreateOnInstall = Wix.YesNoType.yes;
                        break;
                    case "-":
                        registryKey.ForceDeleteOnUninstall = Wix.YesNoType.yes;
                        break;
                    case "*":
                        registryKey.ForceDeleteOnUninstall = Wix.YesNoType.yes;
                        registryKey.ForceCreateOnInstall = Wix.YesNoType.yes;
                        break;
                    }

                    this.core.IndexElement(row, registryKey);
                }
                else
                {
                    var registryValue = new Wix.RegistryValue();

                    registryValue.Id = Convert.ToString(row[0]);

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        registryValue.Root = registryRootType;
                    }

                    registryValue.Key = Convert.ToString(row[2]);

                    if (null != row[3])
                    {
                        registryValue.Name = Convert.ToString(row[3]);
                    }

                    if (null != row[4])
                    {
                        var value = Convert.ToString(row[4]);

                        if (value.StartsWith("#x", StringComparison.Ordinal))
                        {
                            registryValue.Type = Wix.RegistryValue.TypeType.binary;
                            registryValue.Value = value.Substring(2);
                        }
                        else if (value.StartsWith("#%", StringComparison.Ordinal))
                        {
                            registryValue.Type = Wix.RegistryValue.TypeType.expandable;
                            registryValue.Value = value.Substring(2);
                        }
                        else if (value.StartsWith("#", StringComparison.Ordinal) && !value.StartsWith("##", StringComparison.Ordinal))
                        {
                            registryValue.Type = Wix.RegistryValue.TypeType.integer;
                            registryValue.Value = value.Substring(1);
                        }
                        else
                        {
                            if (value.StartsWith("##", StringComparison.Ordinal))
                            {
                                value = value.Substring(1);
                            }

                            if (0 <= value.IndexOf("[~]", StringComparison.Ordinal))
                            {
                                registryValue.Type = Wix.RegistryValue.TypeType.multiString;

                                if ("[~]" == value)
                                {
                                    value = String.Empty;
                                }
                                else if (value.StartsWith("[~]", StringComparison.Ordinal) && value.EndsWith("[~]", StringComparison.Ordinal))
                                {
                                    value = value.Substring(3, value.Length - 6);
                                }
                                else if (value.StartsWith("[~]", StringComparison.Ordinal))
                                {
                                    registryValue.Action = Wix.RegistryValue.ActionType.append;
                                    value = value.Substring(3);
                                }
                                else if (value.EndsWith("[~]", StringComparison.Ordinal))
                                {
                                    registryValue.Action = Wix.RegistryValue.ActionType.prepend;
                                    value = value.Substring(0, value.Length - 3);
                                }

                                var multiValues = NullSplitter.Split(value);
                                foreach (var multiValue in multiValues)
                                {
                                    var multiStringValue = new Wix.MultiStringValue();

                                    multiStringValue.Content = multiValue;

                                    registryValue.AddChild(multiStringValue);
                                }
                            }
                            else
                            {
                                registryValue.Type = Wix.RegistryValue.TypeType.@string;
                                registryValue.Value = value;
                            }
                        }
                    }
                    else
                    {
                        registryValue.Type = Wix.RegistryValue.TypeType.@string;
                        registryValue.Value = String.Empty;
                    }

                    this.core.IndexElement(row, registryValue);
                }
            }
        }

        /// <summary>
        /// Decompile the RegLocator table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRegLocatorTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var registrySearch = new Wix.RegistrySearch();

                registrySearch.Id = Convert.ToString(row[0]);

                switch (Convert.ToInt32(row[1]))
                {
                case WindowsInstallerConstants.MsidbRegistryRootClassesRoot:
                    registrySearch.Root = Wix.RegistrySearch.RootType.HKCR;
                    break;
                case WindowsInstallerConstants.MsidbRegistryRootCurrentUser:
                    registrySearch.Root = Wix.RegistrySearch.RootType.HKCU;
                    break;
                case WindowsInstallerConstants.MsidbRegistryRootLocalMachine:
                    registrySearch.Root = Wix.RegistrySearch.RootType.HKLM;
                    break;
                case WindowsInstallerConstants.MsidbRegistryRootUsers:
                    registrySearch.Root = Wix.RegistrySearch.RootType.HKU;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                    break;
                }

                registrySearch.Key = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    registrySearch.Name = Convert.ToString(row[3]);
                }

                if (null == row[4])
                {
                    registrySearch.Type = Wix.RegistrySearch.TypeType.file;
                }
                else
                {
                    var type = Convert.ToInt32(row[4]);

                    if (WindowsInstallerConstants.MsidbLocatorType64bit == (type & WindowsInstallerConstants.MsidbLocatorType64bit))
                    {
                        registrySearch.Win64 = Wix.YesNoType.yes;
                        type &= ~WindowsInstallerConstants.MsidbLocatorType64bit;
                    }
                    else
                    {
                        registrySearch.Win64 = Wix.YesNoType.no;
                    }

                    switch (type)
                    {
                    case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                        registrySearch.Type = Wix.RegistrySearch.TypeType.directory;
                        break;
                    case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                        registrySearch.Type = Wix.RegistrySearch.TypeType.file;
                        break;
                    case WindowsInstallerConstants.MsidbLocatorTypeRawValue:
                        registrySearch.Type = Wix.RegistrySearch.TypeType.raw;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                    }
                }

                this.core.IndexElement(row, registrySearch);
            }
        }

        /// <summary>
        /// Decompile the RemoveFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRemoveFileTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if (null == row[2])
                {
                    var removeFolder = new Wix.RemoveFolder();

                    removeFolder.Id = Convert.ToString(row[0]);

                    // directory/property is set in FinalizeDecompile

                    switch (Convert.ToInt32(row[4]))
                    {
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall:
                        removeFolder.On = Wix.InstallUninstallType.install;
                        break;
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove:
                        removeFolder.On = Wix.InstallUninstallType.uninstall;
                        break;
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnBoth:
                        removeFolder.On = Wix.InstallUninstallType.both;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                    }

                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                    if (null != component)
                    {
                        component.AddChild(removeFolder);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                    }
                    this.core.IndexElement(row, removeFolder);
                }
                else
                {
                    var removeFile = new Wix.RemoveFile();

                    removeFile.Id = Convert.ToString(row[0]);

                    var names = Common.GetNames(Convert.ToString(row[2]));
                    if (null != names[0] && null != names[1])
                    {
                        removeFile.ShortName = names[0];
                        removeFile.Name = names[1];
                    }
                    else if (null != names[0])
                    {
                        removeFile.Name = names[0];
                    }

                    // directory/property is set in FinalizeDecompile

                    switch (Convert.ToInt32(row[4]))
                    {
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall:
                        removeFile.On = Wix.InstallUninstallType.install;
                        break;
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove:
                        removeFile.On = Wix.InstallUninstallType.uninstall;
                        break;
                    case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnBoth:
                        removeFile.On = Wix.InstallUninstallType.both;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                    }

                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                    if (null != component)
                    {
                        component.AddChild(removeFile);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                    }
                    this.core.IndexElement(row, removeFile);
                }
            }
        }

        /// <summary>
        /// Decompile the RemoveIniFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRemoveIniFileTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var iniFile = new Wix.IniFile();

                iniFile.Id = Convert.ToString(row[0]);

                var names = Common.GetNames(Convert.ToString(row[1]));
                if (null != names[0] && null != names[1])
                {
                    iniFile.ShortName = names[0];
                    iniFile.Name = names[1];
                }
                else if (null != names[0])
                {
                    iniFile.Name = names[0];
                }

                if (null != row[2])
                {
                    iniFile.Directory = Convert.ToString(row[2]);
                }

                iniFile.Section = Convert.ToString(row[3]);

                iniFile.Key = Convert.ToString(row[4]);

                if (null != row[5])
                {
                    iniFile.Value = Convert.ToString(row[5]);
                }

                switch (Convert.ToInt32(row[6]))
                {
                case WindowsInstallerConstants.MsidbIniFileActionRemoveLine:
                    iniFile.Action = Wix.IniFile.ActionType.removeLine;
                    break;
                case WindowsInstallerConstants.MsidbIniFileActionRemoveTag:
                    iniFile.Action = Wix.IniFile.ActionType.removeTag;
                    break;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                    break;
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[7]));
                if (null != component)
                {
                    component.AddChild(iniFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[7]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the RemoveRegistry table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRemoveRegistryTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if ("-" == Convert.ToString(row[3]))
                {
                    var removeRegistryKey = new Wix.RemoveRegistryKey();

                    removeRegistryKey.Id = Convert.ToString(row[0]);

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        removeRegistryKey.Root = registryRootType;
                    }

                    removeRegistryKey.Key = Convert.ToString(row[2]);

                    removeRegistryKey.Action = Wix.RemoveRegistryKey.ActionType.removeOnInstall;

                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[4]));
                    if (null != component)
                    {
                        component.AddChild(removeRegistryKey);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[4]), "Component"));
                    }
                }
                else
                {
                    var removeRegistryValue = new Wix.RemoveRegistryValue();

                    removeRegistryValue.Id = Convert.ToString(row[0]);

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        removeRegistryValue.Root = registryRootType;
                    }

                    removeRegistryValue.Key = Convert.ToString(row[2]);

                    if (null != row[3])
                    {
                        removeRegistryValue.Name = Convert.ToString(row[3]);
                    }

                    var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[4]));
                    if (null != component)
                    {
                        component.AddChild(removeRegistryValue);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[4]), "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the ReserveCost table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileReserveCostTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var reserveCost = new Wix.ReserveCost();

                reserveCost.Id = Convert.ToString(row[0]);

                if (null != row[2])
                {
                    reserveCost.Directory = Convert.ToString(row[2]);
                }

                reserveCost.RunLocal = Convert.ToInt32(row[3]);

                reserveCost.RunFromSource = Convert.ToInt32(row[4]);

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[1]));
                if (null != component)
                {
                    component.AddChild(reserveCost);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[1]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the SelfReg table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSelfRegTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var file = (Wix.File)this.core.GetIndexedElement("File", Convert.ToString(row[0]));

                if (null != file)
                {
                    if (null != row[1])
                    {
                        file.SelfRegCost = Convert.ToInt32(row[1]);
                    }
                    else
                    {
                        file.SelfRegCost = 0;
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", Convert.ToString(row[0]), "File"));
                }
            }
        }

        /// <summary>
        /// Decompile the ServiceControl table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileServiceControlTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var serviceControl = new Wix.ServiceControl();

                serviceControl.Id = Convert.ToString(row[0]);

                serviceControl.Name = Convert.ToString(row[1]);

                var eventValue = Convert.ToInt32(row[2]);
                if (WindowsInstallerConstants.MsidbServiceControlEventStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStart) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStart))
                {
                    serviceControl.Start = Wix.InstallUninstallType.both;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStart))
                {
                    serviceControl.Start = Wix.InstallUninstallType.install;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStart))
                {
                    serviceControl.Start = Wix.InstallUninstallType.uninstall;
                }

                if (WindowsInstallerConstants.MsidbServiceControlEventStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStop) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStop))
                {
                    serviceControl.Stop = Wix.InstallUninstallType.both;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStop))
                {
                    serviceControl.Stop = Wix.InstallUninstallType.install;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStop))
                {
                    serviceControl.Stop = Wix.InstallUninstallType.uninstall;
                }

                if (WindowsInstallerConstants.MsidbServiceControlEventDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventDelete) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete))
                {
                    serviceControl.Remove = Wix.InstallUninstallType.both;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventDelete))
                {
                    serviceControl.Remove = Wix.InstallUninstallType.install;
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete))
                {
                    serviceControl.Remove = Wix.InstallUninstallType.uninstall;
                }

                if (null != row[3])
                {
                    var arguments = NullSplitter.Split(Convert.ToString(row[3]));

                    foreach (var argument in arguments)
                    {
                        var serviceArgument = new Wix.ServiceArgument();

                        serviceArgument.Content = argument;

                        serviceControl.AddChild(serviceArgument);
                    }
                }

                if (null != row[4])
                {
                    if (0 == Convert.ToInt32(row[4]))
                    {
                        serviceControl.Wait = Wix.YesNoType.no;
                    }
                    else
                    {
                        serviceControl.Wait = Wix.YesNoType.yes;
                    }
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[5]));
                if (null != component)
                {
                    component.AddChild(serviceControl);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[5]), "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the ServiceInstall table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileServiceInstallTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var serviceInstall = new Wix.ServiceInstall();

                serviceInstall.Id = Convert.ToString(row[0]);

                serviceInstall.Name = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    serviceInstall.DisplayName = Convert.ToString(row[2]);
                }

                var serviceType = Convert.ToInt32(row[3]);
                if (WindowsInstallerConstants.MsidbServiceInstallInteractive == (serviceType & WindowsInstallerConstants.MsidbServiceInstallInteractive))
                {
                    serviceInstall.Interactive = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbServiceInstallOwnProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallOwnProcess) &&
                    WindowsInstallerConstants.MsidbServiceInstallShareProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallShareProcess))
                {
                    // TODO: warn
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallOwnProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallOwnProcess))
                {
                    serviceInstall.Type = Wix.ServiceInstall.TypeType.ownProcess;
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallShareProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallShareProcess))
                {
                    serviceInstall.Type = Wix.ServiceInstall.TypeType.shareProcess;
                }

                var startType = Convert.ToInt32(row[4]);
                if (WindowsInstallerConstants.MsidbServiceInstallDisabled == startType)
                {
                    serviceInstall.Start = Wix.ServiceInstall.StartType.disabled;
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallDemandStart == startType)
                {
                    serviceInstall.Start = Wix.ServiceInstall.StartType.demand;
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallAutoStart == startType)
                {
                    serviceInstall.Start = Wix.ServiceInstall.StartType.auto;
                }
                else
                {
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                }

                var errorControl = Convert.ToInt32(row[5]);
                if (WindowsInstallerConstants.MsidbServiceInstallErrorCritical == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorCritical))
                {
                    serviceInstall.ErrorControl = Wix.ServiceInstall.ErrorControlType.critical;
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallErrorNormal == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorNormal))
                {
                    serviceInstall.ErrorControl = Wix.ServiceInstall.ErrorControlType.normal;
                }
                else
                {
                    serviceInstall.ErrorControl = Wix.ServiceInstall.ErrorControlType.ignore;
                }

                if (WindowsInstallerConstants.MsidbServiceInstallErrorControlVital == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorControlVital))
                {
                    serviceInstall.Vital = Wix.YesNoType.yes;
                }

                if (null != row[6])
                {
                    serviceInstall.LoadOrderGroup = Convert.ToString(row[6]);
                }

                if (null != row[7])
                {
                    var dependencies = NullSplitter.Split(Convert.ToString(row[7]));

                    foreach (var dependency in dependencies)
                    {
                        if (0 < dependency.Length)
                        {
                            var serviceDependency = new Wix.ServiceDependency();

                            if (dependency.StartsWith("+", StringComparison.Ordinal))
                            {
                                serviceDependency.Group = Wix.YesNoType.yes;
                                serviceDependency.Id = dependency.Substring(1);
                            }
                            else
                            {
                                serviceDependency.Id = dependency;
                            }

                            serviceInstall.AddChild(serviceDependency);
                        }
                    }
                }

                if (null != row[8])
                {
                    serviceInstall.Account = Convert.ToString(row[8]);
                }

                if (null != row[9])
                {
                    serviceInstall.Password = Convert.ToString(row[9]);
                }

                if (null != row[10])
                {
                    serviceInstall.Arguments = Convert.ToString(row[10]);
                }

                if (null != row[12])
                {
                    serviceInstall.Description = Convert.ToString(row[12]);
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[11]));
                if (null != component)
                {
                    component.AddChild(serviceInstall);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[11]), "Component"));
                }
                this.core.IndexElement(row, serviceInstall);
            }
        }

        /// <summary>
        /// Decompile the SFPCatalog table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSFPCatalogTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var sfpCatalog = new Wix.SFPCatalog();

                sfpCatalog.Name = Convert.ToString(row[0]);

                sfpCatalog.SourceFile = Convert.ToString(row[1]);

                this.core.IndexElement(row, sfpCatalog);
            }

            // nest the SFPCatalog elements
            foreach (var row in table.Rows)
            {
                var sfpCatalog = (Wix.SFPCatalog)this.core.GetIndexedElement(row);

                if (null != row[2])
                {
                    var parentSFPCatalog = (Wix.SFPCatalog)this.core.GetIndexedElement("SFPCatalog", Convert.ToString(row[2]));

                    if (null != parentSFPCatalog)
                    {
                        parentSFPCatalog.AddChild(sfpCatalog);
                    }
                    else
                    {
                        sfpCatalog.Dependency = Convert.ToString(row[2]);

                        this.core.RootElement.AddChild(sfpCatalog);
                    }
                }
                else
                {
                    this.core.RootElement.AddChild(sfpCatalog);
                }
            }
        }

        /// <summary>
        /// Decompile the Shortcut table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileShortcutTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var shortcut = new Wix.Shortcut();

                shortcut.Id = Convert.ToString(row[0]);

                shortcut.Directory = Convert.ToString(row[1]);

                var names = Common.GetNames(Convert.ToString(row[2]));
                if (null != names[0] && null != names[1])
                {
                    shortcut.ShortName = names[0];
                    shortcut.Name = names[1];
                }
                else if (null != names[0])
                {
                    shortcut.Name = names[0];
                }

                if (null != row[5])
                {
                    shortcut.Arguments = Convert.ToString(row[5]);
                }

                if (null != row[6])
                {
                    shortcut.Description = Convert.ToString(row[6]);
                }

                if (null != row[7])
                {
                    shortcut.Hotkey = Convert.ToInt32(row[7]);
                }

                if (null != row[8])
                {
                    shortcut.Icon = Convert.ToString(row[8]);
                }

                if (null != row[9])
                {
                    shortcut.IconIndex = Convert.ToInt32(row[9]);
                }

                if (null != row[10])
                {
                    switch (Convert.ToInt32(row[10]))
                    {
                    case 1:
                        shortcut.Show = Wix.Shortcut.ShowType.normal;
                        break;
                    case 3:
                        shortcut.Show = Wix.Shortcut.ShowType.maximized;
                        break;
                    case 7:
                        shortcut.Show = Wix.Shortcut.ShowType.minimized;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[10].Column.Name, row[10]));
                        break;
                    }
                }

                if (null != row[11])
                {
                    shortcut.WorkingDirectory = Convert.ToString(row[11]);
                }

                // Only try to read the MSI 4.0-specific columns if they actually exist
                if (15 < row.Fields.Length)
                {
                    if (null != row[12])
                    {
                        shortcut.DisplayResourceDll = Convert.ToString(row[12]);
                    }

                    if (null != row[13])
                    {
                        shortcut.DisplayResourceId = Convert.ToInt32(row[13]);
                    }

                    if (null != row[14])
                    {
                        shortcut.DescriptionResourceDll = Convert.ToString(row[14]);
                    }

                    if (null != row[15])
                    {
                        shortcut.DescriptionResourceId = Convert.ToInt32(row[15]);
                    }
                }

                var component = (Wix.Component)this.core.GetIndexedElement("Component", Convert.ToString(row[3]));
                if (null != component)
                {
                    component.AddChild(shortcut);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", Convert.ToString(row[3]), "Component"));
                }

                this.core.IndexElement(row, shortcut);
            }
        }

        /// <summary>
        /// Decompile the Signature table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSignatureTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var fileSearch = new Wix.FileSearch();

                fileSearch.Id = Convert.ToString(row[0]);

                var names = Common.GetNames(Convert.ToString(row[1]));
                if (null != names[0])
                {
                    // it is permissable to just have a long name
                    if (!this.core.IsValidShortFilename(names[0], false) && null == names[1])
                    {
                        fileSearch.Name = names[0];
                    }
                    else
                    {
                        fileSearch.ShortName = names[0];
                    }
                }

                if (null != names[1])
                {
                    fileSearch.Name = names[1];
                }

                if (null != row[2])
                {
                    fileSearch.MinVersion = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    fileSearch.MaxVersion = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    fileSearch.MinSize = Convert.ToInt32(row[4]);
                }

                if (null != row[5])
                {
                    fileSearch.MaxSize = Convert.ToInt32(row[5]);
                }

                if (null != row[6])
                {
                    fileSearch.MinDate = this.core.ConvertIntegerToDateTime(Convert.ToInt32(row[6]));
                }

                if (null != row[7])
                {
                    fileSearch.MaxDate = this.core.ConvertIntegerToDateTime(Convert.ToInt32(row[7]));
                }

                if (null != row[8])
                {
                    fileSearch.Languages = Convert.ToString(row[8]);
                }

                this.core.IndexElement(row, fileSearch);
            }
        }

        /// <summary>
        /// Decompile the TargetFiles_OptionalData table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileTargetFiles_OptionalDataTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var targetFile = (Wix.TargetFile)this.patchTargetFiles[row[0]];
                if (null == targetFile)
                {
                    targetFile = new Wix.TargetFile();

                    targetFile.Id = Convert.ToString(row[1]);

                    var targetImage = (Wix.TargetImage)this.core.GetIndexedElement("TargetImages", Convert.ToString(row[0]));
                    if (null != targetImage)
                    {
                        targetImage.AddChild(targetFile);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Target", Convert.ToString(row[0]), "TargetImages"));
                    }
                    this.patchTargetFiles.Add(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), targetFile);
                }

                if (null != row[2])
                {
                    var symbolPaths = (Convert.ToString(row[2])).Split(';');

                    foreach (var symbolPathString in symbolPaths)
                    {
                        var symbolPath = new Wix.SymbolPath();

                        symbolPath.Path = symbolPathString;

                        targetFile.AddChild(symbolPath);
                    }
                }

                if (null != row[3] && null != row[4])
                {
                    var ignoreOffsets = (Convert.ToString(row[3])).Split(',');
                    var ignoreLengths = (Convert.ToString(row[4])).Split(',');

                    if (ignoreOffsets.Length == ignoreLengths.Length)
                    {
                        for (var i = 0; i < ignoreOffsets.Length; i++)
                        {
                            var ignoreRange = new Wix.IgnoreRange();

                            if (ignoreOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                ignoreRange.Offset = Convert.ToInt32(ignoreOffsets[i].Substring(2), 16);
                            }
                            else
                            {
                                ignoreRange.Offset = Convert.ToInt32(ignoreOffsets[i], CultureInfo.InvariantCulture);
                            }

                            if (ignoreLengths[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                ignoreRange.Length = Convert.ToInt32(ignoreLengths[i].Substring(2), 16);
                            }
                            else
                            {
                                ignoreRange.Length = Convert.ToInt32(ignoreLengths[i], CultureInfo.InvariantCulture);
                            }

                            targetFile.AddChild(ignoreRange);
                        }
                    }
                    else
                    {
                        // TODO: warn
                    }
                }
                else if (null != row[3] || null != row[4])
                {
                    // TODO: warn about mismatch between columns
                }

                // the RetainOffsets column is handled in FinalizeFamilyFileRangesTable
            }
        }

        /// <summary>
        /// Decompile the TargetImages table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileTargetImagesTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var targetImage = new Wix.TargetImage();

                targetImage.Id = Convert.ToString(row[0]);

                targetImage.SourceFile = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    var symbolPaths = (Convert.ToString(row[3])).Split(';');

                    foreach (var symbolPathString in symbolPaths)
                    {
                        var symbolPath = new Wix.SymbolPath();

                        symbolPath.Path = symbolPathString;

                        targetImage.AddChild(symbolPath);
                    }
                }

                targetImage.Order = Convert.ToInt32(row[4]);

                if (null != row[5])
                {
                    targetImage.Validation = Convert.ToString(row[5]);
                }

                if (0 != Convert.ToInt32(row[6]))
                {
                    targetImage.IgnoreMissingFiles = Wix.YesNoType.yes;
                }

                var upgradeImage = (Wix.UpgradeImage)this.core.GetIndexedElement("UpgradedImages", Convert.ToString(row[3]));
                if (null != upgradeImage)
                {
                    upgradeImage.AddChild(targetImage);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Upgraded", Convert.ToString(row[3]), "UpgradedImages"));
                }
                this.core.IndexElement(row, targetImage);
            }
        }

        /// <summary>
        /// Decompile the TextStyle table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileTextStyleTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var textStyle = new Wix.TextStyle();

                textStyle.Id = Convert.ToString(row[0]);

                textStyle.FaceName = Convert.ToString(row[1]);

                textStyle.Size = Convert.ToString(row[2]);

                if (null != row[3])
                {
                    var color = Convert.ToInt32(row[3]);

                    textStyle.Red = color & 0xFF;

                    textStyle.Green = (color & 0xFF00) >> 8;

                    textStyle.Blue = (color & 0xFF0000) >> 16;
                }

                if (null != row[4])
                {
                    var styleBits = Convert.ToInt32(row[4]);

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsBold == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsBold))
                    {
                        textStyle.Bold = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic))
                    {
                        textStyle.Italic = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline))
                    {
                        textStyle.Underline = Wix.YesNoType.yes;
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike))
                    {
                        textStyle.Strike = Wix.YesNoType.yes;
                    }
                }

                this.core.UIElement.AddChild(textStyle);
            }
        }

        /// <summary>
        /// Decompile the TypeLib table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileTypeLibTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var typeLib = new Wix.TypeLib();

                typeLib.Id = Convert.ToString(row[0]);

                typeLib.Advertise = Wix.YesNoType.yes;

                typeLib.Language = Convert.ToInt32(row[1]);

                if (null != row[3])
                {
                    var version = Convert.ToInt32(row[3]);

                    if (65536 == version)
                    {
                        this.Messaging.Write(WarningMessages.PossiblyIncorrectTypelibVersion(row.SourceLineNumbers, typeLib.Id));
                    }

                    typeLib.MajorVersion = ((version & 0xFFFF00) >> 8);
                    typeLib.MinorVersion = (version & 0xFF);
                }

                if (null != row[4])
                {
                    typeLib.Description = Convert.ToString(row[4]);
                }

                if (null != row[5])
                {
                    typeLib.HelpDirectory = Convert.ToString(row[5]);
                }

                if (null != row[7])
                {
                    typeLib.Cost = Convert.ToInt32(row[7]);
                }

                // nested under the appropriate File element in FinalizeFileTable
                this.core.IndexElement(row, typeLib);
            }
        }

        /// <summary>
        /// Decompile the Upgrade table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUpgradeTable(Table table)
        {
            var upgradeElements = new Hashtable();

            foreach (UpgradeRow upgradeRow in table.Rows)
            {
                if (Common.UpgradeDetectedProperty == upgradeRow.ActionProperty || Common.DowngradeDetectedProperty == upgradeRow.ActionProperty)
                {
                    continue; // MajorUpgrade rows processed in FinalizeUpgradeTable
                }

                var upgrade = (Wix.Upgrade)upgradeElements[upgradeRow.UpgradeCode];

                // create the parent Upgrade element if it doesn't already exist
                if (null == upgrade)
                {
                    upgrade = new Wix.Upgrade();

                    upgrade.Id = upgradeRow.UpgradeCode;

                    this.core.RootElement.AddChild(upgrade);
                    upgradeElements.Add(upgrade.Id, upgrade);
                }

                var upgradeVersion = new Wix.UpgradeVersion();

                if (null != upgradeRow.VersionMin)
                {
                    upgradeVersion.Minimum = upgradeRow.VersionMin;
                }

                if (null != upgradeRow.VersionMax)
                {
                    upgradeVersion.Maximum = upgradeRow.VersionMax;
                }

                if (null != upgradeRow.Language)
                {
                    upgradeVersion.Language = upgradeRow.Language;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures))
                {
                    upgradeVersion.MigrateFeatures = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect))
                {
                    upgradeVersion.OnlyDetect = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure))
                {
                    upgradeVersion.IgnoreRemoveFailure = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive))
                {
                    upgradeVersion.IncludeMinimum = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive))
                {
                    upgradeVersion.IncludeMaximum = Wix.YesNoType.yes;
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive))
                {
                    upgradeVersion.ExcludeLanguages = Wix.YesNoType.yes;
                }

                if (null != upgradeRow.Remove)
                {
                    upgradeVersion.RemoveFeatures = upgradeRow.Remove;
                }

                upgradeVersion.Property = upgradeRow.ActionProperty;

                upgrade.AddChild(upgradeVersion);
            }
        }

        /// <summary>
        /// Decompile the UpgradedFiles_OptionalData table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUpgradedFiles_OptionalDataTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var upgradeFile = new Wix.UpgradeFile();

                upgradeFile.File = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    var symbolPaths = (Convert.ToString(row[2])).Split(';');

                    foreach (var symbolPathString in symbolPaths)
                    {
                        var symbolPath = new Wix.SymbolPath();

                        symbolPath.Path = symbolPathString;

                        upgradeFile.AddChild(symbolPath);
                    }
                }

                if (null != row[3] && 1 == Convert.ToInt32(row[3]))
                {
                    upgradeFile.AllowIgnoreOnError = Wix.YesNoType.yes;
                }

                if (null != row[4] && 0 != Convert.ToInt32(row[4]))
                {
                    upgradeFile.WholeFile = Wix.YesNoType.yes;
                }

                upgradeFile.Ignore = Wix.YesNoType.no;

                var upgradeImage = (Wix.UpgradeImage)this.core.GetIndexedElement("UpgradedImages", Convert.ToString(row[0]));
                if (null != upgradeImage)
                {
                    upgradeImage.AddChild(upgradeFile);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Upgraded", Convert.ToString(row[0]), "UpgradedImages"));
                }
            }
        }

        /// <summary>
        /// Decompile the UpgradedFilesToIgnore table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUpgradedFilesToIgnoreTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                if ("*" != Convert.ToString(row[0]))
                {
                    var upgradeFile = new Wix.UpgradeFile();

                    upgradeFile.File = Convert.ToString(row[1]);

                    upgradeFile.Ignore = Wix.YesNoType.yes;

                    var upgradeImage = (Wix.UpgradeImage)this.core.GetIndexedElement("UpgradedImages", Convert.ToString(row[0]));
                    if (null != upgradeImage)
                    {
                        upgradeImage.AddChild(upgradeFile);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Upgraded", Convert.ToString(row[0]), "UpgradedImages"));
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, row.Fields[0].Column.Name, row[0]));
                }
            }
        }

        /// <summary>
        /// Decompile the UpgradedImages table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUpgradedImagesTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var upgradeImage = new Wix.UpgradeImage();

                upgradeImage.Id = Convert.ToString(row[0]);

                upgradeImage.SourceFile = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    upgradeImage.SourcePatch = Convert.ToString(row[2]);
                }

                if (null != row[3])
                {
                    var symbolPaths = (Convert.ToString(row[3])).Split(';');

                    foreach (var symbolPathString in symbolPaths)
                    {
                        var symbolPath = new Wix.SymbolPath();

                        symbolPath.Path = symbolPathString;

                        upgradeImage.AddChild(symbolPath);
                    }
                }

                var family = (Wix.Family)this.core.GetIndexedElement("ImageFamilies", Convert.ToString(row[4]));
                if (null != family)
                {
                    family.AddChild(upgradeImage);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Family", Convert.ToString(row[4]), "ImageFamilies"));
                }
                this.core.IndexElement(row, upgradeImage);
            }
        }

        /// <summary>
        /// Decompile the UIText table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUITextTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var uiText = new Wix.UIText();

                uiText.Id = Convert.ToString(row[0]);

                uiText.Content = Convert.ToString(row[1]);

                this.core.UIElement.AddChild(uiText);
            }
        }

        /// <summary>
        /// Decompile the Verb table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileVerbTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var verb = new Wix.Verb();

                verb.Id = Convert.ToString(row[1]);

                if (null != row[2])
                {
                    verb.Sequence = Convert.ToInt32(row[2]);
                }

                if (null != row[3])
                {
                    verb.Command = Convert.ToString(row[3]);
                }

                if (null != row[4])
                {
                    verb.Argument = Convert.ToString(row[4]);
                }

                this.core.IndexElement(row, verb);
            }
        }

        /// <summary>
        /// Gets the RegistryRootType from an integer representation of the root.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the root.</param>
        /// <param name="tableName">The name of the table containing the field.</param>
        /// <param name="field">The field containing the root value.</param>
        /// <param name="registryRootType">The strongly-typed representation of the root.</param>
        /// <returns>true if the value could be converted; false otherwise.</returns>
        private bool GetRegistryRootType(SourceLineNumber sourceLineNumbers, string tableName, Field field, out Wix.RegistryRootType registryRootType)
        {
            switch (Convert.ToInt32(field.Data))
            {
            case (-1):
                registryRootType = Wix.RegistryRootType.HKMU;
                return true;
            case WindowsInstallerConstants.MsidbRegistryRootClassesRoot:
                registryRootType = Wix.RegistryRootType.HKCR;
                return true;
            case WindowsInstallerConstants.MsidbRegistryRootCurrentUser:
                registryRootType = Wix.RegistryRootType.HKCU;
                return true;
            case WindowsInstallerConstants.MsidbRegistryRootLocalMachine:
                registryRootType = Wix.RegistryRootType.HKLM;
                return true;
            case WindowsInstallerConstants.MsidbRegistryRootUsers:
                registryRootType = Wix.RegistryRootType.HKU;
                return true;
            default:
                this.Messaging.Write(WarningMessages.IllegalColumnValue(sourceLineNumbers, tableName, field.Column.Name, field.Data));
                registryRootType = Wix.RegistryRootType.HKCR; // assign anything to satisfy the out parameter
                return false;
            }
        }

        /// <summary>
        /// Set the primary feature for a component.
        /// </summary>
        /// <param name="row">The row which specifies a primary feature.</param>
        /// <param name="featureColumnIndex">The index of the column contaning the feature identifier.</param>
        /// <param name="componentColumnIndex">The index of the column containing the component identifier.</param>
        private void SetPrimaryFeature(Row row, int featureColumnIndex, int componentColumnIndex)
        {
            // only products contain primary features
            if (OutputType.Product == this.OutputType)
            {
                var featureField = row.Fields[featureColumnIndex];
                var componentField = row.Fields[componentColumnIndex];

                var componentRef = (Wix.ComponentRef)this.core.GetIndexedElement("FeatureComponents", Convert.ToString(featureField.Data), Convert.ToString(componentField.Data));

                if (null != componentRef)
                {
                    componentRef.Primary = Wix.YesNoType.yes;
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, row.TableDefinition.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), featureField.Column.Name, Convert.ToString(featureField.Data), componentField.Column.Name, Convert.ToString(componentField.Data), "FeatureComponents"));
                }
            }
        }

        /// <summary>
        /// Checks the InstallExecuteSequence table to determine where RemoveExistingProducts is scheduled and removes it.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private static Wix.MajorUpgrade.ScheduleType DetermineMajorUpgradeScheduling(TableIndexedCollection tables)
        {
            var sequenceRemoveExistingProducts = 0;
            var sequenceInstallValidate = 0;
            var sequenceInstallInitialize = 0;
            var sequenceInstallFinalize = 0;
            var sequenceInstallExecute = 0;
            var sequenceInstallExecuteAgain = 0;

            var installExecuteSequenceTable = tables["InstallExecuteSequence"];
            if (null != installExecuteSequenceTable)
            {
                var removeExistingProductsRow = -1;
                for (var i = 0; i < installExecuteSequenceTable.Rows.Count; i++)
                {
                    var row = installExecuteSequenceTable.Rows[i];
                    var action = Convert.ToString(row[0]);
                    var sequence = Convert.ToInt32(row[2]);

                    switch (action)
                    {
                    case "RemoveExistingProducts":
                        sequenceRemoveExistingProducts = sequence;
                        removeExistingProductsRow = i;
                        break;
                    case "InstallValidate":
                        sequenceInstallValidate = sequence;
                        break;
                    case "InstallInitialize":
                        sequenceInstallInitialize = sequence;
                        break;
                    case "InstallExecute":
                        sequenceInstallExecute = sequence;
                        break;
                    case "InstallExecuteAgain":
                        sequenceInstallExecuteAgain = sequence;
                        break;
                    case "InstallFinalize":
                        sequenceInstallFinalize = sequence;
                        break;
                    }
                }

                installExecuteSequenceTable.Rows.RemoveAt(removeExistingProductsRow);
            }

            if (0 != sequenceInstallValidate && sequenceInstallValidate < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallInitialize)
            {
                return Wix.MajorUpgrade.ScheduleType.afterInstallValidate;
            }
            else if (0 != sequenceInstallInitialize && sequenceInstallInitialize < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallExecute)
            {
                return Wix.MajorUpgrade.ScheduleType.afterInstallInitialize;
            }
            else if (0 != sequenceInstallExecute && sequenceInstallExecute < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallExecuteAgain)
            {
                return Wix.MajorUpgrade.ScheduleType.afterInstallExecute;
            }
            else if (0 != sequenceInstallExecuteAgain && sequenceInstallExecuteAgain < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallFinalize)
            {
                return Wix.MajorUpgrade.ScheduleType.afterInstallExecuteAgain;
            }
            else
            {
                return Wix.MajorUpgrade.ScheduleType.afterInstallFinalize;
            }
        }
    }
}
