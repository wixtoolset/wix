namespace WixToolset.Core.WindowsInstaller.Decompile
{
    using System.Xml.Linq;

    internal static class Names
    {
        public static readonly XNamespace WxsNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        public static readonly XName WixElement = WxsNamespace + "Wix";

        public static readonly XName ProductElement = WxsNamespace + "Product";
        public static readonly XName ModuleElement = WxsNamespace + "Module";
        public static readonly XName PatchCreationElement = WxsNamespace + "PatchCreation";

        public static readonly XName CustomElement = WxsNamespace + "Custom";

        public static readonly XName AdminExecuteSequenceElement = WxsNamespace + "AdminExecuteSequence";
        public static readonly XName AdminUISequenceElement = WxsNamespace + "AdminUISequence";
        public static readonly XName AdvertiseExecuteSequenceElement = WxsNamespace + "AdvertiseExecuteSequence";
        public static readonly XName InstallExecuteSequenceElement = WxsNamespace + "InstallExecuteSequence";
        public static readonly XName InstallUISequenceElement = WxsNamespace + "InstallUISequence";

        public static readonly XName AppSearchElement = WxsNamespace + "AppSearch";

        public static readonly XName PropertyElement = WxsNamespace + "Property";

        public static readonly XName ProtectRangeElement = WxsNamespace + "ProtectRange";
        public static readonly XName ProtectFileElement = WxsNamespace + "ProtectFile";

        public static readonly XName FileElement = WxsNamespace + "File";

        public static readonly XName EnsureTableElement = WxsNamespace + "EnsureTable";
        public static readonly XName PackageElement = WxsNamespace + "Package";
        public static readonly XName PatchInformationElement = WxsNamespace + "PatchInformation";
        
        public static readonly XName ProgressTextElement = WxsNamespace + "ProgressText";
        public static readonly XName UIElement = WxsNamespace + "UI";

        public static readonly XName AppIdElement = WxsNamespace + "AppId";

        public static readonly XName ControlElement = WxsNamespace + "Control";

        public static readonly XName BillboardElement = WxsNamespace + "Billboard";
        public static readonly XName BillboardActionElement = WxsNamespace + "BillboardAction";
        
        public static readonly XName BinaryElement = WxsNamespace + "Binary";

        public static readonly XName ClassElement = WxsNamespace + "Class";

        public static readonly XName FileTypeMaskElement = WxsNamespace + "FileTypeMask";

        public static readonly XName ComboBoxElement = WxsNamespace + "ComboBox";

        public static readonly XName ListItemElement = WxsNamespace + "ListItem";

        public static readonly XName ConditionElement = WxsNamespace + "Condition";
        public static readonly XName PublishElement = WxsNamespace + "Publish";
        public static readonly XName CustomTableElement = WxsNamespace + "CustomTable";
        public static readonly XName ColumnElement = WxsNamespace + "Column";
        public static readonly XName RowElement = WxsNamespace + "Row";
        public static readonly XName DataElement = WxsNamespace + "Data";
        public static readonly XName CreateFolderElement = WxsNamespace + "CreateFolder";

        public static readonly XName CustomActionElement = WxsNamespace + "CustomAction";

        public static readonly XName ComponentSearchElement = WxsNamespace + "ComponentSearch";
        public static readonly XName ComponentElement = WxsNamespace + "Component";

        public static readonly XName LevelElement = WxsNamespace + "Level";
        public static readonly XName DialogElement = WxsNamespace + "Dialog";
        public static readonly XName DirectoryElement = WxsNamespace + "Directory";
        public static readonly XName DirectorySearchElement = WxsNamespace + "DirectorySearch";
        public static readonly XName CopyFileElement = WxsNamespace + "CopyFile";
        public static readonly XName EnvironmentElement = WxsNamespace + "Environment";
        public static readonly XName ErrorElement = WxsNamespace + "Error";
        public static readonly XName SubscribeElement = WxsNamespace + "Subscribe";
        public static readonly XName ExtensionElement = WxsNamespace + "Extension";
        public static readonly XName ExternalFileElement = WxsNamespace + "ExternalFile";
        public static readonly XName SymbolPathElement = WxsNamespace + "SymbolPath";
        public static readonly XName IgnoreRangeElement = WxsNamespace + "IgnoreRange";

        public static readonly XName FeatureElement = WxsNamespace + "Feature";
        public static readonly XName ComponentRefElement = WxsNamespace + "ComponentRef";
        public static readonly XName SFPFileElement = WxsNamespace + "SFPFile";
        public static readonly XName IconElement = WxsNamespace + "Icon";
        public static readonly XName FamilyElement = WxsNamespace + "Family";
        public static readonly XName IniFileElement = WxsNamespace + "IniFile";
        public static readonly XName IniFileSearchElement = WxsNamespace + "IniFileSearch";
        public static readonly XName IsolateComponentElement = WxsNamespace + "IsolateComponent";
        public static readonly XName LaunchElement = WxsNamespace + "Launch";
        public static readonly XName ListBoxElement = WxsNamespace + "ListBox";
        public static readonly XName ListViewElement = WxsNamespace + "ListView";
        public static readonly XName PermissionElement = WxsNamespace + "Permission";
        public static readonly XName MediaElement = WxsNamespace + "Media";
        public static readonly XName MIMEElement = WxsNamespace + "MIME";
        public static readonly XName ConfigurationElement = WxsNamespace + "Configuration";
        public static readonly XName DependencyElement = WxsNamespace + "Dependency";
        public static readonly XName ExclusionElement = WxsNamespace + "Exclusion";
        public static readonly XName IgnoreTableElement = WxsNamespace + "IgnoreTable";
        public static readonly XName SubstitutionElement = WxsNamespace + "Substitution";
        public static readonly XName DigitalCertificateElement = WxsNamespace + "DigitalCertificate";
        public static readonly XName DigitalSignatureElement = WxsNamespace + "DigitalSignature";
        public static readonly XName EmbeddedChainerElement = WxsNamespace + "EmbeddedChainer";
        public static readonly XName EmbeddedUIElement = WxsNamespace + "EmbeddedUI";
        public static readonly XName EmbeddedUIResourceElement = WxsNamespace + "EmbeddedUIResource";
        public static readonly XName PermissionExElement = WxsNamespace + "PermissionEx";
        public static readonly XName PackageCertificatesElement = WxsNamespace + "PackageCertificates";
        public static readonly XName PatchCertificatesElement = WxsNamespace + "PatchCertificates";
        public static readonly XName ShortcutPropertyElement = WxsNamespace + "ShortcutProperty";
        public static readonly XName ODBCDataSourceElement = WxsNamespace + "ODBCDataSource";
        public static readonly XName ODBCDriverElement = WxsNamespace + "ODBCDriver";
        public static readonly XName ODBCTranslatorElement = WxsNamespace + "ODBCTranslator";
        public static readonly XName PatchMetadataElement = WxsNamespace + "PatchMetadata";
        public static readonly XName OptimizeCustomActionsElement = WxsNamespace + "OptimizeCustomActions";
        public static readonly XName CustomPropertyElement = WxsNamespace + "CustomProperty";
        public static readonly XName PatchSequenceElement = WxsNamespace + "PatchSequence";
        public static readonly XName ProgIdElement = WxsNamespace + "ProgId";
        public static readonly XName ReplacePatchElement = WxsNamespace + "ReplacePatch";
        public static readonly XName TargetProductCodeElement = WxsNamespace + "TargetProductCode";
        public static readonly XName PatchPropertyElement = WxsNamespace + "PatchProperty";
        public static readonly XName CategoryElement = WxsNamespace + "Category";
        public static readonly XName RadioButtonElement = WxsNamespace + "RadioButton";
        public static readonly XName RadioButtonGroupElement = WxsNamespace + "RadioButtonGroup";
        public static readonly XName RegistryKeyElement = WxsNamespace + "RegistryKey";
        public static readonly XName RegistryValueElement = WxsNamespace + "RegistryValue";
        public static readonly XName MultiStringElement = WxsNamespace + "MultiString";
        public static readonly XName RegistrySearchElement = WxsNamespace + "RegistrySearch";
        public static readonly XName RemoveFolderElement = WxsNamespace + "RemoveFolder";
        public static readonly XName RemoveFileElement = WxsNamespace + "RemoveFile";
        public static readonly XName RemoveRegistryKeyElement = WxsNamespace + "RemoveRegistryKey";
        public static readonly XName RemoveRegistryValueElement = WxsNamespace + "RemoveRegistryValue";
        public static readonly XName ReserveCostElement = WxsNamespace + "ReserveCost";
        public static readonly XName ServiceControlElement = WxsNamespace + "ServiceControl";
        public static readonly XName ServiceArgumentElement = WxsNamespace + "ServiceArgument";
        public static readonly XName ServiceInstallElement = WxsNamespace + "ServiceInstall";
        public static readonly XName ServiceDependencyElement = WxsNamespace + "ServiceDependency";
        public static readonly XName SFPCatalogElement = WxsNamespace + "SFPCatalog";
        public static readonly XName ShortcutElement = WxsNamespace + "Shortcut";
        public static readonly XName FileSearchElement = WxsNamespace + "FileSearch";
        public static readonly XName TargetFileElement = WxsNamespace + "TargetFile";
        public static readonly XName TargetImageElement = WxsNamespace + "TargetImage";
        public static readonly XName TextStyleElement = WxsNamespace + "TextStyle";
        public static readonly XName TypeLibElement = WxsNamespace + "TypeLib";
        public static readonly XName UpgradeElement = WxsNamespace + "Upgrade";
        public static readonly XName UpgradeVersionElement = WxsNamespace + "UpgradeVersion";
        public static readonly XName UpgradeFileElement = WxsNamespace + "UpgradeFile";
        public static readonly XName UpgradeImageElement = WxsNamespace + "UpgradeImage";
        public static readonly XName UITextElement = WxsNamespace + "UIText";
        public static readonly XName VerbElement = WxsNamespace + "Verb";
        public static readonly XName ComplianceCheckElement = WxsNamespace + "ComplianceCheck";
        public static readonly XName FileSearchRefElement = WxsNamespace + "FileSearchRef";
        public static readonly XName ComplianceDriveElement = WxsNamespace + "ComplianceDrive";
        public static readonly XName DirectorySearchRefElement = WxsNamespace + "DirectorySearchRef";
        public static readonly XName RegistrySearchRefElement = WxsNamespace + "RegistrySearchRef";
        public static readonly XName MajorUpgradeElement = WxsNamespace + "MajorUpgrade";
        //public static readonly XName Element = WxsNamespace + "";
    }
}
