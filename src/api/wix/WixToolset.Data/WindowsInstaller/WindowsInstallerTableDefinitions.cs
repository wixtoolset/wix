// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using WixToolset.Data.WindowsInstaller.Rows;

    public static class WindowsInstallerTableDefinitions
    {
        public static readonly TableDefinition ActionText = new TableDefinition(
            "ActionText",
            SymbolDefinitions.ActionText,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to be described."),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description displayed in progress dialog and log when action is executing."),
                new ColumnDefinition("Template", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Template, description: "Optional localized format template used to format action data records for display during action execution."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition AdminExecuteSequence = new TableDefinition(
            "AdminExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Condition = new TableDefinition(
            "Condition",
            SymbolDefinitions.Condition,
            new[]
            {
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Reference to a Feature entry in Feature table.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Level", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "New selection Level to set in Feature table if Condition evaluates to TRUE."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Expression evaluated to determine if Level in the Feature table is to change.", forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition AdminUISequence = new TableDefinition(
            "AdminUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition AdvtExecuteSequence = new TableDefinition(
            "AdvtExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition AdvtUISequence = new TableDefinition(
            "AdvtUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition AppId = new TableDefinition(
            "AppId",
            SymbolDefinitions.AppId,
            new[]
            {
                new ColumnDefinition("AppId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid),
                new ColumnDefinition("RemoteServerName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted),
                new ColumnDefinition("LocalService", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("ServiceParameters", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("DllSurrogate", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("ActivateAtStorage", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
                new ColumnDefinition("RunAsInteractiveUser", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition AppSearch = new TableDefinition(
            "AppSearch",
            SymbolDefinitions.AppSearch,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The property associated with a Signature"),
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Signature;RegLocator;IniLocator;DrLocator;CompLocator", keyColumn: 1, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature,  RegLocator, IniLocator, CompLocator and the DrLocator tables."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Property = new TableDefinition(
            "Property",
            SymbolDefinitions.Property,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of property, uppercase if settable by launcher or loader."),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "String value for property.  Never null or empty."),
            },
            strongRowType: typeof(PropertyRow),
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition BBControl = new TableDefinition(
            "BBControl",
            SymbolDefinitions.BBControl,
            new[]
            {
                new ColumnDefinition("Billboard_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Billboard", keyColumn: 1, description: "External key to the Billboard table, name of the billboard."),
                new ColumnDefinition("BBControl", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the control. This name must be unique within a billboard, but can repeat on different billboard.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Type", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The type of the control.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Horizontal coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Vertical coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this control."),
                new ColumnDefinition("Text", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A string used to set the initial text contained within a control (if appropriate)."),
            },
            strongRowType: typeof(BBControlRow),
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Billboard = new TableDefinition(
            "Billboard",
            SymbolDefinitions.Billboard,
            new[]
            {
                new ColumnDefinition("Billboard", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the billboard."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "An external key to the Feature Table. The billboard is shown only if this feature is being installed.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Action", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "The name of an action. The billboard is displayed during the progress messages received from this action.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Ordering", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "A positive integer. If there is more than one billboard corresponding to an action they will be shown in the order defined by this column."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Feature = new TableDefinition(
            "Feature",
            SymbolDefinitions.Feature,
            new[]
            {
                new ColumnDefinition("Feature", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular feature record.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Feature_Parent", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Optional key of a parent record in the same table. If the parent is not selected, then the record will not be installed. Null indicates a root item.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Title", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Short text identifying a visible feature item."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Longer descriptive text describing a visible feature item."),
                new ColumnDefinition("Display", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Numeric sort order, used to force a specific display ordering."),
                new ColumnDefinition("Level", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The install level at which record will be initially selected. An install level of 0 will disable an item and prevent its display."),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.UpperCase, keyTable: "Directory", keyColumn: 1, description: "The name of the Directory that can be configured by the UI. A non-null value will enable the browse button.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;2;4;5;6;8;9;10;16;17;18;20;21;22;24;25;26;32;33;34;36;37;38;48;49;50;52;53;54", description: "Feature attributes"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Binary = new TableDefinition(
            "Binary",
            SymbolDefinitions.Binary,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique key identifying the binary data."),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "The unformatted binary data."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition BindImage = new TableDefinition(
            "BindImage",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "The index into the File table. This must be an executable file."),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Paths, description: "A list of ;  delimited paths that represent the paths to be searched for the import DLLS. The list is usually a list of properties each enclosed within square brackets [] ."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition File = new TableDefinition(
            "File",
            SymbolDefinitions.File,
            new[]
            {
                new ColumnDefinition("File", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token, must match identifier in cabinet.  For uncompressed files, this field is ignored."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the file."),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "File name used for installation, may be localized.  This may contain a \"short name|long name\" pair."),
                new ColumnDefinition("FileSize", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("Version", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, keyTable: "File", keyColumn: 1, description: "Version string for versioned files;  Blank for unversioned files.", modularizeType: ColumnModularizeType.CompanionFile),
                new ColumnDefinition("Language", ColumnType.String, 20, primaryKey: false, nullable: true, ColumnCategory.Language, description: "List of decimal language Ids, comma-separated if more than one."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Integer containing bit flags representing file attributes (with the decimal value of each bit position in parentheses)"),
                new ColumnDefinition("Sequence", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 2147483647, description: "Sequence with respect to the media images; order must track cabinet order."),
                new ColumnDefinition("DiskId", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "Disk identifier for the file.", unreal: true),
                new ColumnDefinition("Source", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "Path to source of file.", unreal: true),
            },
            strongRowType: typeof(FileRow),
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition CCPSearch = new TableDefinition(
            "CCPSearch",
            SymbolDefinitions.CCPSearch,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Signature;RegLocator;IniLocator;DrLocator;CompLocator", keyColumn: 1, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature,  RegLocator, IniLocator, CompLocator and the DrLocator tables."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition CheckBox = new TableDefinition(
            "CheckBox",
            SymbolDefinitions.CheckBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to the item."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value string associated with the item."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Class = new TableDefinition(
            "Class",
            SymbolDefinitions.Class,
            new[]
            {
                new ColumnDefinition("CLSID", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "The CLSID of an OLE factory."),
                new ColumnDefinition("Context", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The numeric server context for this server. CLSCTX_xxxx", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent."),
                new ColumnDefinition("ProgId_Default", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "Optional ProgId associated with this CLSID."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description for the Class."),
                new ColumnDefinition("AppId_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, keyTable: "AppId", keyColumn: 1, description: "Optional AppID containing DCOM information for associated application (string GUID)."),
                new ColumnDefinition("FileTypeMask", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Optional string containing information for the HKCRthis CLSID) key. If multiple patterns exist, they must be delimited by a semicolon, and numeric subkeys will be generated: 0,1,2..."),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Optional foreign key into the Icon Table, specifying the icon file associated with this CLSID. Will be written under the DefaultIcon key.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "Optional icon index."),
                new ColumnDefinition("DefInprocHandler", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Filename, possibilities: "1;2;3", description: "Optional default inproc handler.  Only optionally provided if Context=CLSCTX_LOCAL_SERVER.  Typically \"ole32.dll\" or \"mapi32.dll\""),
                new ColumnDefinition("Argument", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "optional argument for LocalServers."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the CLSID factory to be operational.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, maxValue: 32767, description: "Class registration attributes."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Component = new TableDefinition(
            "Component",
            SymbolDefinitions.Component,
            new[]
            {
                new ColumnDefinition("Component", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular component record."),
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, description: "A string GUID unique to this component, version, and language."),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Required key of a Directory table record. This is actually a property name whose value contains the actual path, set either by the AppSearch action or with the default setting obtained from the Directory table."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Remote execution option, one of irsEnum"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "A conditional statement that will disable this component if the specified condition evaluates to the 'True' state. If a component is disabled, it will not be installed, regardless of the 'Action' state associated with the component.", forceLocalizable: true),
                new ColumnDefinition("KeyPath", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File;Registry;ODBCDataSource", keyColumn: 1, description: "Either the primary key into the File table, Registry table, or ODBCDataSource table. This extract path is stored when the component is installed, and is used to detect the presence of the component and to return the path to it."),
            },
            strongRowType: typeof(ComponentRow),
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Icon = new TableDefinition(
            "Icon",
            SymbolDefinitions.Icon,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. Name of the icon file.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "Binary stream. The binary icon data in PE (.DLL or .EXE) or icon (.ICO) format."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ProgId = new TableDefinition(
            "ProgId",
            SymbolDefinitions.ProgId,
            new[]
            {
                new ColumnDefinition("ProgId", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The Program Identifier. Primary key."),
                new ColumnDefinition("ProgId_Parent", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "The Parent Program Identifier. If specified, the ProgId column becomes a version independent prog id."),
                new ColumnDefinition("Class_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, keyTable: "Class", keyColumn: 1, description: "The CLSID of an OLE factory corresponding to the ProgId."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description for the Program identifier."),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Optional foreign key into the Icon Table, specifying the icon file associated with this ProgId. Will be written under the DefaultIcon key.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "Optional icon index."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComboBox = new TableDefinition(
            "ComboBox",
            SymbolDefinitions.ComboBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same combobox."),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list. The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value.", forceLocalizable: true),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CompLocator = new TableDefinition(
            "CompLocator",
            SymbolDefinitions.CompLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table."),
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Guid, description: "A string GUID unique to this component, version, and language."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "A boolean value that determines if the registry value is a filename or a directory location."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Complus = new TableDefinition(
            "Complus",
            SymbolDefinitions.Complus,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the ComPlus component."),
                new ColumnDefinition("ExpType", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "ComPlus component attributes."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Directory = new TableDefinition(
            "Directory",
            SymbolDefinitions.Directory,
            new[]
            {
                new ColumnDefinition("Directory", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for directory entry, primary key. If a property by this name is defined, it contains the full path to the directory."),
                new ColumnDefinition("Directory_Parent", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Reference to the entry in this table specifying the default parent directory. A record parented to itself or with a Null parent represents a root of the install tree."),
                new ColumnDefinition("DefaultDir", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.DefaultDir, description: "The default sub-path under parent's path."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Control = new TableDefinition(
            "Control",
            SymbolDefinitions.Control,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "External key to the Dialog table, name of the dialog."),
                new ColumnDefinition("Control", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the control. This name must be unique within a dialog, but can repeat on different dialogs.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Type", ColumnType.String, 20, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The type of the control."),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Horizontal coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Vertical coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this control."),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "The name of a defined property to be linked to this control. "),
                new ColumnDefinition("Text", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "A string used to set the initial text contained within a control (if appropriate).", modularizeType: ColumnModularizeType.ControlText),
                new ColumnDefinition("Control_Next", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "The name of an other control on the same dialog. This link defines the tab order of the controls. The links have to form one or more cycles!", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Help", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The help strings used with the button. The text is optional. "),
            },
            strongRowType: typeof(ControlRow),
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Dialog = new TableDefinition(
            "Dialog",
            SymbolDefinitions.Dialog,
            new[]
            {
                new ColumnDefinition("Dialog", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the dialog."),
                new ColumnDefinition("HCentering", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 100, description: "Horizontal position of the dialog on a 0-100 scale. 0 means left end, 100 means right end of the screen, 50 center."),
                new ColumnDefinition("VCentering", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 100, description: "Vertical position of the dialog on a 0-100 scale. 0 means top end, 100 means bottom end of the screen, 50 center."),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the dialog."),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the dialog."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this dialog."),
                new ColumnDefinition("Title", ColumnType.Localized, 128, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "A text string specifying the title to be displayed in the title bar of the dialog's window."),
                new ColumnDefinition("Control_First", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the control that has the focus when the dialog is created.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Control_Default", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the default control. Hitting return is equivalent to pushing this button.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Control_Cancel", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the cancel control. Hitting escape or clicking on the close icon on the dialog is equivalent to pushing this button.", modularizeType: ColumnModularizeType.None),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ControlCondition = new TableDefinition(
            "ControlCondition",
            SymbolDefinitions.ControlCondition,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the dialog."),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Action", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "Default;Disable;Enable;Hide;Show", description: "The desired action to be taken on the specified control."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Condition, description: "A standard conditional statement that specifies under which conditions the action should be triggered.", forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ControlEvent = new TableDefinition(
            "ControlEvent",
            SymbolDefinitions.ControlEvent,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the dialog."),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Event", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "An identifier that specifies the type of the event that should take place when the user interacts with control specified by the first two entries."),
                new ColumnDefinition("Argument", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "A value to be used as a modifier when triggering a particular event.", modularizeType: ColumnModularizeType.ControlEventArgument, forceLocalizable: true),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Condition, description: "A standard conditional statement that specifies under which conditions an event should be triggered.", forceLocalizable: true),
                new ColumnDefinition("Ordering", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "An integer used to order several events tied to the same control. Can be left blank."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CreateFolder = new TableDefinition(
            "CreateFolder",
            SymbolDefinitions.CreateFolder,
            new[]
            {
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Primary key, could be foreign key into the Directory table."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CustomAction = new TableDefinition(
            "CustomAction",
            SymbolDefinitions.CustomAction,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, name of action, normally appears in sequence table unless private use."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "The numeric custom action type, consisting of source location, code type, entry, option flags."),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.CustomSource, description: "The table reference of the source of the code."),
                new ColumnDefinition("Target", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Excecution parameter, depends on the type of custom action", forceLocalizable: true),
                new ColumnDefinition("ExtendedType", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A numeric custom action type that extends code type or option flags of the Type column."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition DrLocator = new TableDefinition(
            "DrLocator",
            SymbolDefinitions.DrLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature table."),
                new ColumnDefinition("Parent", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Identifier, description: "The parent file signature. It is also a foreign key in the Signature table. If null and the Path column does not expand to a full path, then all the fixed drives of the user system are searched using the Path."),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.AnyPath, description: "The path on the user system. This is a either a subpath below the value of the Parent or a full path. The path may contain properties enclosed within [ ] that will be expanded."),
                new ColumnDefinition("Depth", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The depth below the path to which the Signature_ is recursively searched. If absent, the depth is assumed to be 0."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition DuplicateFile = new TableDefinition(
            "DuplicateFile",
            SymbolDefinitions.DuplicateFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular file entry"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the duplicate file."),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key referencing the source file to be duplicated."),
                new ColumnDefinition("DestName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Filename, description: "Filename to be given to the duplicate file."),
                new ColumnDefinition("DestFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full pathname to a destination folder."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Environment = new TableDefinition(
            "Environment",
            SymbolDefinitions.Environment,
            new[]
            {
                new ColumnDefinition("Environment", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for the environmental variable setting"),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the environmental value."),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to set in the environmental settings."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the environmental value."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Error = new TableDefinition(
            "Error",
            SymbolDefinitions.Error,
            new[]
            {
                new ColumnDefinition("Error", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Integer error number, obtained from header file IError(...) macros."),
                new ColumnDefinition("Message", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Template, description: "Error formatting template, obtained from user ed. or localizers.", useCData: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition EventMapping = new TableDefinition(
            "EventMapping",
            SymbolDefinitions.EventMapping,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the Dialog."),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Event", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "An identifier that specifies the type of the event that the control subscribes to.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Attribute", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The name of the control attribute, that is set when this event is received.", modularizeType: ColumnModularizeType.None),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Extension = new TableDefinition(
            "Extension",
            SymbolDefinitions.Extension,
            new[]
            {
                new ColumnDefinition("Extension", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The extension associated with the table row."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent."),
                new ColumnDefinition("ProgId_", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "Optional ProgId associated with this extension."),
                new ColumnDefinition("MIME_", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "MIME", keyColumn: 1, description: "Optional Context identifier, typically \"type/format\" associated with the extension"),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the CLSID factory to be operational.", modularizeType: ColumnModularizeType.None),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MIME = new TableDefinition(
            "MIME",
            SymbolDefinitions.MIME,
            new[]
            {
                new ColumnDefinition("ContentType", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key. Context identifier, typically \"type/format\"."),
                new ColumnDefinition("Extension_", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "Extension", keyColumn: 1, description: "Optional associated extension (without dot)"),
                new ColumnDefinition("CLSID", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, description: "Optional associated CLSID."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FeatureComponents = new TableDefinition(
            "FeatureComponents",
            SymbolDefinitions.FeatureComponents,
            new[]
            {
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into Feature table.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FileSFPCatalog = new TableDefinition(
            "FileSFPCatalog",
            SymbolDefinitions.FileSFPCatalog,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "File associated with the catalog", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SFPCatalog_", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Filename, keyTable: "SFPCatalog", keyColumn: 1, description: "Catalog associated with the file"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition SFPCatalog = new TableDefinition(
            "SFPCatalog",
            SymbolDefinitions.SFPCatalog,
            new[]
            {
                new ColumnDefinition("SFPCatalog", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Filename, description: "File name for the catalog."),
                new ColumnDefinition("Catalog", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "SFP Catalog"),
                new ColumnDefinition("Dependency", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Parent catalog - only used by SFP"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Font = new TableDefinition(
            "Font",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Primary key, foreign key into File table referencing font file."),
                new ColumnDefinition("FontTitle", ColumnType.String, 128, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Font name."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IniFile = new TableDefinition(
            "IniFile",
            SymbolDefinitions.IniFile,
            new[]
            {
                new ColumnDefinition("IniFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name in which to write the information"),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Foreign key into the Directory table denoting the directory where the .INI file is."),
                new ColumnDefinition("Section", ColumnType.Localized, 96, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file Section."),
                new ColumnDefinition("Key", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file key below Section."),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value to be written."),
                new ColumnDefinition("Action", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;3", description: "The type of modification to be made, one of iifEnum"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the .INI value."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IniLocator = new TableDefinition(
            "IniLocator",
            SymbolDefinitions.IniLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table."),
                new ColumnDefinition("FileName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name."),
                new ColumnDefinition("Section", ColumnType.String, 96, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Section name within in file (within square brackets in INI file)."),
                new ColumnDefinition("Key", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Key value (followed by an equals sign in INI file)."),
                new ColumnDefinition("Field", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The field in the .INI line. If Field is null or 0 the entire line is read."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "An integer value that determines if the .INI value read is a filename or a directory location or to be used as is w/o interpretation."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition InstallExecuteSequence = new TableDefinition(
            "InstallExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse. If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed. Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition InstallUISequence = new TableDefinition(
            "InstallUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse. If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed. Leave blank to suppress action."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IsolatedComponent = new TableDefinition(
            "IsolatedComponent",
            SymbolDefinitions.IsolatedComponent,
            new[]
            {
                new ColumnDefinition("Component_Shared", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Key to Component table item to be isolated"),
                new ColumnDefinition("Component_Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Key to Component table item for application"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition LaunchCondition = new TableDefinition(
            "LaunchCondition",
            SymbolDefinitions.LaunchCondition,
            new[]
            {
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Condition, description: "Expression which must evaluate to TRUE in order for install to commence.", forceLocalizable: true),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Localizable text to display when condition fails and install must abort."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ListBox = new TableDefinition(
            "ListBox",
            SymbolDefinitions.ListBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same listbox."),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value."),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ListView = new TableDefinition(
            "ListView",
            SymbolDefinitions.ListView,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same listview."),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value."),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value."),
                new ColumnDefinition("Binary_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Binary", keyColumn: 1, description: "The name of the icon to be displayed with the icon. The binary information is looked up from the Binary Table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition LockPermissions = new TableDefinition(
            "LockPermissions",
            SymbolDefinitions.LockPermissions,
            new[]
            {
                new ColumnDefinition("LockObject", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Foreign key into Registry or File table"),
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, possibilities: "Directory;File;Registry", description: "Reference to another table name", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Domain", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Formatted, description: "Domain name for user whose permissions are being set. (usually a property)"),
                new ColumnDefinition("User", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "User for permissions to be set.  (usually a property)"),
                new ColumnDefinition("Permission", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Permission Access mask. Full Control = 268435456 (GENERIC_ALL = 0x10000000)"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiLockPermissionsEx = new TableDefinition(
            "MsiLockPermissionsEx",
            SymbolDefinitions.MsiLockPermissionsEx,
            new[]
            {
                new ColumnDefinition("MsiLockPermissionsEx", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token"),
                new ColumnDefinition("LockObject", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Foreign key into Registry, File, CreateFolder, or ServiceInstall table"),
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Identifier, possibilities: "CreateFolder;File;Registry;ServiceInstall", description: "Reference to another table name", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("SDDLText", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.FormattedSDDLText, description: "String to indicate permissions to be applied to the LockObject"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Expression which must evaluate to TRUE in order for this set of permissions to be applied"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Media = new TableDefinition(
            "Media",
            SymbolDefinitions.Media,
            new[]
            {
                new ColumnDefinition("DiskId", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "Primary key, integer to determine sort order for table."),
                new ColumnDefinition("LastSequence", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "File sequence number for the last file for this media."),
                new ColumnDefinition("DiskPrompt", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Disk name: the visible text actually printed on the disk.  This will be used to prompt the user when this disk needs to be inserted."),
                new ColumnDefinition("Cabinet", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Cabinet, description: "If some or all of the files stored on the media are compressed in a cabinet, the name of that cabinet."),
                new ColumnDefinition("VolumeLabel", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The label attributed to the volume."),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Property, description: "The property defining the location of the cabinet file.", modularizeType: ColumnModularizeType.None),
            },
            strongRowType: typeof(MediaRow),
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MoveFile = new TableDefinition(
            "MoveFile",
            SymbolDefinitions.MoveFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key that uniquely identifies a particular MoveFile record"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "If this component is not \"selected\" for installation or removal, no action will be taken on the associated MoveFile entry"),
                new ColumnDefinition("SourceName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Name of the source file(s) to be moved or copied.  Can contain the '*' or '?' wildcards."),
                new ColumnDefinition("DestName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Filename, description: "Name to be given to the original file after it is moved or copied.  If blank, the destination file will be given the same name as the source file"),
                new ColumnDefinition("SourceFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the source directory"),
                new ColumnDefinition("DestFolder", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the destination directory"),
                new ColumnDefinition("Options", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Integer value specifying the MoveFile operating mode, one of imfoEnum"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiAssembly = new TableDefinition(
            "MsiAssembly",
            SymbolDefinitions.Assembly,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into Feature table.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("File_Manifest", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into the File table denoting the manifest file for the assembly."),
                new ColumnDefinition("File_Application", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into File table, denoting the application context for private assemblies. Null for global assemblies."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Assembly attributes"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiAssemblyName = new TableDefinition(
            "MsiAssemblyName",
            SymbolDefinitions.MsiAssemblyName,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table."),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The name part of the name-value pairs for the assembly name."),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The value part of the name-value pairs for the assembly name."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiDigitalCertificate = new TableDefinition(
            "MsiDigitalCertificate",
            SymbolDefinitions.MsiDigitalCertificate,
            new[]
            {
                new ColumnDefinition("DigitalCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique identifier for the row"),
                new ColumnDefinition("CertData", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "A certificate context blob for a signer certificate"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiDigitalSignature = new TableDefinition(
            "MsiDigitalSignature",
            SymbolDefinitions.MsiDigitalSignature,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "Media", description: "Reference to another table name (only Media table is supported)", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("SignObject", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Foreign key to Media table"),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate"),
                new ColumnDefinition("Hash", ColumnType.Object, 0, primaryKey: false, nullable: true, ColumnCategory.Binary, description: "The encoded hash blob from the digital signature"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiEmbeddedChainer = new TableDefinition(
            "MsiEmbeddedChainer",
            SymbolDefinitions.MsiEmbeddedChainer,
            new[]
            {
                new ColumnDefinition("MsiEmbeddedChainer", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key for the table."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "A conditional statement for running the user-defined function.", forceLocalizable: true),
                new ColumnDefinition("CommandLine", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value in this field is a part of the command line string passed to the executable file identified in the Source column."),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.CustomSource, description: "The location of the executable file for the user-defined function."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "2;18;50", description: "The functions listed in the MsiEmbeddedChainer table are described using the following custom action numeric types."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiEmbeddedUI = new TableDefinition(
            "MsiEmbeddedUI",
            SymbolDefinitions.MsiEmbeddedUI,
            new[]
            {
                new ColumnDefinition("MsiEmbeddedUI", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key for the table."),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the file that receives the binary information in the Data column."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;2;3", description: "Information about the data in the Data column."),
                new ColumnDefinition("MessageFilter", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Specifies the types of messages that are sent to the user interface DLL."),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "This column contains binary information."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiFileHash = new TableDefinition(
            "MsiFileHash",
            SymbolDefinitions.MsiFileHash,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Primary key, foreign key into File table referencing file with this hash"),
                new ColumnDefinition("Options", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Various options and attributes for this hash."),
                new ColumnDefinition("HashPart1", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart2", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart3", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart4", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiPackageCertificate = new TableDefinition(
            "MsiPackageCertificate",
            SymbolDefinitions.MsiPackageCertificate,
            new[]
            {
                new ColumnDefinition("PackageCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row."),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchCertificate = new TableDefinition(
            "MsiPatchCertificate",
            SymbolDefinitions.MsiPatchCertificate,
            new[]
            {
                new ColumnDefinition("PatchCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row."),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchHeaders = new TableDefinition(
            "MsiPatchHeaders",
            SymbolDefinitions.MsiPatchHeaders,
            new[]
            {
                new ColumnDefinition("StreamRef", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Header", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "Binary stream. The patch header, used for patch validation."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchMetadata = new TableDefinition(
            "PatchMetadata",
            SymbolDefinitions.PatchMetadata,
            new[]
            {
                new ColumnDefinition("Company", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Identifier, description: "Primary key. The name of the company.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. The name of the property.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Non-null, non-empty value of the metadata property."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchMetadata = new TableDefinition(
            "MsiPatchMetadata",
            SymbolDefinitions.MsiPatchMetadata,
            new[]
            {
                new ColumnDefinition("Company", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchOldAssemblyFile = new TableDefinition(
            "MsiPatchOldAssemblyFile",
            SymbolDefinitions.MsiPatchOldAssemblyFile,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into File table. Patch-only table."),
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiPatchOldAssemblyName", keyColumn: 1, description: "Foreign key into MsiPatchOldAssemblyName table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchOldAssemblyName = new TableDefinition(
            "MsiPatchOldAssemblyName",
            SymbolDefinitions.MsiPatchOldAssemblyName,
            new[]
            {
                new ColumnDefinition("Assembly", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique identifier for the row."),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The name part of the name-value pairs for the assembly name. This represents the old name for the assembly."),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The value part of the name-value pairs for the assembly name. This represents the old name for the assembly."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchSequence = new TableDefinition(
            "PatchSequence",
            SymbolDefinitions.PatchSequence,
            new[]
            {
                new ColumnDefinition("PatchFamily", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. The name of the family for the patch.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Target", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Text, description: "Primary key. Determines product code filtering for family."),
                new ColumnDefinition("Sequence", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Sequence information in version (x.x.x.x) format."),
                new ColumnDefinition("Supersede", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Indicates that this patch supersedes earlier patches."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchSequence = new TableDefinition(
            "MsiPatchSequence",
            SymbolDefinitions.MsiPatchFamily,
            new[]
            {
                new ColumnDefinition("PatchFamily", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ProductCode", ColumnType.String, 38, primaryKey: true, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Sequence", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCAttribute = new TableDefinition(
            "ODBCAttribute",
            SymbolDefinitions.ODBCAttribute,
            new[]
            {
                new ColumnDefinition("Driver_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ODBCDriver", keyColumn: 1, description: "Reference to ODBC driver in ODBCDriver table"),
                new ColumnDefinition("Attribute", ColumnType.String, 40, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Name of ODBC driver attribute"),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Value for ODBC driver attribute"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCDriver = new TableDefinition(
            "ODBCDriver",
            SymbolDefinitions.ODBCDriver,
            new[]
            {
                new ColumnDefinition("Driver", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for driver"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component"),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for driver, non-localized"),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Reference to key driver file"),
                new ColumnDefinition("File_Setup", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Optional reference to key driver setup DLL"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ODBCDataSource = new TableDefinition(
            "ODBCDataSource",
            SymbolDefinitions.ODBCDataSource,
            new[]
            {
                new ColumnDefinition("DataSource", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for data source"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component"),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for data source"),
                new ColumnDefinition("DriverDescription", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Reference to driver description, may be existing driver"),
                new ColumnDefinition("Registration", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Registration option: 0=machine, 1=user, others t.b.d."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ODBCSourceAttribute = new TableDefinition(
            "ODBCSourceAttribute",
            SymbolDefinitions.ODBCSourceAttribute,
            new[]
            {
                new ColumnDefinition("DataSource_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ODBCDataSource", keyColumn: 1, description: "Reference to ODBC data source in ODBCDataSource table"),
                new ColumnDefinition("Attribute", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Name of ODBC data source attribute"),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Value for ODBC data source attribute"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCTranslator = new TableDefinition(
            "ODBCTranslator",
            SymbolDefinitions.ODBCTranslator,
            new[]
            {
                new ColumnDefinition("Translator", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for translator"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component"),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for translator"),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Reference to key translator file"),
                new ColumnDefinition("File_Setup", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Optional reference to key translator setup DLL"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Patch = new TableDefinition(
            "Patch",
            SymbolDefinitions.Patch,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token, foreign key to File table, must match identifier in cabinet."),
                new ColumnDefinition("Sequence", ColumnType.Number, 4, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Primary key, sequence with respect to the media images; order must track cabinet order."),
                new ColumnDefinition("PatchSize", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Size of patch in bytes (long integer)."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Integer containing bit flags representing patch attributes"),
                new ColumnDefinition("Header", ColumnType.Object, 0, primaryKey: false, nullable: true, ColumnCategory.Binary, description: "Binary stream. The patch header, used for patch validation."),
                new ColumnDefinition("StreamRef_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Identifier. Foreign key to the StreamRef column of the MsiPatchHeaders table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchPackage = new TableDefinition(
            "PatchPackage",
            SymbolDefinitions.PatchPackage,
            new[]
            {
                new ColumnDefinition("PatchId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "A unique string GUID representing this patch."),
                new ColumnDefinition("Media_", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Foreign key to DiskId column of Media table. Indicates the disk containing the patch package."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PublishComponent = new TableDefinition(
            "PublishComponent",
            SymbolDefinitions.PublishComponent,
            new[]
            {
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "A string GUID that represents the component id that will be requested by the alien product."),
                new ColumnDefinition("Qualifier", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "This is defined only when the ComponentId column is an Qualified Component Id. This is the Qualifier for ProvideComponentIndirect."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table."),
                new ColumnDefinition("AppData", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "This is localisable Application specific data that can be associated with a Qualified Component."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into the Feature table.", modularizeType: ColumnModularizeType.None),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition RadioButton = new TableDefinition(
            "RadioButton",
            SymbolDefinitions.RadioButton,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this radio button. All the buttons tied to the same property become part of the same group."),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this button. Selecting the button will set the associated property to this value."),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The horizontal coordinate of the upper left corner of the bounding rectangle of the radio button.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The vertical coordinate of the upper left corner of the bounding rectangle of the radio button.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The width of the button.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The height of the button.", forceLocalizable: true),
                new ColumnDefinition("Text", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The visible title to be assigned to the radio button."),
                new ColumnDefinition("Help", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The help strings used with the button. The text is optional."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Registry = new TableDefinition(
            "Registry",
            SymbolDefinitions.Registry,
            new[]
            {
                new ColumnDefinition("Registry", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -1, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum."),
                new ColumnDefinition("Key", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value."),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name."),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the registry value."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RegLocator = new TableDefinition(
            "RegLocator",
            SymbolDefinitions.RegLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table. If the type is 0, the registry values refers a directory, and _Signature is not a foreign key."),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum."),
                new ColumnDefinition("Key", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value.", forceLocalizable: true),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name.", forceLocalizable: true),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 18, description: "An integer value that determines if the registry value is a filename or a directory location or to be used as is w/o interpretation."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveFile = new TableDefinition(
            "RemoveFile",
            SymbolDefinitions.RemoveFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular file entry"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the file to be removed."),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.WildCardFilename, description: "Name of the file to be removed."),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full pathname to the folder of the file to be removed."),
                new ColumnDefinition("InstallMode", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "1;2;3", description: "Installation option, one of iimEnum."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveIniFile = new TableDefinition(
            "RemoveIniFile",
            null,
            new[]
            {
                new ColumnDefinition("RemoveIniFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name in which to delete the information"),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Foreign key into the Directory table denoting the directory where the .INI file is."),
                new ColumnDefinition("Section", ColumnType.Localized, 96, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file Section."),
                new ColumnDefinition("Key", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file key below Section."),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to be deleted. The value is required when Action is iifIniRemoveTag"),
                new ColumnDefinition("Action", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "2;4", description: "The type of modification to be made, one of iifEnum."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the deletion of the .INI value."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveRegistry = new TableDefinition(
            "RemoveRegistry",
            SymbolDefinitions.RemoveRegistry,
            new[]
            {
                new ColumnDefinition("RemoveRegistry", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -1, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum"),
                new ColumnDefinition("Key", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value."),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the deletion of the registry value."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ReserveCost = new TableDefinition(
            "ReserveCost",
            SymbolDefinitions.ReserveCost,
            new[]
            {
                new ColumnDefinition("ReserveKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key that uniquely identifies a particular ReserveCost record"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reserve a specified amount of space if this component is to be installed."),
                new ColumnDefinition("ReserveFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the destination directory"),
                new ColumnDefinition("ReserveLocal", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Disk space to reserve if linked component is installed locally."),
                new ColumnDefinition("ReserveSource", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Disk space to reserve if linked component is installed to run from the source location."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SelfReg = new TableDefinition(
            "SelfReg",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into the File table denoting the module that needs to be registered."),
                new ColumnDefinition("Cost", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The cost of registering the module."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ServiceControl = new TableDefinition(
            "ServiceControl",
            SymbolDefinitions.ServiceControl,
            new[]
            {
                new ColumnDefinition("ServiceControl", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid"),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 187, description: "Bit field:  Install:  0x1 = Start, 0x2 = Stop, 0x8 = Delete, Uninstall: 0x10 = Start, 0x20 = Stop, 0x80 = Delete"),
                new ColumnDefinition("Arguments", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Arguments for the service.  Separate by [~]."),
                new ColumnDefinition("Wait", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Boolean for whether to wait for the service to fully start"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the startup of the service"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ServiceInstall = new TableDefinition(
            "ServiceInstall",
            SymbolDefinitions.ServiceInstall,
            new[]
            {
                new ColumnDefinition("ServiceInstall", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Internal Name of the Service"),
                new ColumnDefinition("DisplayName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "External Name of the Service"),
                new ColumnDefinition("ServiceType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Type of the service"),
                new ColumnDefinition("StartType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 4, description: "Type of the service"),
                new ColumnDefinition("ErrorControl", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Severity of error if service fails to start"),
                new ColumnDefinition("LoadOrderGroup", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "LoadOrderGroup"),
                new ColumnDefinition("Dependencies", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Other services this depends on to start.  Separate by [~], and end with [~][~]"),
                new ColumnDefinition("StartName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "User or object name to run service as"),
                new ColumnDefinition("Password", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "password to run service with.  (with StartName)"),
                new ColumnDefinition("Arguments", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Arguments to include in every start of the service, passed to WinMain"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the startup of the service"),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Description of service."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiServiceConfig = new TableDefinition(
            "MsiServiceConfig",
            SymbolDefinitions.MsiServiceConfig,
            new[]
            {
                new ColumnDefinition("MsiServiceConfig", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid"),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 7, description: "Bit field:   0x1 = Install, 0x2 = Uninstall, 0x4 = Reinstall"),
                new ColumnDefinition("ConfigType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Service Configuration Option"),
                new ColumnDefinition("Argument", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Argument(s) for service configuration. Value depends on the content of the ConfigType field"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the configuration of the service"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiServiceConfigFailureActions = new TableDefinition(
            "MsiServiceConfigFailureActions",
            SymbolDefinitions.MsiServiceConfigFailureActions,
            new[]
            {
                new ColumnDefinition("MsiServiceConfigFailureActions", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token"),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid"),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 7, description: "Bit field: 0x1 = Install, 0x2 = Uninstall, 0x4 = Reinstall"),
                new ColumnDefinition("ResetPeriod", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Time in seconds after which to reset the failure count to zero. Leave blank if it should never be reset"),
                new ColumnDefinition("RebootMessage", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Message to be broadcast to server users before rebooting"),
                new ColumnDefinition("Command", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Command line of the process to CreateProcess function to execute"),
                new ColumnDefinition("Actions", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A list of integer actions separated by [~] delimiters: 0 = SC_ACTION_NONE, 1 = SC_ACTION_RESTART, 2 = SC_ACTION_REBOOT, 3 = SC_ACTION_RUN_COMMAND. Terminate with [~][~]"),
                new ColumnDefinition("DelayActions", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A list of delays (time in milli-seconds), separated by [~] delmiters, to wait before taking the corresponding Action. Terminate with [~][~]"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the configuration of failure actions for the service"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Shortcut = new TableDefinition(
            "Shortcut",
            SymbolDefinitions.Shortcut,
            new[]
            {
                new ColumnDefinition("Shortcut", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token."),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key into the Directory table denoting the directory where the shortcut file is created."),
                new ColumnDefinition("Name", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The name of the shortcut to be created."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table denoting the component whose selection gates the the shortcut creation/deletion."),
                new ColumnDefinition("Target", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Shortcut, description: "The shortcut target. This is usually a property that is expanded to a file or a folder that the shortcut points to."),
                new ColumnDefinition("Arguments", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The command-line arguments for the shortcut."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The description for the shortcut."),
                new ColumnDefinition("Hotkey", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The hotkey for the shortcut. It has the virtual-key code for the key in the low-order byte, and the modifier flags in the high-order byte. "),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Foreign key into the File table denoting the external icon file for the shortcut.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "The icon index for the shortcut."),
                new ColumnDefinition("ShowCmd", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "1;3;7", description: "The show command for the application window.The following values may be used."),
                new ColumnDefinition("WkDir", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of property defining location of working directory."),
                new ColumnDefinition("DisplayResourceDLL", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The Formatted string providing the full path to the language neutral file containing the MUI Manifest."),
                new ColumnDefinition("DisplayResourceId", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The display name index for the shortcut. This must be a non-negative number."),
                new ColumnDefinition("DescriptionResourceDLL", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The Formatted string providing the full path to the language neutral file containing the MUI Manifest."),
                new ColumnDefinition("DescriptionResourceId", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The description name index for the shortcut. This must be a non-negative number."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiShortcutProperty = new TableDefinition(
            "MsiShortcutProperty",
            SymbolDefinitions.MsiShortcutProperty,
            new[]
            {
                new ColumnDefinition("MsiShortcutProperty", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token"),
                new ColumnDefinition("Shortcut_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Shortcut", keyColumn: 1, description: "Foreign key into the Shortcut table"),
                new ColumnDefinition("PropertyKey", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Canonical string representation of the Property Key being set"),
                new ColumnDefinition("PropVariantValue", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "String representation of the value in the property"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Signature = new TableDefinition(
            "Signature",
            SymbolDefinitions.Signature,
            new[]
            {
                new ColumnDefinition("Signature", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature represents a unique file signature."),
                new ColumnDefinition("FileName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the file. This may contain a \"short name|long name\" pair."),
                new ColumnDefinition("MinVersion", ColumnType.String, 20, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The minimum version of the file."),
                new ColumnDefinition("MaxVersion", ColumnType.String, 20, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The maximum version of the file."),
                new ColumnDefinition("MinSize", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The minimum size of the file."),
                new ColumnDefinition("MaxSize", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The maximum size of the file. "),
                new ColumnDefinition("MinDate", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The minimum creation date of the file."),
                new ColumnDefinition("MaxDate", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The maximum creation date of the file."),
                new ColumnDefinition("Languages", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Language, description: "The languages supported by the file."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SoftwareIdentificationTag = new TableDefinition(
            "SoftwareIdentificationTag",
            SymbolDefinitions.SoftwareIdentificationTag,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "The file that installs the software id tag."),
                new ColumnDefinition("Regid", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The regid for the software id tag."),
                new ColumnDefinition("TagId", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The unique id for the software id tag."),
                new ColumnDefinition("PersistentId", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The type of the software id tag."),
                new ColumnDefinition("Alias", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Alias for the software id tag."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TextStyle = new TableDefinition(
            "TextStyle",
            SymbolDefinitions.TextStyle,
            new[]
            {
                new ColumnDefinition("TextStyle", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the style. The primary key of this table. This name is embedded in the texts to indicate a style change.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("FaceName", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Text, description: "A string indicating the name of the font used. Required. The string must be at most 31 characters long.", forceLocalizable: true),
                new ColumnDefinition("Size", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The size of the font used. This size is given in our units (1/12 of the system font height). Assuming that the system font is set to 12 point size, this is equivalent to the point size.", forceLocalizable: true),
                new ColumnDefinition("Color", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 16777215, description: "A long integer indicating the color of the string in the RGB format (Red, Green, Blue each 0-255, RGB = R + 256*G + 256^2*B)."),
                new ColumnDefinition("StyleBits", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 15, description: "A combination of style bits."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition TypeLib = new TableDefinition(
            "TypeLib",
            SymbolDefinitions.TypeLib,
            new[]
            {
                new ColumnDefinition("LibID", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "The GUID that represents the library."),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The language of the library."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent."),
                new ColumnDefinition("Version", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 16777215, description: "The version of the library. The minor version is in the lower 8 bits of the integer. The major version is in the next 16 bits. "),
                new ColumnDefinition("Description", ColumnType.Localized, 128, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Optional. The foreign key into the Directory table denoting the path to the help file for the type library."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the type library to be operational.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Cost", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The cost associated with the registration of the typelib. This column is currently optional."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UIText = new TableDefinition(
            "UIText",
            SymbolDefinitions.UIText,
            new[]
            {
                new ColumnDefinition("Key", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique key that identifies the particular string."),
                new ColumnDefinition("Text", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The localized version of the string."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Upgrade = new TableDefinition(
            "Upgrade",
            SymbolDefinitions.Upgrade,
            new[]
            {
                new ColumnDefinition("UpgradeCode", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "The UpgradeCode GUID belonging to the products in this set."),
                new ColumnDefinition("VersionMin", ColumnType.String, 20, primaryKey: true, nullable: true, ColumnCategory.Text, description: "The minimum ProductVersion of the products in this set.  The set may or may not include products with this particular version."),
                new ColumnDefinition("VersionMax", ColumnType.String, 20, primaryKey: true, nullable: true, ColumnCategory.Text, description: "The maximum ProductVersion of the products in this set.  The set may or may not include products with this particular version."),
                new ColumnDefinition("Language", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Language, description: "A comma-separated list of languages for either products in this set or products not in this set.", forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The attributes of this product set."),
                new ColumnDefinition("Remove", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The list of features to remove when uninstalling a product from this set.  The default is \"ALL\"."),
                new ColumnDefinition("ActionProperty", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.UpperCase, description: "The property to set when a product in this set is found."),
            },
            strongRowType: typeof(UpgradeRow),
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Verb = new TableDefinition(
            "Verb",
            SymbolDefinitions.Verb,
            new[]
            {
                new ColumnDefinition("Extension_", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "Extension", keyColumn: 1, description: "The extension associated with the table row.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Verb", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The verb for the command."),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Order within the verbs for a particular extension. Also used simply to specify the default verb."),
                new ColumnDefinition("Command", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The command text."),
                new ColumnDefinition("Argument", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional value for the command arguments."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleAdminExecuteSequence = new TableDefinition(
            "ModuleAdminExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdminExecuteSequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdminUISequence = new TableDefinition(
            "ModuleAdminUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdminUISequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdvtExecuteSequence = new TableDefinition(
            "ModuleAdvtExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdvtExecuteSequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdvtUISequence = new TableDefinition(
            "ModuleAdvtUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdvtUISequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleComponents = new TableDefinition(
            "ModuleComponents",
            SymbolDefinitions.ModuleComponents,
            new[]
            {
                new ColumnDefinition("Component", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component contained in the module."),
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "Module containing the component."),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "Default language ID for module (may be changed by transform).", forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleSignature = new TableDefinition(
            "ModuleSignature",
            SymbolDefinitions.WixModule,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Module identifier (String.GUID)."),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Default decimal language of module.", forceLocalizable: true),
                new ColumnDefinition("Version", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Version, description: "Version of the module."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleConfiguration = new TableDefinition(
            "ModuleConfiguration",
            SymbolDefinitions.ModuleConfiguration,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for this row.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Format", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "Format of this item."),
                new ColumnDefinition("Type", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Additional type information for this item."),
                new ColumnDefinition("ContextData", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Additional context information about this item."),
                new ColumnDefinition("DefaultValue", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Default value for this item."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "Additional type-specific attributes."),
                new ColumnDefinition("DisplayName", ColumnType.Localized, 72, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A short human-readable name for this item."),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A human-readable description."),
                new ColumnDefinition("HelpLocation", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Filename or namespace of the context-sensitive help for this item."),
                new ColumnDefinition("HelpKeyword", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Keyword index into the HelpLocation for this item."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleDependency = new TableDefinition(
            "ModuleDependency",
            SymbolDefinitions.ModuleDependency,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "Module requiring the dependency."),
                new ColumnDefinition("ModuleLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "Language of module requiring the dependency.", forceLocalizable: true),
                new ColumnDefinition("RequiredID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "String.GUID of required module."),
                new ColumnDefinition("RequiredLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "LanguageID of the required module.", forceLocalizable: true),
                new ColumnDefinition("RequiredVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Version of the required version."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleExclusion = new TableDefinition(
            "ModuleExclusion",
            SymbolDefinitions.ModuleExclusion,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "String.GUID of module with exclusion requirement."),
                new ColumnDefinition("ModuleLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "LanguageID of module with exclusion requirement.", forceLocalizable: true),
                new ColumnDefinition("ExcludedID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "String.GUID of excluded module."),
                new ColumnDefinition("ExcludedLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Language of excluded module.", forceLocalizable: true),
                new ColumnDefinition("ExcludedMinVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Minimum version of excluded module."),
                new ColumnDefinition("ExcludedMaxVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Maximum version of excluded module."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleIgnoreTable = new TableDefinition(
            "ModuleIgnoreTable",
            SymbolDefinitions.ModuleIgnoreTable,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Table name to ignore during merge operation.", modularizeType: ColumnModularizeType.None),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleInstallExecuteSequence = new TableDefinition(
            "ModuleInstallExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleInstallExecuteSequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleInstallUISequence = new TableDefinition(
            "ModuleInstallUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleInstallUISequence", keyColumn: 1, description: "Base action to determine insert location."),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleSubstitution = new TableDefinition(
            "ModuleSubstitution",
            SymbolDefinitions.ModuleSubstitution,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Table containing the data to be modified.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Row", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Row containing the data to be modified.", modularizeType: ColumnModularizeType.SemicolonDelimited),
                new ColumnDefinition("Column", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Column containing the data to be modified.", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Template for modification data."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Properties = new TableDefinition(
            "Properties",
            SymbolDefinitions.Properties,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key, non-localized token"),
                new ColumnDefinition("Value", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Value of the property"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ImageFamilies = new TableDefinition(
            "ImageFamilies",
            SymbolDefinitions.ImageFamilies,
            new[]
            {
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key"),
                new ColumnDefinition("MediaSrcPropName", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("MediaDiskId", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
                new ColumnDefinition("FileSequenceStart", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Integer, minValue: 1, maxValue: 214743647),
                new ColumnDefinition("DiskPrompt", ColumnType.String, 128, primaryKey: false, nullable: true, ColumnCategory.Text, forceLocalizable: true),
                new ColumnDefinition("VolumeLabel", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Text),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedImages = new TableDefinition(
            "UpgradedImages",
            SymbolDefinitions.UpgradedImages,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key"),
                new ColumnDefinition("MsiPath", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("PatchMsiPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "ImageFamilies", keyColumn: 1, description: "Foreign key, Family to which this image belongs"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedFilesToIgnore = new TableDefinition(
            "UpgradedFilesToIgnore",
            SymbolDefinitions.UpgradedFilesToIgnore,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "UpgradedImages", keyColumn: 1, description: "Foreign key, Upgraded image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedFilesOptionalData = new TableDefinition(
            "UpgradedFiles_OptionalData",
            SymbolDefinitions.UpgradedFilesOptionalData,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "UpgradedImages", keyColumn: 1, description: "Foreign key, Upgraded image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("AllowIgnoreOnPatchError", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
                new ColumnDefinition("IncludeWholeFile", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TargetImages = new TableDefinition(
            "TargetImages",
            SymbolDefinitions.TargetImages,
            new[]
            {
                new ColumnDefinition("Target", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("MsiPath", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "UpgradedImages", keyColumn: 1, description: "Foreign key, Upgraded image"),
                new ColumnDefinition("Order", ColumnType.Number, 0, primaryKey: false, nullable: false, ColumnCategory.Integer),
                new ColumnDefinition("ProductValidateFlags", ColumnType.String, 16, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreMissingSrcFiles", ColumnType.Number, 0, primaryKey: false, nullable: false, ColumnCategory.Integer),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TargetFilesOptionalData = new TableDefinition(
            "TargetFiles_OptionalData",
            SymbolDefinitions.TargetFilesOptionalData,
            new[]
            {
                new ColumnDefinition("Target", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "TargetImages", keyColumn: 1, description: "Foreign key, Target image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreLengths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("RetainOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FamilyFileRanges = new TableDefinition(
            "FamilyFileRanges",
            SymbolDefinitions.FamilyFileRanges,
            new[]
            {
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "ImageFamilies", keyColumn: 1, description: "Foreign key, Family"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RetainOffsets", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("RetainLengths", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ExternalFiles = new TableDefinition(
            "ExternalFiles",
            SymbolDefinitions.ExternalFiles,
            new[]
            {
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "ImageFamilies", keyColumn: 1, description: "Foreign key, Family"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FilePath", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreLengths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("RetainOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Order", ColumnType.Number, 0, primaryKey: false, nullable: false, ColumnCategory.Integer),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Streams = new TableDefinition(
            "_Streams",
            null,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 62, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            unreal: true,
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition SummaryInformation = new TableDefinition(
            "_SummaryInformation",
            SymbolDefinitions.SummaryInformation,
            new[]
            {
                new ColumnDefinition("PropertyId", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TransformView = new TableDefinition(
            "_TransformView",
            null,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Column", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Row", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Data", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Current", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Validation = new TableDefinition(
            "_Validation",
            null,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of table", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Column", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of column", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("Nullable", ColumnType.String, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "Y;N", description: "Whether the column is nullable"),
                new ColumnDefinition("MinValue", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Minimum value allowed"),
                new ColumnDefinition("MaxValue", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Maximum value allowed"),
                new ColumnDefinition("KeyTable", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "For foreign key, Name of table to which data must link", modularizeType: ColumnModularizeType.None),
                new ColumnDefinition("KeyColumn", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 1, maxValue: 32, description: "Column to which foreign key connects"),
                new ColumnDefinition("Category", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "Text;Formatted;Template;Condition;Guid;Path;Version;Language;Identifier;Binary;UpperCase;LowerCase;Filename;Paths;AnyPath;WildCardFilename;RegPath;CustomSource;Property;Cabinet;Shortcut;FormattedSDDLText;Integer;DoubleInteger;TimeDate;DefaultDir", description: "String category"),
                new ColumnDefinition("Set", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Set of values that are permitted"),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Description of column"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixDependencyProvider = new TableDefinition(
            "Wix4DependencyProvider",
            SymbolDefinitions.WixDependencyProvider,
            new[]
            {
                        new ColumnDefinition("WixDependencyProvider", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table."),
                        new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "The foreign key into the Component table used to determine install state.", modularizeType: ColumnModularizeType.Column),
                        new ColumnDefinition("ProviderKey", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the registry key that holds the provider identity."),
                        new ColumnDefinition("Version", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The version of the package."),
                        new ColumnDefinition("DisplayName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The display name of the package."),
                        new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixDependency = new TableDefinition(
            "Wix4Dependency",
            SymbolDefinitions.WixDependency,
            new[]
            {
                new ColumnDefinition("WixDependency", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table."),
                new ColumnDefinition("ProviderKey", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the registry key that holds the provider identity."),
                new ColumnDefinition("MinVersion", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The minimum version of the provider supported."),
                new ColumnDefinition("MaxVersion", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Version, description: "The maximum version of the provider supported."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixDependencyRef = new TableDefinition(
            "Wix4DependencyRef",
            SymbolDefinitions.WixDependencyRef,
            new[]
            {
                new ColumnDefinition("WixDependencyProvider_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4DependencyProvider", keyColumn: 1, description: "Foreign key into the Component table."),
                new ColumnDefinition("WixDependency_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4Dependency", keyColumn: 1, description: "Foreign key into the WixDependency table."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition[] All = new[]
        {
            ActionText,
            AdminExecuteSequence,
            Condition,
            AdminUISequence,
            AdvtExecuteSequence,
            AdvtUISequence,
            AppId,
            AppSearch,
            Property,
            BBControl,
            Billboard,
            Feature,
            Binary,
            BindImage,
            File,
            CCPSearch,
            CheckBox,
            Class,
            Component,
            Icon,
            ProgId,
            ComboBox,
            CompLocator,
            Complus,
            Directory,
            Control,
            Dialog,
            ControlCondition,
            ControlEvent,
            CreateFolder,
            CustomAction,
            DrLocator,
            DuplicateFile,
            Environment,
            Error,
            EventMapping,
            Extension,
            MIME,
            FeatureComponents,
            FileSFPCatalog,
            SFPCatalog,
            Font,
            IniFile,
            IniLocator,
            InstallExecuteSequence,
            InstallUISequence,
            IsolatedComponent,
            LaunchCondition,
            ListBox,
            ListView,
            LockPermissions,
            MsiLockPermissionsEx,
            Media,
            MoveFile,
            MsiAssembly,
            MsiAssemblyName,
            MsiDigitalCertificate,
            MsiDigitalSignature,
            MsiEmbeddedChainer,
            MsiEmbeddedUI,
            MsiFileHash,
            MsiPackageCertificate,
            MsiPatchCertificate,
            MsiPatchHeaders,
            PatchMetadata,
            MsiPatchMetadata,
            MsiPatchOldAssemblyFile,
            MsiPatchOldAssemblyName,
            PatchSequence,
            MsiPatchSequence,
            ODBCAttribute,
            ODBCDriver,
            ODBCDataSource,
            ODBCSourceAttribute,
            ODBCTranslator,
            Patch,
            PatchPackage,
            PublishComponent,
            RadioButton,
            Registry,
            RegLocator,
            RemoveFile,
            RemoveIniFile,
            RemoveRegistry,
            ReserveCost,
            SelfReg,
            ServiceControl,
            ServiceInstall,
            MsiServiceConfig,
            MsiServiceConfigFailureActions,
            Shortcut,
            MsiShortcutProperty,
            Signature,
            SoftwareIdentificationTag,
            TextStyle,
            TypeLib,
            UIText,
            Upgrade,
            Verb,
            ModuleAdminExecuteSequence,
            ModuleAdminUISequence,
            ModuleAdvtExecuteSequence,
            ModuleAdvtUISequence,
            ModuleComponents,
            ModuleSignature,
            ModuleConfiguration,
            ModuleDependency,
            ModuleExclusion,
            ModuleIgnoreTable,
            ModuleInstallExecuteSequence,
            ModuleInstallUISequence,
            ModuleSubstitution,
            Properties,
            ImageFamilies,
            UpgradedImages,
            UpgradedFilesToIgnore,
            UpgradedFilesOptionalData,
            TargetImages,
            TargetFilesOptionalData,
            FamilyFileRanges,
            ExternalFiles,
            Streams,
            SummaryInformation,
            TransformView,
            Validation,
            WixDependency,
            WixDependencyProvider,
            WixDependencyRef,
        };
    }
}
