// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using WixToolset.Data.WindowsInstaller.Rows;

    public static class WindowsInstallerTableDefinitions
    {
        public static readonly TableDefinition ActionText = new TableDefinition(
            "ActionText",
            TupleDefinitions.ActionText,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to be described.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description displayed in progress dialog and log when action is executing."),
                new ColumnDefinition("Template", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Template, description: "Optional localized format template used to format action data records for display during action execution.", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
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
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Condition = new TableDefinition(
            "Condition",
            TupleDefinitions.Condition,
            new[]
            {
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Reference to a Feature entry in Feature table."),
                new ColumnDefinition("Level", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "New selection Level to set in Feature table if Condition evaluates to TRUE."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Expression evaluated to determine if Level in the Feature table is to change.", forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: false
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
            tupleIdIsPrimaryKey: true
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
            tupleIdIsPrimaryKey: true
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
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition AppId = new TableDefinition(
            "AppId",
            TupleDefinitions.AppId,
            new[]
            {
                new ColumnDefinition("AppId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid),
                new ColumnDefinition("RemoteServerName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("LocalService", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("ServiceParameters", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("DllSurrogate", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("ActivateAtStorage", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
                new ColumnDefinition("RunAsInteractiveUser", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition AppSearch = new TableDefinition(
            "AppSearch",
            TupleDefinitions.AppSearch,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The property associated with a Signature", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Signature;RegLocator;IniLocator;DrLocator;CompLocator", keyColumn: 1, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature,  RegLocator, IniLocator, CompLocator and the DrLocator tables.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Property = new TableDefinition(
            "Property",
            TupleDefinitions.Property,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of property, uppercase if settable by launcher or loader.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "String value for property.  Never null or empty."),
            },
            strongRowType: typeof(PropertyRow),
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition BBControl = new TableDefinition(
            "BBControl",
            TupleDefinitions.BBControl,
            new[]
            {
                new ColumnDefinition("Billboard_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Billboard", keyColumn: 1, description: "External key to the Billboard table, name of the billboard.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("BBControl", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the control. This name must be unique within a billboard, but can repeat on different billboard."),
                new ColumnDefinition("Type", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The type of the control."),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Horizontal coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Vertical coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this control."),
                new ColumnDefinition("Text", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A string used to set the initial text contained within a control (if appropriate)."),
            },
            strongRowType: typeof(BBControlRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Billboard = new TableDefinition(
            "Billboard",
            TupleDefinitions.Billboard,
            new[]
            {
                new ColumnDefinition("Billboard", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the billboard.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "An external key to the Feature Table. The billboard is shown only if this feature is being installed."),
                new ColumnDefinition("Action", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "The name of an action. The billboard is displayed during the progress messages received from this action."),
                new ColumnDefinition("Ordering", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "A positive integer. If there is more than one billboard corresponding to an action they will be shown in the order defined by this column."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Feature = new TableDefinition(
            "Feature",
            TupleDefinitions.Feature,
            new[]
            {
                new ColumnDefinition("Feature", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular feature record."),
                new ColumnDefinition("Feature_Parent", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Optional key of a parent record in the same table. If the parent is not selected, then the record will not be installed. Null indicates a root item."),
                new ColumnDefinition("Title", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Short text identifying a visible feature item."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Longer descriptive text describing a visible feature item."),
                new ColumnDefinition("Display", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Numeric sort order, used to force a specific display ordering."),
                new ColumnDefinition("Level", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The install level at which record will be initially selected. An install level of 0 will disable an item and prevent its display."),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.UpperCase, keyTable: "Directory", keyColumn: 1, description: "The name of the Directory that can be configured by the UI. A non-null value will enable the browse button.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;2;4;5;6;8;9;10;16;17;18;20;21;22;24;25;26;32;33;34;36;37;38;48;49;50;52;53;54", description: "Feature attributes"),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Binary = new TableDefinition(
            "Binary",
            TupleDefinitions.Binary,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique key identifying the binary data.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "The unformatted binary data."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition BindImage = new TableDefinition(
            "BindImage",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "The index into the File table. This must be an executable file.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Paths, description: "A list of ;  delimited paths that represent the paths to be searched for the import DLLS. The list is usually a list of properties each enclosed within square brackets [] .", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition File = new TableDefinition(
            "File",
            TupleDefinitions.File,
            new[]
            {
                new ColumnDefinition("File", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token, must match identifier in cabinet.  For uncompressed files, this field is ignored.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the file.", modularizeType: ColumnModularizeType.Column),
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
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition CCPSearch = new TableDefinition(
            "CCPSearch",
            TupleDefinitions.CCPSearch,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Signature;RegLocator;IniLocator;DrLocator;CompLocator", keyColumn: 1, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature,  RegLocator, IniLocator, CompLocator and the DrLocator tables."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition CheckBox = new TableDefinition(
            "CheckBox",
            TupleDefinitions.CheckBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to the item.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value string associated with the item.", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Class = new TableDefinition(
            "Class",
            TupleDefinitions.Class,
            new[]
            {
                new ColumnDefinition("CLSID", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "The CLSID of an OLE factory."),
                new ColumnDefinition("Context", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The numeric server context for this server. CLSCTX_xxxx"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ProgId_Default", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "Optional ProgId associated with this CLSID."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description for the Class."),
                new ColumnDefinition("AppId_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, keyTable: "AppId", keyColumn: 1, description: "Optional AppID containing DCOM information for associated application (string GUID)."),
                new ColumnDefinition("FileTypeMask", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Optional string containing information for the HKCRthis CLSID) key. If multiple patterns exist, they must be delimited by a semicolon, and numeric subkeys will be generated: 0,1,2..."),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Optional foreign key into the Icon Table, specifying the icon file associated with this CLSID. Will be written under the DefaultIcon key.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "Optional icon index."),
                new ColumnDefinition("DefInprocHandler", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Filename, possibilities: "1;2;3", description: "Optional default inproc handler.  Only optionally provided if Context=CLSCTX_LOCAL_SERVER.  Typically \"ole32.dll\" or \"mapi32.dll\""),
                new ColumnDefinition("Argument", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "optional argument for LocalServers."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the CLSID factory to be operational."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, maxValue: 32767, description: "Class registration attributes."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Component = new TableDefinition(
            "Component",
            TupleDefinitions.Component,
            new[]
            {
                new ColumnDefinition("Component", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular component record.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, description: "A string GUID unique to this component, version, and language."),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Required key of a Directory table record. This is actually a property name whose value contains the actual path, set either by the AppSearch action or with the default setting obtained from the Directory table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Remote execution option, one of irsEnum"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "A conditional statement that will disable this component if the specified condition evaluates to the 'True' state. If a component is disabled, it will not be installed, regardless of the 'Action' state associated with the component.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
                new ColumnDefinition("KeyPath", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File;Registry;ODBCDataSource", keyColumn: 1, description: "Either the primary key into the File table, Registry table, or ODBCDataSource table. This extract path is stored when the component is installed, and is used to detect the presence of the component and to return the path to it.", modularizeType: ColumnModularizeType.Column),
            },
            strongRowType: typeof(ComponentRow),
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Icon = new TableDefinition(
            "Icon",
            TupleDefinitions.Icon,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. Name of the icon file.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "Binary stream. The binary icon data in PE (.DLL or .EXE) or icon (.ICO) format."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ProgId = new TableDefinition(
            "ProgId",
            TupleDefinitions.ProgId,
            new[]
            {
                new ColumnDefinition("ProgId", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The Program Identifier. Primary key."),
                new ColumnDefinition("ProgId_Parent", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "The Parent Program Identifier. If specified, the ProgId column becomes a version independent prog id."),
                new ColumnDefinition("Class_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, keyTable: "Class", keyColumn: 1, description: "The CLSID of an OLE factory corresponding to the ProgId."),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Localized description for the Program identifier."),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Optional foreign key into the Icon Table, specifying the icon file associated with this ProgId. Will be written under the DefaultIcon key.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "Optional icon index."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ComboBox = new TableDefinition(
            "ComboBox",
            TupleDefinitions.ComboBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same combobox.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list. The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value.", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value.", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CompLocator = new TableDefinition(
            "CompLocator",
            TupleDefinitions.CompLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Guid, description: "A string GUID unique to this component, version, and language."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "A boolean value that determines if the registry value is a filename or a directory location."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Complus = new TableDefinition(
            "Complus",
            TupleDefinitions.Complus,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the ComPlus component.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ExpType", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "ComPlus component attributes."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Directory = new TableDefinition(
            "Directory",
            TupleDefinitions.Directory,
            new[]
            {
                new ColumnDefinition("Directory", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for directory entry, primary key. If a property by this name is defined, it contains the full path to the directory.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Directory_Parent", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Reference to the entry in this table specifying the default parent directory. A record parented to itself or with a Null parent represents a root of the install tree.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DefaultDir", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.DefaultDir, description: "The default sub-path under parent's path."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Control = new TableDefinition(
            "Control",
            TupleDefinitions.Control,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "External key to the Dialog table, name of the dialog.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Control", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the control. This name must be unique within a dialog, but can repeat on different dialogs. "),
                new ColumnDefinition("Type", ColumnType.String, 20, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The type of the control."),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Horizontal coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Vertical coordinate of the upper left corner of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the control.", forceLocalizable: true),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this control."),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "The name of a defined property to be linked to this control. ", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Text", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "A string used to set the initial text contained within a control (if appropriate).", modularizeType: ColumnModularizeType.ControlText),
                new ColumnDefinition("Control_Next", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "The name of an other control on the same dialog. This link defines the tab order of the controls. The links have to form one or more cycles!"),
                new ColumnDefinition("Help", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The help strings used with the button. The text is optional. "),
            },
            strongRowType: typeof(ControlRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Dialog = new TableDefinition(
            "Dialog",
            TupleDefinitions.Dialog,
            new[]
            {
                new ColumnDefinition("Dialog", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the dialog.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HCentering", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 100, description: "Horizontal position of the dialog on a 0-100 scale. 0 means left end, 100 means right end of the screen, 50 center."),
                new ColumnDefinition("VCentering", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 100, description: "Vertical position of the dialog on a 0-100 scale. 0 means top end, 100 means bottom end of the screen, 50 center."),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Width of the bounding rectangle of the dialog."),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Height of the bounding rectangle of the dialog."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A 32-bit word that specifies the attribute flags to be applied to this dialog."),
                new ColumnDefinition("Title", ColumnType.Localized, 128, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "A text string specifying the title to be displayed in the title bar of the dialog's window.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Control_First", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the control that has the focus when the dialog is created."),
                new ColumnDefinition("Control_Default", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the default control. Hitting return is equivalent to pushing this button."),
                new ColumnDefinition("Control_Cancel", ColumnType.String, 50, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "Defines the cancel control. Hitting escape or clicking on the close icon on the dialog is equivalent to pushing this button."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ControlCondition = new TableDefinition(
            "ControlCondition",
            TupleDefinitions.ControlCondition,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the dialog.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control."),
                new ColumnDefinition("Action", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "Default;Disable;Enable;Hide;Show", description: "The desired action to be taken on the specified control."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Condition, description: "A standard conditional statement that specifies under which conditions the action should be triggered.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ControlEvent = new TableDefinition(
            "ControlEvent",
            TupleDefinitions.ControlEvent,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the dialog.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control"),
                new ColumnDefinition("Event", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "An identifier that specifies the type of the event that should take place when the user interacts with control specified by the first two entries.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Argument", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "A value to be used as a modifier when triggering a particular event.", modularizeType: ColumnModularizeType.ControlEventArgument, forceLocalizable: true),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Condition, description: "A standard conditional statement that specifies under which conditions an event should be triggered.", modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
                new ColumnDefinition("Ordering", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "An integer used to order several events tied to the same control. Can be left blank."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CreateFolder = new TableDefinition(
            "CreateFolder",
            TupleDefinitions.CreateFolder,
            new[]
            {
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Primary key, could be foreign key into the Directory table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition CustomAction = new TableDefinition(
            "CustomAction",
            TupleDefinitions.CustomAction,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, name of action, normally appears in sequence table unless private use.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "The numeric custom action type, consisting of source location, code type, entry, option flags."),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.CustomSource, description: "The table reference of the source of the code.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Target", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Excecution parameter, depends on the type of custom action", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("ExtendedType", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "A numeric custom action type that extends code type or option flags of the Type column."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition DrLocator = new TableDefinition(
            "DrLocator",
            TupleDefinitions.DrLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The Signature_ represents a unique file signature and is also the foreign key in the Signature table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Parent", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Identifier, description: "The parent file signature. It is also a foreign key in the Signature table. If null and the Path column does not expand to a full path, then all the fixed drives of the user system are searched using the Path.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.AnyPath, description: "The path on the user system. This is a either a subpath below the value of the Parent or a full path. The path may contain properties enclosed within [ ] that will be expanded.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Depth", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The depth below the path to which the Signature_ is recursively searched. If absent, the depth is assumed to be 0."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition DuplicateFile = new TableDefinition(
            "DuplicateFile",
            TupleDefinitions.DuplicateFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular file entry", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the duplicate file.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key referencing the source file to be duplicated.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DestName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Filename, description: "Filename to be given to the duplicate file."),
                new ColumnDefinition("DestFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full pathname to a destination folder.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Environment = new TableDefinition(
            "Environment",
            TupleDefinitions.Environment,
            new[]
            {
                new ColumnDefinition("Environment", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for the environmental variable setting", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the environmental value."),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to set in the environmental settings.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the environmental value.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Error = new TableDefinition(
            "Error",
            TupleDefinitions.Error,
            new[]
            {
                new ColumnDefinition("Error", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Integer error number, obtained from header file IError(...) macros."),
                new ColumnDefinition("Message", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Template, description: "Error formatting template, obtained from user ed. or localizers.", modularizeType: ColumnModularizeType.Property, useCData: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition EventMapping = new TableDefinition(
            "EventMapping",
            TupleDefinitions.EventMapping,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Dialog", keyColumn: 1, description: "A foreign key to the Dialog table, name of the Dialog.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Control", keyColumn: 2, description: "A foreign key to the Control table, name of the control."),
                new ColumnDefinition("Event", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "An identifier that specifies the type of the event that the control subscribes to."),
                new ColumnDefinition("Attribute", ColumnType.String, 50, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "The name of the control attribute, that is set when this event is received."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Extension = new TableDefinition(
            "Extension",
            TupleDefinitions.Extension,
            new[]
            {
                new ColumnDefinition("Extension", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The extension associated with the table row."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ProgId_", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "ProgId", keyColumn: 1, description: "Optional ProgId associated with this extension."),
                new ColumnDefinition("MIME_", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Text, keyTable: "MIME", keyColumn: 1, description: "Optional Context identifier, typically \"type/format\" associated with the extension"),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the CLSID factory to be operational."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MIME = new TableDefinition(
            "MIME",
            TupleDefinitions.MIME,
            new[]
            {
                new ColumnDefinition("ContentType", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key. Context identifier, typically \"type/format\"."),
                new ColumnDefinition("Extension_", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "Extension", keyColumn: 1, description: "Optional associated extension (without dot)"),
                new ColumnDefinition("CLSID", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid, description: "Optional associated CLSID."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FeatureComponents = new TableDefinition(
            "FeatureComponents",
            TupleDefinitions.FeatureComponents,
            new[]
            {
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into Feature table."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FileSFPCatalog = new TableDefinition(
            "FileSFPCatalog",
            TupleDefinitions.FileSFPCatalog,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "File associated with the catalog", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SFPCatalog_", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Filename, keyTable: "SFPCatalog", keyColumn: 1, description: "Catalog associated with the file"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition SFPCatalog = new TableDefinition(
            "SFPCatalog",
            TupleDefinitions.SFPCatalog,
            new[]
            {
                new ColumnDefinition("SFPCatalog", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Filename, description: "File name for the catalog."),
                new ColumnDefinition("Catalog", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "SFP Catalog"),
                new ColumnDefinition("Dependency", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Parent catalog - only used by SFP", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Font = new TableDefinition(
            "Font",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Primary key, foreign key into File table referencing font file.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FontTitle", ColumnType.String, 128, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Font name."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IniFile = new TableDefinition(
            "IniFile",
            TupleDefinitions.IniFile,
            new[]
            {
                new ColumnDefinition("IniFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name in which to write the information"),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Foreign key into the Directory table denoting the directory where the .INI file is.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Section", ColumnType.Localized, 96, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file Section.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Key", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file key below Section.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value to be written.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Action", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;3", description: "The type of modification to be made, one of iifEnum"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the .INI value.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IniLocator = new TableDefinition(
            "IniLocator",
            TupleDefinitions.IniLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name."),
                new ColumnDefinition("Section", ColumnType.String, 96, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Section name within in file (within square brackets in INI file)."),
                new ColumnDefinition("Key", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Key value (followed by an equals sign in INI file)."),
                new ColumnDefinition("Field", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The field in the .INI line. If Field is null or 0 the entire line is read."),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "An integer value that determines if the .INI value read is a filename or a directory location or to be used as is w/o interpretation."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition InstallExecuteSequence = new TableDefinition(
            "InstallExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition InstallUISequence = new TableDefinition(
            "InstallUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of action to invoke, either in the engine or the handler DLL."),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "Optional expression which skips the action if evaluates to expFalse.If the expression syntax is invalid, the engine will terminate, returning iesBadActionData.", forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Number that determines the sort order in which the actions are to be executed.  Leave blank to suppress action."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IsolatedComponent = new TableDefinition(
            "IsolatedComponent",
            TupleDefinitions.IsolatedComponent,
            new[]
            {
                new ColumnDefinition("Component_Shared", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Key to Component table item to be isolated", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Key to Component table item for application", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition LaunchCondition = new TableDefinition(
            "LaunchCondition",
            TupleDefinitions.LaunchCondition,
            new[]
            {
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Condition, description: "Expression which must evaluate to TRUE in order for install to commence.", forceLocalizable: true),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Localizable text to display when condition fails and install must abort."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ListBox = new TableDefinition(
            "ListBox",
            TupleDefinitions.ListBox,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same listbox.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ListView = new TableDefinition(
            "ListView",
            TupleDefinitions.ListView,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this item. All the items tied to the same property become part of the same listview.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this item. Selecting the line will set the associated property to this value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Text", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The visible text to be assigned to the item. Optional. If this entry or the entire column is missing, the text is the same as the value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Binary_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Binary", keyColumn: 1, description: "The name of the icon to be displayed with the icon. The binary information is looked up from the Binary Table.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition LockPermissions = new TableDefinition(
            "LockPermissions",
            TupleDefinitions.LockPermissions,
            new[]
            {
                new ColumnDefinition("LockObject", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Foreign key into Registry or File table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, possibilities: "Directory;File;Registry", description: "Reference to another table name"),
                new ColumnDefinition("Domain", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Formatted, description: "Domain name for user whose permissions are being set. (usually a property)", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("User", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Formatted, description: "User for permissions to be set.  (usually a property)", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Permission", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Permission Access mask.  Full Control = 268435456 (GENERIC_ALL = 0x10000000)"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiLockPermissionsEx = new TableDefinition(
            "MsiLockPermissionsEx",
            TupleDefinitions.MsiLockPermissionsEx,
            new[]
            {
                new ColumnDefinition("MsiLockPermissionsEx", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("LockObject", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Foreign key into Registry, File, CreateFolder, or ServiceInstall table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Identifier, possibilities: "CreateFolder;File;Registry;ServiceInstall", description: "Reference to another table name"),
                new ColumnDefinition("SDDLText", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.FormattedSDDLText, description: "String to indicate permissions to be applied to the LockObject", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Expression which must evaluate to TRUE in order for this set of permissions to be applied", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Media = new TableDefinition(
            "Media",
            TupleDefinitions.Media,
            new[]
            {
                new ColumnDefinition("DiskId", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "Primary key, integer to determine sort order for table."),
                new ColumnDefinition("LastSequence", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "File sequence number for the last file for this media."),
                new ColumnDefinition("DiskPrompt", ColumnType.Localized, 64, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Disk name: the visible text actually printed on the disk.  This will be used to prompt the user when this disk needs to be inserted."),
                new ColumnDefinition("Cabinet", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Cabinet, description: "If some or all of the files stored on the media are compressed in a cabinet, the name of that cabinet."),
                new ColumnDefinition("VolumeLabel", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The label attributed to the volume."),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Property, description: "The property defining the location of the cabinet file."),
            },
            strongRowType: typeof(MediaRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MoveFile = new TableDefinition(
            "MoveFile",
            TupleDefinitions.MoveFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key that uniquely identifies a particular MoveFile record", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "If this component is not \"selected\" for installation or removal, no action will be taken on the associated MoveFile entry", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SourceName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Name of the source file(s) to be moved or copied.  Can contain the '*' or '?' wildcards."),
                new ColumnDefinition("DestName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Filename, description: "Name to be given to the original file after it is moved or copied.  If blank, the destination file will be given the same name as the source file"),
                new ColumnDefinition("SourceFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the source directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DestFolder", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the destination directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Options", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Integer value specifying the MoveFile operating mode, one of imfoEnum"),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiAssembly = new TableDefinition(
            "MsiAssembly",
            TupleDefinitions.Assembly,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into Feature table."),
                new ColumnDefinition("File_Manifest", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into the File table denoting the manifest file for the assembly.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_Application", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into File table, denoting the application context for private assemblies. Null for global assemblies.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Assembly attributes"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiAssemblyName = new TableDefinition(
            "MsiAssemblyName",
            TupleDefinitions.MsiAssemblyName,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into Component table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The name part of the name-value pairs for the assembly name."),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The value part of the name-value pairs for the assembly name."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiDigitalCertificate = new TableDefinition(
            "MsiDigitalCertificate",
            TupleDefinitions.MsiDigitalCertificate,
            new[]
            {
                new ColumnDefinition("DigitalCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique identifier for the row"),
                new ColumnDefinition("CertData", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "A certificate context blob for a signer certificate"),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiDigitalSignature = new TableDefinition(
            "MsiDigitalSignature",
            TupleDefinitions.MsiDigitalSignature,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "Media", description: "Reference to another table name (only Media table is supported)"),
                new ColumnDefinition("SignObject", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Foreign key to Media table"),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate"),
                new ColumnDefinition("Hash", ColumnType.Object, 0, primaryKey: false, nullable: true, ColumnCategory.Binary, description: "The encoded hash blob from the digital signature"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiEmbeddedChainer = new TableDefinition(
            "MsiEmbeddedChainer",
            TupleDefinitions.MsiEmbeddedChainer,
            new[]
            {
                new ColumnDefinition("MsiEmbeddedChainer", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, description: "A conditional statement for running the user-defined function.", forceLocalizable: true),
                new ColumnDefinition("CommandLine", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value in this field is a part of the command line string passed to the executable file identified in the Source column.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Source", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.CustomSource, description: "The location of the executable file for the user-defined function.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "2;18;50", description: "The functions listed in the MsiEmbeddedChainer table are described using the following custom action numeric types."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiEmbeddedUI = new TableDefinition(
            "MsiEmbeddedUI",
            TupleDefinitions.MsiEmbeddedUI,
            new[]
            {
                new ColumnDefinition("MsiEmbeddedUI", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the file that receives the binary information in the Data column."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;2;3", description: "Information about the data in the Data column."),
                new ColumnDefinition("MessageFilter", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Specifies the types of messages that are sent to the user interface DLL."),
                new ColumnDefinition("Data", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "This column contains binary information."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiFileHash = new TableDefinition(
            "MsiFileHash",
            TupleDefinitions.MsiFileHash,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Primary key, foreign key into File table referencing file with this hash", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Options", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Various options and attributes for this hash."),
                new ColumnDefinition("HashPart1", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart2", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart3", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
                new ColumnDefinition("HashPart4", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Size of file in bytes (long integer)."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiPackageCertificate = new TableDefinition(
            "MsiPackageCertificate",
            TupleDefinitions.MsiPackageCertificate,
            new[]
            {
                new ColumnDefinition("PackageCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row."),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchCertificate = new TableDefinition(
            "MsiPatchCertificate",
            TupleDefinitions.MsiPatchCertificate,
            new[]
            {
                new ColumnDefinition("PatchCertificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row."),
                new ColumnDefinition("DigitalCertificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiDigitalCertificate", keyColumn: 1, description: "Foreign key to MsiDigitalCertificate table identifying the signer certificate."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchHeaders = new TableDefinition(
            "MsiPatchHeaders",
            TupleDefinitions.MsiPatchHeaders,
            new[]
            {
                new ColumnDefinition("StreamRef", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. A unique identifier for the row."),
                new ColumnDefinition("Header", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Binary, description: "Binary stream. The patch header, used for patch validation."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchMetadata = new TableDefinition(
            "PatchMetadata",
            TupleDefinitions.PatchMetadata,
            new[]
            {
                new ColumnDefinition("Company", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Identifier, description: "Primary key. The name of the company."),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. The name of the property."),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Non-null, non-empty value of the metadata property."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchMetadata = new TableDefinition(
            "MsiPatchMetadata",
            TupleDefinitions.MsiPatchMetadata,
            new[]
            {
                new ColumnDefinition("Company", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchOldAssemblyFile = new TableDefinition(
            "MsiPatchOldAssemblyFile",
            TupleDefinitions.MsiPatchOldAssemblyFile,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into File table. Patch-only table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Assembly_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "MsiPatchOldAssemblyName", keyColumn: 1, description: "Foreign key into MsiPatchOldAssemblyName table.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchOldAssemblyName = new TableDefinition(
            "MsiPatchOldAssemblyName",
            TupleDefinitions.MsiPatchOldAssemblyName,
            new[]
            {
                new ColumnDefinition("Assembly", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique identifier for the row.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The name part of the name-value pairs for the assembly name. This represents the old name for the assembly."),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The value part of the name-value pairs for the assembly name. This represents the old name for the assembly."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchSequence = new TableDefinition(
            "PatchSequence",
            TupleDefinitions.PatchSequence,
            new[]
            {
                new ColumnDefinition("PatchFamily", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key. The name of the family for the patch."),
                new ColumnDefinition("Target", ColumnType.String, 72, primaryKey: true, nullable: true, ColumnCategory.Text, description: "Primary key. Determines product code filtering for family."),
                new ColumnDefinition("Sequence", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Sequence information in version (x.x.x.x) format."),
                new ColumnDefinition("Supersede", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Indicates that this patch supersedes earlier patches."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition MsiPatchSequence = new TableDefinition(
            "MsiPatchSequence",
            TupleDefinitions.MsiPatchSequence,
            new[]
            {
                new ColumnDefinition("PatchFamily", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ProductCode", ColumnType.String, 38, primaryKey: true, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Sequence", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCAttribute = new TableDefinition(
            "ODBCAttribute",
            TupleDefinitions.ODBCAttribute,
            new[]
            {
                new ColumnDefinition("Driver_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ODBCDriver", keyColumn: 1, description: "Reference to ODBC driver in ODBCDriver table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attribute", ColumnType.String, 40, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Name of ODBC driver attribute"),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Value for ODBC driver attribute"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCDriver = new TableDefinition(
            "ODBCDriver",
            TupleDefinitions.ODBCDriver,
            new[]
            {
                new ColumnDefinition("Driver", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for driver", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for driver, non-localized"),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Reference to key driver file", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_Setup", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Optional reference to key driver setup DLL", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ODBCDataSource = new TableDefinition(
            "ODBCDataSource",
            TupleDefinitions.ODBCDataSource,
            new[]
            {
                new ColumnDefinition("DataSource", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for data source", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for data source"),
                new ColumnDefinition("DriverDescription", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Reference to driver description, may be existing driver"),
                new ColumnDefinition("Registration", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Registration option: 0=machine, 1=user, others t.b.d."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ODBCSourceAttribute = new TableDefinition(
            "ODBCSourceAttribute",
            TupleDefinitions.ODBCSourceAttribute,
            new[]
            {
                new ColumnDefinition("DataSource_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ODBCDataSource", keyColumn: 1, description: "Reference to ODBC data source in ODBCDataSource table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attribute", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Name of ODBC data source attribute"),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Value for ODBC data source attribute"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ODBCTranslator = new TableDefinition(
            "ODBCTranslator",
            TupleDefinitions.ODBCTranslator,
            new[]
            {
                new ColumnDefinition("Translator", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized.internal token for translator", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reference to associated component", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Text used as registered name for translator"),
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Reference to key translator file", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_Setup", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Optional reference to key translator setup DLL", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Patch = new TableDefinition(
            "Patch",
            TupleDefinitions.Patch,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token, foreign key to File table, must match identifier in cabinet.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 4, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Primary key, sequence with respect to the media images; order must track cabinet order."),
                new ColumnDefinition("PatchSize", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Size of patch in bytes (long integer)."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Integer containing bit flags representing patch attributes"),
                new ColumnDefinition("Header", ColumnType.Object, 0, primaryKey: false, nullable: true, ColumnCategory.Binary, description: "Binary stream. The patch header, used for patch validation."),
                new ColumnDefinition("StreamRef_", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Identifier. Foreign key to the StreamRef column of the MsiPatchHeaders table."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PatchPackage = new TableDefinition(
            "PatchPackage",
            TupleDefinitions.PatchPackage,
            new[]
            {
                new ColumnDefinition("PatchId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "A unique string GUID representing this patch."),
                new ColumnDefinition("Media_", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Foreign key to DiskId column of Media table. Indicates the disk containing the patch package."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition PublishComponent = new TableDefinition(
            "PublishComponent",
            TupleDefinitions.PublishComponent,
            new[]
            {
                new ColumnDefinition("ComponentId", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "A string GUID that represents the component id that will be requested by the alien product."),
                new ColumnDefinition("Qualifier", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, description: "This is defined only when the ComponentId column is an Qualified Component Id. This is the Qualifier for ProvideComponentIndirect."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("AppData", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "This is localisable Application specific data that can be associated with a Qualified Component."),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Foreign key into the Feature table."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition RadioButton = new TableDefinition(
            "RadioButton",
            TupleDefinitions.RadioButton,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A named property to be tied to this radio button. All the buttons tied to the same property become part of the same group.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Order", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 32767, description: "A positive integer used to determine the ordering of the items within one list..The integers do not have to be consecutive."),
                new ColumnDefinition("Value", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The value string associated with this button. Selecting the button will set the associated property to this value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("X", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The horizontal coordinate of the upper left corner of the bounding rectangle of the radio button.", forceLocalizable: true),
                new ColumnDefinition("Y", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The vertical coordinate of the upper left corner of the bounding rectangle of the radio button.", forceLocalizable: true),
                new ColumnDefinition("Width", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The width of the button.", forceLocalizable: true),
                new ColumnDefinition("Height", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The height of the button.", forceLocalizable: true),
                new ColumnDefinition("Text", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The visible title to be assigned to the radio button."),
                new ColumnDefinition("Help", ColumnType.Localized, 50, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The help strings used with the button. The text is optional."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Registry = new TableDefinition(
            "Registry",
            TupleDefinitions.Registry,
            new[]
            {
                new ColumnDefinition("Registry", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -1, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum."),
                new ColumnDefinition("Key", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the installing of the registry value.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RegLocator = new TableDefinition(
            "RegLocator",
            TupleDefinitions.RegLocator,
            new[]
            {
                new ColumnDefinition("Signature_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature_ represents a unique file signature and is also the foreign key in the Signature table. If the type is 0, the registry values refers a directory, and _Signature is not a foreign key.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum."),
                new ColumnDefinition("Key", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value.", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name.", modularizeType: ColumnModularizeType.Property, forceLocalizable: true),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 18, description: "An integer value that determines if the registry value is a filename or a directory location or to be used as is w/o interpretation."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveFile = new TableDefinition(
            "RemoveFile",
            TupleDefinitions.RemoveFile,
            new[]
            {
                new ColumnDefinition("FileKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular file entry", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the file to be removed.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.WildCardFilename, description: "Name of the file to be removed."),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full pathname to the folder of the file to be removed.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("InstallMode", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "1;2;3", description: "Installation option, one of iimEnum."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveIniFile = new TableDefinition(
            "RemoveIniFile",
            null,
            new[]
            {
                new ColumnDefinition("RemoveIniFile", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The .INI file name in which to delete the information"),
                new ColumnDefinition("DirProperty", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Foreign key into the Directory table denoting the directory where the .INI file is.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Section", ColumnType.Localized, 96, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file Section.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Key", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The .INI file key below Section.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The value to be deleted. The value is required when Action is iifIniRemoveTag", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Action", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "2;4", description: "The type of modification to be made, one of iifEnum."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the deletion of the .INI value.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition RemoveRegistry = new TableDefinition(
            "RemoveRegistry",
            TupleDefinitions.RemoveRegistry,
            new[]
            {
                new ColumnDefinition("RemoveRegistry", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Root", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -1, maxValue: 3, description: "The predefined root key for the registry value, one of rrkEnum"),
                new ColumnDefinition("Key", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.RegPath, description: "The key for the registry value.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The registry value name.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing component that controls the deletion of the registry value.", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ReserveCost = new TableDefinition(
            "ReserveCost",
            TupleDefinitions.ReserveCost,
            new[]
            {
                new ColumnDefinition("ReserveKey", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key that uniquely identifies a particular ReserveCost record", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Reserve a specified amount of space if this component is to be installed.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ReserveFolder", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of a property whose value is assumed to resolve to the full path to the destination directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ReserveLocal", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Disk space to reserve if linked component is installed locally."),
                new ColumnDefinition("ReserveSource", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Disk space to reserve if linked component is installed to run from the source location."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition SelfReg = new TableDefinition(
            "SelfReg",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, description: "Foreign key into the File table denoting the module that needs to be registered.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Cost", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The cost of registering the module."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ServiceControl = new TableDefinition(
            "ServiceControl",
            TupleDefinitions.ServiceControl,
            new[]
            {
                new ColumnDefinition("ServiceControl", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 187, description: "Bit field:  Install:  0x1 = Start, 0x2 = Stop, 0x8 = Delete, Uninstall: 0x10 = Start, 0x20 = Stop, 0x80 = Delete"),
                new ColumnDefinition("Arguments", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Arguments for the service.  Separate by [~].", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Wait", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Boolean for whether to wait for the service to fully start"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the startup of the service", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ServiceInstall = new TableDefinition(
            "ServiceInstall",
            TupleDefinitions.ServiceInstall,
            new[]
            {
                new ColumnDefinition("ServiceInstall", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Internal Name of the Service", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DisplayName", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "External Name of the Service", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("ServiceType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Type of the service"),
                new ColumnDefinition("StartType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 4, description: "Type of the service"),
                new ColumnDefinition("ErrorControl", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Severity of error if service fails to start"),
                new ColumnDefinition("LoadOrderGroup", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "LoadOrderGroup", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Dependencies", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Other services this depends on to start.  Separate by [~], and end with [~][~]", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("StartName", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "User or object name to run service as", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Password", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "password to run service with.  (with StartName)", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Arguments", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Arguments to include in every start of the service, passed to WinMain", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the startup of the service", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Description of service.", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiServiceConfig = new TableDefinition(
            "MsiServiceConfig",
            TupleDefinitions.MsiServiceConfig,
            new[]
            {
                new ColumnDefinition("MsiServiceConfig", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 7, description: "Bit field:   0x1 = Install, 0x2 = Uninstall, 0x4 = Reinstall"),
                new ColumnDefinition("ConfigType", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Service Configuration Option"),
                new ColumnDefinition("Argument", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Argument(s) for service configuration. Value depends on the content of the ConfigType field"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the configuration of the service", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiServiceConfigFailureActions = new TableDefinition(
            "MsiServiceConfigFailureActions",
            TupleDefinitions.MsiServiceConfigFailureActions,
            new[]
            {
                new ColumnDefinition("MsiServiceConfigFailureActions", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of a service. /, \\, comma and space are invalid", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Event", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 7, description: "Bit field:   0x1 = Install, 0x2 = Uninstall, 0x4 = Reinstall"),
                new ColumnDefinition("ResetPeriod", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Time in seconds after which to reset the failure count to zero. Leave blank if it should never be reset"),
                new ColumnDefinition("RebootMessage", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Message to be broadcast to server users before rebooting"),
                new ColumnDefinition("Command", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Command line of the process to CreateProcess function to execute"),
                new ColumnDefinition("Actions", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A list of integer actions separated by [~] delimiters: 0 = SC_ACTION_NONE, 1 = SC_ACTION_RESTART, 2 = SC_ACTION_REBOOT, 3 = SC_ACTION_RUN_COMMAND. Terminate with [~][~]"),
                new ColumnDefinition("DelayActions", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A list of delays (time in milli-seconds), separated by [~] delmiters, to wait before taking the corresponding Action. Terminate with [~][~]"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table that controls the configuration of failure actions for the service", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Shortcut = new TableDefinition(
            "Shortcut",
            TupleDefinitions.Shortcut,
            new[]
            {
                new ColumnDefinition("Shortcut", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key into the Directory table denoting the directory where the shortcut file is created.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 128, primaryKey: false, nullable: false, ColumnCategory.Filename, description: "The name of the shortcut to be created."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table denoting the component whose selection gates the the shortcut creation/deletion.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Target", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Shortcut, description: "The shortcut target. This is usually a property that is expanded to a file or a folder that the shortcut points to.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Arguments", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The command-line arguments for the shortcut.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The description for the shortcut."),
                new ColumnDefinition("Hotkey", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The hotkey for the shortcut. It has the virtual-key code for the key in the low-order byte, and the modifier flags in the high-order byte. "),
                new ColumnDefinition("Icon_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Icon", keyColumn: 1, description: "Foreign key into the File table denoting the external icon file for the shortcut.", modularizeType: ColumnModularizeType.Icon),
                new ColumnDefinition("IconIndex", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -32767, maxValue: 32767, description: "The icon index for the shortcut."),
                new ColumnDefinition("ShowCmd", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "1;3;7", description: "The show command for the application window.The following values may be used."),
                new ColumnDefinition("WkDir", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Name of property defining location of working directory.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DisplayResourceDLL", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The Formatted string providing the full path to the language neutral file containing the MUI Manifest.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DisplayResourceId", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The display name index for the shortcut. This must be a non-negative number."),
                new ColumnDefinition("DescriptionResourceDLL", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The Formatted string providing the full path to the language neutral file containing the MUI Manifest.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DescriptionResourceId", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The description name index for the shortcut. This must be a non-negative number."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition MsiShortcutProperty = new TableDefinition(
            "MsiShortcutProperty",
            TupleDefinitions.MsiShortcutProperty,
            new[]
            {
                new ColumnDefinition("MsiShortcutProperty", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Shortcut_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Shortcut", keyColumn: 1, description: "Foreign key into the Shortcut table", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("PropertyKey", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Canonical string representation of the Property Key being set", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PropVariantValue", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "String representation of the value in the property", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Signature = new TableDefinition(
            "Signature",
            TupleDefinitions.Signature,
            new[]
            {
                new ColumnDefinition("Signature", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The table key. The Signature represents a unique file signature.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("FileName", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "The name of the file. This may contain a \"short name|long name\" pair."),
                new ColumnDefinition("MinVersion", ColumnType.String, 20, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The minimum version of the file."),
                new ColumnDefinition("MaxVersion", ColumnType.String, 20, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The maximum version of the file."),
                new ColumnDefinition("MinSize", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The minimum size of the file."),
                new ColumnDefinition("MaxSize", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The maximum size of the file. "),
                new ColumnDefinition("MinDate", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The minimum creation date of the file."),
                new ColumnDefinition("MaxDate", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The maximum creation date of the file."),
                new ColumnDefinition("Languages", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Language, description: "The languages supported by the file."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition TextStyle = new TableDefinition(
            "TextStyle",
            TupleDefinitions.TextStyle,
            new[]
            {
                new ColumnDefinition("TextStyle", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the style. The primary key of this table. This name is embedded in the texts to indicate a style change."),
                new ColumnDefinition("FaceName", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Text, description: "A string indicating the name of the font used. Required. The string must be at most 31 characters long.", forceLocalizable: true),
                new ColumnDefinition("Size", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The size of the font used. This size is given in our units (1/12 of the system font height). Assuming that the system font is set to 12 point size, this is equivalent to the point size.", forceLocalizable: true),
                new ColumnDefinition("Color", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 16777215, description: "A long integer indicating the color of the string in the RGB format (Red, Green, Blue each 0-255, RGB = R + 256*G + 256^2*B)."),
                new ColumnDefinition("StyleBits", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 15, description: "A combination of style bits."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition TypeLib = new TableDefinition(
            "TypeLib",
            TupleDefinitions.TypeLib,
            new[]
            {
                new ColumnDefinition("LibID", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Guid, description: "The GUID that represents the library."),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "The language of the library."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Required foreign key into the Component Table, specifying the component for which to return a path when called through LocateComponent.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Version", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 16777215, description: "The version of the library. The minor version is in the lower 8 bits of the integer. The major version is in the next 16 bits. "),
                new ColumnDefinition("Description", ColumnType.Localized, 128, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Optional. The foreign key into the Directory table denoting the path to the help file for the type library.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Feature", keyColumn: 1, description: "Required foreign key into the Feature Table, specifying the feature to validate or install in order for the type library to be operational."),
                new ColumnDefinition("Cost", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "The cost associated with the registration of the typelib. This column is currently optional."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UIText = new TableDefinition(
            "UIText",
            TupleDefinitions.UIText,
            new[]
            {
                new ColumnDefinition("Key", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "A unique key that identifies the particular string."),
                new ColumnDefinition("Text", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "The localized version of the string."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition Upgrade = new TableDefinition(
            "Upgrade",
            TupleDefinitions.Upgrade,
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
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Verb = new TableDefinition(
            "Verb",
            TupleDefinitions.Verb,
            new[]
            {
                new ColumnDefinition("Extension_", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "Extension", keyColumn: 1, description: "The extension associated with the table row."),
                new ColumnDefinition("Verb", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Text, description: "The verb for the command."),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 32767, description: "Order within the verbs for a particular extension. Also used simply to specify the default verb."),
                new ColumnDefinition("Command", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "The command text.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Argument", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional value for the command arguments.", modularizeType: ColumnModularizeType.Property),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleAdminExecuteSequence = new TableDefinition(
            "ModuleAdminExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdminExecuteSequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdminUISequence = new TableDefinition(
            "ModuleAdminUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdminUISequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdvtExecuteSequence = new TableDefinition(
            "ModuleAdvtExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdvtExecuteSequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleAdvtUISequence = new TableDefinition(
            "ModuleAdvtUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleAdvtUISequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleComponents = new TableDefinition(
            "ModuleComponents",
            TupleDefinitions.ModuleComponents,
            new[]
            {
                new ColumnDefinition("Component", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component contained in the module.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "Module containing the component.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "Default language ID for module (may be changed by transform).", forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleSignature = new TableDefinition(
            "ModuleSignature",
            TupleDefinitions.ModuleSignature,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Module identifier (String.GUID).", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Default decimal language of module.", forceLocalizable: true),
                new ColumnDefinition("Version", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Version, description: "Version of the module."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleConfiguration = new TableDefinition(
            "ModuleConfiguration",
            TupleDefinitions.ModuleConfiguration,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique identifier for this row."),
                new ColumnDefinition("Format", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "Format of this item."),
                new ColumnDefinition("Type", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "Additional type information for this item."),
                new ColumnDefinition("ContextData", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Additional context information about this item."),
                new ColumnDefinition("DefaultValue", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Default value for this item."),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "Additional type-specific attributes."),
                new ColumnDefinition("DisplayName", ColumnType.Localized, 72, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A short human-readable name for this item."),
                new ColumnDefinition("Description", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "A human-readable description."),
                new ColumnDefinition("HelpLocation", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Filename or namespace of the context-sensitive help for this item."),
                new ColumnDefinition("HelpKeyword", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Keyword index into the HelpLocation for this item."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleDependency = new TableDefinition(
            "ModuleDependency",
            TupleDefinitions.ModuleDependency,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "Module requiring the dependency.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ModuleLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "Language of module requiring the dependency.", forceLocalizable: true),
                new ColumnDefinition("RequiredID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "String.GUID of required module."),
                new ColumnDefinition("RequiredLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "LanguageID of the required module.", forceLocalizable: true),
                new ColumnDefinition("RequiredVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Version of the required version."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleExclusion = new TableDefinition(
            "ModuleExclusion",
            TupleDefinitions.ModuleExclusion,
            new[]
            {
                new ColumnDefinition("ModuleID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "ModuleSignature", keyColumn: 1, description: "String.GUID of module with exclusion requirement.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ModuleLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "ModuleSignature", keyColumn: 2, description: "LanguageID of module with exclusion requirement.", forceLocalizable: true),
                new ColumnDefinition("ExcludedID", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "String.GUID of excluded module."),
                new ColumnDefinition("ExcludedLanguage", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Language of excluded module.", forceLocalizable: true),
                new ColumnDefinition("ExcludedMinVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Minimum version of excluded module."),
                new ColumnDefinition("ExcludedMaxVersion", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Version, description: "Maximum version of excluded module."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ModuleIgnoreTable = new TableDefinition(
            "ModuleIgnoreTable",
            TupleDefinitions.ModuleIgnoreTable,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Table name to ignore during merge operation."),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleInstallExecuteSequence = new TableDefinition(
            "ModuleInstallExecuteSequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleInstallExecuteSequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleInstallUISequence = new TableDefinition(
            "ModuleInstallUISequence",
            null,
            new[]
            {
                new ColumnDefinition("Action", ColumnType.String, 64, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Action to insert", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -4, maxValue: 32767, description: "Standard Sequence number"),
                new ColumnDefinition("BaseAction", ColumnType.String, 64, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "ModuleInstallUISequence", keyColumn: 1, description: "Base action to determine insert location.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("After", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1, description: "Before (0) or After (1)"),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Condition, modularizeType: ColumnModularizeType.Condition, forceLocalizable: true),
            },
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition ModuleSubstitution = new TableDefinition(
            "ModuleSubstitution",
            TupleDefinitions.ModuleSubstitution,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Table containing the data to be modified."),
                new ColumnDefinition("Row", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Row containing the data to be modified.", modularizeType: ColumnModularizeType.SemicolonDelimited),
                new ColumnDefinition("Column", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Column containing the data to be modified."),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Template for modification data."),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Properties = new TableDefinition(
            "Properties",
            TupleDefinitions.Properties,
            new[]
            {
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key, non-localized token"),
                new ColumnDefinition("Value", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Value of the property"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ImageFamilies = new TableDefinition(
            "ImageFamilies",
            TupleDefinitions.ImageFamilies,
            new[]
            {
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key"),
                new ColumnDefinition("MediaSrcPropName", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("MediaDiskId", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
                new ColumnDefinition("FileSequenceStart", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Integer, minValue: 1, maxValue: 214743647),
                new ColumnDefinition("DiskPrompt", ColumnType.String, 128, primaryKey: false, nullable: true, ColumnCategory.Text, forceLocalizable: true),
                new ColumnDefinition("VolumeLabel", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Text),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedImages = new TableDefinition(
            "UpgradedImages",
            TupleDefinitions.UpgradedImages,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, description: "Primary key"),
                new ColumnDefinition("MsiPath", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("PatchMsiPath", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: false, nullable: false, ColumnCategory.Text, keyTable: "ImageFamilies", keyColumn: 1, description: "Foreign key, Family to which this image belongs"),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedFilesToIgnore = new TableDefinition(
            "UpgradedFilesToIgnore",
            TupleDefinitions.UpgradedFilesToIgnore,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "UpgradedImages", keyColumn: 1, description: "Foreign key, Upgraded image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition UpgradedFilesOptionalData = new TableDefinition(
            "UpgradedFiles_OptionalData",
            TupleDefinitions.UpgradedFilesOptionalData,
            new[]
            {
                new ColumnDefinition("Upgraded", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "UpgradedImages", keyColumn: 1, description: "Foreign key, Upgraded image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("AllowIgnoreOnPatchError", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
                new ColumnDefinition("IncludeWholeFile", ColumnType.Number, 0, primaryKey: false, nullable: true, ColumnCategory.Integer),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TargetImages = new TableDefinition(
            "TargetImages",
            TupleDefinitions.TargetImages,
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
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition TargetFilesOptionalData = new TableDefinition(
            "TargetFiles_OptionalData",
            TupleDefinitions.TargetFilesOptionalData,
            new[]
            {
                new ColumnDefinition("Target", ColumnType.String, 13, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "TargetImages", keyColumn: 1, description: "Foreign key, Target image"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SymbolPaths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreLengths", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("RetainOffsets", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition FamilyFileRanges = new TableDefinition(
            "FamilyFileRanges",
            TupleDefinitions.FamilyFileRanges,
            new[]
            {
                new ColumnDefinition("Family", ColumnType.String, 8, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "ImageFamilies", keyColumn: 1, description: "Foreign key, Family"),
                new ColumnDefinition("FTK", ColumnType.String, 255, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "File", keyColumn: 1, description: "Foreign key, File to ignore", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RetainOffsets", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text),
                new ColumnDefinition("RetainLengths", ColumnType.String, 128, primaryKey: false, nullable: false, ColumnCategory.Text),
            },
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition ExternalFiles = new TableDefinition(
            "ExternalFiles",
            TupleDefinitions.ExternalFiles,
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
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixAction = new TableDefinition(
            "WixAction",
            TupleDefinitions.WixAction,
            new[]
            {
                new ColumnDefinition("SequenceTable", ColumnType.String, 62, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Condition", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Unknown, forceLocalizable: true),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Before", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("After", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Overridable", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixActionRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixBBControl = new TableDefinition(
            "WixBBControl",
            null,
            new[]
            {
                new ColumnDefinition("Billboard_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Unknown, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("BBControl_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("SourceFile", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixComplexReference = new TableDefinition(
            "WixComplexReference",
            TupleDefinitions.WixComplexReference,
            new[]
            {
                new ColumnDefinition("Parent", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown, forceLocalizable: true),
                new ColumnDefinition("ParentAttributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ParentLanguage", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Child", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown, forceLocalizable: true),
                new ColumnDefinition("ChildAttributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixComplexReferenceRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixComponentGroup = new TableDefinition(
            "WixComponentGroup",
            TupleDefinitions.WixComponentGroup,
            new[]
            {
                new ColumnDefinition("WixComponentGroup", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixControl = new TableDefinition(
            "WixControl",
            null,
            new[]
            {
                new ColumnDefinition("Dialog_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Control_", ColumnType.String, 50, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("SourceFile", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixCustomRow = new TableDefinition(
            "WixCustomRow",
            TupleDefinitions.WixCustomRow,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 62, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("FieldData", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixCustomTable = new TableDefinition(
            "WixCustomTable",
            TupleDefinitions.WixCustomTable,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 62, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ColumnCount", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ColumnNames", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ColumnTypes", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("PrimaryKeys", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("MinValues", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("MaxValues", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("KeyTables", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("KeyColumns", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Categories", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Sets", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Descriptions", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Modularizations", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("BootstrapperApplicationData", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixDirectory = new TableDefinition(
            "WixDirectory",
            null,
            new[]
            {
                new ColumnDefinition("Directory_", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ComponentGuidGenerationSeed", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixEnsureTable = new TableDefinition(
            "WixEnsureTable",
            TupleDefinitions.WixEnsureTable,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 31, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixFeatureGroup = new TableDefinition(
            "WixFeatureGroup",
            TupleDefinitions.WixFeatureGroup,
            new[]
            {
                new ColumnDefinition("WixFeatureGroup", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixPatchFamilyGroup = new TableDefinition(
            "WixPatchFamilyGroup",
            TupleDefinitions.WixPatchFamilyGroup,
            new[]
            {
                new ColumnDefinition("WixPatchFamilyGroup", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixGroup = new TableDefinition(
            "WixGroup",
            TupleDefinitions.WixGroup,
            new[]
            {
                new ColumnDefinition("ParentId", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular record in a parent table."),
                new ColumnDefinition("ParentType", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Primary key used to identify a particular parent type in a parent table."),
                new ColumnDefinition("ChildId", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify a particular record in a child table."),
                new ColumnDefinition("ChildType", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Primary key used to identify a particular child type in a child table."),
            },
            unreal: true,
            strongRowType: typeof(WixGroupRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixFeatureModules = new TableDefinition(
            "WixFeatureModules",
            TupleDefinitions.WixFeatureModules,
            new[]
            {
                new ColumnDefinition("Feature_", ColumnType.String, 38, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("WixMerge_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixFile = new TableDefinition(
            "WixFile",
            null,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("AssemblyType", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
                new ColumnDefinition("File_AssemblyManifest", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File_AssemblyApplication", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown, keyTable: "Directory", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DiskId", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Source", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ProcessorArchitecture", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("PatchGroup", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("PatchAttributes", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("DeltaPatchHeaderSource", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixBindUpdatedFiles = new TableDefinition(
            "WixBindUpdatedFiles",
            TupleDefinitions.WixBindUpdatedFiles,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "File", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixBuildInfo = new TableDefinition(
            "WixBuildInfo",
            TupleDefinitions.WixBuildInfo,
            new[]
            {
                new ColumnDefinition("WixVersion", ColumnType.String, 20, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Version number of WiX."),
                new ColumnDefinition("WixOutputFile", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Path to output file, if supplied."),
                new ColumnDefinition("WixProjectFile", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Path to .wixproj file, if supplied."),
                new ColumnDefinition("WixPdbFile", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Path to .wixpdb file, if supplied."),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixFragment = new TableDefinition(
            "WixFragment",
            TupleDefinitions.WixFragment,
            new[]
            {
                new ColumnDefinition("WixFragment", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixInstanceComponent = new TableDefinition(
            "WixInstanceComponent",
            TupleDefinitions.WixInstanceComponent,
            new[]
            {
                new ColumnDefinition("Component_", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, modularizeType: ColumnModularizeType.Column),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixInstanceTransforms = new TableDefinition(
            "WixInstanceTransforms",
            TupleDefinitions.WixInstanceTransforms,
            new[]
            {
                new ColumnDefinition("Id", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("PropertyId", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ProductCode", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Guid),
                new ColumnDefinition("ProductName", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown, forceLocalizable: true),
                new ColumnDefinition("UpgradeCode", ColumnType.String, 38, primaryKey: false, nullable: true, ColumnCategory.Guid),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixMedia = new TableDefinition(
            "WixMedia",
            null,
            new[]
            {
                new ColumnDefinition("DiskId_", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("CompressionLevel", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 4),
                new ColumnDefinition("Layout", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixMediaRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixMediaTemplate = new TableDefinition(
            "WixMediaTemplate",
            TupleDefinitions.WixMediaTemplate,
            new[]
            {
                new ColumnDefinition("CabinetTemplate", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("CompressionLevel", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 4),
                new ColumnDefinition("DiskPrompt", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("VolumeLabel", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("MaximumUncompressedMediaSize", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("MaximumCabinetSizeForLargeFileSplitting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixMediaTemplateRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixMerge = new TableDefinition(
            "WixMerge",
            TupleDefinitions.WixMerge,
            new[]
            {
                new ColumnDefinition("WixMerge", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Language", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, forceLocalizable: true),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("SourceFile", ColumnType.Object, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("DiskId", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("FileCompression", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("ConfigurationData", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Feature_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixMergeRow),
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixOrdering = new TableDefinition(
            "WixOrdering",
            TupleDefinitions.WixOrdering,
            new[]
            {
                new ColumnDefinition("ItemType", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Primary key used to identify the item in another table."),
                new ColumnDefinition("ItemId_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Reference to an entry in another table."),
                new ColumnDefinition("DependsOnType", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "Primary key used to identify the item in another table."),
                new ColumnDefinition("DependsOnId_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Reference to an entry in another table."),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixDeltaPatchFile = new TableDefinition(
            "WixDeltaPatchFile",
            TupleDefinitions.WixDeltaPatchFile,
            new[]
            {
                new ColumnDefinition("File_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown, keyTable: "File", keyColumn: 1, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RetainLengths", ColumnType.Preserved, 0, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreOffsets", ColumnType.Preserved, 0, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("IgnoreLengths", ColumnType.Preserved, 0, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("RetainOffsets", ColumnType.Preserved, 0, primaryKey: false, nullable: true, ColumnCategory.Text),
                new ColumnDefinition("SymbolPaths", ColumnType.Preserved, 0, primaryKey: false, nullable: true, ColumnCategory.Text),
            },
            unreal: true,
            strongRowType: typeof(WixDeltaPatchFileRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixDeltaPatchSymbolPaths = new TableDefinition(
            "WixDeltaPatchSymbolPaths",
            TupleDefinitions.WixDeltaPatchSymbolPaths,
            new[]
            {
                new ColumnDefinition("Id", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Type", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 4),
                new ColumnDefinition("SymbolPaths", ColumnType.Preserved, 0, primaryKey: false, nullable: false, ColumnCategory.Text),
            },
            unreal: true,
            strongRowType: typeof(WixDeltaPatchSymbolPathsRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixProperty = new TableDefinition(
            "WixProperty",
            TupleDefinitions.WixProperty,
            new[]
            {
                new ColumnDefinition("Property_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown, modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixPropertyRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixSimpleReference = new TableDefinition(
            "WixSimpleReference",
            TupleDefinitions.WixSimpleReference,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("PrimaryKeys", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            strongRowType: typeof(WixSimpleReferenceRow),
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixSuppressAction = new TableDefinition(
            "WixSuppressAction",
            TupleDefinitions.WixSuppressAction,
            new[]
            {
                new ColumnDefinition("SequenceTable", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Action", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixSuppressModularization = new TableDefinition(
            "WixSuppressModularization",
            TupleDefinitions.WixSuppressModularization,
            new[]
            {
                new ColumnDefinition("WixSuppressModularization", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixPatchBaseline = new TableDefinition(
            "WixPatchBaseline",
            TupleDefinitions.WixPatchBaseline,
            new[]
            {
                new ColumnDefinition("WixPatchBaseline", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key used to identify sets of transforms in a patch."),
                new ColumnDefinition("DiskId", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ValidationFlags", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Integer, description: "Patch transform validation flags for the associated patch baseline."),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixPatchRef = new TableDefinition(
            "WixPatchRef",
            TupleDefinitions.WixPatchRef,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("PrimaryKeys", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixPatchId = new TableDefinition(
            "WixPatchId",
            TupleDefinitions.WixPatchId,
            new[]
            {
                new ColumnDefinition("ProductCode", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("ClientPatchId", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("OptimizePatchSizeForLargeFiles", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 1),
                new ColumnDefinition("ApiPatchingSymbolFlags", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 0, maxValue: 7),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixPatchTarget = new TableDefinition(
            "WixPatchTarget",
            TupleDefinitions.WixPatchTarget,
            new[]
            {
                new ColumnDefinition("ProductCode", ColumnType.String, 38, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixPatchMetadata = new TableDefinition(
            "WixPatchMetadata",
            null,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixUI = new TableDefinition(
            "WixUI",
            TupleDefinitions.WixUI,
            new[]
            {
                new ColumnDefinition("WixUI", ColumnType.String, 0, primaryKey: true, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixVariable = new TableDefinition(
            "WixVariable",
            TupleDefinitions.WixVariable,
            new[]
            {
                new ColumnDefinition("WixVariable", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 0, primaryKey: false, nullable: true, ColumnCategory.Unknown),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            unreal: true,
            tupleIdIsPrimaryKey: true
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
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition SummaryInformation = new TableDefinition(
            "_SummaryInformation",
            TupleDefinitions.SummaryInformation,
            new[]
            {
                new ColumnDefinition("PropertyId", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown),
                new ColumnDefinition("Value", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Unknown),
            },
            tupleIdIsPrimaryKey: false
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
            tupleIdIsPrimaryKey: false
        );

        public static readonly TableDefinition Validation = new TableDefinition(
            "_Validation",
            null,
            new[]
            {
                new ColumnDefinition("Table", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of table"),
                new ColumnDefinition("Column", ColumnType.String, 32, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of column"),
                new ColumnDefinition("Nullable", ColumnType.String, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "Y;N", description: "Whether the column is nullable"),
                new ColumnDefinition("MinValue", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Minimum value allowed"),
                new ColumnDefinition("MaxValue", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: -2147483647, maxValue: 2147483647, description: "Maximum value allowed"),
                new ColumnDefinition("KeyTable", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Identifier, description: "For foreign key, Name of table to which data must link"),
                new ColumnDefinition("KeyColumn", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, minValue: 1, maxValue: 32, description: "Column to which foreign key connects"),
                new ColumnDefinition("Category", ColumnType.String, 32, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "Text;Formatted;Template;Condition;Guid;Path;Version;Language;Identifier;Binary;UpperCase;LowerCase;Filename;Paths;AnyPath;WildCardFilename;RegPath;CustomSource;Property;Cabinet;Shortcut;FormattedSDDLText;Integer;DoubleInteger;TimeDate;DefaultDir", description: "String category"),
                new ColumnDefinition("Set", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Set of values that are permitted"),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Description of column"),
            },
            tupleIdIsPrimaryKey: false
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
            WixAction,
            WixBBControl,
            WixComplexReference,
            WixComponentGroup,
            WixControl,
            WixCustomRow,
            WixCustomTable,
            WixDirectory,
            WixEnsureTable,
            WixFeatureGroup,
            WixPatchFamilyGroup,
            WixGroup,
            WixFeatureModules,
            WixFile,
            WixBindUpdatedFiles,
            WixBuildInfo,
            WixFragment,
            WixInstanceComponent,
            WixInstanceTransforms,
            WixMedia,
            WixMediaTemplate,
            WixMerge,
            WixOrdering,
            WixDeltaPatchFile,
            WixDeltaPatchSymbolPaths,
            WixProperty,
            WixSimpleReference,
            WixSuppressAction,
            WixSuppressModularization,
            WixPatchBaseline,
            WixPatchRef,
            WixPatchId,
            WixPatchTarget,
            WixPatchMetadata,
            WixUI,
            WixVariable,
            Streams,
            SummaryInformation,
            TransformView,
            Validation,
        };
    }
}
