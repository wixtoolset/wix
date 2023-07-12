// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Decompile
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

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
        private XElement uiElement;

        /// <summary>
        /// Creates a new decompiler object with a default set of table definitions.
        /// </summary>
        public Decompiler(IMessaging messaging, IBackendHelper backendHelper, IWindowsInstallerDecompilerHelper decompilerHelper, IEnumerable<IWindowsInstallerDecompilerExtension> extensions, IEnumerable<IExtensionData> extensionData, ISymbolDefinitionCreator creator, string baseSourcePath, bool suppressCustomTables, bool suppressDroppingEmptyTables, bool suppressRelativeActionSequencing, bool suppressUI, bool treatProductAsModule)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.DecompilerHelper = decompilerHelper;
            this.Extensions = extensions;
            this.ExtensionData = extensionData;
            this.SymbolDefinitionCreator = creator;
            this.BaseSourcePath = baseSourcePath ?? "SourceDir";
            this.SuppressCustomTables = suppressCustomTables;
            this.SuppressDroppingEmptyTables = suppressDroppingEmptyTables;
            this.SuppressRelativeActionSequencing = suppressRelativeActionSequencing;
            this.SuppressUI = suppressUI;
            this.TreatProductAsModule = treatProductAsModule;

            this.ExtensionsByTableName = new Dictionary<string, IWindowsInstallerDecompilerExtension>();
            this.StandardActions = WindowsInstallerStandard.StandardActions().ToDictionary(a => a.Id.Id);

            this.TableDefinitions = new TableDefinitionCollection();
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IWindowsInstallerDecompilerHelper DecompilerHelper { get; }

        private IEnumerable<IWindowsInstallerDecompilerExtension> Extensions { get; }

        private IEnumerable<IExtensionData> ExtensionData { get; }

        private ISymbolDefinitionCreator SymbolDefinitionCreator { get; }

        private Dictionary<string, IWindowsInstallerDecompilerExtension> ExtensionsByTableName { get; }

        private string BaseSourcePath { get; }

        private bool SuppressCustomTables { get; }

        private bool SuppressDroppingEmptyTables { get; }

        private bool SuppressRelativeActionSequencing { get; }

        private bool SuppressUI { get; }

        private bool TreatProductAsModule { get; }

        private OutputType OutputType { get; set; }

        private Dictionary<string, WixActionSymbol> StandardActions { get; }

        private bool Compressed { get; set; }

        private TableDefinitionCollection TableDefinitions { get; }

        private bool ShortNames { get; set; }

        private string ModularizationGuid { get; set; }

        private XElement UIElement
        {
            get
            {
                if (null == this.uiElement)
                {
                    this.uiElement = this.DecompilerHelper.AddElementToRoot(new XElement(Names.UIElement));
                }

                return this.uiElement;
            }
        }

        private Dictionary<string, XElement> Singletons { get; } = new Dictionary<string, XElement>();

        private Dictionary<string, XElement> PatchTargetFiles { get; } = new Dictionary<string, XElement>();

        /// <summary>
        /// Decompile the database file.
        /// </summary>
        /// <param name="output">The output to decompile.</param>
        /// <returns>The serialized WiX source code.</returns>
        public XDocument Decompile(WindowsInstallerData output)
        {
            this.OutputType = output.Type;

            switch (this.OutputType)
            {
                case OutputType.Module:
                    this.DecompilerHelper.RootElement = new XElement(Names.ModuleElement);
                    break;
                case OutputType.PatchCreation:
                    this.DecompilerHelper.RootElement = new XElement(Names.PatchCreationElement);
                    break;
                case OutputType.Package:
                    this.DecompilerHelper.RootElement = new XElement(Names.PackageElement);
                    break;
                default:
                    throw new InvalidOperationException("Unknown output type.");
            }

            // collect the table definitions from the output
            this.TableDefinitions.Clear();
            foreach (var table in output.Tables)
            {
                this.TableDefinitions.Add(table.Definition);
            }

            // add any missing standard and wix-specific table definitions
            foreach (var tableDefinition in WindowsInstallerTableDefinitions.All)
            {
                if (!this.TableDefinitions.Contains(tableDefinition.Name))
                {
                    this.TableDefinitions.Add(tableDefinition);
                }
            }

            // add any missing extension table definitions
            foreach (var extension in this.Extensions)
            {
                this.AddExtensionTableDefinitions(extension);
            }

            // try to decompile the database file
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

            // return the XML document only if decompilation completed successfully
            return this.Messaging.EncounteredError ? null : new XDocument(new XElement(Names.WixElement, this.DecompilerHelper.RootElement));
        }

        private void AddExtensionTableDefinitions(IWindowsInstallerDecompilerExtension extension)
        {
            if (null != extension.TableDefinitions)
            {
                foreach (var tableDefinition in extension.TableDefinitions)
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

        internal static Platform? GetPlatformFromTemplateSummaryInformation(string[] template)
        {
            if (null != template && 1 < template.Length && null != template[0] && 0 < template[0].Length)
            {
                switch (template[0])
                {
                    case "Intel":
                        return Platform.X86;
                    case "x64":
                        return Platform.X64;
                    case "Arm64":
                        return Platform.ARM64;
                }
            }

            return null;
        }

        private Dictionary<string, List<XElement>> IndexTableOneToMany(IEnumerable<Row> rows, int column = 0)
        {
            return rows
                .ToLookup(row => row.FieldAsString(column), row => this.DecompilerHelper.GetIndexedElement(row))
                .ToDictionary(lookup => lookup.Key, lookup => lookup.ToList());
        }

        private Dictionary<string, List<XElement>> IndexTableOneToMany(TableIndexedCollection tables, string tableName, int column = 0)
        {
            return this.IndexTableOneToMany(tables[tableName]?.Rows ?? Enumerable.Empty<Row>(), column);
        }

        private Dictionary<string, List<XElement>> IndexTableOneToMany(Table table, int column = 0)
        {
            return this.IndexTableOneToMany(table?.Rows ?? Enumerable.Empty<Row>(), column);
        }

        private void AddChildToParent(string parentName, XElement xChild, Row row, int column)
        {
            var key = row.FieldAsString(column);
            if (this.DecompilerHelper.TryGetIndexedElement(parentName, key, out var xParent))
            {
                xParent.Add(xChild);
            }
            else
            {
                this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, row.Table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), row.Fields[column].Column.Name, key, parentName));
            }
        }

        private static XAttribute XAttributeIfNotNull(string attributeName, string value)
        {
            return value is null ? null : new XAttribute(attributeName, value);
        }

        private static XAttribute XAttributeIfNotNull(string attributeName, Row row, int column)
        {
            return row.IsColumnNull(column) ? null : new XAttribute(attributeName, row.FieldAsString(column));
        }

        private static void SetAttributeIfNotNull(XElement xElement, string attributeName, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                xElement.SetAttributeValue(attributeName, value);
            }
        }

        private static void SetAttributeIfNotNull(XElement xElement, string attributeName, int? value)
        {
            if (value.HasValue)
            {
                xElement.SetAttributeValue(attributeName, value);
            }
        }

        /// <summary>
        /// Convert an Int32 into a DateTime.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        /// <returns>The DateTime.</returns>
        private static DateTime ConvertIntegerToDateTime(int value)
        {
            var date = value / 65536;
            var time = value % 65536;

            return new DateTime(1980 + (date / 512), (date % 512) / 32, date % 32, time / 2048, (time % 2048) / 32, (time % 32) * 2);
        }

        /// <summary>
        /// Set the common control attributes in a control element.
        /// </summary>
        /// <param name="attributes">The control attributes.</param>
        /// <param name="xControl">The control element.</param>
        private static void SetControlAttributes(int attributes, XElement xControl)
        {
            if (0 == (attributes & WindowsInstallerConstants.MsidbControlAttributesEnabled))
            {
                xControl.SetAttributeValue("Disabled", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesIndirect == (attributes & WindowsInstallerConstants.MsidbControlAttributesIndirect))
            {
                xControl.SetAttributeValue("Indirect", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesInteger == (attributes & WindowsInstallerConstants.MsidbControlAttributesInteger))
            {
                xControl.SetAttributeValue("Integer", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesLeftScroll == (attributes & WindowsInstallerConstants.MsidbControlAttributesLeftScroll))
            {
                xControl.SetAttributeValue("LeftScroll", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesRightAligned == (attributes & WindowsInstallerConstants.MsidbControlAttributesRightAligned))
            {
                xControl.SetAttributeValue("RightAligned", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesRTLRO == (attributes & WindowsInstallerConstants.MsidbControlAttributesRTLRO))
            {
                xControl.SetAttributeValue("RightToLeft", "yes");
            }

            if (WindowsInstallerConstants.MsidbControlAttributesSunken == (attributes & WindowsInstallerConstants.MsidbControlAttributesSunken))
            {
                xControl.SetAttributeValue("Sunken", "yes");
            }

            if (0 == (attributes & WindowsInstallerConstants.MsidbControlAttributesVisible))
            {
                xControl.SetAttributeValue("Hidden", "yes");
            }
        }

        /// <summary>
        /// Creates an action element.
        /// </summary>
        /// <param name="actionSymbol">The action from which the element should be created.</param>
        private void CreateActionElement(WixActionSymbol actionSymbol)
        {
            XElement xAction;

            if (this.DecompilerHelper.TryGetIndexedElement("CustomAction", actionSymbol.Action, out var _)) // custom action
            {
                xAction = new XElement(Names.CustomElement,
                    new XAttribute("Action", actionSymbol.Action),
                    String.IsNullOrEmpty(actionSymbol.Condition) ? null : new XAttribute("Condition", actionSymbol.Condition));

                AssignActionSequence(actionSymbol, xAction);
            }
            else if (this.DecompilerHelper.TryGetIndexedElement("Dialog", actionSymbol.Action, out var _)) // dialog
            {
                xAction = new XElement(Names.ShowElement,
                    new XAttribute("Dialog", actionSymbol.Action),
                    XAttributeIfNotNull("Condition", actionSymbol.Condition));

                AssignActionSequence(actionSymbol, xAction);
            }
            else // possibly a standard action without suggested sequence information
            {
                xAction = this.CreateStandardActionElement(actionSymbol);
            }

            // add the action element to the appropriate sequence element
            if (null != xAction)
            {
                var sequenceTable = actionSymbol.SequenceTable.ToString();
                if (!this.Singletons.TryGetValue(sequenceTable, out var xSequence))
                {
                    xSequence = new XElement(Names.WxsNamespace + sequenceTable);

                    this.DecompilerHelper.AddElementToRoot(xSequence);
                    this.Singletons.Add(sequenceTable, xSequence);
                }

                try
                {
                    xSequence.Add(xAction);
                }
                catch (ArgumentException) // action/dialog is not valid for this sequence
                {
                    this.Messaging.Write(WarningMessages.IllegalActionInSequence(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                }
            }
        }

        /// <summary>
        /// Creates a standard action element.
        /// </summary>
        /// <param name="actionSymbol">The action row from which the element should be created.</param>
        /// <returns>The created element.</returns>
        private XElement CreateStandardActionElement(WixActionSymbol actionSymbol)
        {
            XElement xStandardAction = null;

            switch (actionSymbol.Action)
            {
                case "AllocateRegistrySpace":
                case "BindImage":
                case "CostFinalize":
                case "CostInitialize":
                case "CreateFolders":
                case "CreateShortcuts":
                case "DeleteServices":
                case "DuplicateFiles":
                case "ExecuteAction":
                case "FileCost":
                case "InstallAdminPackage":
                case "InstallFiles":
                case "InstallFinalize":
                case "InstallInitialize":
                case "InstallODBC":
                case "InstallServices":
                case "InstallValidate":
                case "IsolateComponents":
                case "MigrateFeatureStates":
                case "MoveFiles":
                case "MsiPublishAssemblies":
                case "MsiUnpublishAssemblies":
                case "PatchFiles":
                case "ProcessComponents":
                case "PublishComponents":
                case "PublishFeatures":
                case "PublishProduct":
                case "RegisterClassInfo":
                case "RegisterComPlus":
                case "RegisterExtensionInfo":
                case "RegisterFonts":
                case "RegisterMIMEInfo":
                case "RegisterProduct":
                case "RegisterProgIdInfo":
                case "RegisterTypeLibraries":
                case "RegisterUser":
                case "RemoveDuplicateFiles":
                case "RemoveEnvironmentStrings":
                case "RemoveFiles":
                case "RemoveFolders":
                case "RemoveIniValues":
                case "RemoveODBC":
                case "RemoveRegistryValues":
                case "RemoveShortcuts":
                case "SelfRegModules":
                case "SelfUnregModules":
                case "SetODBCFolders":
                case "StartServices":
                case "StopServices":
                case "UnpublishComponents":
                case "UnpublishFeatures":
                case "UnregisterClassInfo":
                case "UnregisterComPlus":
                case "UnregisterExtensionInfo":
                case "UnregisterFonts":
                case "UnregisterMIMEInfo":
                case "UnregisterProgIdInfo":
                case "UnregisterTypeLibraries":
                case "ValidateProductID":
                case "WriteEnvironmentStrings":
                case "WriteIniValues":
                case "WriteRegistryValues":
                    xStandardAction = new XElement(Names.WxsNamespace + actionSymbol.Action);
                    break;

                case "AppSearch":
                    this.StandardActions.TryGetValue(actionSymbol.Id.Id, out var appSearchActionRow);

                    if (null != actionSymbol.Before || null != actionSymbol.After || (null != appSearchActionRow && actionSymbol.Sequence != appSearchActionRow.Sequence))
                    {
                        xStandardAction = new XElement(Names.AppSearchElement);

                        SetAttributeIfNotNull(xStandardAction, "Condition", actionSymbol.Condition);
                        SetAttributeIfNotNull(xStandardAction, "Before", actionSymbol.Before);
                        SetAttributeIfNotNull(xStandardAction, "After", actionSymbol.After);
                        SetAttributeIfNotNull(xStandardAction, "Sequence", actionSymbol.Sequence);

                        return xStandardAction;
                    }
                    break;

                case "CCPSearch":
                case "DisableRollback":
                case "FindRelatedProducts":
                case "ForceReboot":
                case "InstallExecute":
                case "InstallExecuteAgain":
                case "LaunchConditions":
                case "RemoveExistingProducts":
                case "ResolveSource":
                case "RMCCPSearch":
                case "ScheduleReboot":
                    xStandardAction = new XElement(Names.WxsNamespace + actionSymbol.Action);
                    Decompiler.SequenceRelativeAction(actionSymbol, xStandardAction);
                    return xStandardAction;

                default:
                    this.Messaging.Write(WarningMessages.UnknownAction(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                    return null;
            }

            if (xStandardAction != null)
            {
                this.SequenceStandardAction(actionSymbol, xStandardAction);
            }

            return xStandardAction;
        }

        /// <summary>
        /// Applies the condition and sequence to a standard action element based on the action symbol data.
        /// </summary>
        /// <param name="actionSymbol">Action data from the database.</param>
        /// <param name="xAction">Element to be sequenced.</param>
        private void SequenceStandardAction(WixActionSymbol actionSymbol, XElement xAction)
        {
            xAction.SetAttributeValue("Condition", actionSymbol.Condition);

            if ((null != actionSymbol.Before || null != actionSymbol.After) && 0 == actionSymbol.Sequence)
            {
                this.Messaging.Write(WarningMessages.DecompiledStandardActionRelativelyScheduledInModule(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
            }
            else if (actionSymbol.Sequence.HasValue)
            {
                xAction.SetAttributeValue("Sequence", actionSymbol.Sequence.Value);
            }
        }

        /// <summary>
        /// Applies the condition and relative sequence to an action element based on the action row data.
        /// </summary>
        /// <param name="actionSymbol">Action data from the database.</param>
        /// <param name="xAction">Element to be sequenced.</param>
        private static void SequenceRelativeAction(WixActionSymbol actionSymbol, XElement xAction)
        {
            SetAttributeIfNotNull(xAction, "Condition", actionSymbol.Condition);
            SetAttributeIfNotNull(xAction, "Before", actionSymbol.Before);
            SetAttributeIfNotNull(xAction, "After", actionSymbol.After);
            SetAttributeIfNotNull(xAction, "Sequence", actionSymbol.Sequence);
        }

        /// <summary>
        /// Ensure that a particular property exists in the decompiled output.
        /// </summary>
        /// <param name="id">The identifier of the property.</param>
        /// <returns>The property element.</returns>
        private XElement EnsureProperty(string id)
        {
            if (!this.DecompilerHelper.TryGetIndexedElement("Property", id, out var xProperty))
            {
                xProperty = new XElement(Names.PropertyElement, new XAttribute("Id", id));

                this.DecompilerHelper.AddElementToRoot(xProperty);
                this.DecompilerHelper.IndexElement("Property", id, xProperty);
            }

            return xProperty;
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
                this.FinalizeSummaryInformationStream(tables);
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

            foreach (var extension in this.Extensions)
            {
                extension.PostDecompileTables(tables);
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

            var checkBoxes = checkBoxTable?.Rows.ToDictionary(row => row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter));
            var checkBoxProperties = checkBoxTable?.Rows.ToDictionary(row => row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), row => false);

            // enumerate through the Control table, adding CheckBox values where appropriate
            if (null != controlTable)
            {
                foreach (var row in controlTable.Rows)
                {
                    var xControl = this.DecompilerHelper.GetIndexedElement(row);

                    if ("CheckBox" == row.FieldAsString(2))
                    {
                        var property = row.FieldAsString(8);
                        if (!String.IsNullOrEmpty(property) && checkBoxes.TryGetValue(property, out var checkBoxRow))
                        {
                            // if we've seen this property already, create a reference to it
                            if (checkBoxProperties.TryGetValue(property, out var seen) && seen)
                            {
                                xControl.SetAttributeValue("CheckBoxPropertyRef", property);
                            }
                            else
                            {
                                xControl.SetAttributeValue("Property", property);
                                checkBoxProperties[property] = true;
                            }

                            xControl.SetAttributeValue("CheckBoxValue", checkBoxRow.FieldAsString(1));
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Control", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Property", row.FieldAsString(8), "CheckBox"));
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
                // Add the TARGETDIR StandardDirectory if a component is directly parented there.
                if (componentTable.Rows.Any(row => row.FieldAsString(2) == "TARGETDIR")
                    && this.DecompilerHelper.TryGetIndexedElement("Directory", "TARGETDIR", out var xDirectory))
                {
                    this.DecompilerHelper.AddElementToRoot(xDirectory);
                }

                foreach (var row in componentTable.Rows)
                {
                    var attributes = row.FieldAsInteger(3);
                    var keyPath = row.FieldAsString(5);

                    if (String.IsNullOrEmpty(keyPath))
                    {
                        var xComponent = this.DecompilerHelper.GetIndexedElement("Component", row.FieldAsString(0));
                        xComponent.SetAttributeValue("KeyPath", "yes");
                    }
                    else if (WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath == (attributes & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("Registry", keyPath, out var xRegistry))
                        {
                            if (xRegistry.Name.LocalName == "RegistryValue")
                            {
                                xRegistry.SetAttributeValue("KeyPath", "yes");
                            }
                            else
                            {
                                this.Messaging.Write(WarningMessages.IllegalRegistryKeyPath(row.SourceLineNumbers, "Component", keyPath));
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", keyPath, "Registry"));
                        }
                    }
                    else if (WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource == (attributes & WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("ODBCDataSource", keyPath, out var xOdbcDataSource))
                        {
                            xOdbcDataSource.SetAttributeValue("KeyPath", "yes");
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", keyPath, "ODBCDataSource"));
                        }
                    }
                    else
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("File", keyPath, out var xFile))
                        {
                            xFile.SetAttributeValue("KeyPath", "yes");
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Component", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyPath", keyPath, "File"));
                        }
                    }
                }
            }

            // add the File children elements
            if (null != fileTable)
            {
                foreach (FileRow fileRow in fileTable.Rows)
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", fileRow.Component, out var xComponent)
                        && this.DecompilerHelper.TryGetIndexedElement(fileRow, out var xFile))
                    {
                        xComponent.Add(xFile);
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
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(1), out var xComponent)
                        && this.DecompilerHelper.TryGetIndexedElement(row, out var xOdbcDataSource))
                    {
                        xComponent.Add(xOdbcDataSource);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "ODBCDataSource", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", row.FieldAsString(1), "Component"));
                    }
                }
            }

            // add the Registry children elements
            if (null != registryTable)
            {
                foreach (var row in registryTable.Rows)
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(5), out var xComponent)
                        && this.DecompilerHelper.TryGetIndexedElement(row, out var xRegistry))
                    {
                        xComponent.Add(xRegistry);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Registry", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", row.FieldAsString(5), "Component"));
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

            var addedControls = new HashSet<XElement>();

            var controlTable = tables["Control"];
            var controlRows = controlTable?.Rows.ToDictionary(row => row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter));

            var dialogTable = tables["Dialog"];
            if (null != dialogTable)
            {
                foreach (var dialogRow in dialogTable.Rows)
                {
                    var xDialog = this.DecompilerHelper.GetIndexedElement(dialogRow);
                    var dialogId = dialogRow.FieldAsString(0);

                    if (!this.DecompilerHelper.TryGetIndexedElement("Control", dialogId, dialogRow.FieldAsString(7), out var xControl))
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(dialogRow.SourceLineNumbers, "Dialog", dialogRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_First", dialogRow.FieldAsString(7), "Control"));
                    }

                    // add tabbable controls
                    while (null != xControl)
                    {
                        var controlId = xControl.Attribute("Id").Value;
                        var controlRow = controlRows[String.Concat(dialogId, DecompilerConstants.PrimaryKeyDelimiter, controlId)];

                        xControl.SetAttributeValue("TabSkip", "no");

                        xDialog.Add(xControl);
                        addedControls.Add(xControl);

                        var controlNext = controlRow.FieldAsString(10);
                        if (!String.IsNullOrEmpty(controlNext))
                        {
                            if (this.DecompilerHelper.TryGetIndexedElement("Control", dialogId, controlNext, out xControl))
                            {
                                // looped back to the first control in the dialog
                                if (addedControls.Contains(xControl))
                                {
                                    xControl = null;
                                }
                            }
                            else
                            {
                                this.Messaging.Write(WarningMessages.ExpectedForeignRow(controlRow.SourceLineNumbers, "Control", controlRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", dialogId, "Control_Next", controlNext, "Control"));
                            }
                        }
                        else
                        {
                            xControl = null;
                        }
                    }

                    // set default control
                    var controlDefault = dialogRow.FieldAsString(8);
                    if (!String.IsNullOrEmpty(controlDefault))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("Control", dialogId, controlDefault, out var xDefaultControl))
                        {
                            xDefaultControl.SetAttributeValue("Default", "yes");
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(dialogRow.SourceLineNumbers, "Dialog", dialogRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_Default", Convert.ToString(dialogRow[8]), "Control"));
                        }
                    }

                    // set cancel control
                    var controlCancel = dialogRow.FieldAsString(9);
                    if (!String.IsNullOrEmpty(controlCancel))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("Control", dialogId, controlCancel, out var xCancelControl))
                        {
                            xCancelControl.SetAttributeValue("Cancel", "yes");
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(dialogRow.SourceLineNumbers, "Dialog", dialogRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog", dialogId, "Control_Cancel", Convert.ToString(dialogRow[9]), "Control"));
                        }
                    }
                }
            }

            // add the non-tabbable controls to the dialog
            if (null != controlTable)
            {
                foreach (var controlRow in controlTable.Rows)
                {
                    var dialogId = controlRow.FieldAsString(0);
                    if (!this.DecompilerHelper.TryGetIndexedElement("Dialog", dialogId, out var xDialog))
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(controlRow.SourceLineNumbers, "Control", controlRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", dialogId, "Dialog"));
                        continue;
                    }

                    var xControl = this.DecompilerHelper.GetIndexedElement(controlRow);
                    if (!addedControls.Contains(xControl))
                    {
                        xControl.SetAttributeValue("TabSkip", "yes");
                        xDialog.Add(xControl);
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
            if (null != duplicateFileTable)
            {
                foreach (var row in duplicateFileTable.Rows)
                {
                    var xCopyFile = this.DecompilerHelper.GetIndexedElement(row);
                    var destination = row.FieldAsString(4);
                    if (!String.IsNullOrEmpty(destination))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("Directory", destination, out var _))
                        {
                            xCopyFile.SetAttributeValue("DestinationDirectory", destination);
                        }
                        else
                        {
                            xCopyFile.SetAttributeValue("DestinationProperty", destination);
                        }
                    }
                }
            }

            var moveFileTable = tables["MoveFile"];
            if (null != moveFileTable)
            {
                foreach (var row in moveFileTable.Rows)
                {
                    var xCopyFile = this.DecompilerHelper.GetIndexedElement(row);
                    var source = row.FieldAsString(4);
                    if (!String.IsNullOrEmpty(source))
                    {
                        if (this.DecompilerHelper.TryGetIndexedElement("Directory", source, out var _))
                        {
                            xCopyFile.SetAttributeValue("SourceDirectory", source);
                        }
                        else
                        {
                            xCopyFile.SetAttributeValue("SourceProperty", source);
                        }
                    }

                    var destination = row.FieldAsString(5);
                    if (this.DecompilerHelper.TryGetIndexedElement("Directory", destination, out var _))
                    {
                        xCopyFile.SetAttributeValue("DestinationDirectory", destination);
                    }
                    else
                    {
                        xCopyFile.SetAttributeValue("DestinationProperty", destination);
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
            var familyFileRangesTable = tables["FamilyFileRanges"];
            if (null != familyFileRangesTable)
            {
                foreach (var row in familyFileRangesTable.Rows)
                {
                    var xProtectRange = new XElement(Names.ProtectRangeElement);

                    if (!row.IsColumnNull(2) && !row.IsColumnNull(3))
                    {
                        var retainOffsets = row.FieldAsString(2).Split(',');
                        var retainLengths = row.FieldAsString(3).Split(',');

                        if (retainOffsets.Length == retainLengths.Length)
                        {
                            for (var i = 0; i < retainOffsets.Length; i++)
                            {
                                if (retainOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                                {
                                    xProtectRange.SetAttributeValue("Offset", Convert.ToInt32(retainOffsets[i].Substring(2), 16));
                                }
                                else
                                {
                                    xProtectRange.SetAttributeValue("Offset", Convert.ToInt32(retainOffsets[i], CultureInfo.InvariantCulture));
                                }

                                if (retainLengths[i].StartsWith("0x", StringComparison.Ordinal))
                                {
                                    xProtectRange.SetAttributeValue("Length", Convert.ToInt32(retainLengths[i].Substring(2), 16));
                                }
                                else
                                {
                                    xProtectRange.SetAttributeValue("Length", Convert.ToInt32(retainLengths[i], CultureInfo.InvariantCulture));
                                }
                            }
                        }
                        else
                        {
                            // TODO: warn
                        }
                    }
                    else if (!row.IsColumnNull(2) || !row.IsColumnNull(3))
                    {
                        // TODO: warn about mismatch between columns
                    }

                    this.DecompilerHelper.IndexElement(row, xProtectRange);
                }
            }

            var usedProtectRanges = new HashSet<XElement>();
            var externalFilesTable = tables["ExternalFiles"];
            if (null != externalFilesTable)
            {
                foreach (var row in externalFilesTable.Rows)
                {
                    if (this.DecompilerHelper.TryGetIndexedElement(row, out var xExternalFile)
                        && this.DecompilerHelper.TryGetIndexedElement("FamilyFileRanges", row.FieldAsString(0), row.FieldAsString(0), out var xProtectRange))
                    {
                        xExternalFile.Add(xProtectRange);
                        usedProtectRanges.Add(xProtectRange);
                    }
                }
            }

            var targetFiles_OptionalDataTable = tables["TargetFiles_OptionalData"];
            if (null != targetFiles_OptionalDataTable)
            {
                var targetImagesTable = tables["TargetImages"];
                var targetImageRows = targetImagesTable?.Rows.ToDictionary(row => row.FieldAsString(0));

                var upgradedImagesTable = tables["UpgradedImages"];
                var upgradedImagesRows = upgradedImagesTable?.Rows.ToDictionary(row => row.FieldAsString(0));

                foreach (var row in targetFiles_OptionalDataTable.Rows)
                {
                    var xTargetFile = this.PatchTargetFiles[row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter)];

                    if (!targetImageRows.TryGetValue(row.FieldAsString(0), out var targetImageRow))
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, targetFiles_OptionalDataTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Target", row.FieldAsString(0), "TargetImages"));
                        continue;
                    }

                    if (!upgradedImagesRows.TryGetValue(row.FieldAsString(3), out var upgradedImagesRow))
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(targetImageRow.SourceLineNumbers, targetImageRow.Table.Name, targetImageRow.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Upgraded", row.FieldAsString(3), "UpgradedImages"));
                        continue;
                    }

                    if (this.DecompilerHelper.TryGetIndexedElement("FamilyFileRanges", upgradedImagesRow.FieldAsString(4), row.FieldAsString(1), out var xProtectRange))
                    {
                        xTargetFile.Add(xProtectRange);
                        usedProtectRanges.Add(xProtectRange);
                    }
                }
            }

            if (null != familyFileRangesTable)
            {
                foreach (var row in familyFileRangesTable.Rows)
                {
                    var xProtectRange = this.DecompilerHelper.GetIndexedElement(row);

                    if (!usedProtectRanges.Contains(xProtectRange))
                    {
                        var xProtectFile = new XElement(Names.ProtectFileElement, new XAttribute("File", row.FieldAsString(1)));
                        xProtectFile.Add(xProtectRange);

                        this.AddChildToParent("ImageFamilies", xProtectFile, row, 0);
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
            if (null != classTable)
            {
                foreach (var row in classTable.Rows)
                {
                    this.SetPrimaryFeature(row, 11, 2);
                }
            }

            var extensionTable = tables["Extension"];
            if (null != extensionTable)
            {
                foreach (var row in extensionTable.Rows)
                {
                    this.SetPrimaryFeature(row, 4, 1);
                }
            }

            var msiAssemblyTable = tables["MsiAssembly"];
            if (null != msiAssemblyTable)
            {
                foreach (var row in msiAssemblyTable.Rows)
                {
                    this.SetPrimaryFeature(row, 1, 0);
                }
            }

            var publishComponentTable = tables["PublishComponent"];
            if (null != publishComponentTable)
            {
                foreach (var row in publishComponentTable.Rows)
                {
                    this.SetPrimaryFeature(row, 4, 2);
                }
            }

            var typeLibTable = tables["TypeLib"];
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
            // index the media table by media id
            var mediaTable = tables["Media"];
            var mediaRows = new RowDictionary<MediaRow>(mediaTable);

            // set the disk identifiers and sources for files
            foreach (var fileRow in tables["File"]?.Rows.Cast<FileRow>() ?? Enumerable.Empty<FileRow>())
            {
                var xFile = this.DecompilerHelper.GetIndexedElement("File", fileRow.File);

                // Don't bother processing files that are orphaned (and won't show up in the output anyway)
                if (null != xFile.Parent)
                {
                    // set the diskid
                    if (null != mediaTable)
                    {
                        foreach (MediaRow mediaRow in mediaTable.Rows)
                        {
                            if (fileRow.Sequence <= mediaRow.LastSequence && mediaRow.DiskId != 1)
                            {
                                xFile.SetAttributeValue("DiskId", mediaRow.DiskId);
                                break;
                            }
                        }
                    }

                    var fileId = xFile?.Attribute("Id")?.Value;
                    var fileCompressed = xFile?.Attribute("Compressed")?.Value;
                    var fileShortName = xFile?.Attribute("ShortName")?.Value;
                    var fileName = xFile?.Attribute("Name")?.Value;

                    // set the source (done here because it requires information from the Directory table)
                    if (OutputType.Module == this.OutputType && !this.TreatProductAsModule)
                    {
                        xFile.SetAttributeValue("Source", String.Concat(this.BaseSourcePath, Path.DirectorySeparatorChar, "File", Path.DirectorySeparatorChar, fileId, '.', this.ModularizationGuid.Substring(1, 36).Replace('-', '_')));
                    }
                    else if (fileCompressed == "yes" || (fileCompressed != "no" && this.Compressed) || OutputType.Module == this.OutputType)
                    {
                        xFile.SetAttributeValue("Source", String.Concat(this.BaseSourcePath, Path.DirectorySeparatorChar, "File", Path.DirectorySeparatorChar, fileId));
                    }
                    else // uncompressed
                    {
                        var name = (!this.ShortNames && !String.IsNullOrEmpty(fileName)) ? fileName : fileShortName ?? fileName;

                        if (this.Compressed) // uncompressed at the root of the source image
                        {
                            xFile.SetAttributeValue("Source", String.Concat("SourceDir", Path.DirectorySeparatorChar, name));
                        }
                        else
                        {
                            var sourcePath = this.GetSourcePath(xFile);
                            xFile.SetAttributeValue("Source", Path.Combine(sourcePath, name));
                        }
                    }
                }
            }

            // set the file assemblies and manifests
            foreach (var row in tables["MsiAssembly"]?.Rows ?? Enumerable.Empty<Row>())
            {
                if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(0), out var xComponent))
                {
                    foreach (var xFile in xComponent.Elements(Names.FileElement).Where(x => x.Attribute("KeyPath")?.Value == "yes"))
                    {
                        xFile.SetAttributeValue("AssemblyManifest", row.FieldAsString(2));
                        xFile.SetAttributeValue("AssemblyApplication", row.FieldAsString(3));
                        xFile.SetAttributeValue("Assembly", row.FieldAsInteger(4) == 0 ? ".net" : "win32");
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MsiAssembly", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", row.FieldAsString(0), "Component"));
                }
            }

            // nest the TypeLib elements
            foreach (var row in tables["TypeLib"]?.Rows ?? Enumerable.Empty<Row>())
            {
                var xComponent = this.DecompilerHelper.GetIndexedElement("Component", row.FieldAsString(2));
                var xTypeLib = this.DecompilerHelper.GetIndexedElement(row);

                foreach (var xFile in xComponent.Elements(Names.FileElement).Where(x => x.Attribute("KeyPath")?.Value == "yes"))
                {
                    xFile.Add(xTypeLib);
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
            var extensionRows = tables["Extension"]?.Rows ?? Enumerable.Empty<Row>();
            foreach (var row in extensionRows)
            {
                // set the default MIME element for this extension
                var mimeRef = row.FieldAsString(3);
                if (null != mimeRef)
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("MIME", mimeRef, out var xMime))
                    {
                        xMime.SetAttributeValue("Default", "yes");
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Extension", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "MIME_", row.FieldAsString(3), "MIME"));
                    }
                }
            }

            var extensionsByExtensionId = this.IndexTableOneToMany(extensionRows);

            foreach (var row in tables["MIME"]?.Rows ?? Enumerable.Empty<Row>())
            {
                var xMime = this.DecompilerHelper.GetIndexedElement(row);

                if (extensionsByExtensionId.TryGetValue(row.FieldAsString(1), out var xExtensions))
                {
                    foreach (var extension in xExtensions)
                    {
                        extension.Add(xMime);
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "MIME", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Extension_", row.FieldAsString(1), "Extension"));
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
            // add the default ProgIds for each class (and index the class table)
            var classRows = tables["Class"]?.Rows?.Where(row => row.FieldAsString(3) != null) ?? Enumerable.Empty<Row>();

            var classesByCLSID = this.IndexTableOneToMany(classRows);

            var addedProgIds = new Dictionary<XElement, string>();

            foreach (var row in classRows)
            {
                var clsid = row.FieldAsString(0);
                var xClass = this.DecompilerHelper.GetIndexedElement(row);

                if (this.DecompilerHelper.TryGetIndexedElement("ProgId", row.FieldAsString(3), out var xProgId))
                {
                    if (addedProgIds.TryGetValue(xProgId, out var progid))
                    {
                        this.Messaging.Write(WarningMessages.TooManyProgIds(row.SourceLineNumbers, row.FieldAsString(0), row.FieldAsString(3), progid));
                    }
                    else
                    {
                        xClass.Add(xProgId);
                        addedProgIds.Add(xProgId, clsid);
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Class", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ProgId_Default", row.FieldAsString(3), "ProgId"));
                }
            }

            // add the remaining non-default ProgId entries for each class
            foreach (var row in tables["ProgId"]?.Rows ?? Enumerable.Empty<Row>())
            {
                var clsid = row.FieldAsString(2);
                var xProgId = this.DecompilerHelper.GetIndexedElement(row);

                if (!addedProgIds.ContainsKey(xProgId) && null != clsid && null == xProgId.Parent)
                {
                    if (classesByCLSID.TryGetValue(clsid, out var xClasses))
                    {
                        foreach (var xClass in xClasses)
                        {
                            xClass.Add(xProgId);
                            addedProgIds.Add(xProgId, clsid);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "ProgId", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Class_", row.FieldAsString(2), "Class"));
                    }
                }
            }

            // Check for any progIds that are not hooked up to a class and hook them up to the component specified by the extension
            var componentsById = this.IndexTableOneToMany(tables, "Component");

            foreach (var row in tables["Extension"]?.Rows?.Where(row => row.FieldAsString(2) != null) ?? Enumerable.Empty<Row>())
            {
                var xProgId = this.DecompilerHelper.GetIndexedElement("ProgId", row.FieldAsString(2));

                // Haven't added the progId yet and it doesn't have a parent progId
                if (!addedProgIds.ContainsKey(xProgId) && null == xProgId.Parent)
                {
                    if (componentsById.TryGetValue(row.FieldAsString(1), out var xComponents))
                    {
                        foreach (var xComponent in xComponents)
                        {
                            xComponent.Add(xProgId);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "Extension", row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", row.FieldAsString(1), "Component"));
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
            foreach (var row in tables["CustomAction"]?.Rows ?? Enumerable.Empty<Row>())
            {
                // If no other fields on the property are set we must have created it in the backend.
                var bits = row.FieldAsInteger(1);
                if (WindowsInstallerConstants.MsidbCustomActionTypeHideTarget == (bits & WindowsInstallerConstants.MsidbCustomActionTypeHideTarget)
                    && WindowsInstallerConstants.MsidbCustomActionTypeInScript == (bits & WindowsInstallerConstants.MsidbCustomActionTypeInScript)
                    && this.DecompilerHelper.TryGetIndexedElement("Property", row.FieldAsString(0), out var xProperty)
                    && String.IsNullOrEmpty(xProperty.Attribute("Value")?.Value)
                    && xProperty.Attribute("Secure")?.Value != "yes"
                    && xProperty.Attribute("SuppressModularization")?.Value != "yes")
                {
                    xProperty.Remove();
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
            foreach (var row in tables["RemoveFile"]?.Rows ?? Enumerable.Empty<Row>())
            {
                var xRemove = this.DecompilerHelper.GetIndexedElement(row);
                var property = row.FieldAsString(3);

                if (this.DecompilerHelper.TryGetIndexedElement("Directory", property, out var _))
                {
                    xRemove.SetAttributeValue("Directory", property);
                }
                else
                {
                    xRemove.SetAttributeValue("Property", property);
                }
            }
        }

        /// <summary>
        /// Finalize the LockPermissions or MsiLockPermissionsEx table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <param name="tableName">Which table to finalize.</param>
        /// <remarks>
        /// Nests the Permission elements below their parent elements.  There are no declared foreign
        /// keys for the parents of the LockPermissions table.
        /// </remarks>
        private void FinalizePermissionsTable(TableIndexedCollection tables, string tableName)
        {
            var createFoldersById = this.IndexTableOneToMany(tables, tableName);

            foreach (var row in tables[tableName]?.Rows ?? Enumerable.Empty<Row>())
            {
                var id = row.FieldAsString(0);
                var table = row.FieldAsString(1);
                var xPermission = this.DecompilerHelper.GetIndexedElement(row);

                if ("CreateFolder" == table)
                {
                    if (createFoldersById.TryGetValue(id, out var xCreateFolders))
                    {
                        foreach (var xCreateFolder in xCreateFolders)
                        {
                            xCreateFolder.Add(xPermission);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, tableName, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
                    }
                }
                else
                {
                    if (this.DecompilerHelper.TryGetIndexedElement(table, id, out var xParent))
                    {
                        xParent.Add(xPermission);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, tableName, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "LockObject", id, table));
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
            this.FinalizePermissionsTable(tables, "LockPermissions");
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
            this.FinalizePermissionsTable(tables, "MsiLockPermissionsEx");
        }

        private static Dictionary<string, List<string>> IndexTable(Table table, int keyColumn, int? dataColumn)
        {
            if (table == null)
            {
                return new Dictionary<string, List<string>>();
            }

            return table.Rows
                .ToLookup(row => row.FieldAsString(keyColumn), row => dataColumn.HasValue ? row.FieldAsString(dataColumn.Value) : null)
                .ToDictionary(lookup => lookup.Key, lookup => lookup.ToList());
        }

        private static XElement FindComplianceDrive(XElement xSearch)
        {
            var xComplianceDrive = xSearch.Element(Names.ComplianceDriveElement);
            if (null == xComplianceDrive)
            {
                xComplianceDrive = new XElement(Names.ComplianceDriveElement);
                xSearch.Add(xComplianceDrive);
            }

            return xComplianceDrive;
        }

        /// <summary>
        /// Finalize the search tables.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>Does all the complex linking required for the search tables.</remarks>
        private void FinalizeSearchTables(TableIndexedCollection tables)
        {
            var appSearches = IndexTable(tables["AppSearch"], keyColumn: 1, dataColumn: 0);
            var ccpSearches = IndexTable(tables["CCPSearch"], keyColumn: 0, dataColumn: null);
            var drLocators = tables["DrLocator"]?.Rows.ToDictionary(row => this.DecompilerHelper.GetIndexedElement(row), row => row);

            var xComplianceCheck = new XElement(Names.ComplianceCheckElement);
            if (ccpSearches.Keys.Any(ccpSignature => !appSearches.ContainsKey(ccpSignature)))
            {
                this.DecompilerHelper.AddElementToRoot(xComplianceCheck);
            }

            // index the locator tables by their signatures
            var locators =
                new[] { "CompLocator", "RegLocator", "IniLocator", "DrLocator", "Signature" }
                .SelectMany(table => tables[table]?.Rows ?? Enumerable.Empty<Row>())
                .ToLookup(row => row.FieldAsString(0), row => row)
                .ToDictionary(lookup => lookup.Key, lookup => lookup.ToList());

            // move the DrLocator rows with a parent of CCP_DRIVE first to ensure they get FileSearch children (not FileSearchRef)
            foreach (var locatorRows in locators.Values)
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

            var xUsedSearches = new HashSet<XElement>();
            var xUnusedSearches = new Dictionary<string, XElement>();

            foreach (var signature in locators.Keys)
            {
                var locatorRows = locators[signature];
                var xSignatureSearches = new List<XElement>();

                foreach (var locatorRow in locatorRows)
                {
                    var used = true;
                    var xSearch = this.DecompilerHelper.GetIndexedElement(locatorRow);

                    if ("Signature" == locatorRow.TableDefinition.Name && 0 < xSignatureSearches.Count)
                    {
                        foreach (var xSearchParent in xSignatureSearches)
                        {
                            if (!xUsedSearches.Contains(xSearch))
                            {
                                xSearchParent.Add(xSearch);
                                xUsedSearches.Add(xSearch);
                            }
                            else
                            {
                                var xFileSearchRef = new XElement(Names.FileSearchRefElement,
                                    new XAttribute("Id", signature));

                                xSearchParent.Add(xFileSearchRef);
                            }
                        }
                    }
                    else if ("DrLocator" == locatorRow.TableDefinition.Name && !locatorRow.IsColumnNull(1))
                    {
                        var parentSignature = locatorRow.FieldAsString(1);

                        if ("CCP_DRIVE" == parentSignature)
                        {
                            if (appSearches.ContainsKey(signature)
                                && appSearches.TryGetValue(signature, out var appSearchPropertyIds))
                            {
                                foreach (var propertyId in appSearchPropertyIds)
                                {
                                    var xProperty = this.EnsureProperty(propertyId);

                                    if (ccpSearches.ContainsKey(signature))
                                    {
                                        xProperty.SetAttributeValue("ComplianceCheck", "yes");
                                    }

                                    var xComplianceDrive = FindComplianceDrive(xProperty);

                                    if (!xUsedSearches.Contains(xSearch))
                                    {
                                        xComplianceDrive.Add(xSearch);
                                        xUsedSearches.Add(xSearch);
                                    }
                                    else
                                    {
                                        var directorySearchRef = new XElement(Names.DirectorySearchRefElement,
                                            new XAttribute("Id", signature),
                                            XAttributeIfNotNull("Parent", locatorRow, 1),
                                            XAttributeIfNotNull("Path", locatorRow, 2));

                                        xComplianceDrive.Add(directorySearchRef);
                                        xSignatureSearches.Add(directorySearchRef);
                                    }
                                }
                            }
                            else if (ccpSearches.ContainsKey(signature))
                            {
                                var xComplianceDrive = FindComplianceDrive(xComplianceCheck);

                                if (!xUsedSearches.Contains(xSearch))
                                {
                                    xComplianceDrive.Add(xSearch);
                                    xUsedSearches.Add(xSearch);
                                }
                                else
                                {
                                    var directorySearchRef = new XElement(Names.DirectorySearchRefElement,
                                        new XAttribute("Id", signature),
                                        XAttributeIfNotNull("Parent", locatorRow, 1),
                                        XAttributeIfNotNull("Path", locatorRow, 2));

                                    xComplianceDrive.Add(directorySearchRef);
                                    xSignatureSearches.Add(directorySearchRef);
                                }
                            }
                        }
                        else
                        {
                            var usedDrLocator = false;

                            if (locators.TryGetValue(parentSignature, out var parentLocatorRows))
                            {
                                foreach (var parentLocatorRow in parentLocatorRows)
                                {
                                    if ("DrLocator" == parentLocatorRow.TableDefinition.Name)
                                    {
                                        var xParentSearch = this.DecompilerHelper.GetIndexedElement(parentLocatorRow);

                                        if (xParentSearch.HasElements)
                                        {
                                            var parentDrLocatorRow = drLocators[xParentSearch];
                                            var xDirectorySearchRef = new XElement(Names.DirectorySearchRefElement,
                                                new XAttribute("Id", parentSignature),
                                                XAttributeIfNotNull("Parent", parentDrLocatorRow, 1),
                                                XAttributeIfNotNull("Path", parentDrLocatorRow, 2));

                                            xParentSearch = xDirectorySearchRef;
                                            xUnusedSearches.Add(parentSignature, xDirectorySearchRef);
                                        }

                                        if (!xUsedSearches.Contains(xSearch))
                                        {
                                            xParentSearch.Add(xSearch);
                                            xUsedSearches.Add(xSearch);
                                            usedDrLocator = true;
                                        }
                                        else
                                        {
                                            var xDirectorySearchRef = new XElement(Names.DirectorySearchRefElement,
                                                new XAttribute("Id", signature),
                                                new XAttribute("Parent", parentSignature),
                                                XAttributeIfNotNull("Path", locatorRow, 2));

                                            xParentSearch.Add(xSearch);
                                            usedDrLocator = true;
                                        }
                                    }
                                    else if ("RegLocator" == parentLocatorRow.TableDefinition.Name)
                                    {
                                        var xParentSearch = this.DecompilerHelper.GetIndexedElement(parentLocatorRow);

                                        xParentSearch.Add(xSearch);
                                        xUsedSearches.Add(xSearch);
                                        usedDrLocator = true;
                                    }
                                }

                                // keep track of unused DrLocator rows
                                if (!usedDrLocator)
                                {
                                    xUnusedSearches.Add(xSearch.Attribute("Id").Value, xSearch);
                                }
                            }
                            else
                            {
                                // TODO: warn
                            }
                        }
                    }
                    else if (appSearches.ContainsKey(signature)
                        && appSearches.TryGetValue(signature, out var appSearchPropertyIds))
                    {
                        foreach (var propertyId in appSearchPropertyIds)
                        {
                            var xProperty = this.EnsureProperty(propertyId);

                            if (ccpSearches.ContainsKey(signature))
                            {
                                xProperty.SetAttributeValue("ComplianceCheck", "yes");
                            }

                            if (!xUsedSearches.Contains(xSearch))
                            {
                                xProperty.Add(xSearch);
                                xUsedSearches.Add(xSearch);
                            }
                            else if ("RegLocator" == locatorRow.TableDefinition.Name)
                            {
                                var xRegistrySearchRef = new XElement(Names.RegistrySearchRefElement,
                                    new XAttribute("Id", signature));

                                xProperty.Add(xRegistrySearchRef);
                                xSignatureSearches.Add(xRegistrySearchRef);
                            }
                            else
                            {
                                // TODO: warn about unavailable Ref element
                            }
                        }
                    }
                    else if (ccpSearches.ContainsKey(signature))
                    {
                        if (!xUsedSearches.Contains(xSearch))
                        {
                            xComplianceCheck.Add(xSearch);
                            xUsedSearches.Add(xSearch);
                        }
                        else if ("RegLocator" == locatorRow.TableDefinition.Name)
                        {
                            var xRegistrySearchRef = new XElement(Names.RegistrySearchRefElement,
                                new XAttribute("Id", signature));

                            xComplianceCheck.Add(xRegistrySearchRef);
                            xSignatureSearches.Add(xRegistrySearchRef);
                        }
                        else
                        {
                            // TODO: warn about unavailable Ref element
                        }
                    }
                    else
                    {
                        if (xSearch.Name.LocalName == "DirectorySearch" || xSearch.Name.LocalName == "RegistrySearch")
                        {
                            xUnusedSearches.Add(xSearch.Attribute("Id").Value, xSearch);
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
                        xSignatureSearches.Add(xSearch);
                    }
                }
            }

            // Iterate through the unused elements through a sorted list of their ids so the output is deterministic.
            foreach (var unusedSearch in xUnusedSearches.OrderBy(kvp => kvp.Key))
            {
                var used = false;

                XElement xLeafDirectorySearch = null;
                var xUnusedSearch = unusedSearch.Value;
                var xParent = xUnusedSearch;
                var updatedLeaf = true;
                while (updatedLeaf)
                {
                    updatedLeaf = false;

                    var xDirectorySearch = xParent.Element(Names.DirectorySearchElement);
                    if (xDirectorySearch != null)
                    {
                        xParent = xLeafDirectorySearch = xDirectorySearch;
                        updatedLeaf = true;
                    }
                }

                if (xLeafDirectorySearch != null)
                {
                    var leafDirectorySearchId = xLeafDirectorySearch.Attribute("Id").Value;
                    if (appSearches.TryGetValue(leafDirectorySearchId, out var appSearchPropertyIds))
                    {
                        var xProperty = this.EnsureProperty(appSearchPropertyIds[0]);
                        xProperty.Add(xUnusedSearch);
                        used = true;
                    }
                    else if (ccpSearches.ContainsKey(leafDirectorySearchId))
                    {
                        xComplianceCheck.Add(xUnusedSearch);
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
                var xShortcut = this.DecompilerHelper.GetIndexedElement(row);

                var target = row.FieldAsString(4);

                if (this.DecompilerHelper.TryGetIndexedElement("Feature", target, out var _))
                {
                    xShortcut.SetAttributeValue("Advertise", "yes");
                    this.SetPrimaryFeature(row, 4, 3);
                }
                else
                {
                    // TODO: use this value to do a "more-correct" nesting under the indicated File or CreateDirectory element
                    xShortcut.SetAttributeValue("Target", target);
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
            if (OutputType.Package == this.OutputType)
            {
                foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
                {
                    var sequenceTableName = sequenceTable.WindowsInstallerTableName();

                    // if suppressing UI elements, skip UI-related sequence tables
                    if (this.SuppressUI && ("AdminUISequence" == sequenceTableName || "InstallUISequence" == sequenceTableName))
                    {
                        continue;
                    }

                    var table = tables[sequenceTableName];

                    if (null != table)
                    {
                        var actionSymbols = new List<WixActionSymbol>();
                        var needAbsoluteScheduling = this.SuppressRelativeActionSequencing;
                        var nonSequencedActionRows = new Dictionary<string, WixActionSymbol>();
                        var suppressedRelativeActionRows = new Dictionary<string, WixActionSymbol>();

                        // create a sorted array of actions in this table
                        foreach (var row in table.Rows)
                        {
                            var action = row.FieldAsString(0);
                            var actionSymbol = new WixActionSymbol(null, new Identifier(AccessModifier.Global, sequenceTable, action));

                            actionSymbol.Action = action;

                            if (!row.IsColumnNull(1))
                            {
                                actionSymbol.Condition = row.FieldAsString(1);
                            }

                            actionSymbol.Sequence = row.FieldAsInteger(2);

                            actionSymbol.SequenceTable = sequenceTable;

                            actionSymbols.Add(actionSymbol);
                        }
                        actionSymbols = actionSymbols.OrderBy(t => t.Sequence).ToList();

                        for (var i = 0; i < actionSymbols.Count && !needAbsoluteScheduling; i++)
                        {
                            var actionSymbol = actionSymbols[i];
                            this.StandardActions.TryGetValue(actionSymbol.Id.Id, out var standardActionRow);

                            // create actions for custom actions, dialogs, AppSearch when its moved, and standard actions with non-standard conditions
                            if ("AppSearch" == actionSymbol.Action || null == standardActionRow || actionSymbol.Condition != standardActionRow.Condition)
                            {
                                WixActionSymbol previousActionSymbol = null;
                                WixActionSymbol nextActionSymbol = null;

                                // find the previous action row if there is one
                                if (0 <= i - 1)
                                {
                                    previousActionSymbol = actionSymbols[i - 1];
                                }

                                // find the next action row if there is one
                                if (actionSymbols.Count > i + 1)
                                {
                                    nextActionSymbol = actionSymbols[i + 1];
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
                                if ((null != previousActionSymbol && actionSymbol.Sequence == previousActionSymbol.Sequence) || (null != nextActionSymbol && actionSymbol.Sequence == nextActionSymbol.Sequence))
                                {
                                    needAbsoluteScheduling = true;
                                }
                                else if (null != nextActionSymbol && this.StandardActions.ContainsKey(nextActionSymbol.Id.Id) && actionSymbol.Sequence + 1 == nextActionSymbol.Sequence)
                                {
                                    actionSymbol.Before = nextActionSymbol.Action;
                                }
                                else if (null != previousActionSymbol && this.StandardActions.ContainsKey(previousActionSymbol.Id.Id) && actionSymbol.Sequence - 1 == previousActionSymbol.Sequence)
                                {
                                    actionSymbol.After = previousActionSymbol.Action;
                                }
                                else if (null == standardActionRow && null != previousActionSymbol && actionSymbol.Sequence - 1 == previousActionSymbol.Sequence && previousActionSymbol.Before != actionSymbol.Action)
                                {
                                    actionSymbol.After = previousActionSymbol.Action;
                                }
                                else if (null == standardActionRow && null != previousActionSymbol && actionSymbol.Sequence != previousActionSymbol.Sequence && null != nextActionSymbol && actionSymbol.Sequence + 1 == nextActionSymbol.Sequence)
                                {
                                    actionSymbol.Before = nextActionSymbol.Action;
                                }
                                else if ("AppSearch" == actionSymbol.Action && null != standardActionRow && actionSymbol.Sequence == standardActionRow.Sequence && actionSymbol.Condition == standardActionRow.Condition)
                                {
                                    // ignore an AppSearch row which has the WiX standard sequence and a standard condition
                                }
                                else if (null != standardActionRow && actionSymbol.Condition != standardActionRow.Condition) // standard actions get their standard sequence numbers
                                {
                                    nonSequencedActionRows.Add(actionSymbol.Id.Id, actionSymbol);
                                }
                                else if (0 < actionSymbol.Sequence)
                                {
                                    needAbsoluteScheduling = true;
                                }
                            }
                            else
                            {
                                suppressedRelativeActionRows.Add(actionSymbol.Id.Id, actionSymbol);
                            }
                        }

                        // create the actions now that we know if they must be absolutely or relatively scheduled
                        foreach (var actionRow in actionSymbols)
                        {
                            var key = actionRow.Id.Id;

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
            else if (OutputType.Module == this.OutputType) // finalize the Module sequence tables
            {
                foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
                {
                    var sequenceTableName = sequenceTable.WindowsInstallerTableName();

                    // if suppressing UI elements, skip UI-related sequence tables
                    if (this.SuppressUI && ("AdminUISequence" == sequenceTableName || "InstallUISequence" == sequenceTableName))
                    {
                        continue;
                    }

                    var table = tables[String.Concat("Module", sequenceTableName)];

                    if (null != table)
                    {
                        foreach (var row in table.Rows)
                        {
                            var actionRow = new WixActionSymbol(null, new Identifier(AccessModifier.Global, sequenceTable, row.FieldAsString(0)));

                            actionRow.Action = row.FieldAsString(0);

                            if (!row.IsColumnNull(1))
                            {
                                actionRow.Sequence = row.FieldAsInteger(1);
                            }

                            if (!row.IsColumnNull(2) && !row.IsColumnNull(3))
                            {
                                switch (row.FieldAsInteger(3))
                                {
                                    case 0:
                                        actionRow.Before = row.FieldAsString(2);
                                        break;
                                    case 1:
                                        actionRow.After = row.FieldAsString(2);
                                        break;
                                    default:
                                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[3].Column.Name, row[3]));
                                        break;
                                }
                            }

                            if (!row.IsColumnNull(4))
                            {
                                actionRow.Condition = row.FieldAsString(4);
                            }

                            actionRow.SequenceTable = sequenceTable;

                            // create action elements for non-standard actions
                            if (!this.StandardActions.ContainsKey(actionRow.Id.Id) || null != actionRow.After || null != actionRow.Before)
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

            // find the DowngradePreventedCondition launch condition message
            if (null != launchConditionTable && 0 < launchConditionTable.Rows.Count)
            {
                foreach (var launchRow in launchConditionTable.Rows)
                {
                    if (WixUpgradeConstants.DowngradePreventedCondition == Convert.ToString(launchRow[0]))
                    {
                        downgradeErrorMessage = Convert.ToString(launchRow[1]);
                    }
                    else if (WixUpgradeConstants.UpgradePreventedCondition == Convert.ToString(launchRow[0]))
                    {
                        disallowUpgradeErrorMessage = Convert.ToString(launchRow[1]);
                    }
                }
            }

            if (null != upgradeTable && 0 < upgradeTable.Rows.Count)
            {
                XElement xMajorUpgrade = null;

                foreach (UpgradeRow upgradeRow in upgradeTable.Rows)
                {
                    if (WixUpgradeConstants.UpgradeDetectedProperty == upgradeRow.ActionProperty)
                    {
                        var attr = upgradeRow.Attributes;
                        var removeFeatures = upgradeRow.Remove;
                        xMajorUpgrade = xMajorUpgrade ?? new XElement(Names.MajorUpgradeElement);

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive == (attr & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive))
                        {
                            xMajorUpgrade.SetAttributeValue("AllowSameVersionUpgrades", "yes");
                        }

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures != (attr & WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures))
                        {
                            xMajorUpgrade.SetAttributeValue("MigrateFeatures", "no");
                        }

                        if (WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure == (attr & WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure))
                        {
                            xMajorUpgrade.SetAttributeValue("IgnoreRemoveFailure", "yes");
                        }

                        if (!String.IsNullOrEmpty(removeFeatures))
                        {
                            xMajorUpgrade.SetAttributeValue("RemoveFeatures", removeFeatures);
                        }
                    }
                    else if (WixUpgradeConstants.DowngradeDetectedProperty == upgradeRow.ActionProperty)
                    {
                        xMajorUpgrade = xMajorUpgrade ?? new XElement(Names.MajorUpgradeElement);
                        xMajorUpgrade.SetAttributeValue("DowngradeErrorMessage", downgradeErrorMessage);
                    }
                }

                if (xMajorUpgrade != null)
                {
                    if (String.IsNullOrEmpty(downgradeErrorMessage))
                    {
                        xMajorUpgrade.SetAttributeValue("AllowDowngrades", "yes");
                    }

                    if (!String.IsNullOrEmpty(disallowUpgradeErrorMessage))
                    {
                        xMajorUpgrade.SetAttributeValue("Disallow", "yes");
                        xMajorUpgrade.SetAttributeValue("DisallowUpgradeErrorMessage", disallowUpgradeErrorMessage);
                    }

                    var scheduledType = DetermineMajorUpgradeScheduling(tables);
                    if (scheduledType != "afterInstallValidate")
                    {
                        xMajorUpgrade.SetAttributeValue("Schedule", scheduledType);
                    }

                    this.DecompilerHelper.AddElementToRoot(xMajorUpgrade);
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
            var xExtensions = this.IndexTableOneToMany(tables["Extension"]);

            var verbTable = tables["Verb"];
            if (null != verbTable)
            {
                foreach (var row in verbTable.Rows)
                {
                    if (xExtensions.TryGetValue(row.FieldAsString(0), out var xVerbExtensions))
                    {
                        var xVerb = this.DecompilerHelper.GetIndexedElement(row);

                        foreach (var xVerbExtension in xVerbExtensions)
                        {
                            xVerbExtension.Add(xVerb);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, verbTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Extension_", row.FieldAsString(0), "Extension"));
                    }
                }
            }
        }

        /// <summary>
        /// Get the path to a file in the source image.
        /// </summary>
        /// <param name="xFile">The file.</param>
        /// <returns>The path to the file in the source image.</returns>
        private string GetSourcePath(XElement xFile)
        {
            var sourcePath = new StringBuilder();

            var component = xFile.Parent;
            var xDirectory = component.Parent;

            while (xDirectory?.Name.LocalName == "Directory")
            {
                string name;

                var dirSourceName = xDirectory.Attribute("SourceName")?.Value;
                var dirShortSourceName = xDirectory.Attribute("ShortSourceName")?.Value;
                var dirShortName = xDirectory.Attribute("ShortName")?.Value;
                var dirName = xDirectory.Attribute("Name")?.Value;

                if (!this.ShortNames && null != dirSourceName)
                {
                    name = dirSourceName;
                }
                else if (null != dirShortSourceName)
                {
                    name = dirShortSourceName;
                }
                else if (!this.ShortNames || null == dirShortName)
                {
                    name = dirName;
                }
                else
                {
                    name = dirShortName;
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

                xDirectory = xDirectory.Parent;
            }

            if (xDirectory?.Name.LocalName == "StandardDirectory" && WindowsInstallerStandard.TryGetStandardDirectory(xDirectory.Attribute("Id").Value, out var standardDirectory))
            {
                sourcePath.Insert(0, Path.DirectorySeparatorChar);
                sourcePath.Insert(0, standardDirectory.Name);
            }

            return sourcePath.ToString();
        }

        /// <summary>
        /// Resolve the dependencies for a table (this is a helper method for GetSortedTableNames).
        /// </summary>
        /// <param name="tableName">The name of the table to resolve.</param>
        /// <param name="unsortedTableNames">The unsorted table names.</param>
        /// <param name="sortedTableNames">The sorted table names.</param>
        private void ResolveTableDependencies(string tableName, List<string> unsortedTableNames, HashSet<string> sortedTableNames)
        {
            unsortedTableNames.Remove(tableName);

            foreach (var columnDefinition in this.TableDefinitions[tableName].Columns)
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
                    else if (!this.TableDefinitions.Contains(keyTable))
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
        private HashSet<string> GetOrderedTableNames()
        {
            var orderedTableNames = new HashSet<string>();
            var unsortedTableNames = new List<string>(this.TableDefinitions.Select(t => t.Name));

            // resolve the dependencies for each table
            while (0 < unsortedTableNames.Count)
            {
                this.ResolveTableDependencies(unsortedTableNames[0], unsortedTableNames, orderedTableNames);
            }

            return orderedTableNames;
        }

        /// <summary>
        /// Initialize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <param name="codepage"></param>
        private void InitializeDecompile(TableIndexedCollection tables, int codepage)
        {
            // reset all the state information
            this.Compressed = false;
            this.ShortNames = false;

            this.Singletons.Clear();
            //this.IndexedElements.Clear();
            this.PatchTargetFiles.Clear();

            // set the codepage if its not neutral (0)
            if (0 != codepage)
            {
                this.DecompilerHelper.RootElement.SetAttributeValue("Codepage", codepage);
            }

            if (this.OutputType == OutputType.Module)
            {
                var table = tables["_SummaryInformation"];
                var row = table.Rows.SingleOrDefault(r => r.FieldAsInteger(0) == 9);
                this.ModularizationGuid = row?.FieldAsString(1);
                this.DecompilerHelper.RootElement.SetAttributeValue("Guid", this.ModularizationGuid);
            }

            foreach (var extension in this.Extensions)
            {
                extension.PreDecompileTables(tables);
            }

            this.RemoveExtensionDataFromTables(tables);
        }

        private void RemoveExtensionDataFromTables(TableIndexedCollection tables)
        {
            var tableDefinitionBySymbolDefinitionName = this.TableDefinitions.Where(t => t.SymbolDefinition != null).ToDictionary(t => t.SymbolDefinition.Name);

            // index the rows from the extension libraries
            var indexedExtensionTables = new Dictionary<string, HashSet<string>>();
            foreach (var extension in this.ExtensionData)
            {
                // Get the optional library from the extension with the rows to be removed.
                var library = extension.GetLibrary(this.SymbolDefinitionCreator);
                if (library != null)
                {
                    foreach (var symbol in library.Sections.SelectMany(s => s.Symbols))
                    {
                        if (this.TryGetPrimaryKeyFromSymbol(tableDefinitionBySymbolDefinitionName, symbol, out var tableName, out var primaryKey))
                        {
                            //// the Actions table needs to be handled specially
                            //if (table.Name == "WixAction")
                            //{
                            //    primaryKey = symbol.FieldAsString(1);
                            //    tableName = symbol.FieldAsString(0);

                            //    if (this.outputType == OutputType.Module)
                            //    {
                            //        tableName = "Module" + tableName;
                            //    }
                            //}
                            //else
                            //{
                            //    primaryKey = symbol.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter);
                            //    tableName = table.Name;
                            //}

                            if (!indexedExtensionTables.TryGetValue(tableName, out var indexedExtensionRows))
                            {
                                indexedExtensionRows = new HashSet<string>();
                                indexedExtensionTables.Add(tableName, indexedExtensionRows);
                            }

                            indexedExtensionRows.Add(primaryKey);
                        }
                    }
                }
            }

            // remove the rows from the extension libraries (to allow full round-tripping)
            foreach (var kvp in indexedExtensionTables)
            {
                var tableName = kvp.Key;
                var indexedExtensionRows = kvp.Value;

                if (tables.TryGetTable(tableName, out var table))
                {
                    var originalRows = new RowDictionary<Row>(table);

                    // remove the original rows so that they can be added back if they should remain
                    table.Rows.Clear();

                    foreach (var row in originalRows.Values)
                    {
                        if (!indexedExtensionRows.Contains(row.GetPrimaryKey()))
                        {
                            table.Rows.Add(row);
                        }
                    }
                }
            }
        }

        private bool TryGetPrimaryKeyFromSymbol(Dictionary<string, TableDefinition> tableDefinitionBySymbolDefinitionName, IntermediateSymbol symbol, out string tableName, out string primaryKey)
        {
            tableName = null;
            primaryKey = null;

            if (symbol is WixActionSymbol actionSymbol)
            {
                tableName = actionSymbol.SequenceTable.WindowsInstallerTableName();
                primaryKey = actionSymbol.Action;
                return true;
            }

            if (!tableDefinitionBySymbolDefinitionName.TryGetValue(symbol.Definition.Name, out var tableDefinition))
            {
                return false;
            }

            tableName = tableDefinition.Name;

            if (tableDefinition.SymbolIdIsPrimaryKey)
            {
                primaryKey = symbol.Id.Id;
            }
            else
            {
                var sb = new StringBuilder();

                for (var i = 0; i < symbol.Fields.Length && i < tableDefinition.Columns.Length; ++i)
                {
                    var column = tableDefinition.Columns[i];
                    var field = symbol.Fields[i];

                    if (column.PrimaryKey)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append('/');
                        }

                        sb.Append(field.AsString());
                    }
                }

                primaryKey = sb.ToString();
            }

            return true;
        }

        /// <summary>
        /// Decompile the tables.
        /// </summary>
        /// <param name="output">The output being decompiled.</param>
        private void DecompileTables(WindowsInstallerData output)
        {
            var orderedTableNames = this.GetOrderedTableNames();
            foreach (var tableName in orderedTableNames)
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
                    this.DecompilerHelper.AddElementToRoot(new XElement(Names.EnsureTableElement, new XAttribute("Id", table.Name)));
                }

                switch (table.Name)
                {
                    case "_SummaryInformation":
                        // handled in FinalizeDecompile
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
                        if (this.ExtensionsByTableName.TryGetValue(table.Name, out var extension))
                        {
                            extension.TryDecompileTable(table);
                        }
                        else if (!this.SuppressCustomTables)
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
        /// <param name="tables">The tables to decompile.</param>
        private void FinalizeSummaryInformationStream(TableIndexedCollection tables)
        {
            var table = tables["_SummaryInformation"];

            if (OutputType.Module == this.OutputType || OutputType.Package == this.OutputType)
            {
                var xSummaryInformation = new XElement(Names.SummaryInformationElement);

                foreach (var row in table.Rows)
                {
                    var value = row.FieldAsString(1);

                    if (!String.IsNullOrEmpty(value))
                    {
                        switch (row.FieldAsInteger(0))
                        {
                            case 1:
                                if ("1252" != value)
                                {
                                    xSummaryInformation.SetAttributeValue("Codepage", value);
                                }
                                break;
                            case 3:
                            {
                                var productName = this.DecompilerHelper.RootElement.Attribute("Name")?.Value;
                                if (value != productName)
                                {
                                    xSummaryInformation.SetAttributeValue("Description", value);
                                }
                                break;
                            }
                            case 4:
                            {
                                var productManufacturer = this.DecompilerHelper.RootElement.Attribute("Manufacturer")?.Value;
                                if (value != productManufacturer)
                                {
                                    xSummaryInformation.SetAttributeValue("Manufacturer", value);
                                }
                                break;
                            }
                            case 5:
                                if ("Installer" != value)
                                {
                                    xSummaryInformation.SetAttributeValue("Keywords", value);
                                }
                                break;
                            case 7:
                                var template = value.Split(';');
                                if (0 < template.Length && 0 < template[template.Length - 1].Length)
                                {
                                    this.DecompilerHelper.RootElement.SetAttributeValue("Language", template[template.Length - 1]);
                                }
                                break;
                            case 14:
                                var installerVersion = row.FieldAsInteger(1);
                                // Default InstallerVersion.
                                if (installerVersion != 500)
                                {
                                    this.DecompilerHelper.RootElement.SetAttributeValue("InstallerVersion", installerVersion);
                                }
                                break;
                            case 15:
                                var wordCount = row.FieldAsInteger(1);
                                if (0x1 == (wordCount & 0x1))
                                {
                                    this.ShortNames = true;
                                    if (OutputType.Package == this.OutputType)
                                    {
                                        this.DecompilerHelper.RootElement.SetAttributeValue("ShortNames", "yes");
                                    }
                                }

                                if (0x2 == (wordCount & 0x2))
                                {
                                    this.Compressed = true;
                                }

                                if (OutputType.Package == this.OutputType)
                                {
                                    if (0x8 == (wordCount & 0x8))
                                    {
                                        this.DecompilerHelper.RootElement.SetAttributeValue("Scope", "perUser");
                                    }
                                    else
                                    {
                                        var xAllUsers = this.DecompilerHelper.RootElement.Elements(Names.PropertyElement).SingleOrDefault(p => p.Attribute("Id")?.Value == "ALLUSERS");
                                        if (xAllUsers?.Attribute("Value")?.Value == "1")
                                        {
                                            xAllUsers?.Remove();
                                        }
                                    }
                                }

                                break;
                        }
                    }
                }

                if (OutputType.Package == this.OutputType && !this.Compressed)
                {
                    this.DecompilerHelper.RootElement.SetAttributeValue("Compressed", "no");
                }

                if (xSummaryInformation.HasAttributes)
                {
                    this.DecompilerHelper.AddElementToRoot(xSummaryInformation);
                }
            }
            else
            {
                var xPatchInformation = new XElement(Names.PatchInformationElement);

                foreach (var row in table.Rows)
                {
                    var propertyId = row.FieldAsInteger(0);
                    var value = row.FieldAsString(1);

                    if (!String.IsNullOrEmpty(value))
                    {
                        switch (propertyId)
                        {
                            case 1:
                                if ("1252" != value)
                                {
                                    xPatchInformation.SetAttributeValue("SummaryCodepage", value);
                                }
                                break;
                            case 3:
                                xPatchInformation.SetAttributeValue("Description", value);
                                break;
                            case 4:
                                xPatchInformation.SetAttributeValue("Manufacturer", value);
                                break;
                            case 5:
                                if ("Installer,Patching,PCP,Database" != value)
                                {
                                    xPatchInformation.SetAttributeValue("Keywords", value);
                                }
                                break;
                            case 6:
                                xPatchInformation.SetAttributeValue("Comments", value);
                                break;
                            case 19:
                                var security = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                                switch (security)
                                {
                                    case 0:
                                        xPatchInformation.SetAttributeValue("ReadOnly", "no");
                                        break;
                                    case 4:
                                        xPatchInformation.SetAttributeValue("ReadOnly", "yes");
                                        break;
                                }
                                break;
                        }
                    }
                }

                this.DecompilerHelper.AddElementToRoot(xPatchInformation);
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
                var progressText = new XElement(Names.ProgressTextElement,
                    new XAttribute("Action", row.FieldAsString(0)),
                    row.IsColumnNull(1) ? null : new XAttribute("Message", row.FieldAsString(1)),
                    row.IsColumnNull(2) ? null : new XAttribute("Template", row.FieldAsString(2)));

                this.UIElement.Add(progressText);
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
                var appId = new XElement(Names.AppIdElement,
                    new XAttribute("Advertise", "yes"),
                    new XAttribute("Id", row.FieldAsString(0)),
                    row.IsColumnNull(1) ? null : new XAttribute("RemoteServerName", row.FieldAsString(1)),
                    row.IsColumnNull(2) ? null : new XAttribute("LocalService", row.FieldAsString(2)),
                    row.IsColumnNull(3) ? null : new XAttribute("ServiceParameters", row.FieldAsString(3)),
                    row.IsColumnNull(4) ? null : new XAttribute("DllSurrogate", row.FieldAsString(4)),
                    row.IsColumnNull(5) || row.FieldAsInteger(5) != 1 ? null : new XAttribute("ActivateAtStorage", "yes"),
                    row.IsColumnNull(6) || row.FieldAsInteger(6) != 1 ? null : new XAttribute("RunAsInteractiveUser", "yes"));

                this.DecompilerHelper.AddElementToRoot(appId);
                this.DecompilerHelper.IndexElement(row, appId);
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
                var xControl = new XElement(Names.ControlElement,
                    new XAttribute("Id", bbControlRow.BBControl),
                    new XAttribute("Type", bbControlRow.Type),
                    new XAttribute("X", bbControlRow.X),
                    new XAttribute("Y", bbControlRow.Y),
                    new XAttribute("Width", bbControlRow.Width),
                    new XAttribute("Height", bbControlRow.Height),
                    null == bbControlRow.Text ? null : new XAttribute("Text", bbControlRow.Text));

                if (null != bbControlRow[7])
                {
                    SetControlAttributes(bbControlRow.Attributes, xControl);
                }

                if (this.DecompilerHelper.TryGetIndexedElement("Billboard", bbControlRow.Billboard, out var xBillboard))
                {
                    xBillboard.Add(xControl);
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
            var billboards = new SortedList<string, Row>();

            foreach (var row in table.Rows)
            {
                var xBillboard = new XElement(Names.BillboardElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Feature", row.FieldAsString(1)));

                this.DecompilerHelper.IndexElement(row, xBillboard);
                billboards.Add(String.Format(CultureInfo.InvariantCulture, "{0}|{1:0000000000}", row[0], row[3]), row);
            }

            var billboardActions = new Dictionary<string, XElement>();

            foreach (var row in billboards.Values)
            {
                var xBillboard = this.DecompilerHelper.GetIndexedElement(row);

                if (!billboardActions.TryGetValue(row.FieldAsString(2), out var xBillboardAction))
                {
                    xBillboardAction = new XElement(Names.BillboardActionElement,
                        new XAttribute("Id", row.FieldAsString(2)));

                    this.UIElement.Add(xBillboardAction);
                    billboardActions.Add(row.FieldAsString(2), xBillboardAction);
                }

                xBillboardAction.Add(xBillboard);
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
                var xBinary = new XElement(Names.BinaryElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("SourceFile", row.FieldAsString(1)));

                this.DecompilerHelper.AddElementToRoot(xBinary);
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
                if (this.DecompilerHelper.TryGetIndexedElement("File", row.FieldAsString(0), out var xFile))
                {
                    xFile.SetAttributeValue("BindPath", row.FieldAsString(1));
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", row.FieldAsString(0), "File"));
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
                var xClass = new XElement(Names.ClassElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Advertise", "yes"),
                    new XAttribute("Context", row.FieldAsString(1)),
                    row.IsColumnNull(4) ? null : new XAttribute("Description", row.FieldAsString(4)),
                    row.IsColumnNull(5) ? null : new XAttribute("AppId", row.FieldAsString(5)),
                    row.IsColumnNull(7) ? null : new XAttribute("Icon", row.FieldAsString(7)),
                    row.IsColumnNull(8) ? null : new XAttribute("IconIndex", row.FieldAsString(8)),
                    row.IsColumnNull(9) ? null : new XAttribute("Handler", row.FieldAsString(9)),
                    row.IsColumnNull(10) ? null : new XAttribute("Argument", row.FieldAsString(10)));

                if (!row.IsColumnNull(6))
                {
                    var fileTypeMaskStrings = row.FieldAsString(6).Split(';');

                    try
                    {
                        foreach (var fileTypeMaskString in fileTypeMaskStrings)
                        {
                            var fileTypeMaskParts = fileTypeMaskString.Split(',');

                            if (4 == fileTypeMaskParts.Length)
                            {
                                var xFileTypeMask = new XElement(Names.FileTypeMaskElement,
                                    new XAttribute("Offset", Convert.ToInt32(fileTypeMaskParts[0], CultureInfo.InvariantCulture)),
                                    new XAttribute("Mask", fileTypeMaskParts[2]),
                                    new XAttribute("Value", fileTypeMaskParts[3]));

                                xClass.Add(xFileTypeMask);
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

                if (!row.IsColumnNull(12))
                {
                    if (1 == row.FieldAsInteger(12))
                    {
                        xClass.SetAttributeValue("RelativePath", "yes");
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[12].Column.Name, row[12]));
                    }
                }

                this.AddChildToParent("Component", xClass, row, 2);
                this.DecompilerHelper.IndexElement(row, xClass);
            }
        }

        /// <summary>
        /// Decompile the ComboBox table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileComboBoxTable(Table table)
        {
            // sort the combo boxes by their property and order
            var comboBoxRows = table.Rows.Select(row => row).OrderBy(row => String.Format("{0}|{1:0000000000}", row.FieldAsString(0), row.FieldAsInteger(1)));

            XElement xComboBox = null;
            string property = null;
            foreach (var row in comboBoxRows)
            {
                if (null == xComboBox || row.FieldAsString(0) != property)
                {
                    property = row.FieldAsString(0);

                    xComboBox = new XElement(Names.ComboBoxElement,
                        new XAttribute("Property", property));

                    this.UIElement.Add(xComboBox);
                }

                var xListItem = new XElement(Names.ListItemElement,
                        new XAttribute("Value", row.FieldAsString(2)),
                        row.IsColumnNull(3) ? null : new XAttribute("Text", row.FieldAsString(3)));
                xComboBox.Add(xListItem);
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
                var xControl = new XElement(Names.ControlElement,
                    new XAttribute("Id", controlRow.Control),
                    new XAttribute("Type", controlRow.Type),
                    new XAttribute("X", controlRow.X),
                    new XAttribute("Y", controlRow.Y),
                    new XAttribute("Width", controlRow.Width),
                    new XAttribute("Height", controlRow.Height),
                    XAttributeIfNotNull("Text", controlRow.Text));

                if (!controlRow.IsColumnNull(7))
                {
                    string[] specialAttributes;

                    // sets various common attributes like Disabled, Indirect, Integer, ...
                    SetControlAttributes(controlRow.Attributes, xControl);

                    switch (controlRow.Type)
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
                                        xControl.SetAttributeValue("Bitmap", "yes");
                                        break;
                                    case "CDROM":
                                        xControl.SetAttributeValue("CDROM", "yes");
                                        break;
                                    case "ComboList":
                                        xControl.SetAttributeValue("ComboList", "yes");
                                        break;
                                    case "ElevationShield":
                                        xControl.SetAttributeValue("ElevationShield", "yes");
                                        break;
                                    case "Fixed":
                                        xControl.SetAttributeValue("Fixed", "yes");
                                        break;
                                    case "FixedSize":
                                        xControl.SetAttributeValue("FixedSize", "yes");
                                        break;
                                    case "Floppy":
                                        xControl.SetAttributeValue("Floppy", "yes");
                                        break;
                                    case "FormatSize":
                                        xControl.SetAttributeValue("FormatSize", "yes");
                                        break;
                                    case "HasBorder":
                                        xControl.SetAttributeValue("HasBorder", "yes");
                                        break;
                                    case "Icon":
                                        xControl.SetAttributeValue("Icon", "yes");
                                        break;
                                    case "Icon16":
                                        if (iconSizeSet)
                                        {
                                            xControl.SetAttributeValue("IconSize", "48");
                                        }
                                        else
                                        {
                                            iconSizeSet = true;
                                            xControl.SetAttributeValue("IconSize", "16");
                                        }
                                        break;
                                    case "Icon32":
                                        if (iconSizeSet)
                                        {
                                            xControl.SetAttributeValue("IconSize", "48");
                                        }
                                        else
                                        {
                                            iconSizeSet = true;
                                            xControl.SetAttributeValue("IconSize", "32");
                                        }
                                        break;
                                    case "Image":
                                        xControl.SetAttributeValue("Image", "yes");
                                        break;
                                    case "Multiline":
                                        xControl.SetAttributeValue("Multiline", "yes");
                                        break;
                                    case "NoPrefix":
                                        xControl.SetAttributeValue("NoPrefix", "yes");
                                        break;
                                    case "NoWrap":
                                        xControl.SetAttributeValue("NoWrap", "yes");
                                        break;
                                    case "Password":
                                        xControl.SetAttributeValue("Password", "yes");
                                        break;
                                    case "ProgressBlocks":
                                        xControl.SetAttributeValue("ProgressBlocks", "yes");
                                        break;
                                    case "PushLike":
                                        xControl.SetAttributeValue("PushLike", "yes");
                                        break;
                                    case "RAMDisk":
                                        xControl.SetAttributeValue("RAMDisk", "yes");
                                        break;
                                    case "Remote":
                                        xControl.SetAttributeValue("Remote", "yes");
                                        break;
                                    case "Removable":
                                        xControl.SetAttributeValue("Removable", "yes");
                                        break;
                                    case "ShowRollbackCost":
                                        xControl.SetAttributeValue("ShowRollbackCost", "yes");
                                        break;
                                    case "Sorted":
                                        xControl.SetAttributeValue("Sorted", "yes");
                                        break;
                                    case "Transparent":
                                        xControl.SetAttributeValue("Transparent", "yes");
                                        break;
                                    case "UserLanguage":
                                        xControl.SetAttributeValue("UserLanguage", "yes");
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
                if (null != controlRow.Property && 0 != String.CompareOrdinal("CheckBox", controlRow.Type))
                {
                    xControl.SetAttributeValue("Property", controlRow.Property);
                }

                if (null != controlRow.Help)
                {
                    var help = controlRow.Help.Split('|');

                    if (2 == help.Length)
                    {
                        if (0 < help[0].Length)
                        {
                            xControl.SetAttributeValue("ToolTip", help[0]);
                        }

                        if (0 < help[1].Length)
                        {
                            xControl.SetAttributeValue("Help", help[1]);
                        }
                    }
                }

                this.DecompilerHelper.IndexElement(controlRow, xControl);
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
                if (this.DecompilerHelper.TryGetIndexedElement("Control", row.FieldAsString(0), row.FieldAsString(1), out var xControl))
                {
                    switch (row.FieldAsString(2))
                    {
                        case "Default":
                            xControl.SetAttributeValue("DefaultCondition", row.FieldAsString(3));
                            break;
                        case "Disable":
                            xControl.SetAttributeValue("DisableCondition", row.FieldAsString(3));
                            break;
                        case "Enable":
                            xControl.SetAttributeValue("EnableCondition", row.FieldAsString(3));
                            break;
                        case "Hide":
                            xControl.SetAttributeValue("HideCondition", row.FieldAsString(3));
                            break;
                        case "Show":
                            xControl.SetAttributeValue("ShowCondition", row.FieldAsString(3));
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                            break;
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", row.FieldAsString(0), "Control_", row.FieldAsString(1), "Control"));
                }
            }
        }

        /// <summary>
        /// Decompile the ControlEvent table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileControlEventTable(Table table)
        {
            var controlEvents = new SortedList<string, Row>();

            foreach (var row in table.Rows)
            {
                var xPublish = new XElement(Names.PublishElement);
                var condition = row.FieldAsString(4);

                if (!String.IsNullOrEmpty(condition) && condition != "1")
                {
                    xPublish.Add(new XAttribute("Condition", condition));
                }

                var publishEvent = row.FieldAsString(2);
                if (publishEvent.StartsWith("[", StringComparison.Ordinal) && publishEvent.EndsWith("]", StringComparison.Ordinal))
                {
                    xPublish.SetAttributeValue("Property", publishEvent.Substring(1, publishEvent.Length - 2));

                    if ("{}" != row.FieldAsString(3))
                    {
                        xPublish.SetAttributeValue("Value", row.FieldAsString(3));
                    }
                }
                else
                {
                    xPublish.SetAttributeValue("Event", publishEvent);
                    xPublish.SetAttributeValue("Value", row.FieldAsString(3));
                }

                controlEvents.Add(String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2:0000000000}|{3}|{4}|{5}", row.FieldAsString(0), row.FieldAsString(1), row.FieldAsNullableInteger(5) ?? 0, row.FieldAsString(2), row.FieldAsString(3), row.FieldAsString(4)), row);

                this.DecompilerHelper.IndexElement(row, xPublish);
            }

            foreach (var row in controlEvents.Values)
            {
                if (this.DecompilerHelper.TryGetIndexedElement("Control", row.FieldAsString(0), row.FieldAsString(1), out var xControl))
                {
                    var xPublish = this.DecompilerHelper.GetIndexedElement(row);
                    xControl.Add(xPublish);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", row.FieldAsString(0), "Control_", row.FieldAsString(1), "Control"));
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
                this.Messaging.Write(WarningMessages.DecompilingAsCustomTable(table.Rows[0].SourceLineNumbers, table.Name));

                var xCustomTable = new XElement(Names.CustomTableElement,
                    new XAttribute("Id", table.Name));

                foreach (var columnDefinition in table.Definition.Columns)
                {
                    var xColumn = new XElement(Names.ColumnElement,
                        new XAttribute("Id", columnDefinition.Name),
                        columnDefinition.Description == null ? null : new XAttribute("Description", columnDefinition.Description),
                        columnDefinition.KeyTable == null ? null : new XAttribute("KeyTable", columnDefinition.KeyTable),
                        !columnDefinition.KeyColumn.HasValue ? null : new XAttribute("KeyColumn", columnDefinition.KeyColumn.Value),
                        !columnDefinition.IsLocalizable ? null : new XAttribute("Localizable", "yes"),
                        !columnDefinition.MaxValue.HasValue ? null : new XAttribute("MaxValue", columnDefinition.MaxValue.Value),
                        !columnDefinition.MinValue.HasValue ? null : new XAttribute("MinValue", columnDefinition.MinValue.Value),
                        !columnDefinition.Nullable ? null : new XAttribute("Nullable", "yes"),
                        !columnDefinition.PrimaryKey ? null : new XAttribute("PrimaryKey", "yes"),
                        columnDefinition.Possibilities == null ? null : new XAttribute("Possibilities", "yes"),
                        new XAttribute("Width", columnDefinition.Length));

                    if (ColumnCategory.Unknown != columnDefinition.Category)
                    {
                        switch (columnDefinition.Category)
                        {
                            case ColumnCategory.Text:
                                xColumn.SetAttributeValue("Category", "text");
                                break;
                            case ColumnCategory.UpperCase:
                                xColumn.SetAttributeValue("Category", "upperCase");
                                break;
                            case ColumnCategory.LowerCase:
                                xColumn.SetAttributeValue("Category", "lowerCase");
                                break;
                            case ColumnCategory.Integer:
                                xColumn.SetAttributeValue("Category", "integer");
                                break;
                            case ColumnCategory.DoubleInteger:
                                xColumn.SetAttributeValue("Category", "doubleInteger");
                                break;
                            case ColumnCategory.TimeDate:
                                xColumn.SetAttributeValue("Category", "timeDate");
                                break;
                            case ColumnCategory.Identifier:
                                xColumn.SetAttributeValue("Category", "identifier");
                                break;
                            case ColumnCategory.Property:
                                xColumn.SetAttributeValue("Category", "property");
                                break;
                            case ColumnCategory.Filename:
                                xColumn.SetAttributeValue("Category", "filename");
                                break;
                            case ColumnCategory.WildCardFilename:
                                xColumn.SetAttributeValue("Category", "wildCardFilename");
                                break;
                            case ColumnCategory.Path:
                                xColumn.SetAttributeValue("Category", "path");
                                break;
                            case ColumnCategory.Paths:
                                xColumn.SetAttributeValue("Category", "paths");
                                break;
                            case ColumnCategory.AnyPath:
                                xColumn.SetAttributeValue("Category", "anyPath");
                                break;
                            case ColumnCategory.DefaultDir:
                                xColumn.SetAttributeValue("Category", "defaultDir");
                                break;
                            case ColumnCategory.RegPath:
                                xColumn.SetAttributeValue("Category", "regPath");
                                break;
                            case ColumnCategory.Formatted:
                                xColumn.SetAttributeValue("Category", "formatted");
                                break;
                            case ColumnCategory.FormattedSDDLText:
                                xColumn.SetAttributeValue("Category", "formattedSddl");
                                break;
                            case ColumnCategory.Template:
                                xColumn.SetAttributeValue("Category", "template");
                                break;
                            case ColumnCategory.Condition:
                                xColumn.SetAttributeValue("Category", "condition");
                                break;
                            case ColumnCategory.Guid:
                                xColumn.SetAttributeValue("Category", "guid");
                                break;
                            case ColumnCategory.Version:
                                xColumn.SetAttributeValue("Category", "version");
                                break;
                            case ColumnCategory.Language:
                                xColumn.SetAttributeValue("Category", "language");
                                break;
                            case ColumnCategory.Binary:
                                xColumn.SetAttributeValue("Category", "binary");
                                break;
                            case ColumnCategory.CustomSource:
                                xColumn.SetAttributeValue("Category", "customSource");
                                break;
                            case ColumnCategory.Cabinet:
                                xColumn.SetAttributeValue("Category", "cabinet");
                                break;
                            case ColumnCategory.Shortcut:
                                xColumn.SetAttributeValue("Category", "shortcut");
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown custom column category '{columnDefinition.Category.ToString()}'.");
                        }
                    }

                    if (ColumnModularizeType.None != columnDefinition.ModularizeType)
                    {
                        switch (columnDefinition.ModularizeType)
                        {
                            case ColumnModularizeType.Column:
                                xColumn.SetAttributeValue("Modularize", "Column");
                                break;
                            case ColumnModularizeType.Condition:
                                xColumn.SetAttributeValue("Modularize", "Condition");
                                break;
                            case ColumnModularizeType.Icon:
                                xColumn.SetAttributeValue("Modularize", "Icon");
                                break;
                            case ColumnModularizeType.Property:
                                xColumn.SetAttributeValue("Modularize", "Property");
                                break;
                            case ColumnModularizeType.SemicolonDelimited:
                                xColumn.SetAttributeValue("Modularize", "SemicolonDelimited");
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown custom column modularization type '{columnDefinition.ModularizeType.ToString()}'.");
                        }
                    }

                    if (ColumnType.Unknown != columnDefinition.Type)
                    {
                        switch (columnDefinition.Type)
                        {
                            case ColumnType.Localized:
                                xColumn.SetAttributeValue("Localizable", "yes");
                                xColumn.SetAttributeValue("Type", "string");
                                break;
                            case ColumnType.Number:
                                xColumn.SetAttributeValue("Type", "int");
                                break;
                            case ColumnType.Object:
                                xColumn.SetAttributeValue("Type", "binary");
                                break;
                            case ColumnType.Preserved:
                            case ColumnType.String:
                                xColumn.SetAttributeValue("Type", "string");
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown custom column type '{columnDefinition.Type}'.");
                        }
                    }

                    xCustomTable.Add(xColumn);
                }

                foreach (var row in table.Rows)
                {
                    var xRow = new XElement(Names.RowElement);

                    foreach (var field in row.Fields.Where(f => f.Data != null))
                    {
                        var xData = new XElement(Names.DataElement,
                            new XAttribute("Column", field.Column.Name),
                            new XAttribute("Value", field.AsString()));

                        xRow.Add(xData);
                    }

                    xCustomTable.Add(xRow);
                }

                this.DecompilerHelper.AddElementToRoot(xCustomTable);
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
                var xCreateFolder = new XElement(Names.CreateFolderElement,
                    new XAttribute("Directory", row.FieldAsString(0)));

                this.AddChildToParent("Component", xCreateFolder, row, 1);
                this.DecompilerHelper.IndexElement(row, xCreateFolder);
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
                var xCustomAction = new XElement(Names.CustomActionElement,
                    new XAttribute("Id", row.FieldAsString(0)));

                var type = row.FieldAsInteger(1);

                if (WindowsInstallerConstants.MsidbCustomActionTypeHideTarget == (type & WindowsInstallerConstants.MsidbCustomActionTypeHideTarget))
                {
                    xCustomAction.SetAttributeValue("HideTarget", "yes");
                }

                if (WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate == (type & WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate))
                {
                    xCustomAction.SetAttributeValue("Impersonate", "no");
                }

                if (WindowsInstallerConstants.MsidbCustomActionTypeTSAware == (type & WindowsInstallerConstants.MsidbCustomActionTypeTSAware))
                {
                    xCustomAction.SetAttributeValue("TerminalServerAware", "yes");
                }

                if (WindowsInstallerConstants.MsidbCustomActionType64BitScript == (type & WindowsInstallerConstants.MsidbCustomActionType64BitScript))
                {
                    xCustomAction.SetAttributeValue("Bitness", "always64");
                }
                else if (WindowsInstallerConstants.MsidbCustomActionTypeVBScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeVBScript) ||
                    WindowsInstallerConstants.MsidbCustomActionTypeJScript == (type & WindowsInstallerConstants.MsidbCustomActionTypeJScript))
                {
                    xCustomAction.SetAttributeValue("Bitness", "always32");
                }

                switch (type & WindowsInstallerConstants.MsidbCustomActionTypeExecuteBits)
                {
                    case 0:
                        // this is the default value
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence:
                        xCustomAction.SetAttributeValue("Execute", "firstSequence");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess:
                        xCustomAction.SetAttributeValue("Execute", "oncePerProcess");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat:
                        xCustomAction.SetAttributeValue("Execute", "secondSequence");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeInScript:
                        xCustomAction.SetAttributeValue("Execute", "deferred");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeInScript + WindowsInstallerConstants.MsidbCustomActionTypeRollback:
                        xCustomAction.SetAttributeValue("Execute", "rollback");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeInScript + WindowsInstallerConstants.MsidbCustomActionTypeCommit:
                        xCustomAction.SetAttributeValue("Execute", "commit");
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
                        xCustomAction.SetAttributeValue("Return", "ignore");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeAsync:
                        xCustomAction.SetAttributeValue("Return", "asyncWait");
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeAsync + WindowsInstallerConstants.MsidbCustomActionTypeContinue:
                        xCustomAction.SetAttributeValue("Return", "asyncNoWait");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        break;
                }

                var source = type & WindowsInstallerConstants.MsidbCustomActionTypeSourceBits;
                switch (source)
                {
                    case WindowsInstallerConstants.MsidbCustomActionTypeBinaryData:
                        xCustomAction.SetAttributeValue("BinaryRef", row.FieldAsString(2));
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeSourceFile:
                        if (!row.IsColumnNull(2))
                        {
                            xCustomAction.SetAttributeValue("FileRef", row.FieldAsString(2));
                        }
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeDirectory:
                        if (!row.IsColumnNull(2))
                        {
                            xCustomAction.SetAttributeValue("Directory", row.FieldAsString(2));
                        }
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeProperty:
                        xCustomAction.SetAttributeValue("Property", row.FieldAsString(2));
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        break;
                }

                switch (type & WindowsInstallerConstants.MsidbCustomActionTypeTargetBits)
                {
                    case WindowsInstallerConstants.MsidbCustomActionTypeDll:
                        xCustomAction.SetAttributeValue("DllEntry", row.FieldAsString(3));
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeExe:
                        xCustomAction.SetAttributeValue("ExeCommand", row.FieldAsString(3));
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeTextData:
                        if (WindowsInstallerConstants.MsidbCustomActionTypeSourceFile == source)
                        {
                            xCustomAction.SetAttributeValue("Error", row.FieldAsString(3));
                        }
                        else
                        {
                            xCustomAction.SetAttributeValue("Value", row.FieldAsString(3));
                        }
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeJScript:
                        if (WindowsInstallerConstants.MsidbCustomActionTypeDirectory == source)
                        {
                            xCustomAction.SetAttributeValue("Script", "jscript");
                            // TODO: Extract to @ScriptFile?
                            // xCustomAction.Content = row.FieldAsString(3);
                        }
                        else
                        {
                            xCustomAction.SetAttributeValue("JScriptCall", row.FieldAsString(3));
                        }
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeVBScript:
                        if (WindowsInstallerConstants.MsidbCustomActionTypeDirectory == source)
                        {
                            xCustomAction.SetAttributeValue("Script", "vbscript");
                            // TODO: Extract to @ScriptFile?
                            // xCustomAction.Content = row.FieldAsString(3);
                        }
                        else
                        {
                            xCustomAction.SetAttributeValue("VBScriptCall", row.FieldAsString(3));
                        }
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeInstall:
                        this.Messaging.Write(WarningMessages.NestedInstall(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        continue;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        break;
                }

                var extype = 4 < row.Fields.Length && !row.IsColumnNull(4) ? row.FieldAsInteger(4) : 0;
                if (WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall == (extype & WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall))
                {
                    xCustomAction.SetAttributeValue("PatchUninstall", "yes");
                }

                this.DecompilerHelper.AddElementToRoot(xCustomAction);
                this.DecompilerHelper.IndexElement(row, xCustomAction);
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
                var xComponentSearch = new XElement(Names.ComponentSearchElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Guid", row.FieldAsString(1)));

                if (!row.IsColumnNull(2))
                {
                    switch (row.FieldAsInteger(2))
                    {
                        case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                            xComponentSearch.SetAttributeValue("Type", "directory");
                            break;
                        case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                            // this is the default value
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                            break;
                    }
                }

                this.DecompilerHelper.IndexElement(row, xComponentSearch);
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
                if (!row.IsColumnNull(1))
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(0), out var xComponent))
                    {
                        xComponent.SetAttributeValue("ComPlusFlags", row.FieldAsInteger(1));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", row.FieldAsString(0), "Component"));
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
                var xComponent = new XElement(Names.ComponentElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Guid", row.FieldAsString(1) ?? String.Empty));

                var attributes = row.FieldAsInteger(3);

                if (WindowsInstallerConstants.MsidbComponentAttributesSourceOnly == (attributes & WindowsInstallerConstants.MsidbComponentAttributesSourceOnly))
                {
                    xComponent.SetAttributeValue("Location", "source");
                }
                else if (WindowsInstallerConstants.MsidbComponentAttributesOptional == (attributes & WindowsInstallerConstants.MsidbComponentAttributesOptional))
                {
                    xComponent.SetAttributeValue("Location", "either");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount == (attributes & WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount))
                {
                    xComponent.SetAttributeValue("SharedDllRefCount", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesPermanent == (attributes & WindowsInstallerConstants.MsidbComponentAttributesPermanent))
                {
                    xComponent.SetAttributeValue("Permanent", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesTransitive == (attributes & WindowsInstallerConstants.MsidbComponentAttributesTransitive))
                {
                    xComponent.SetAttributeValue("Transitive", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite == (attributes & WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite))
                {
                    xComponent.SetAttributeValue("NeverOverwrite", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributes64bit == (attributes & WindowsInstallerConstants.MsidbComponentAttributes64bit))
                {
                    xComponent.SetAttributeValue("Bitness", "always64");
                }
                else
                {
                    xComponent.SetAttributeValue("Bitness", "always32");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection == (attributes & WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection))
                {
                    xComponent.SetAttributeValue("DisableRegistryReflection", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence == (attributes & WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence))
                {
                    xComponent.SetAttributeValue("UninstallWhenSuperseded", "yes");
                }

                if (WindowsInstallerConstants.MsidbComponentAttributesShared == (attributes & WindowsInstallerConstants.MsidbComponentAttributesShared))
                {
                    xComponent.SetAttributeValue("Shared", "yes");
                }

                if (!row.IsColumnNull(4))
                {
                    xComponent.SetAttributeValue("Condition", row.FieldAsString(4));
                }

                this.AddChildToParent("Directory", xComponent, row, 2);
                this.DecompilerHelper.IndexElement(row, xComponent);
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
                if (this.DecompilerHelper.TryGetIndexedElement("Feature", row.FieldAsString(0), out var xFeature))
                {
                    var xLevel = new XElement(Names.LevelElement,
                        row.IsColumnNull(2) ? null : new XAttribute("Condition", row.FieldAsString(2)),
                        new XAttribute("Level", row.FieldAsInteger(1)));

                    xFeature.Add(xLevel);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Feature_", row.FieldAsString(0), "Feature"));
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
                var attributes = row.FieldAsNullableInteger(5) ?? 0;

                var xDialog = new XElement(Names.DialogElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("X", row.FieldAsString(1)),
                    new XAttribute("Y", row.FieldAsString(2)),
                    new XAttribute("Width", row.FieldAsString(3)),
                    new XAttribute("Height", row.FieldAsString(4)),
                    0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesVisible) ? new XAttribute("Hidden", "yes") : null,
                    0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesModal) ? new XAttribute("Modeless", "yes") : null,
                    0 == (attributes & WindowsInstallerConstants.MsidbDialogAttributesMinimize) ? new XAttribute("NoMinimize", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesSysModal == (attributes & WindowsInstallerConstants.MsidbDialogAttributesSysModal) ? new XAttribute("SystemModal", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesKeepModeless == (attributes & WindowsInstallerConstants.MsidbDialogAttributesKeepModeless) ? new XAttribute("KeepModeless", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace == (attributes & WindowsInstallerConstants.MsidbDialogAttributesTrackDiskSpace) ? new XAttribute("TrackDiskSpace", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette == (attributes & WindowsInstallerConstants.MsidbDialogAttributesUseCustomPalette) ? new XAttribute("CustomPalette", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesLeftScroll == (attributes & WindowsInstallerConstants.MsidbDialogAttributesLeftScroll) ? new XAttribute("LeftScroll", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesError == (attributes & WindowsInstallerConstants.MsidbDialogAttributesError) ? new XAttribute("ErrorDialog", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesRightAligned == (attributes & WindowsInstallerConstants.MsidbDialogAttributesRightAligned) ? new XAttribute("RightAligned", "yes") : null,
                    WindowsInstallerConstants.MsidbDialogAttributesRTLRO == (attributes & WindowsInstallerConstants.MsidbDialogAttributesRTLRO) ? new XAttribute("RightToLeft", "yes") : null,
                    !row.IsColumnNull(6) ? new XAttribute("Title", row.FieldAsString(6)) : null);

                this.UIElement.Add(xDialog);
                this.DecompilerHelper.IndexElement(row, xDialog);
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
                var id = row.FieldAsString(0);
                var elementName = WindowsInstallerStandard.IsStandardDirectory(id) ? Names.StandardDirectoryElement : Names.DirectoryElement;
                var xDirectory = new XElement(elementName,
                    new XAttribute("Id", id));

                if (!WindowsInstallerStandard.IsStandardDirectory(id))
                {
                    var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(2));

                    if (id == "TARGETDIR" && names[0] != "SourceDir")
                    {
                        this.Messaging.Write(WarningMessages.TargetDirCorrectedDefaultDir());
                        xDirectory.SetAttributeValue("Name", "SourceDir");
                    }
                    else
                    {
                        if (null != names[0] && "." != names[0])
                        {
                            if (null != names[1])
                            {
                                xDirectory.SetAttributeValue("ShortName", names[0]);
                            }
                            else
                            {
                                xDirectory.SetAttributeValue("Name", names[0]);
                            }
                        }

                        if (null != names[1])
                        {
                            xDirectory.SetAttributeValue("Name", names[1]);
                        }
                    }

                    if (null != names[2])
                    {
                        if (null != names[3])
                        {
                            xDirectory.SetAttributeValue("ShortSourceName", names[2]);
                        }
                        else
                        {
                            xDirectory.SetAttributeValue("SourceName", names[2]);
                        }
                    }

                    if (null != names[3])
                    {
                        xDirectory.SetAttributeValue("SourceName", names[3]);
                    }
                }

                this.DecompilerHelper.IndexElement(row, xDirectory);
            }

            // nest the directories
            foreach (var row in table.Rows)
            {
                var xDirectory = this.DecompilerHelper.GetIndexedElement(row);

                var id = row.FieldAsString(0);

                if (id == "TARGETDIR")
                {
                    // Skip TARGETDIR -- but it will be added for any components directly targeted.
                }
                else if (row.IsColumnNull(1) || WindowsInstallerStandard.IsStandardDirectory(id))
                {
                    this.DecompilerHelper.AddElementToRoot(xDirectory);
                }
                else
                {
                    var parentDirectoryId = row.FieldAsString(1);

                    if (!this.DecompilerHelper.TryGetIndexedElement("Directory", parentDirectoryId, out var xParentDirectory))
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Directory_Parent", row.FieldAsString(1), "Directory"));
                    }
                    else if (xParentDirectory == xDirectory) // another way to specify a root directory
                    {
                        this.DecompilerHelper.AddElementToRoot(xDirectory);
                    }
                    else
                    {
                        // TARGETDIR is omitted but if this directory is a first-generation descendant, add it as a root.
                        if (parentDirectoryId == "TARGETDIR")
                        {
                            this.DecompilerHelper.AddElementToRoot(xDirectory);
                        }
                        else
                        {
                            xParentDirectory.Add(xDirectory);
                        }
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
                var xDirectorySearch = new XElement(Names.DirectorySearchElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    XAttributeIfNotNull("Path", row, 2),
                    XAttributeIfNotNull("Depth", row, 3));

                this.DecompilerHelper.IndexElement(row, xDirectorySearch);
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
                var xCopyFile = new XElement(Names.CopyFileElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("FileId", row.FieldAsString(2)));

                if (!row.IsColumnNull(3))
                {
                    var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(3));
                    if (null != names[0] && null != names[1])
                    {
                        xCopyFile.SetAttributeValue("DestinationShortName", names[0]);
                        xCopyFile.SetAttributeValue("DestinationName", names[1]);
                    }
                    else if (null != names[0])
                    {
                        xCopyFile.SetAttributeValue("DestinationName", names[0]);
                    }
                }

                // destination directory/property is set in FinalizeDuplicateMoveFileTables

                this.AddChildToParent("Component", xCopyFile, row, 1);
                this.DecompilerHelper.IndexElement(row, xCopyFile);
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
                var xEnvironment = new XElement(Names.EnvironmentElement,
                    new XAttribute("Id", row.FieldAsString(0)));

                var done = false;
                var permanent = true;
                var name = row.FieldAsString(1);
                for (var i = 0; i < name.Length && !done; i++)
                {
                    switch (name[i])
                    {
                        case '=':
                            xEnvironment.SetAttributeValue("Action", "set");
                            break;
                        case '+':
                            xEnvironment.SetAttributeValue("Action", "create");
                            break;
                        case '-':
                            permanent = false;
                            break;
                        case '!':
                            xEnvironment.SetAttributeValue("Action", "remove");
                            break;
                        case '*':
                            xEnvironment.SetAttributeValue("System", "yes");
                            break;
                        default:
                            xEnvironment.SetAttributeValue("Name", name.Substring(i));
                            done = true;
                            break;
                    }
                }

                if (permanent)
                {
                    xEnvironment.SetAttributeValue("Permanent", "yes");
                }

                if (!row.IsColumnNull(2))
                {
                    var value = row.FieldAsString(2);

                    if (value.StartsWith("[~]", StringComparison.Ordinal))
                    {
                        xEnvironment.SetAttributeValue("Part", "last");

                        if (3 < value.Length)
                        {
                            xEnvironment.SetAttributeValue("Separator", value.Substring(3, 1));
                            xEnvironment.SetAttributeValue("Value", value.Substring(4));
                        }
                    }
                    else if (value.EndsWith("[~]", StringComparison.Ordinal))
                    {
                        xEnvironment.SetAttributeValue("Part", "first");

                        if (3 < value.Length)
                        {
                            xEnvironment.SetAttributeValue("Separator", value.Substring(value.Length - 4, 1));
                            xEnvironment.SetAttributeValue("Value", value.Substring(0, value.Length - 4));
                        }
                    }
                    else
                    {
                        xEnvironment.SetAttributeValue("Value", value);
                    }
                }

                this.AddChildToParent("Component", xEnvironment, row, 3);
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
                var xError = new XElement(Names.ErrorElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Message", row.FieldAsString(1)));

                this.UIElement.Add(xError);
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
                var xSubscribe = new XElement(Names.SubscribeElement,
                    new XAttribute("Event", row.FieldAsString(2)),
                    new XAttribute("Attribute", row.FieldAsString(3)));

                if (this.DecompilerHelper.TryGetIndexedElement("Control", row.FieldAsString(0), row.FieldAsString(1), out var xControl))
                {
                    xControl.Add(xSubscribe);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Dialog_", row.FieldAsString(0), "Control_", row.FieldAsString(1), "Control"));
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
                var xExtension = new XElement(Names.ExtensionElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Advertise", "yes"));

                if (!row.IsColumnNull(3))
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("MIME", row.FieldAsString(3), out var xMime))
                    {
                        xMime.SetAttributeValue("Default", "yes");
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "MIME_", row.FieldAsString(3), "MIME"));
                    }
                }

                if (!row.IsColumnNull(2))
                {
                    this.AddChildToParent("ProgId", xExtension, row, 2);
                }
                else
                {
                    this.AddChildToParent("Component", xExtension, row, 1);
                }

                this.DecompilerHelper.IndexElement(row, xExtension);
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
                var xExternalFile = new XElement(Names.ExternalFileElement,
                    new XAttribute("File", row.FieldAsString(1)),
                    new XAttribute("Source", row.FieldAsString(2)));

                AddSymbolPaths(row, 3, xExternalFile);

                if (!row.IsColumnNull(4) && !row.IsColumnNull(5))
                {
                    var ignoreOffsets = row.FieldAsString(4).Split(',');
                    var ignoreLengths = row.FieldAsString(5).Split(',');

                    if (ignoreOffsets.Length == ignoreLengths.Length)
                    {
                        for (var i = 0; i < ignoreOffsets.Length; i++)
                        {
                            var xIgnoreRange = new XElement(Names.IgnoreRangeElement);

                            if (ignoreOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                xIgnoreRange.SetAttributeValue("Offset", Convert.ToInt32(ignoreOffsets[i].Substring(2), 16));
                            }
                            else
                            {
                                xIgnoreRange.SetAttributeValue("Offset", Convert.ToInt32(ignoreOffsets[i], CultureInfo.InvariantCulture));
                            }

                            if (ignoreLengths[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                xIgnoreRange.SetAttributeValue("Length", Convert.ToInt32(ignoreLengths[i].Substring(2), 16));
                            }
                            else
                            {
                                xIgnoreRange.SetAttributeValue("Length", Convert.ToInt32(ignoreLengths[i], CultureInfo.InvariantCulture));
                            }

                            xExternalFile.Add(xIgnoreRange);
                        }
                    }
                    else
                    {
                        // TODO: warn
                    }
                }
                else if (!row.IsColumnNull(4) || !row.IsColumnNull(5))
                {
                    // TODO: warn about mismatch between columns
                }

                // the RetainOffsets column is handled in FinalizeFamilyFileRangesTable

                if (!row.IsColumnNull(7))
                {
                    xExternalFile.SetAttributeValue("Order", row.FieldAsInteger(7));
                }

                this.AddChildToParent("ImageFamilies", xExternalFile, row, 0);
                this.DecompilerHelper.IndexElement(row, xExternalFile);
            }
        }

        /// <summary>
        /// Decompile the Feature table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFeatureTable(Table table)
        {
            var sortedFeatures = new SortedList<string, Row>();

            foreach (var row in table.Rows)
            {
                var feature = new XElement(Names.FeatureElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    row.IsColumnNull(2) ? null : new XAttribute("Title", row.FieldAsString(2)),
                    row.IsColumnNull(3) ? null : new XAttribute("Description", row.FieldAsString(3)),
                    new XAttribute("Level", row.FieldAsInteger(5)),
                    row.IsColumnNull(6) ? null : new XAttribute("ConfigurableDirectory", row.FieldAsString(6)));

                if (row.IsColumnNull(4))
                {
                    feature.SetAttributeValue("Display", "hidden");
                }
                else
                {
                    var display = row.FieldAsInteger(4);

                    if (0 == display)
                    {
                        feature.SetAttributeValue("Display", "hidden");
                    }
                    else if (1 == display % 2)
                    {
                        feature.SetAttributeValue("Display", "expand");
                    }
                }

                var attributes = row.FieldAsInteger(7);

                if (WindowsInstallerConstants.MsidbFeatureAttributesFavorSource == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorSource) && WindowsInstallerConstants.MsidbFeatureAttributesFollowParent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFollowParent))
                {
                    // TODO: display a warning for setting favor local and follow parent together
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesFavorSource == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorSource))
                {
                    feature.SetAttributeValue("InstallDefault", "source");
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesFollowParent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFollowParent))
                {
                    feature.SetAttributeValue("InstallDefault", "followParent");
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise))
                {
                    feature.SetAttributeValue("InstallDefault", "advertise");
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise) &&
                    WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise))
                {
                    this.Messaging.Write(WarningMessages.InvalidAttributeCombination(row.SourceLineNumbers, "msidbFeatureAttributesDisallowAdvertise", "msidbFeatureAttributesNoUnsupportedAdvertise", "Feature.AllowAdvertiseType", "no"));
                    feature.SetAttributeValue("AllowAdvertise", "no");
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise))
                {
                    feature.SetAttributeValue("AllowAdvertise", "no");
                }
                else if (WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesNoUnsupportedAdvertise))
                {
                    feature.SetAttributeValue("AllowAdvertise", "system");
                }

                if (WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent == (attributes & WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent))
                {
                    feature.SetAttributeValue("Absent", "disallow");
                }

                this.DecompilerHelper.IndexElement(row, feature);

                // sort the features by their display column (and append the identifier to ensure unique keys)
                sortedFeatures.Add(String.Format(CultureInfo.InvariantCulture, "{0:00000}|{1}", row.FieldAsInteger(4), row[0]), row);
            }

            // nest the features
            foreach (var row in sortedFeatures.Values)
            {
                var xFeature = this.DecompilerHelper.GetIndexedElement("Feature", row.FieldAsString(0));

                if (row.IsColumnNull(1))
                {
                    this.DecompilerHelper.AddElementToRoot(xFeature);
                }
                else
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Feature", row.FieldAsString(1), out var xParentFeature))
                    {
                        if (xParentFeature == xFeature)
                        {
                            // TODO: display a warning about self-nesting
                        }
                        else
                        {
                            xParentFeature.Add(xFeature);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Feature_Parent", row.FieldAsString(1), "Feature"));
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
                var xComponentRef = new XElement(Names.ComponentRefElement,
                    new XAttribute("Id", row.FieldAsString(1)));

                this.AddChildToParent("Feature", xComponentRef, row, 0);
                this.DecompilerHelper.IndexElement(row, xComponentRef);
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
                var xFile = new XElement(Names.FileElement,
                    new XAttribute("Id", fileRow.File),
                    WindowsInstallerConstants.MsidbFileAttributesReadOnly == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesReadOnly) ? new XAttribute("ReadOnly", "yes") : null,
                    WindowsInstallerConstants.MsidbFileAttributesHidden == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesHidden) ? new XAttribute("Hidden", "yes") : null,
                    WindowsInstallerConstants.MsidbFileAttributesSystem == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesSystem) ? new XAttribute("System", "yes") : null,
                    WindowsInstallerConstants.MsidbFileAttributesChecksum == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesChecksum) ? new XAttribute("Checksum", "yes") : null,
                    WindowsInstallerConstants.MsidbFileAttributesVital != (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesVital) ? new XAttribute("Vital", "no") : null,
                    null != fileRow.Version && 0 < fileRow.Version.Length && !Char.IsDigit(fileRow.Version[0]) ? new XAttribute("CompanionFile", fileRow.Version) : null);

                var names = this.BackendHelper.SplitMsiFileName(fileRow.FileName);
                if (null != names[0] && null != names[1])
                {
                    xFile.SetAttributeValue("ShortName", names[0]);
                    xFile.SetAttributeValue("Name", names[1]);
                }
                else if (null != names[0])
                {
                    xFile.SetAttributeValue("Name", names[0]);
                }

                if (WindowsInstallerConstants.MsidbFileAttributesNoncompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) &&
                    WindowsInstallerConstants.MsidbFileAttributesCompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed))
                {
                    // TODO: error
                }
                else if (WindowsInstallerConstants.MsidbFileAttributesNoncompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed))
                {
                    xFile.SetAttributeValue("Compressed", "no");
                }
                else if (WindowsInstallerConstants.MsidbFileAttributesCompressed == (fileRow.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed))
                {
                    xFile.SetAttributeValue("Compressed", "yes");
                }

                this.DecompilerHelper.IndexElement(fileRow, xFile);
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
                var xSfpFile = new XElement(Names.SFPFileElement,
                    new XAttribute("Id", row.FieldAsString(0)));

                this.AddChildToParent("SFPCatalog", xSfpFile, row, 1);
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
                if (this.DecompilerHelper.TryGetIndexedElement("File", row.FieldAsString(0), out var xFile))
                {
                    if (!row.IsColumnNull(1))
                    {
                        xFile.SetAttributeValue("FontTitle", row.FieldAsString(1));
                    }
                    else
                    {
                        xFile.SetAttributeValue("TrueType", "yes");
                    }
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", row.FieldAsString(0), "File"));
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
                var icon = new XElement(Names.IconElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("SourceFile", row.FieldAsString(1)));

                this.DecompilerHelper.AddElementToRoot(icon);
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
                var family = new XElement(Names.FamilyElement,
                    new XAttribute("Name", row.FieldAsString(0)),
                    row.IsColumnNull(1) ? null : new XAttribute("MediaSrcProp", row.FieldAsString(1)),
                    row.IsColumnNull(2) ? null : new XAttribute("DiskId", row.FieldAsString(2)),
                    row.IsColumnNull(3) ? null : new XAttribute("SequenceStart", row.FieldAsString(3)),
                    row.IsColumnNull(4) ? null : new XAttribute("DiskPrompt", row.FieldAsString(4)),
                    row.IsColumnNull(5) ? null : new XAttribute("VolumeLabel", row.FieldAsString(5)));

                this.DecompilerHelper.AddElementToRoot(family);
                this.DecompilerHelper.IndexElement(row, family);
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
                var xIniFile = new XElement(Names.IniFileElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Section", row.FieldAsString(3)),
                    new XAttribute("Key", row.FieldAsString(4)),
                    new XAttribute("Value", row.FieldAsString(5)),
                    row.IsColumnNull(2) ? null : new XAttribute("Directory", row.FieldAsString(2)));

                var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(1));

                if (null != names[0])
                {
                    if (null == names[1])
                    {
                        xIniFile.SetAttributeValue("Name", names[0]);
                    }
                    else
                    {
                        xIniFile.SetAttributeValue("ShortName", names[0]);
                    }
                }

                if (null != names[1])
                {
                    xIniFile.SetAttributeValue("Name", names[1]);
                }

                switch (row.FieldAsInteger(6))
                {
                    case WindowsInstallerConstants.MsidbIniFileActionAddLine:
                        xIniFile.SetAttributeValue("Action", "addLine");
                        break;
                    case WindowsInstallerConstants.MsidbIniFileActionCreateLine:
                        xIniFile.SetAttributeValue("Action", "createLine");
                        break;
                    case WindowsInstallerConstants.MsidbIniFileActionAddTag:
                        xIniFile.SetAttributeValue("Action", "addTag");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                        break;
                }

                this.AddChildToParent("Component", xIniFile, row, 7);
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
                var xIniFileSearch = new XElement(Names.IniFileSearchElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Section", row.FieldAsString(2)),
                    new XAttribute("Key", row.FieldAsString(3)),
                    row.IsColumnNull(4) || row.FieldAsInteger(4) == 0 ? null : new XAttribute("Field", row.FieldAsInteger(4)));

                var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(1));
                if (null != names[0] && null != names[1])
                {
                    xIniFileSearch.SetAttributeValue("ShortName", names[0]);
                    xIniFileSearch.SetAttributeValue("Name", names[1]);
                }
                else if (null != names[0])
                {
                    xIniFileSearch.SetAttributeValue("Name", names[0]);
                }

                if (!row.IsColumnNull(5))
                {
                    switch (row.FieldAsInteger(5))
                    {
                        case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                            xIniFileSearch.SetAttributeValue("Type", "directory");
                            break;
                        case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                            // this is the default value
                            break;
                        case WindowsInstallerConstants.MsidbLocatorTypeRawValue:
                            xIniFileSearch.SetAttributeValue("Type", "raw");
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[5].Column.Name, row[5]));
                            break;
                    }
                }

                this.DecompilerHelper.IndexElement(row, xIniFileSearch);
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
                var xIsolateComponent = new XElement(Names.IsolateComponentElement,
                    new XAttribute("Shared", row.FieldAsString(0)));

                this.AddChildToParent("Component", xIsolateComponent, row, 1);
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
                if (WixUpgradeConstants.DowngradePreventedCondition == row.FieldAsString(0) || WixUpgradeConstants.UpgradePreventedCondition == row.FieldAsString(0))
                {
                    continue; // MajorUpgrade rows processed in FinalizeUpgradeTable
                }

                var condition = new XElement(Names.LaunchElement,
                    new XAttribute("Condition", row.FieldAsString(0)),
                    new XAttribute("Message", row.FieldAsString(1)));

                this.DecompilerHelper.AddElementToRoot(condition);
            }
        }

        /// <summary>
        /// Decompile the ListBox table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileListBoxTable(Table table)
        {
            // sort the list boxes by their property and order
            var listBoxRows = table.Rows.OrderBy(row => row.FieldAsString(0)).ThenBy(row => row.FieldAsInteger(1)).ToList();

            XElement xListBox = null;
            foreach (Row row in listBoxRows)
            {
                if (null == xListBox || row.FieldAsString(0) != xListBox.Attribute("Property")?.Value)
                {
                    xListBox = new XElement(Names.ListBoxElement,
                        new XAttribute("Property", row.FieldAsString(0)));

                    this.UIElement.Add(xListBox);
                }

                var listItem = new XElement(Names.ListItemElement,
                    new XAttribute("Value", row.FieldAsString(2)),
                    row.IsColumnNull(3) ? null : new XAttribute("Text", row.FieldAsString(3)));

                xListBox.Add(listItem);
            }
        }

        /// <summary>
        /// Decompile the ListView table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileListViewTable(Table table)
        {
            // sort the list views by their property and order
            var listViewRows = table.Rows.OrderBy(row => row.FieldAsString(0)).ThenBy(row => row.FieldAsInteger(1)).ToList();

            XElement xListView = null;
            foreach (var row in listViewRows)
            {
                if (null == xListView || row.FieldAsString(0) != xListView.Attribute("Property")?.Value)
                {
                    xListView = new XElement(Names.ListViewElement,
                        new XAttribute("Property", row.FieldAsString(0)));

                    this.UIElement.Add(xListView);
                }

                var listItem = new XElement(Names.ListItemElement,
                    new XAttribute("Value", row.FieldAsString(2)),
                    row.IsColumnNull(3) ? null : new XAttribute("Text", row.FieldAsString(3)),
                    row.IsColumnNull(4) ? null : new XAttribute("Icon", row.FieldAsString(4)));

                xListView.Add(listItem);
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
                var xPermission = new XElement(Names.PermissionElement,
                    row.IsColumnNull(2) ? null : new XAttribute("Domain", row.FieldAsString(2)),
                    new XAttribute("User", row.FieldAsString(3)));

                string[] specialPermissions;

                switch (row.FieldAsString(1))
                {
                    case "CreateFolder":
                        specialPermissions = LockPermissionConstants.FolderPermissions;
                        break;
                    case "File":
                        specialPermissions = LockPermissionConstants.FilePermissions;
                        break;
                    case "Registry":
                        specialPermissions = LockPermissionConstants.RegistryPermissions;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, row.Table.Name, row.Fields[1].Column.Name, row[1]));
                        return;
                }

                var permissionBits = row.FieldAsInteger(4);
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
                        else if (28 > i && LockPermissionConstants.StandardPermissions.Length > (i - 16))
                        {
                            name = LockPermissionConstants.StandardPermissions[i - 16];
                        }
                        else if (0 <= (i - 28) && LockPermissionConstants.GenericPermissions.Length > (i - 28))
                        {
                            name = LockPermissionConstants.GenericPermissions[i - 28];
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
                                case "ChangePermission":
                                case "CreateChild":
                                case "CreateFile":
                                case "CreateLink":
                                case "CreateSubkeys":
                                case "Delete":
                                case "DeleteChild":
                                case "EnumerateSubkeys":
                                case "Execute":
                                case "FileAllRights":
                                case "GenericAll":
                                case "GenericExecute":
                                case "GenericRead":
                                case "GenericWrite":
                                case "Notify":
                                case "Read":
                                case "ReadAttributes":
                                case "ReadExtendedAttributes":
                                case "ReadPermission":
                                case "SpecificRightsAll":
                                case "Synchronize":
                                case "TakeOwnership":
                                case "Traverse":
                                case "Write":
                                case "WriteAttributes":
                                case "WriteExtendedAttributes":
                                    xPermission.SetAttributeValue(name, "yes");
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown permission attribute '{name}'.");
                            }
                        }
                    }
                }

                this.DecompilerHelper.IndexElement(row, xPermission);
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
                var xMedia = new XElement(Names.MediaElement,
                    new XAttribute("Id", mediaRow.DiskId),
                    mediaRow.DiskPrompt == null ? null : new XAttribute("DiskPrompt", mediaRow.DiskPrompt),
                    mediaRow.VolumeLabel == null ? null : new XAttribute("VolumeLabel", mediaRow.VolumeLabel));

                if (null != mediaRow.Cabinet)
                {
                    var cabinet = mediaRow.Cabinet;

                    if (cabinet.StartsWith("#", StringComparison.Ordinal))
                    {
                        xMedia.SetAttributeValue("EmbedCab", "yes");
                        cabinet = cabinet.Substring(1);
                    }

                    xMedia.SetAttributeValue("Cabinet", cabinet);
                }

                this.DecompilerHelper.AddElementToRoot(xMedia);
                this.DecompilerHelper.IndexElement(mediaRow, xMedia);
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
                var mime = new XElement(Names.MIMEElement,
                    new XAttribute("ContentType", row.FieldAsString(0)),
                    row.IsColumnNull(2) ? null : new XAttribute("Class", row.FieldAsString(2)));

                this.DecompilerHelper.IndexElement(row, mime);
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
                var configuration = new XElement(Names.ConfigurationElement,
                    new XAttribute("Name", row.FieldAsString(0)),
                    XAttributeIfNotNull("Type", row, 2),
                    XAttributeIfNotNull("ContextData", row, 3),
                    XAttributeIfNotNull("DefaultValue", row, 4),
                    XAttributeIfNotNull("DisplayName", row, 6),
                    XAttributeIfNotNull("Description", row, 7),
                    XAttributeIfNotNull("HelpLocation", row, 8),
                    XAttributeIfNotNull("HelpKeyword", row, 9));

                switch (row.FieldAsInteger(1))
                {
                    case 0:
                        configuration.SetAttributeValue("Format", "Text");
                        break;
                    case 1:
                        configuration.SetAttributeValue("Format", "Key");
                        break;
                    case 2:
                        configuration.SetAttributeValue("Format", "Integer");
                        break;
                    case 3:
                        configuration.SetAttributeValue("Format", "Bitfield");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        break;
                }

                if (!row.IsColumnNull(5))
                {
                    var attributes = row.FieldAsInteger(5);

                    if (WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan == (attributes & WindowsInstallerConstants.MsidbMsmConfigurableOptionKeyNoOrphan))
                    {
                        configuration.SetAttributeValue("KeyNoOrphan", "yes");
                    }

                    if (WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable == (attributes & WindowsInstallerConstants.MsidbMsmConfigurableOptionNonNullable))
                    {
                        configuration.SetAttributeValue("NonNullable", "yes");
                    }

                    if (3 < attributes)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[5].Column.Name, row[5]));
                    }
                }

                this.DecompilerHelper.AddElementToRoot(configuration);
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
                var xDependency = new XElement(Names.DependencyElement,
                    new XAttribute("RequiredId", row.FieldAsString(2)),
                    new XAttribute("RequiredLanguage", row.FieldAsString(3)),
                    XAttributeIfNotNull("RequiredVersion", row, 4));

                this.DecompilerHelper.AddElementToRoot(xDependency);
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
                var xExclusion = new XElement(Names.ExclusionElement,
                    new XAttribute("ExcludedId", row.FieldAsString(2)),
                    XAttributeIfNotNull("ExcludedMinVersion", row, 4),
                    XAttributeIfNotNull("ExcludedMaxVersion", row, 5));

                var excludedLanguage = row.FieldAsInteger(3);
                if (0 < excludedLanguage)
                {
                    xExclusion.SetAttributeValue("ExcludeLanguage", excludedLanguage);
                }
                else if (0 > excludedLanguage)
                {
                    xExclusion.SetAttributeValue("ExcludeExceptLanguage", -excludedLanguage);
                }

                this.DecompilerHelper.AddElementToRoot(xExclusion);
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
                var tableName = row.FieldAsString(0);

                // the linker automatically adds a ModuleIgnoreTable row for some tables
                if ("ModuleConfiguration" != tableName && "ModuleSubstitution" != tableName)
                {
                    var xIgnoreTable = new XElement(Names.IgnoreTableElement,
                        new XAttribute("Id", tableName));

                    this.DecompilerHelper.AddElementToRoot(xIgnoreTable);
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

                this.DecompilerHelper.RootElement.SetAttributeValue("Id", row.FieldAsString(0));
                // support Language columns that are treated as integers as well as strings (the WiX default, to support localizability)
                this.DecompilerHelper.RootElement.SetAttributeValue("Language", row.FieldAsString(1));
                this.DecompilerHelper.RootElement.SetAttributeValue("Version", row.FieldAsString(2));
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
                var xSubstitution = new XElement(Names.SubstitutionElement,
                    new XAttribute("Table", row.FieldAsString(0)),
                    new XAttribute("Row", row.FieldAsString(1)),
                    new XAttribute("Column", row.FieldAsString(2)),
                    XAttributeIfNotNull("Value", row, 3));

                this.DecompilerHelper.AddElementToRoot(xSubstitution);
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
                var xCopyFile = new XElement(Names.CopyFileElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    XAttributeIfNotNull("SourceName", row, 2));

                if (!row.IsColumnNull(3))
                {
                    var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(3));
                    if (null != names[0] && null != names[1])
                    {
                        xCopyFile.SetAttributeValue("DestinationShortName", names[0]);
                        xCopyFile.SetAttributeValue("DestinationName", names[1]);
                    }
                    else if (null != names[0])
                    {
                        xCopyFile.SetAttributeValue("DestinationName", names[0]);
                    }
                }

                // source/destination directory/property is set in FinalizeDuplicateMoveFileTables

                switch (row.FieldAsInteger(6))
                {
                    case 0:
                        break;
                    case WindowsInstallerConstants.MsidbMoveFileOptionsMove:
                        xCopyFile.SetAttributeValue("Delete", "yes");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                        break;
                }

                this.AddChildToParent("Component", xCopyFile, row, 1);
                this.DecompilerHelper.IndexElement(row, xCopyFile);
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
                var xDigitalCertificate = new XElement(Names.DigitalCertificateElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("SourceFile", row.FieldAsString(1)));

                this.DecompilerHelper.IndexElement(row, xDigitalCertificate);
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
                var xDigitalSignature = new XElement(Names.DigitalSignatureElement,
                    XAttributeIfNotNull("SourceFile", row, 3));

                this.AddChildToParent("MsiDigitalCertificate", xDigitalSignature, row, 2);

                if (this.DecompilerHelper.TryGetIndexedElement(row.FieldAsString(0), row.FieldAsString(1), out var xParentElement))
                {
                    xParentElement.Add(xDigitalSignature);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "SignObject", row.FieldAsString(1), row.FieldAsString(0)));
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
                var xEmbeddedChainer = new XElement(Names.EmbeddedChainerElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Condition", row.FieldAsString(1)),
                    XAttributeIfNotNull("CommandLine", row, 2));

                switch (row.FieldAsInteger(4))
                {
                    case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeBinaryData:
                        xEmbeddedChainer.SetAttributeValue("BinarySource", row.FieldAsString(3));
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeSourceFile:
                        xEmbeddedChainer.SetAttributeValue("FileSource", row.FieldAsString(3));
                        break;
                    case WindowsInstallerConstants.MsidbCustomActionTypeExe + WindowsInstallerConstants.MsidbCustomActionTypeProperty:
                        xEmbeddedChainer.SetAttributeValue("PropertySource", row.FieldAsString(3));
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                }

                this.DecompilerHelper.AddElementToRoot(xEmbeddedChainer);
            }
        }

        /// <summary>
        /// Decompile the MsiEmbeddedUI table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiEmbeddedUITable(Table table)
        {
            var xEmbeddedUI = new XElement(Names.EmbeddedUIElement);

            var foundEmbeddedUI = false;
            var foundEmbeddedResources = false;

            foreach (var row in table.Rows)
            {
                var attributes = row.FieldAsInteger(2);

                if (WindowsInstallerConstants.MsidbEmbeddedUI == (attributes & WindowsInstallerConstants.MsidbEmbeddedUI))
                {
                    if (foundEmbeddedUI)
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[2].Column.Name, row[2]));
                    }
                    else
                    {
                        xEmbeddedUI.SetAttributeValue("Id", row.FieldAsString(0));
                        xEmbeddedUI.SetAttributeValue("Name", row.FieldAsString(1));

                        var messageFilter = row.FieldAsInteger(3);
                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_FATALEXIT))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreFatalExit", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ERROR))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreError", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_WARNING))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreWarning", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_USER))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreUser", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INFO))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreInfo", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_FILESINUSE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreFilesInUse", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_RESOLVESOURCE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreResolveSource", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_OUTOFDISKSPACE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreOutOfDiskSpace", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ACTIONSTART))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreActionStart", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_ACTIONDATA))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreActionData", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_PROGRESS))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreProgress", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_COMMONDATA))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreCommonData", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INITIALIZE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreInitialize", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_TERMINATE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreTerminate", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_SHOWDIALOG))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreShowDialog", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_RMFILESINUSE))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreRMFilesInUse", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INSTALLSTART))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreInstallStart", "yes");
                        }

                        if (0 == (messageFilter & WindowsInstallerConstants.INSTALLLOGMODE_INSTALLEND))
                        {
                            xEmbeddedUI.SetAttributeValue("IgnoreInstallEnd", "yes");
                        }

                        if (WindowsInstallerConstants.MsidbEmbeddedHandlesBasic == (attributes & WindowsInstallerConstants.MsidbEmbeddedHandlesBasic))
                        {
                            xEmbeddedUI.SetAttributeValue("SupportBasicUI", "yes");
                        }

                        xEmbeddedUI.SetAttributeValue("SourceFile", row.FieldAsString(4));

                        this.UIElement.Add(xEmbeddedUI);
                        foundEmbeddedUI = true;
                    }
                }
                else
                {
                    var xEmbeddedResource = new XElement(Names.EmbeddedUIResourceElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Name", row.FieldAsString(1)),
                        new XAttribute("SourceFile", row.FieldAsString(4)));

                    xEmbeddedUI.Add(xEmbeddedResource);
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
                var xPermissionEx = new XElement(Names.PermissionExElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Sddl", row.FieldAsString(3)),
                    XAttributeIfNotNull("Condition", row, 4));

                switch (row.FieldAsString(2))
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

                this.DecompilerHelper.IndexElement(row, xPermissionEx);
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
                var xPackageCertificates = new XElement(Names.PatchCertificatesElement);
                this.DecompilerHelper.AddElementToRoot(xPackageCertificates);
                this.AddCertificates(table, xPackageCertificates);
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
                var xPatchCertificates = new XElement(Names.PatchCertificatesElement);
                this.DecompilerHelper.AddElementToRoot(xPatchCertificates);
                this.AddCertificates(table, xPatchCertificates);
            }
        }

        /// <summary>
        /// Insert DigitalCertificate records associated with passed msiPackageCertificate or msiPatchCertificate table.
        /// </summary>
        /// <param name="table">The table being decompiled.</param>
        /// <param name="parent">DigitalCertificate parent</param>
        private void AddCertificates(Table table, XElement parent)
        {
            foreach (var row in table.Rows)
            {
                if (this.DecompilerHelper.TryGetIndexedElement("MsiDigitalCertificate", row.FieldAsString(1), out var xDigitalCertificate))
                {
                    parent.Add(xDigitalCertificate);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "DigitalCertificate_", row.FieldAsString(1), "MsiDigitalCertificate"));
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
                var xProperty = new XElement(Names.ShortcutPropertyElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Key", row.FieldAsString(2)),
                    new XAttribute("Value", row.FieldAsString(3)));

                this.AddChildToParent("Shortcut", xProperty, row, 1);
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
                var xProperty = new XElement(Names.PropertyElement,
                    new XAttribute("Id", row.FieldAsString(1)),
                    row.IsColumnNull(2) ? null : new XAttribute("Value", row.FieldAsString(2)));

                this.AddChildToParent("ODBCDriver", xProperty, row, 0);
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
                var xOdbcDataSource = new XElement(Names.ODBCDataSourceElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2)),
                    new XAttribute("DriverName", row.FieldAsString(3)));

                switch (row.FieldAsInteger(4))
                {
                    case WindowsInstallerConstants.MsidbODBCDataSourceRegistrationPerMachine:
                        xOdbcDataSource.SetAttributeValue("Registration", "machine");
                        break;
                    case WindowsInstallerConstants.MsidbODBCDataSourceRegistrationPerUser:
                        xOdbcDataSource.SetAttributeValue("Registration", "user");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                        break;
                }

                this.DecompilerHelper.IndexElement(row, xOdbcDataSource);
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
                var xOdbcDriver = new XElement(Names.ODBCDriverElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2)),
                    new XAttribute("File", row.FieldAsString(3)),
                    XAttributeIfNotNull("SetupFile", row, 4));

                this.AddChildToParent("Component", xOdbcDriver, row, 1);
                this.DecompilerHelper.IndexElement(row, xOdbcDriver);
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
                var xProperty = new XElement(Names.PropertyElement,
                    new XAttribute("Id", row.FieldAsString(1)),
                    XAttributeIfNotNull("Value", row, 2));

                this.AddChildToParent("ODBCDataSource", xProperty, row, 0);
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
                var xOdbcTranslator = new XElement(Names.ODBCTranslatorElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2)),
                    new XAttribute("File", row.FieldAsString(3)),
                    XAttributeIfNotNull("SetupFile", row, 4));

                this.AddChildToParent("Component", xOdbcTranslator, row, 1);
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
                var xPatchMetadata = new XElement(Names.PatchMetadataElement);

                foreach (var row in table.Rows)
                {
                    var value = row.FieldAsString(2);

                    switch (row.FieldAsString(1))
                    {
                        case "AllowRemoval":
                            if ("1" == value)
                            {
                                xPatchMetadata.SetAttributeValue("AllowRemoval", "yes");
                            }
                            break;
                        case "Classification":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("Classification", value);
                            }
                            break;
                        case "CreationTimeUTC":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("CreationTimeUTC", value);
                            }
                            break;
                        case "Description":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("Description", value);
                            }
                            break;
                        case "DisplayName":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("DisplayName", value);
                            }
                            break;
                        case "ManufacturerName":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("ManufacturerName", value);
                            }
                            break;
                        case "MinorUpdateTargetRTM":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("MinorUpdateTargetRTM", value);
                            }
                            break;
                        case "MoreInfoURL":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("MoreInfoURL", value);
                            }
                            break;
                        case "OptimizeCA":
                            var xOptimizeCustomActions = new XElement(Names.OptimizeCustomActionsElement);
                            var optimizeCA = Int32.Parse(value, CultureInfo.InvariantCulture);
                            if (0 != (Convert.ToInt32(OptimizeCAFlags.SkipAssignment) & optimizeCA))
                            {
                                xOptimizeCustomActions.SetAttributeValue("SkipAssignment", "yes");
                            }

                            if (0 != (Convert.ToInt32(OptimizeCAFlags.SkipImmediate) & optimizeCA))
                            {
                                xOptimizeCustomActions.SetAttributeValue("SkipImmediate", "yes");
                            }

                            if (0 != (Convert.ToInt32(OptimizeCAFlags.SkipDeferred) & optimizeCA))
                            {
                                xOptimizeCustomActions.SetAttributeValue("SkipDeferred", "yes");
                            }

                            xPatchMetadata.Add(xOptimizeCustomActions);
                            break;
                        case "OptimizedInstallMode":
                            if ("1" == value)
                            {
                                xPatchMetadata.SetAttributeValue("OptimizedInstallMode", "yes");
                            }
                            break;
                        case "TargetProductName":
                            if (null != value)
                            {
                                xPatchMetadata.SetAttributeValue("TargetProductName", value);
                            }
                            break;
                        default:
                            var xCustomProperty = new XElement(Names.CustomPropertyElement,
                                XAttributeIfNotNull("Company", row, 0),
                                XAttributeIfNotNull("Property", row, 1),
                                XAttributeIfNotNull("Value", row, 2));

                            xPatchMetadata.Add(xCustomProperty);
                            break;
                    }
                }

                this.DecompilerHelper.AddElementToRoot(xPatchMetadata);
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
                var patchSequence = new XElement(Names.PatchSequenceElement,
                    new XAttribute("PatchFamily", row.FieldAsString(0)));

                if (!row.IsColumnNull(1))
                {
                    try
                    {
                        var guid = new Guid(row.FieldAsString(1));

                        patchSequence.SetAttributeValue("ProductCode", row.FieldAsString(1));
                    }
                    catch // non-guid value
                    {
                        patchSequence.SetAttributeValue("TargetImage", row.FieldAsString(1));
                    }
                }

                if (!row.IsColumnNull(2))
                {
                    patchSequence.SetAttributeValue("Sequence", row.FieldAsString(2));
                }

                if (!row.IsColumnNull(3) && 0x1 == row.FieldAsInteger(3))
                {
                    patchSequence.SetAttributeValue("Supersede", "yes");
                }

                this.DecompilerHelper.AddElementToRoot(patchSequence);
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
                var xProgId = new XElement(Names.ProgIdElement,
                    new XAttribute("Advertise", "yes"),
                    new XAttribute("Id", row.FieldAsString(0)),
                    XAttributeIfNotNull("Description", row, 3),
                    XAttributeIfNotNull("Icon", row, 4),
                    XAttributeIfNotNull("IconIndex", row, 5));

                this.DecompilerHelper.IndexElement(row, xProgId);
            }

            // nest the ProgIds
            foreach (var row in table.Rows)
            {
                var xProgId = this.DecompilerHelper.GetIndexedElement(row);

                if (!row.IsColumnNull(1))
                {
                    this.AddChildToParent("ProgId", xProgId, row, 1);
                }
                else if (!row.IsColumnNull(2))
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
            foreach (var row in table.Rows)
            {
                var name = row.FieldAsString(0);
                var value = row.FieldAsString(1);

                switch (name)
                {
                    case "AllowProductCodeMismatches":
                        if ("1" == value)
                        {
                            this.DecompilerHelper.RootElement.SetAttributeValue("AllowProductCodeMismatches", "yes");
                        }
                        break;
                    case "AllowProductVersionMajorMismatches":
                        if ("1" == value)
                        {
                            this.DecompilerHelper.RootElement.SetAttributeValue("AllowMajorVersionMismatches", "yes");
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

                                this.DecompilerHelper.RootElement.SetAttributeValue("SymbolFlags", Convert.ToInt32(value, 16));
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
                            this.DecompilerHelper.RootElement.SetAttributeValue("CleanWorkingFolder", "no");
                        }
                        break;
                    case "IncludeWholeFilesOnly":
                        if ("1" == value)
                        {
                            this.DecompilerHelper.RootElement.SetAttributeValue("WholeFilesOnly", "yes");
                        }
                        break;
                    case "ListOfPatchGUIDsToReplace":
                        if (null != value)
                        {
                            var guidRegex = new Regex(@"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}");
                            var guidMatches = guidRegex.Matches(value);

                            foreach (Match guidMatch in guidMatches)
                            {
                                var xReplacePatch = new XElement(Names.ReplacePatchElement,
                                    new XAttribute("Id", guidMatch.Value));

                                this.DecompilerHelper.AddElementToRoot(xReplacePatch);
                            }
                        }
                        break;
                    case "ListOfTargetProductCodes":
                        if (null != value)
                        {
                            var targetProductCodes = value.Split(';');

                            foreach (var targetProductCodeString in targetProductCodes)
                            {
                                var xTargetProductCode = new XElement(Names.TargetProductCodeElement,
                                    new XAttribute("Id", targetProductCodeString));

                                this.DecompilerHelper.AddElementToRoot(xTargetProductCode);
                            }
                        }
                        break;
                    case "PatchGUID":
                        this.DecompilerHelper.RootElement.SetAttributeValue("Id", value);
                        break;
                    case "PatchSourceList":
                        this.DecompilerHelper.RootElement.SetAttributeValue("SourceList", value);
                        break;
                    case "PatchOutputPath":
                        this.DecompilerHelper.RootElement.SetAttributeValue("OutputPath", value);
                        break;
                    default:
                        var patchProperty = new XElement(Names.PatchPropertyElement,
                            new XAttribute("Name", name),
                            new XAttribute("Value", value));

                        this.DecompilerHelper.AddElementToRoot(patchProperty);
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
                var id = row.FieldAsString(0);
                var value = row.FieldAsString(1);

                if ("AdminProperties" == id || "MsiHiddenProperties" == id || "SecureCustomProperties" == id)
                {
                    if (0 < value.Length)
                    {
                        foreach (var propertyId in value.Split(';'))
                        {
                            if (WixUpgradeConstants.DowngradeDetectedProperty == propertyId || WixUpgradeConstants.UpgradeDetectedProperty == propertyId)
                            {
                                continue;
                            }

                            var property = propertyId;
                            var suppressModulularization = false;
                            if (OutputType.Module == this.OutputType)
                            {
                                if (propertyId.EndsWith(this.ModularizationGuid.Substring(1, 36).Replace('-', '_'), StringComparison.Ordinal))
                                {
                                    property = propertyId.Substring(0, propertyId.Length - this.ModularizationGuid.Length + 1);
                                }
                                else
                                {
                                    suppressModulularization = true;
                                }
                            }

                            var xSpecialProperty = this.EnsureProperty(property);
                            if (suppressModulularization)
                            {
                                xSpecialProperty.SetAttributeValue("SuppressModularization", "yes");
                            }

                            switch (id)
                            {
                                case "AdminProperties":
                                    xSpecialProperty.SetAttributeValue("Admin", "yes");
                                    break;
                                case "MsiHiddenProperties":
                                    xSpecialProperty.SetAttributeValue("Hidden", "yes");
                                    break;
                                case "SecureCustomProperties":
                                    xSpecialProperty.SetAttributeValue("Secure", "yes");
                                    break;
                            }
                        }
                    }

                    continue;
                }
                else if (OutputType.Package == this.OutputType)
                {
                    switch (id)
                    {
                        case "Manufacturer":
                            this.DecompilerHelper.RootElement.SetAttributeValue("Manufacturer", value);
                            continue;
                        case "ProductCode":
                            this.DecompilerHelper.RootElement.SetAttributeValue("ProductCode", value.ToUpper(CultureInfo.InvariantCulture));
                            continue;
                        case "ProductLanguage":
                            this.DecompilerHelper.RootElement.SetAttributeValue("Language", value);
                            continue;
                        case "ProductName":
                            this.DecompilerHelper.RootElement.SetAttributeValue("Name", value);
                            continue;
                        case "ProductVersion":
                            this.DecompilerHelper.RootElement.SetAttributeValue("Version", value);
                            continue;
                        case "UpgradeCode":
                            this.DecompilerHelper.RootElement.SetAttributeValue("UpgradeCode", value);
                            continue;
                    }
                }

                if (!this.SuppressUI || "ErrorDialog" != id)
                {
                    var xProperty = this.EnsureProperty(id);

                    xProperty.SetAttributeValue("Value", value);
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
                var category = new XElement(Names.CategoryElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Qualifier", row.FieldAsString(1)),
                    XAttributeIfNotNull("AppData", row, 3));

                this.AddChildToParent("Component", category, row, 2);
            }
        }

        /// <summary>
        /// Decompile the RadioButton table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileRadioButtonTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var radioButton = new XElement(Names.RadioButtonElement,
                    new XAttribute("Value", row.FieldAsString(2)),
                    new XAttribute("X", row.FieldAsInteger(3)),
                    new XAttribute("Y", row.FieldAsInteger(4)),
                    new XAttribute("Width", row.FieldAsInteger(5)),
                    new XAttribute("Height", row.FieldAsInteger(6)),
                    XAttributeIfNotNull("Text", row, 7));

                if (!row.IsColumnNull(8))
                {
                    var help = (row.FieldAsString(8)).Split('|');

                    if (2 == help.Length)
                    {
                        if (0 < help[0].Length)
                        {
                            radioButton.SetAttributeValue("ToolTip", help[0]);
                        }

                        if (0 < help[1].Length)
                        {
                            radioButton.SetAttributeValue("Help", help[1]);
                        }
                    }
                }

                this.DecompilerHelper.IndexElement(row, radioButton);
            }

            // nest the radio buttons
            var xRadioButtonGroups = new Dictionary<string, XElement>();
            foreach (var row in table.Rows.OrderBy(row => row.FieldAsString(0)).ThenBy(row => row.FieldAsInteger(1)))
            {
                var xRadioButton = this.DecompilerHelper.GetIndexedElement(row);

                if (!xRadioButtonGroups.TryGetValue(row.FieldAsString(0), out var xRadioButtonGroup))
                {
                    xRadioButtonGroup = new XElement(Names.RadioButtonGroupElement,
                        new XAttribute("Property", row.FieldAsString(0)));

                    this.UIElement.Add(xRadioButtonGroup);
                    xRadioButtonGroups.Add(row.FieldAsString(0), xRadioButtonGroup);
                }

                xRadioButtonGroup.Add(xRadioButton);
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
                if (("-" == row.FieldAsString(3) || "+" == row.FieldAsString(3) || "*" == row.FieldAsString(3)) && row.IsColumnNull(4))
                {
                    var xRegistryKey = new XElement(Names.RegistryKeyElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Key", row.FieldAsString(2)));

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        xRegistryKey.SetAttributeValue("Root", registryRootType);
                    }

                    switch (row.FieldAsString(3))
                    {
                        case "+":
                            xRegistryKey.SetAttributeValue("ForceCreateOnInstall", "yes");
                            break;
                        case "-":
                            xRegistryKey.SetAttributeValue("ForceDeleteOnUninstall", "yes");
                            break;
                        case "*":
                            xRegistryKey.SetAttributeValue("ForceCreateOnInstall", "yes");
                            xRegistryKey.SetAttributeValue("ForceDeleteOnUninstall", "yes");
                            break;
                    }

                    this.DecompilerHelper.IndexElement(row, xRegistryKey);
                }
                else
                {
                    var xRegistryValue = new XElement(Names.RegistryValueElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Key", row.FieldAsString(2)),
                        XAttributeIfNotNull("Name", row, 3));

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        xRegistryValue.SetAttributeValue("Root", registryRootType);
                    }

                    if (!row.IsColumnNull(4))
                    {
                        var value = row.FieldAsString(4);

                        if (value.StartsWith("#x", StringComparison.Ordinal))
                        {
                            xRegistryValue.SetAttributeValue("Type", "binary");
                            xRegistryValue.SetAttributeValue("Value", value.Substring(2));
                        }
                        else if (value.StartsWith("#%", StringComparison.Ordinal))
                        {
                            xRegistryValue.SetAttributeValue("Type", "expandable");
                            xRegistryValue.SetAttributeValue("Value", value.Substring(2));
                        }
                        else if (value.StartsWith("#", StringComparison.Ordinal) && !value.StartsWith("##", StringComparison.Ordinal))
                        {
                            xRegistryValue.SetAttributeValue("Type", "integer");
                            xRegistryValue.SetAttributeValue("Value", value.Substring(1));
                        }
                        else
                        {
                            if (value.StartsWith("##", StringComparison.Ordinal))
                            {
                                value = value.Substring(1);
                            }

                            if (0 <= value.IndexOf("[~]", StringComparison.Ordinal))
                            {
                                xRegistryValue.SetAttributeValue("Type", "multiString");

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
                                    xRegistryValue.SetAttributeValue("Action", "append");
                                    value = value.Substring(3);
                                }
                                else if (value.EndsWith("[~]", StringComparison.Ordinal))
                                {
                                    xRegistryValue.SetAttributeValue("Action", "prepend");
                                    value = value.Substring(0, value.Length - 3);
                                }

                                var multiValues = NullSplitter.Split(value);
                                foreach (var multiValue in multiValues)
                                {
                                    var xMultiStringValue = new XElement(Names.MultiStringElement,
                                        new XAttribute("Value", multiValue));

                                    xRegistryValue.Add(xMultiStringValue);
                                }
                            }
                            else
                            {
                                xRegistryValue.SetAttributeValue("Type", "string");
                                xRegistryValue.SetAttributeValue("Value", value);
                            }
                        }
                    }
                    else
                    {
                        xRegistryValue.SetAttributeValue("Type", "string");
                        xRegistryValue.SetAttributeValue("Value", String.Empty);
                    }

                    this.DecompilerHelper.IndexElement(row, xRegistryValue);
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
                var xRegistrySearch = new XElement(Names.RegistrySearchElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Key", row.FieldAsString(2)),
                    XAttributeIfNotNull("Name", row, 3));

                switch (row.FieldAsInteger(1))
                {
                    case WindowsInstallerConstants.MsidbRegistryRootClassesRoot:
                        xRegistrySearch.SetAttributeValue("Root", "HKCR");
                        break;
                    case WindowsInstallerConstants.MsidbRegistryRootCurrentUser:
                        xRegistrySearch.SetAttributeValue("Root", "HKCU");
                        break;
                    case WindowsInstallerConstants.MsidbRegistryRootLocalMachine:
                        xRegistrySearch.SetAttributeValue("Root", "HKLM");
                        break;
                    case WindowsInstallerConstants.MsidbRegistryRootUsers:
                        xRegistrySearch.SetAttributeValue("Root", "HKU");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[1].Column.Name, row[1]));
                        break;
                }

                if (row.IsColumnNull(4))
                {
                    xRegistrySearch.SetAttributeValue("Type", "file");
                }
                else
                {
                    var type = row.FieldAsInteger(4);

                    if (WindowsInstallerConstants.MsidbLocatorType64bit == (type & WindowsInstallerConstants.MsidbLocatorType64bit))
                    {
                        xRegistrySearch.SetAttributeValue("Bitness", "always64");
                        type &= ~WindowsInstallerConstants.MsidbLocatorType64bit;
                    }
                    else
                    {
                        xRegistrySearch.SetAttributeValue("Bitness", "always32");
                    }

                    switch (type)
                    {
                        case WindowsInstallerConstants.MsidbLocatorTypeDirectory:
                            xRegistrySearch.SetAttributeValue("Type", "directory");
                            break;
                        case WindowsInstallerConstants.MsidbLocatorTypeFileName:
                            xRegistrySearch.SetAttributeValue("Type", "file");
                            break;
                        case WindowsInstallerConstants.MsidbLocatorTypeRawValue:
                            xRegistrySearch.SetAttributeValue("Type", "raw");
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                            break;
                    }
                }

                this.DecompilerHelper.IndexElement(row, xRegistrySearch);
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
                if (row.IsColumnNull(2))
                {
                    var xRemoveFolder = new XElement(Names.RemoveFolderElement,
                        new XAttribute("Id", row.FieldAsString(0)));

                    // directory/property is set in FinalizeDecompile

                    switch (row.FieldAsInteger(4))
                    {
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall:
                            xRemoveFolder.SetAttributeValue("On", "install");
                            break;
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove:
                            xRemoveFolder.SetAttributeValue("On", "uninstall");
                            break;
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnBoth:
                            xRemoveFolder.SetAttributeValue("On", "both");
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                            break;
                    }

                    this.AddChildToParent("Component", xRemoveFolder, row, 1);
                    this.DecompilerHelper.IndexElement(row, xRemoveFolder);
                }
                else
                {
                    var xRemoveFile = new XElement(Names.RemoveFileElement,
                        new XAttribute("Id", row.FieldAsString(0)));

                    var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(2));
                    if (null != names[0] && null != names[1])
                    {
                        xRemoveFile.SetAttributeValue("ShortName", names[0]);
                        xRemoveFile.SetAttributeValue("Name", names[1]);
                    }
                    else if (null != names[0])
                    {
                        xRemoveFile.SetAttributeValue("Name", names[0]);
                    }

                    // directory/property is set in FinalizeDecompile

                    switch (row.FieldAsInteger(4))
                    {
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall:
                            xRemoveFile.SetAttributeValue("On", "install");
                            break;
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove:
                            xRemoveFile.SetAttributeValue("On", "uninstall");
                            break;
                        case WindowsInstallerConstants.MsidbRemoveFileInstallModeOnBoth:
                            xRemoveFile.SetAttributeValue("On", "both");
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                            break;
                    }

                    this.AddChildToParent("Component", xRemoveFile, row, 1);
                    this.DecompilerHelper.IndexElement(row, xRemoveFile);
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
                var xIniFile = new XElement(Names.IniFileElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        XAttributeIfNotNull("Directory", row, 2),
                        new XAttribute("Section", row.FieldAsString(3)),
                        new XAttribute("Key", row.FieldAsString(4)),
                        XAttributeIfNotNull("Value", row, 5));

                var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(1));
                if (null != names[0] && null != names[1])
                {
                    xIniFile.SetAttributeValue("ShortName", names[0]);
                    xIniFile.SetAttributeValue("Name", names[1]);
                }
                else if (null != names[0])
                {
                    xIniFile.SetAttributeValue("Name", names[0]);
                }

                switch (row.FieldAsInteger(6))
                {
                    case WindowsInstallerConstants.MsidbIniFileActionRemoveLine:
                        xIniFile.SetAttributeValue("Action", "removeLine");
                        break;
                    case WindowsInstallerConstants.MsidbIniFileActionRemoveTag:
                        xIniFile.SetAttributeValue("Action", "removeTag");
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[6].Column.Name, row[6]));
                        break;
                }

                this.AddChildToParent("Component", xIniFile, row, 7);
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
                if ("-" == row.FieldAsString(3))
                {
                    var xRemoveRegistryKey = new XElement(Names.RemoveRegistryKeyElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Key", row.FieldAsString(2)),
                        new XAttribute("Action", "removeOnInstall"));

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        xRemoveRegistryKey.SetAttributeValue("Root", registryRootType);
                    }

                    this.AddChildToParent("Component", xRemoveRegistryKey, row, 4);
                }
                else
                {
                    var xRemoveRegistryValue = new XElement(Names.RemoveRegistryValueElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Key", row.FieldAsString(2)),
                        XAttributeIfNotNull("Name", row, 3));

                    if (this.GetRegistryRootType(row.SourceLineNumbers, table.Name, row.Fields[1], out var registryRootType))
                    {
                        xRemoveRegistryValue.SetAttributeValue("Root", registryRootType);
                    }

                    this.AddChildToParent("Component", xRemoveRegistryValue, row, 4);
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
                var xReserveCost = new XElement(Names.ReserveCostElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        XAttributeIfNotNull("Directory", row, 2),
                        new XAttribute("RunLocal", row.FieldAsString(3)),
                        new XAttribute("RunFromSource", row.FieldAsString(4)));

                this.AddChildToParent("Component", xReserveCost, row, 4);
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
                if (this.DecompilerHelper.TryGetIndexedElement("File", row.FieldAsString(0), out var xFile))
                {
                    xFile.SetAttributeValue("SelfRegCost", row.IsColumnNull(1) ? 0 : row.FieldAsInteger(1));
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "File_", row.FieldAsString(0), "File"));
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
                var xServiceControl = new XElement(Names.ServiceControlElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Name", row.FieldAsString(1)));

                var eventValue = row.FieldAsInteger(2);
                if (WindowsInstallerConstants.MsidbServiceControlEventStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStart) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStart))
                {
                    xServiceControl.SetAttributeValue("Start", "both");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStart))
                {
                    xServiceControl.SetAttributeValue("Start", "install");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallStart == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStart))
                {
                    xServiceControl.SetAttributeValue("Start", "uninstall");
                }

                if (WindowsInstallerConstants.MsidbServiceControlEventStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStop) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStop))
                {
                    xServiceControl.SetAttributeValue("Stop", "both");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventStop))
                {
                    xServiceControl.SetAttributeValue("Stop", "install");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallStop == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallStop))
                {
                    xServiceControl.SetAttributeValue("Stop", "uninstall");
                }

                if (WindowsInstallerConstants.MsidbServiceControlEventDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventDelete) &&
                    WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete))
                {
                    xServiceControl.SetAttributeValue("Remove", "both");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventDelete))
                {
                    xServiceControl.SetAttributeValue("Remove", "install");
                }
                else if (WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete == (eventValue & WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete))
                {
                    xServiceControl.SetAttributeValue("Remove", "uninstall");
                }

                if (!row.IsColumnNull(3))
                {
                    var arguments = NullSplitter.Split(row.FieldAsString(3));

                    foreach (var argument in arguments)
                    {
                        var xServiceArgument = new XElement(Names.ServiceArgumentElement,
                            new XAttribute("Value", argument));

                        xServiceControl.Add(xServiceArgument);
                    }
                }

                if (!row.IsColumnNull(4))
                {
                    xServiceControl.SetAttributeValue("Wait", row.FieldAsInteger(4) == 0 ? "no" : "yes");
                }

                this.AddChildToParent("Component", xServiceControl, row, 5);
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
                var xServiceInstall = new XElement(Names.ServiceInstallElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("Name", row.FieldAsString(1)),
                        XAttributeIfNotNull("DisplayName", row, 2),
                        XAttributeIfNotNull("LoadOrderGroup", row, 6),
                        XAttributeIfNotNull("Account", row, 8),
                        XAttributeIfNotNull("Password", row, 9),
                        XAttributeIfNotNull("Arguments", row, 10),
                        XAttributeIfNotNull("Description", row, 12));

                var serviceType = row.FieldAsInteger(3);
                if (WindowsInstallerConstants.MsidbServiceInstallInteractive == (serviceType & WindowsInstallerConstants.MsidbServiceInstallInteractive))
                {
                    xServiceInstall.SetAttributeValue("Interactive", "yes");
                }

                if (WindowsInstallerConstants.MsidbServiceInstallOwnProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallOwnProcess) &&
                    WindowsInstallerConstants.MsidbServiceInstallShareProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallShareProcess))
                {
                    // TODO: warn
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallOwnProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallOwnProcess))
                {
                    xServiceInstall.SetAttributeValue("Type", "ownProcess");
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallShareProcess == (serviceType & WindowsInstallerConstants.MsidbServiceInstallShareProcess))
                {
                    xServiceInstall.SetAttributeValue("Type", "shareProcess");
                }

                var startType = row.FieldAsInteger(4);
                if (WindowsInstallerConstants.MsidbServiceInstallDisabled == startType)
                {
                    xServiceInstall.SetAttributeValue("Start", "disabled");
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallDemandStart == startType)
                {
                    xServiceInstall.SetAttributeValue("Start", "demand");
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallAutoStart == startType)
                {
                    xServiceInstall.SetAttributeValue("Start", "auto");
                }
                else
                {
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[4].Column.Name, row[4]));
                }

                var errorControl = row.FieldAsInteger(5);
                if (WindowsInstallerConstants.MsidbServiceInstallErrorCritical == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorCritical))
                {
                    xServiceInstall.SetAttributeValue("ErrorControl", "critical");
                }
                else if (WindowsInstallerConstants.MsidbServiceInstallErrorNormal == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorNormal))
                {
                    xServiceInstall.SetAttributeValue("ErrorControl", "normal");
                }
                else
                {
                    xServiceInstall.SetAttributeValue("ErrorControl", "ignore");
                }

                if (WindowsInstallerConstants.MsidbServiceInstallErrorControlVital == (errorControl & WindowsInstallerConstants.MsidbServiceInstallErrorControlVital))
                {
                    xServiceInstall.SetAttributeValue("Vital", "yes");
                }

                if (!row.IsColumnNull(7))
                {
                    var dependencies = NullSplitter.Split(row.FieldAsString(7));

                    foreach (var dependency in dependencies)
                    {
                        if (0 < dependency.Length)
                        {
                            var xServiceDependency = new XElement(Names.ServiceDependencyElement);

                            if (dependency.StartsWith("+", StringComparison.Ordinal))
                            {
                                xServiceDependency.SetAttributeValue("Group", "yes");
                                xServiceDependency.SetAttributeValue("Id", dependency.Substring(1));
                            }
                            else
                            {
                                xServiceDependency.SetAttributeValue("Id", dependency);
                            }

                            xServiceInstall.Add(xServiceDependency);
                        }
                    }
                }

                this.AddChildToParent("Component", xServiceInstall, row, 11);
                this.DecompilerHelper.IndexElement(row, xServiceInstall);
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
                var xSfpCatalog = new XElement(Names.SFPCatalogElement,
                    new XAttribute("Name", row.FieldAsString(0)),
                    new XAttribute("SourceFile", row.FieldAsString(1)));

                this.DecompilerHelper.IndexElement(row, xSfpCatalog);
            }

            // nest the SFPCatalog elements
            foreach (var row in table.Rows)
            {
                var xSfpCatalog = this.DecompilerHelper.GetIndexedElement(row);

                if (!row.IsColumnNull(2))
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("SFPCatalog", row.FieldAsString(2), out var xParentSFPCatalog))
                    {
                        xParentSFPCatalog.Add(xSfpCatalog);
                    }
                    else
                    {
                        xSfpCatalog.SetAttributeValue("Dependency", row.FieldAsString(2));

                        this.DecompilerHelper.AddElementToRoot(xSfpCatalog);
                    }
                }
                else
                {
                    this.DecompilerHelper.AddElementToRoot(xSfpCatalog);
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
                var xShortcut = new XElement(Names.ShortcutElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Directory", row.FieldAsString(1)),
                    XAttributeIfNotNull("Arguments", row, 5),
                    XAttributeIfNotNull("Description", row, 6),
                    XAttributeIfNotNull("Hotkey", row, 7),
                    XAttributeIfNotNull("Icon", row, 8),
                    XAttributeIfNotNull("IconIndex", row, 9),
                    XAttributeIfNotNull("WorkingDirectory", row, 11));

                var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(2));
                if (null != names[0] && null != names[1])
                {
                    xShortcut.SetAttributeValue("ShortName", names[0]);
                    xShortcut.SetAttributeValue("Name", names[1]);
                }
                else if (null != names[0])
                {
                    xShortcut.SetAttributeValue("Name", names[0]);
                }

                if (!row.IsColumnNull(10))
                {
                    switch (row.FieldAsInteger(10))
                    {
                        case 1:
                            xShortcut.SetAttributeValue("Show", "normal");
                            break;
                        case 3:
                            xShortcut.SetAttributeValue("Show", "maximized");
                            break;
                        case 7:
                            xShortcut.SetAttributeValue("Show", "minimized");
                            break;
                        default:
                            this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, table.Name, row.Fields[10].Column.Name, row[10]));
                            break;
                    }
                }

                // Only try to read the MSI 4.0-specific columns if they actually exist
                if (15 < row.Fields.Length)
                {
                    if (!row.IsColumnNull(12))
                    {
                        xShortcut.SetAttributeValue("DisplayResourceDll", row.FieldAsString(12));
                    }

                    if (null != row[13])
                    {
                        xShortcut.SetAttributeValue("DisplayResourceId", row.FieldAsInteger(13));
                    }

                    if (null != row[14])
                    {
                        xShortcut.SetAttributeValue("DescriptionResourceDll", row.FieldAsString(14));
                    }

                    if (null != row[15])
                    {
                        xShortcut.SetAttributeValue("DescriptionResourceId", row.FieldAsInteger(15));
                    }
                }

                this.AddChildToParent("Component", xShortcut, row, 3);
                this.DecompilerHelper.IndexElement(row, xShortcut);
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
                var fileSearch = new XElement(Names.FileSearchElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    XAttributeIfNotNull("MinVersion", row, 2),
                    XAttributeIfNotNull("MaxVersion", row, 3),
                    XAttributeIfNotNull("MinSize", row, 4),
                    XAttributeIfNotNull("MaxSize", row, 5),
                    XAttributeIfNotNull("Languages", row, 8));

                var names = this.BackendHelper.SplitMsiFileName(row.FieldAsString(1));
                if (null != names[0])
                {
                    // it is permissable to just have a long name
                    if (!this.BackendHelper.IsValidShortFilename(names[0], false) && null == names[1])
                    {
                        fileSearch.SetAttributeValue("Name", names[0]);
                    }
                    else
                    {
                        fileSearch.SetAttributeValue("ShortName", names[0]);
                    }
                }

                if (null != names[1])
                {
                    fileSearch.SetAttributeValue("Name", names[1]);
                }

                if (!row.IsColumnNull(6))
                {
                    fileSearch.SetAttributeValue("MinDate", ConvertIntegerToDateTime(row.FieldAsInteger(6)));
                }

                if (!row.IsColumnNull(7))
                {
                    fileSearch.SetAttributeValue("MaxDate", ConvertIntegerToDateTime(row.FieldAsInteger(7)));
                }

                this.DecompilerHelper.IndexElement(row, fileSearch);
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
                if (!this.PatchTargetFiles.TryGetValue(row.FieldAsString(0), out var xPatchTargetFile))
                {
                    xPatchTargetFile = new XElement(Names.TargetFileElement,
                        new XAttribute("Id", row.FieldAsString(1)));

                    if (this.DecompilerHelper.TryGetIndexedElement("TargetImages", row.FieldAsString(0), out var xTargetImage))
                    {
                        xTargetImage.Add(xPatchTargetFile);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Target", row.FieldAsString(0), "TargetImages"));
                    }

                    this.PatchTargetFiles.Add(row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), xPatchTargetFile);
                }

                AddSymbolPaths(row, 2, xPatchTargetFile);

                if (!row.IsColumnNull(3) && !row.IsColumnNull(4))
                {
                    var ignoreOffsets = row.FieldAsString(3).Split(',');
                    var ignoreLengths = row.FieldAsString(4).Split(',');

                    if (ignoreOffsets.Length == ignoreLengths.Length)
                    {
                        for (var i = 0; i < ignoreOffsets.Length; i++)
                        {
                            var xIgnoreRange = new XElement(Names.IgnoreRangeElement);

                            if (ignoreOffsets[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                xIgnoreRange.SetAttributeValue("Offset", Convert.ToInt32(ignoreOffsets[i].Substring(2), 16));
                            }
                            else
                            {
                                xIgnoreRange.SetAttributeValue("Offset", Convert.ToInt32(ignoreOffsets[i], CultureInfo.InvariantCulture));
                            }

                            if (ignoreLengths[i].StartsWith("0x", StringComparison.Ordinal))
                            {
                                xIgnoreRange.SetAttributeValue("Length", Convert.ToInt32(ignoreLengths[i].Substring(2), 16));
                            }
                            else
                            {
                                xIgnoreRange.SetAttributeValue("Length", Convert.ToInt32(ignoreLengths[i], CultureInfo.InvariantCulture));
                            }

                            xPatchTargetFile.Add(xIgnoreRange);
                        }
                    }
                    else
                    {
                        // TODO: warn
                    }
                }
                else if (!row.IsColumnNull(3) || !row.IsColumnNull(4))
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
                var xTargetImage = new XElement(Names.TargetImageElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("SourceFile", row.FieldAsString(1)),
                    new XAttribute("Order", row.FieldAsInteger(4)),
                    XAttributeIfNotNull("Validation", row, 5));

                AddSymbolPaths(row, 2, xTargetImage);

                if (0 != row.FieldAsInteger(6))
                {
                    xTargetImage.SetAttributeValue("IgnoreMissingFiles", "yes");
                }

                this.AddChildToParent("UpgradedImages", xTargetImage, row, 3);
                this.DecompilerHelper.IndexElement(row, xTargetImage);
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
                var xTextStyle = new XElement(Names.TextStyleElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("FaceName", row.FieldAsString(1)),
                    new XAttribute("Size", row.FieldAsString(2)));

                if (!row.IsColumnNull(3))
                {
                    var color = row.FieldAsInteger(3);

                    xTextStyle.SetAttributeValue("Red", color & 0xFF);
                    xTextStyle.SetAttributeValue("Green", (color & 0xFF00) >> 8);
                    xTextStyle.SetAttributeValue("Blue", (color & 0xFF0000) >> 16);
                }

                if (!row.IsColumnNull(4))
                {
                    var styleBits = row.FieldAsInteger(4);

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsBold == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsBold))
                    {
                        xTextStyle.SetAttributeValue("Bold", "yes");
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsItalic))
                    {
                        xTextStyle.SetAttributeValue("Italic", "yes");
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsUnderline))
                    {
                        xTextStyle.SetAttributeValue("Underline", "yes");
                    }

                    if (WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike == (styleBits & WindowsInstallerConstants.MsidbTextStyleStyleBitsStrike))
                    {
                        xTextStyle.SetAttributeValue("Strike", "yes");
                    }
                }

                this.UIElement.Add(xTextStyle);
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
                var id = row.FieldAsString(0);
                var xTypeLib = new XElement(Names.TypeLibElement,
                    new XAttribute("Advertise", "yes"),
                    new XAttribute("Id", id),
                    new XAttribute("Language", row.FieldAsInteger(1)),
                    XAttributeIfNotNull("Description", row, 4),
                    XAttributeIfNotNull("HelpDirectory", row, 5));

                if (!row.IsColumnNull(3))
                {
                    var version = row.FieldAsInteger(3);

                    if (65536 == version)
                    {
                        this.Messaging.Write(WarningMessages.PossiblyIncorrectTypelibVersion(row.SourceLineNumbers, id));
                    }

                    xTypeLib.SetAttributeValue("MajorVersion", (version & 0xFFFF00) >> 8);
                    xTypeLib.SetAttributeValue("MinorVersion", version & 0xFF);
                }

                if (!row.IsColumnNull(7))
                {
                    xTypeLib.SetAttributeValue("Cost", row.FieldAsInteger(7));
                }

                // nested under the appropriate File element in FinalizeFileTable
                this.DecompilerHelper.IndexElement(row, xTypeLib);
            }
        }

        /// <summary>
        /// Decompile the Upgrade table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUpgradeTable(Table table)
        {
            var xUpgrades = new Dictionary<string, XElement>();

            foreach (UpgradeRow upgradeRow in table.Rows)
            {
                if (WixUpgradeConstants.UpgradeDetectedProperty == upgradeRow.ActionProperty || WixUpgradeConstants.DowngradeDetectedProperty == upgradeRow.ActionProperty)
                {
                    continue; // MajorUpgrade rows processed in FinalizeUpgradeTable
                }

                if (!xUpgrades.TryGetValue(upgradeRow.UpgradeCode, out var xUpgrade))
                {
                    xUpgrade = new XElement(Names.UpgradeElement,
                        new XAttribute("Id", upgradeRow.UpgradeCode));

                    this.DecompilerHelper.AddElementToRoot(xUpgrade);
                    xUpgrades.Add(upgradeRow.UpgradeCode, xUpgrade);
                }

                var xUpgradeVersion = new XElement(Names.UpgradeVersionElement,
                        new XAttribute("Id", upgradeRow.UpgradeCode),
                        new XAttribute("Property", upgradeRow.ActionProperty));

                if (null != upgradeRow.VersionMin)
                {
                    xUpgradeVersion.SetAttributeValue("Minimum", upgradeRow.VersionMin);
                }

                if (null != upgradeRow.VersionMax)
                {
                    xUpgradeVersion.SetAttributeValue("Maximum", upgradeRow.VersionMax);
                }

                if (null != upgradeRow.Language)
                {
                    xUpgradeVersion.SetAttributeValue("Language", upgradeRow.Language);
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures))
                {
                    xUpgradeVersion.SetAttributeValue("MigrateFeatures", "yes");
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect))
                {
                    xUpgradeVersion.SetAttributeValue("OnlyDetect", "yes");
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure))
                {
                    xUpgradeVersion.SetAttributeValue("IgnoreRemoveFailure", "yes");
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive))
                {
                    xUpgradeVersion.SetAttributeValue("IncludeMinimum", "yes");
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive))
                {
                    xUpgradeVersion.SetAttributeValue("IncludeMaximum", "yes");
                }

                if (WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive == (upgradeRow.Attributes & WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive))
                {
                    xUpgradeVersion.SetAttributeValue("ExcludeLanguages", "yes");
                }

                if (null != upgradeRow.Remove)
                {
                    xUpgradeVersion.SetAttributeValue("RemoveFeatures", upgradeRow.Remove);
                }

                xUpgrade.Add(xUpgradeVersion);
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
                var xUpgradeFile = new XElement(Names.UpgradeFileElement,
                    new XAttribute("File", row.FieldAsString(1)),
                    new XAttribute("Ignore", "no"));

                AddSymbolPaths(row, 2, xUpgradeFile);

                if (!row.IsColumnNull(3) && 1 == row.FieldAsInteger(3))
                {
                    xUpgradeFile.SetAttributeValue("AllowIgnoreOnError", "yes");
                }

                if (!row.IsColumnNull(4) && 0 != row.FieldAsInteger(4))
                {
                    xUpgradeFile.SetAttributeValue("WholeFile", "yes");
                }

                this.AddChildToParent("UpgradedImages", xUpgradeFile, row, 0);
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
                if ("*" != row.FieldAsString(0))
                {
                    var xUpgradeFile = new XElement(Names.UpgradeFileElement,
                        new XAttribute("File", row.FieldAsString(1)),
                        new XAttribute("Ignore", "yes"));

                    this.AddChildToParent("UpgradedImages", xUpgradeFile, row, 0);
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
                var xUpgradeImage = new XElement(Names.UpgradeImageElement,
                        new XAttribute("Id", row.FieldAsString(0)),
                        new XAttribute("SourceFile", row.FieldAsString(1)),
                        XAttributeIfNotNull("SourcePatch", row, 2));

                AddSymbolPaths(row, 3, xUpgradeImage);

                this.AddChildToParent("ImageFamilies", xUpgradeImage, row, 4);
                this.DecompilerHelper.IndexElement(row, xUpgradeImage);
            }
        }

        private static void AddSymbolPaths(Row row, int column, XElement xParent)
        {
            if (!row.IsColumnNull(column))
            {
                var symbolPaths = row.FieldAsString(column).Split(';');

                foreach (var symbolPath in symbolPaths)
                {
                    var xSymbolPath = new XElement(Names.SymbolPathElement,
                        new XAttribute("Path", symbolPath));

                    xParent.Add(xSymbolPath);
                }
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
                var xUiText = new XElement(Names.UITextElement,
                    new XAttribute("Id", row.FieldAsString(0)),
                    XAttributeIfNotNull("Value", row, 1));

                this.UIElement.Add(xUiText);
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
                var verb = new XElement(Names.VerbElement,
                    new XAttribute("Id", row.FieldAsString(1)),
                    XAttributeIfNotNull("Sequence", row, 2),
                    XAttributeIfNotNull("Command", row, 3),
                    XAttributeIfNotNull("Argument", row, 4));

                this.DecompilerHelper.IndexElement(row, verb);
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
        private bool GetRegistryRootType(SourceLineNumber sourceLineNumbers, string tableName, Field field, out string registryRootType)
        {
            switch (Convert.ToInt32(field.Data))
            {
                case (-1):
                    registryRootType = "HKMU";
                    return true;
                case WindowsInstallerConstants.MsidbRegistryRootClassesRoot:
                    registryRootType = "HKCR";
                    return true;
                case WindowsInstallerConstants.MsidbRegistryRootCurrentUser:
                    registryRootType = "HKCU";
                    return true;
                case WindowsInstallerConstants.MsidbRegistryRootLocalMachine:
                    registryRootType = "HKLM";
                    return true;
                case WindowsInstallerConstants.MsidbRegistryRootUsers:
                    registryRootType = "HKU";
                    return true;
                default:
                    this.Messaging.Write(WarningMessages.IllegalColumnValue(sourceLineNumbers, tableName, field.Column.Name, field.Data));
                    registryRootType = null; // assign anything to satisfy the out parameter
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
            if (OutputType.Package == this.OutputType)
            {
                var featureField = row.Fields[featureColumnIndex];
                var componentField = row.Fields[componentColumnIndex];

                if (this.DecompilerHelper.TryGetIndexedElement("FeatureComponents", featureField.AsString(), componentField.AsString(), out var xComponentRef))
                {
                    xComponentRef.SetAttributeValue("Primary", "yes");
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, row.TableDefinition.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), featureField.Column.Name, Convert.ToString(featureField.Data), componentField.Column.Name, Convert.ToString(componentField.Data), "FeatureComponents"));
                }
            }
        }

        private static void AssignActionSequence(WixActionSymbol actionSymbol, XElement xAction)
        {
            switch (actionSymbol.Sequence)
            {
                case (-4):
                    xAction.SetAttributeValue("OnExit", "suspend");
                    break;
                case (-3):
                    xAction.SetAttributeValue("OnExit", "error");
                    break;
                case (-2):
                    xAction.SetAttributeValue("OnExit", "cancel");
                    break;
                case (-1):
                    xAction.SetAttributeValue("OnExit", "success");
                    break;
                default:
                    if (null != actionSymbol.Before)
                    {
                        xAction.SetAttributeValue("Before", actionSymbol.Before);
                    }
                    else if (null != actionSymbol.After)
                    {
                        xAction.SetAttributeValue("After", actionSymbol.After);
                    }
                    else if (actionSymbol.Sequence.HasValue)
                    {
                        xAction.SetAttributeValue("Sequence", actionSymbol.Sequence.Value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Checks the InstallExecuteSequence table to determine where RemoveExistingProducts is scheduled and removes it.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private static string DetermineMajorUpgradeScheduling(TableIndexedCollection tables)
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
                    var action = row.FieldAsString(0);
                    var sequence = row.FieldAsInteger(2);

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
                return "afterInstallValidate";
            }
            else if (0 != sequenceInstallInitialize && sequenceInstallInitialize < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallExecute)
            {
                return "afterInstallInitialize";
            }
            else if (0 != sequenceInstallExecute && sequenceInstallExecute < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallExecuteAgain)
            {
                return "afterInstallExecute";
            }
            else if (0 != sequenceInstallExecuteAgain && sequenceInstallExecuteAgain < sequenceRemoveExistingProducts && sequenceRemoveExistingProducts < sequenceInstallFinalize)
            {
                return "afterInstallExecuteAgain";
            }
            else
            {
                return "afterInstallFinalize";
            }
        }
    }
}
