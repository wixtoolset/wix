// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters.Symbolizer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Wix3 = Microsoft.Tools.WindowsInstallerXml;

#pragma warning disable 1591 // TODO: add documentation
    public static class ConvertSymbols
    {
        public static Intermediate ConvertFile(string path)
        {
            var output = Wix3.Output.Load(path, suppressVersionCheck: true, suppressSchema: true);
            return ConvertOutput(output);
        }

        public static Intermediate ConvertOutput(Wix3.Output output)
#pragma warning restore 1591
        {
            var section = new IntermediateSection(String.Empty, OutputType3ToSectionType4(output.Type));

            var wixMediaByDiskId = IndexWixMediaTableByDiskId(output);
            var componentsById = IndexById<Wix3.Row>(output, "Component");
            var bindPathsById = IndexById<Wix3.Row>(output, "BindPath");
            var fontsById = IndexById<Wix3.Row>(output, "Font");
            var selfRegById = IndexById<Wix3.Row>(output, "SelfReg");
            var wixDirectoryById = IndexById<Wix3.Row>(output, "WixDirectory");
            var wixFileById = IndexById<Wix3.Row>(output, "WixFile");

            foreach (Wix3.Table table in output.Tables)
            {
                foreach (Wix3.Row row in table.Rows)
                {
                    var symbol = GenerateSymbolFromRow(row, wixMediaByDiskId, componentsById, fontsById, bindPathsById, selfRegById, wixFileById, wixDirectoryById);
                    if (symbol != null)
                    {
                        section.AddSymbol(symbol);
                    }
                }
            }

            return new Intermediate(String.Empty, new[] { section }, localizationsByCulture: null);
        }

        private static Dictionary<int, Wix3.WixMediaRow> IndexWixMediaTableByDiskId(Wix3.Output output)
        {
            var wixMediaByDiskId = new Dictionary<int, Wix3.WixMediaRow>();
            var wixMediaTable = output.Tables["WixMedia"];

            if (wixMediaTable != null)
            {
                foreach (Wix3.WixMediaRow row in wixMediaTable.Rows)
                {
                    wixMediaByDiskId.Add(FieldAsInt(row, 0), row);
                }
            }

            return wixMediaByDiskId;
        }

        private static Dictionary<string, T> IndexById<T>(Wix3.Output output, string tableName) where T : Wix3.Row
        {
            var byId = new Dictionary<string, T>();
            var table = output.Tables[tableName];

            if (table != null)
            {
                foreach (T row in table.Rows)
                {
                    byId.Add(FieldAsString(row, 0), row);
                }
            }

            return byId;
        }

        private static IntermediateSymbol GenerateSymbolFromRow(Wix3.Row row, Dictionary<int, Wix3.WixMediaRow> wixMediaByDiskId, Dictionary<string, Wix3.Row> componentsById, Dictionary<string, Wix3.Row> fontsById, Dictionary<string, Wix3.Row> bindPathsById, Dictionary<string, Wix3.Row> selfRegById, Dictionary<string, Wix3.Row> wixFileById, Dictionary<string, Wix3.Row> wixDirectoryById)
        {
            var name = row.Table.Name;
            switch (name)
            {
            case "_SummaryInformation":
                return DefaultSymbolFromRow(typeof(SummaryInformationSymbol), row, columnZeroIsId: false);
            case "ActionText":
                return DefaultSymbolFromRow(typeof(ActionTextSymbol), row, columnZeroIsId: false);
            case "AppId":
                return DefaultSymbolFromRow(typeof(AppIdSymbol), row, columnZeroIsId: false);
            case "AppSearch":
                return DefaultSymbolFromRow(typeof(AppSearchSymbol), row, columnZeroIsId: false);
            case "Billboard":
                return DefaultSymbolFromRow(typeof(BillboardSymbol), row, columnZeroIsId: true);
            case "Binary":
                return DefaultSymbolFromRow(typeof(BinarySymbol), row, columnZeroIsId: true);
            case "BindPath":
                return null;
            case "CCPSearch":
                return DefaultSymbolFromRow(typeof(CCPSearchSymbol), row, columnZeroIsId: true);
            case "Class":
                return DefaultSymbolFromRow(typeof(ClassSymbol), row, columnZeroIsId: false);
            case "CompLocator":
                return DefaultSymbolFromRow(typeof(CompLocatorSymbol), row, columnZeroIsId: false);
            case "Component":
            {
                var attributes = FieldAsNullableInt(row, 3);

                var location = ComponentLocation.LocalOnly;
                if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesSourceOnly) == WindowsInstallerConstants.MsidbComponentAttributesSourceOnly)
                {
                    location = ComponentLocation.SourceOnly;
                }
                else if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesOptional) == WindowsInstallerConstants.MsidbComponentAttributesOptional)
                {
                    location = ComponentLocation.Either;
                }

                var keyPath = FieldAsString(row, 5);
                var keyPathType = String.IsNullOrEmpty(keyPath) ? ComponentKeyPathType.Directory : ComponentKeyPathType.File;
                if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath) == WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath)
                {
                    keyPathType = ComponentKeyPathType.Registry;
                }
                else if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource) == WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource)
                {
                    keyPathType = ComponentKeyPathType.OdbcDataSource;
                }

                return new ComponentSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    ComponentId = FieldAsString(row, 1),
                    DirectoryRef = FieldAsString(row, 2),
                    Condition = FieldAsString(row, 4),
                    KeyPath = keyPath,
                    Location = location,
                    DisableRegistryReflection = (attributes & WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection) == WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection,
                    NeverOverwrite = (attributes & WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite) == WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite,
                    Permanent = (attributes & WindowsInstallerConstants.MsidbComponentAttributesPermanent) == WindowsInstallerConstants.MsidbComponentAttributesPermanent,
                    SharedDllRefCount = (attributes & WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount) == WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount,
                    Shared = (attributes & WindowsInstallerConstants.MsidbComponentAttributesShared) == WindowsInstallerConstants.MsidbComponentAttributesShared,
                    Transitive = (attributes & WindowsInstallerConstants.MsidbComponentAttributesTransitive) == WindowsInstallerConstants.MsidbComponentAttributesTransitive,
                    UninstallWhenSuperseded = (attributes & WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence) == WindowsInstallerConstants.MsidbComponentAttributesUninstallOnSupersedence,
                    Win64 = (attributes & WindowsInstallerConstants.MsidbComponentAttributes64bit) == WindowsInstallerConstants.MsidbComponentAttributes64bit,
                    KeyPathType = keyPathType,
                };
            }

            case "Condition":
                return DefaultSymbolFromRow(typeof(ConditionSymbol), row, columnZeroIsId: false);
            case "CreateFolder":
                return DefaultSymbolFromRow(typeof(CreateFolderSymbol), row, columnZeroIsId: false);
            case "CustomAction":
            {
                var caType = FieldAsInt(row, 1);
                var executionType = DetermineCustomActionExecutionType(caType);
                var sourceType = DetermineCustomActionSourceType(caType);
                var targetType = DetermineCustomActionTargetType(caType);

                return new CustomActionSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    ExecutionType = executionType,
                    SourceType = sourceType,
                    Source = FieldAsString(row, 2),
                    TargetType = targetType,
                    Target = FieldAsString(row, 3),
                    Win64 = (caType & WindowsInstallerConstants.MsidbCustomActionType64BitScript) == WindowsInstallerConstants.MsidbCustomActionType64BitScript,
                    TSAware = (caType & WindowsInstallerConstants.MsidbCustomActionTypeTSAware) == WindowsInstallerConstants.MsidbCustomActionTypeTSAware,
                    Impersonate = (caType & WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate) != WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate,
                    IgnoreResult = (caType & WindowsInstallerConstants.MsidbCustomActionTypeContinue) == WindowsInstallerConstants.MsidbCustomActionTypeContinue,
                    Hidden = (caType & WindowsInstallerConstants.MsidbCustomActionTypeHideTarget) == WindowsInstallerConstants.MsidbCustomActionTypeHideTarget,
                    Async = (caType & WindowsInstallerConstants.MsidbCustomActionTypeAsync) == WindowsInstallerConstants.MsidbCustomActionTypeAsync,
                };
            }

            case "Directory":
            {
                var id = FieldAsString(row, 0);
                var splits = SplitDefaultDir(FieldAsString(row, 2));

                var symbol = new DirectorySymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, id))
                {
                    ParentDirectoryRef = FieldAsString(row, 1),
                    Name = splits[0],
                    ShortName = splits[1],
                    SourceName = splits[2],
                    SourceShortName = splits[3]
                };

                if (wixDirectoryById.TryGetValue(id, out var wixDirectoryRow))
                {
                    symbol.ComponentGuidGenerationSeed = FieldAsString(wixDirectoryRow, 1);
                }

                return symbol;
            }
            case "DrLocator":
                return DefaultSymbolFromRow(typeof(DrLocatorSymbol), row, columnZeroIsId: false);
            case "DuplicateFile":
            {
                var splitName = FieldAsString(row, 3)?.Split('|');

                var symbol = new DuplicateFileSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    ComponentRef = FieldAsString(row, 1),
                    FileRef = FieldAsString(row, 2),
                    DestinationName = splitName == null ? null : splitName.Length > 1 ? splitName[1] : splitName[0],
                    DestinationShortName = splitName == null ? null : splitName.Length > 1 ? splitName[0] : null,
                    DestinationFolder = FieldAsString(row, 4)
                };

                return symbol;
            }
            case "Error":
                return DefaultSymbolFromRow(typeof(ErrorSymbol), row, columnZeroIsId: false);
            case "Extension":
                return DefaultSymbolFromRow(typeof(ExtensionSymbol), row, columnZeroIsId: false);
            case "Feature":
            {
                var attributes = FieldAsInt(row, 7);
                var installDefault = FeatureInstallDefault.Local;
                if ((attributes & WindowsInstallerConstants.MsidbFeatureAttributesFollowParent) == WindowsInstallerConstants.MsidbFeatureAttributesFollowParent)
                {
                    installDefault = FeatureInstallDefault.FollowParent;
                }
                else if ((attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorSource) == WindowsInstallerConstants.MsidbFeatureAttributesFavorSource)
                {
                    installDefault = FeatureInstallDefault.Source;
                }

                return new FeatureSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    ParentFeatureRef = FieldAsString(row, 1),
                    Title = FieldAsString(row, 2),
                    Description = FieldAsString(row, 3),
                    Display = FieldAsInt(row, 4), // BUGBUGBUG: FieldAsNullableInt(row, 4),
                    Level = FieldAsInt(row, 5),
                    DirectoryRef = FieldAsString(row, 6),
                    DisallowAbsent = (attributes & WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent) == WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent,
                    DisallowAdvertise = (attributes & WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise) == WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise,
                    InstallDefault = installDefault,
                    TypicalDefault = (attributes & WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise) == WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise ? FeatureTypicalDefault.Advertise : FeatureTypicalDefault.Install,
                };
            }

            case "FeatureComponents":
                return DefaultSymbolFromRow(typeof(FeatureComponentsSymbol), row, columnZeroIsId: false);
            case "File":
            {
                var attributes = FieldAsNullableInt(row, 6);

                FileSymbolAttributes symbolAttributes = 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesReadOnly) == WindowsInstallerConstants.MsidbFileAttributesReadOnly ? FileSymbolAttributes.ReadOnly : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesHidden) == WindowsInstallerConstants.MsidbFileAttributesHidden ? FileSymbolAttributes.Hidden : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesSystem) == WindowsInstallerConstants.MsidbFileAttributesSystem ? FileSymbolAttributes.System : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesVital) == WindowsInstallerConstants.MsidbFileAttributesVital ? FileSymbolAttributes.Vital : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesChecksum) == WindowsInstallerConstants.MsidbFileAttributesChecksum ? FileSymbolAttributes.Checksum : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) == WindowsInstallerConstants.MsidbFileAttributesNoncompressed ? FileSymbolAttributes.Uncompressed : 0;
                symbolAttributes |= (attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed) == WindowsInstallerConstants.MsidbFileAttributesCompressed ? FileSymbolAttributes.Compressed : 0;

                var id = FieldAsString(row, 0);
                var splitName = FieldAsString(row, 2).Split('|');

                var symbol = new FileSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, id))
                {
                    ComponentRef = FieldAsString(row, 1),
                    Name = splitName.Length > 1 ? splitName[1] : splitName[0],
                    ShortName = splitName.Length > 1 ? splitName[0] : null,
                    FileSize = FieldAsInt(row, 3),
                    Version = FieldAsString(row, 4),
                    Language = FieldAsString(row, 5),
                    Attributes = symbolAttributes
                };

                if (bindPathsById.TryGetValue(id, out var bindPathRow))
                {
                    symbol.BindPath = FieldAsString(bindPathRow, 1) ?? String.Empty;
                }

                if (fontsById.TryGetValue(id, out var fontRow))
                {
                    symbol.FontTitle = FieldAsString(fontRow, 1) ?? String.Empty;
                }

                if (selfRegById.TryGetValue(id, out var selfRegRow))
                {
                    symbol.SelfRegCost = FieldAsNullableInt(selfRegRow, 1) ?? 0;
                }

                if (wixFileById.TryGetValue(id, out var wixFileRow))
                {
                    symbol.DirectoryRef = FieldAsString(wixFileRow, 4);
                    symbol.DiskId = FieldAsNullableInt(wixFileRow, 5) ?? 0;
                    symbol.Source = new IntermediateFieldPathValue { Path = FieldAsString(wixFileRow, 6) };
                    symbol.PatchGroup = FieldAsInt(wixFileRow, 8);
                    symbol.PatchAttributes = (PatchAttributeType)FieldAsInt(wixFileRow, 10);
                }

                return symbol;
            }
            case "Font":
                return null;
            case "Icon":
                return DefaultSymbolFromRow(typeof(IconSymbol), row, columnZeroIsId: true);
            case "IniFile":
            {
                var splitName = FieldAsString(row, 1).Split('|');
                var action = FieldAsInt(row, 6);

                var symbol = new IniFileSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    FileName = splitName.Length > 1 ? splitName[1] : splitName[0],
                    ShortFileName = splitName.Length > 1 ? splitName[0] : null,
                    DirProperty = FieldAsString(row, 2),
                    Section = FieldAsString(row, 3),
                    Key = FieldAsString(row, 4),
                    Value = FieldAsString(row, 5),
                    Action = action == 3 ? IniFileActionType.AddTag : action == 1 ? IniFileActionType.CreateLine : IniFileActionType.AddLine,
                    ComponentRef = FieldAsString(row, 7),
                };

                return symbol;
            }
            case "IniLocator":
            {
                var splitName = FieldAsString(row, 1).Split('|');

                var symbol = new IniLocatorSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    FileName = splitName.Length > 1 ? splitName[1] : splitName[0],
                    ShortFileName = splitName.Length > 1 ? splitName[0] : null,
                    Section = FieldAsString(row, 2),
                    Key = FieldAsString(row, 3),
                    Field = FieldAsInt(row, 4),
                    Type = FieldAsInt(row, 5),
                };

                return symbol;
            }
            case "LockPermissions":
                return DefaultSymbolFromRow(typeof(LockPermissionsSymbol), row, columnZeroIsId: false);
            case "Media":
            {
                var diskId = FieldAsInt(row, 0);
                var symbol = new MediaSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, diskId))
                {
                    DiskId = diskId,
                    LastSequence = FieldAsNullableInt(row, 1),
                    DiskPrompt = FieldAsString(row, 2),
                    Cabinet = FieldAsString(row, 3),
                    VolumeLabel = FieldAsString(row, 4),
                    Source = FieldAsString(row, 5)
                };

                if (wixMediaByDiskId.TryGetValue(diskId, out var wixMediaRow))
                {
                    var compressionLevel = FieldAsString(wixMediaRow, 1);

                    symbol.CompressionLevel = String.IsNullOrEmpty(compressionLevel) ? null : (CompressionLevel?)Enum.Parse(typeof(CompressionLevel), compressionLevel, true);
                    symbol.Layout = wixMediaRow.Layout;
                }

                return symbol;
            }
            case "MIME":
                return DefaultSymbolFromRow(typeof(MIMESymbol), row, columnZeroIsId: false);
            case "ModuleIgnoreTable":
                return DefaultSymbolFromRow(typeof(ModuleIgnoreTableSymbol), row, columnZeroIsId: true);
            case "MoveFile":
                return DefaultSymbolFromRow(typeof(MoveFileSymbol), row, columnZeroIsId: true);
            case "MsiAssembly":
            {
                var componentId = FieldAsString(row, 0);
                if (componentsById.TryGetValue(componentId, out var componentRow))
                {
                    return new AssemblySymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(componentRow, 5)))
                    {
                        ComponentRef = componentId,
                        FeatureRef = FieldAsString(row, 1),
                        ManifestFileRef = FieldAsString(row, 2),
                        ApplicationFileRef = FieldAsString(row, 3),
                        Type = FieldAsNullableInt(row, 4) == 1 ? AssemblyType.Win32Assembly : AssemblyType.DotNetAssembly,
                    };
                }

                return null;
            }
            case "MsiLockPermissionsEx":
                return DefaultSymbolFromRow(typeof(MsiLockPermissionsExSymbol), row, columnZeroIsId: true);
            case "MsiShortcutProperty":
                return DefaultSymbolFromRow(typeof(MsiShortcutPropertySymbol), row, columnZeroIsId: true);
            case "ODBCDataSource":
                return DefaultSymbolFromRow(typeof(ODBCDataSourceSymbol), row, columnZeroIsId: true);
            case "ODBCDriver":
                return DefaultSymbolFromRow(typeof(ODBCDriverSymbol), row, columnZeroIsId: true);
            case "ODBCTranslator":
                return DefaultSymbolFromRow(typeof(ODBCTranslatorSymbol), row, columnZeroIsId: true);
            case "ProgId":
                return DefaultSymbolFromRow(typeof(ProgIdSymbol), row, columnZeroIsId: false);
            case "Property":
                return DefaultSymbolFromRow(typeof(PropertySymbol), row, columnZeroIsId: true);
            case "PublishComponent":
                return DefaultSymbolFromRow(typeof(PublishComponentSymbol), row, columnZeroIsId: false);
            case "Registry":
            {
                var value = FieldAsString(row, 4);
                var valueType = RegistryValueType.String;
                var valueAction = RegistryValueActionType.Write;

                if (!String.IsNullOrEmpty(value))
                {
                    if (value.StartsWith("#x", StringComparison.Ordinal))
                    {
                        valueType = RegistryValueType.Binary;
                        value = value.Substring(2);
                    }
                    else if (value.StartsWith("#%", StringComparison.Ordinal))
                    {
                        valueType = RegistryValueType.Expandable;
                        value = value.Substring(2);
                    }
                    else if (value.StartsWith("#", StringComparison.Ordinal))
                    {
                        valueType = RegistryValueType.Integer;
                        value = value.Substring(1);
                    }
                    else if (value.StartsWith("[~]", StringComparison.Ordinal) && value.EndsWith("[~]", StringComparison.Ordinal))
                    {
                        value = value.Substring(3, value.Length - 6);
                        valueType = RegistryValueType.MultiString;
                        valueAction = RegistryValueActionType.Write;
                    }
                    else if (value.StartsWith("[~]", StringComparison.Ordinal))
                    {
                        value = value.Substring(3);
                        valueType = RegistryValueType.MultiString;
                        valueAction = RegistryValueActionType.Append;
                    }
                    else if (value.EndsWith("[~]", StringComparison.Ordinal))
                    {
                        value = value.Substring(0, value.Length - 3);
                        valueType = RegistryValueType.MultiString;
                        valueAction = RegistryValueActionType.Prepend;
                    }
                }

                return new RegistrySymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    Root = (RegistryRootType)FieldAsInt(row, 1),
                    Key = FieldAsString(row, 2),
                    Name = FieldAsString(row, 3),
                    Value = value,
                    ComponentRef = FieldAsString(row, 5),
                    ValueAction = valueAction,
                    ValueType = valueType,
                };
            }
            case "RegLocator":
            {
                var type = FieldAsInt(row, 4);

                return new RegLocatorSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    Root = (RegistryRootType)FieldAsInt(row, 1),
                    Key = FieldAsString(row, 2),
                    Name = FieldAsString(row, 3),
                    Type = (RegLocatorType)(type & 0xF),
                    Win64 = (type & WindowsInstallerConstants.MsidbLocatorType64bit) == WindowsInstallerConstants.MsidbLocatorType64bit
                };
            }
            case "RemoveFile":
            {
                var splitName = FieldAsString(row, 2).Split('|');
                var installMode = FieldAsInt(row, 4);

                return new RemoveFileSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    ComponentRef = FieldAsString(row, 1),
                    FileName = splitName.Length > 1 ? splitName[1] : splitName[0],
                    ShortFileName = splitName.Length > 1 ? splitName[0] : null,
                    DirPropertyRef = FieldAsString(row, 3),
                    OnInstall = (installMode & WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall) == WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall ? (bool?)true : null,
                    OnUninstall = (installMode & WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove) == WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove ? (bool?)true : null
                };
            }
            case "RemoveRegistry":
            {
                return new RemoveRegistrySymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    Action = RemoveRegistryActionType.RemoveOnInstall,
                    Root = (RegistryRootType)FieldAsInt(row, 1),
                    Key = FieldAsString(row, 2),
                    Name = FieldAsString(row, 3),
                    ComponentRef = FieldAsString(row, 4),
                };
            }

            case "ReserveCost":
                return DefaultSymbolFromRow(typeof(ReserveCostSymbol), row, columnZeroIsId: true);
            case "SelfReg":
                return null;
            case "ServiceControl":
            {
                var events = FieldAsInt(row, 2);
                var wait = FieldAsNullableInt(row, 4);
                return new ServiceControlSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    Name = FieldAsString(row, 1),
                    Arguments = FieldAsString(row, 3),
                    Wait = !wait.HasValue || wait.Value == 1,
                    ComponentRef = FieldAsString(row, 5),
                    InstallRemove = (events & WindowsInstallerConstants.MsidbServiceControlEventDelete) == WindowsInstallerConstants.MsidbServiceControlEventDelete,
                    UninstallRemove = (events & WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete) == WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete,
                    InstallStart = (events & WindowsInstallerConstants.MsidbServiceControlEventStart) == WindowsInstallerConstants.MsidbServiceControlEventStart,
                    UninstallStart = (events & WindowsInstallerConstants.MsidbServiceControlEventUninstallStart) == WindowsInstallerConstants.MsidbServiceControlEventUninstallStart,
                    InstallStop = (events & WindowsInstallerConstants.MsidbServiceControlEventStop) == WindowsInstallerConstants.MsidbServiceControlEventStop,
                    UninstallStop = (events & WindowsInstallerConstants.MsidbServiceControlEventUninstallStop) == WindowsInstallerConstants.MsidbServiceControlEventUninstallStop,
                };
            }

            case "ServiceInstall":
                return DefaultSymbolFromRow(typeof(ServiceInstallSymbol), row, columnZeroIsId: true);
            case "Shortcut":
            {
                var splitName = FieldAsString(row, 2).Split('|');

                return new ShortcutSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    DirectoryRef = FieldAsString(row, 1),
                    Name = splitName.Length > 1 ? splitName[1] : splitName[0],
                    ShortName =  splitName.Length > 1 ? splitName[0] : null,
                    ComponentRef = FieldAsString(row, 3),
                    Target = FieldAsString(row, 4),
                    Arguments = FieldAsString(row, 5),
                    Description = FieldAsString(row, 6),
                    Hotkey = FieldAsNullableInt(row, 7),
                    IconRef = FieldAsString(row, 8),
                    IconIndex = FieldAsNullableInt(row, 9),
                    Show = (ShortcutShowType?)FieldAsNullableInt(row, 10),
                    WorkingDirectory = FieldAsString(row, 11),
                    DisplayResourceDll = FieldAsString(row, 12),
                    DisplayResourceId = FieldAsNullableInt(row, 13),
                    DescriptionResourceDll = FieldAsString(row, 14),
                    DescriptionResourceId= FieldAsNullableInt(row, 15),
                };
            }
            case "Signature":
                return DefaultSymbolFromRow(typeof(SignatureSymbol), row, columnZeroIsId: true);
            case "UIText":
                return DefaultSymbolFromRow(typeof(UITextSymbol), row, columnZeroIsId: true);
            case "Upgrade":
            {
                var attributes = FieldAsInt(row, 4);
                return new UpgradeSymbol(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Global, FieldAsString(row, 0)))
                {
                    UpgradeCode = FieldAsString(row, 0),
                    VersionMin = FieldAsString(row, 1),
                    VersionMax = FieldAsString(row, 2),
                    Language = FieldAsString(row, 3),
                    Remove = FieldAsString(row, 5),
                    ActionProperty = FieldAsString(row, 6),
                    MigrateFeatures = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures) == WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures,
                    OnlyDetect = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect) == WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect,
                    IgnoreRemoveFailures = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure) == WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure,
                    VersionMinInclusive = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive,
                    VersionMaxInclusive = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive,
                    ExcludeLanguages = (attributes & WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive,
                };
            }
            case "Verb":
                return DefaultSymbolFromRow(typeof(VerbSymbol), row, columnZeroIsId: false);
            case "WixAction":
            {
                var sequenceTable = FieldAsString(row, 0);
                return new WixActionSymbol(SourceLineNumber4(row.SourceLineNumbers))
                {
                    SequenceTable = (SequenceTable)Enum.Parse(typeof(SequenceTable), sequenceTable == "AdvtExecuteSequence" ? nameof(SequenceTable.AdvertiseExecuteSequence) : sequenceTable),
                    Action = FieldAsString(row, 1),
                    Condition = FieldAsString(row, 2),
                    Sequence = FieldAsNullableInt(row, 3),
                    Before = FieldAsString(row, 4),
                    After = FieldAsString(row, 5),
                    Overridable = FieldAsNullableInt(row, 6) != 0,
                };
            }
            case "WixBootstrapperApplication":
                return DefaultSymbolFromRow(typeof(WixBootstrapperApplicationSymbol), row, columnZeroIsId: true);
            case "WixBundleContainer":
                return DefaultSymbolFromRow(typeof(WixBundleContainerSymbol), row, columnZeroIsId: true);
            case "WixBundleVariable":
                return DefaultSymbolFromRow(typeof(WixBundleVariableSymbol), row, columnZeroIsId: true);
            case "WixChainItem":
                return DefaultSymbolFromRow(typeof(WixChainItemSymbol), row, columnZeroIsId: true);
            case "WixCustomTable":
                return DefaultSymbolFromRow(typeof(WixCustomTableSymbol), row, columnZeroIsId: true);
            case "WixDirectory":
                return null;
            case "WixFile":
                return null;
            case "WixInstanceTransforms":
                return DefaultSymbolFromRow(typeof(WixInstanceTransformsSymbol), row, columnZeroIsId: true);
            case "WixMedia":
                return null;
            case "WixMerge":
                return DefaultSymbolFromRow(typeof(WixMergeSymbol), row, columnZeroIsId: true);
            case "WixPatchBaseline":
                return DefaultSymbolFromRow(typeof(WixPatchBaselineSymbol), row, columnZeroIsId: true);
            case "WixProperty":
            {
                var attributes = FieldAsInt(row, 1);
                return new WixPropertySymbol(SourceLineNumber4(row.SourceLineNumbers))
                {
                    PropertyRef = FieldAsString(row, 0),
                    Admin = (attributes & 0x1) == 0x1,
                    Hidden = (attributes & 0x2) == 0x2,
                    Secure = (attributes & 0x4) == 0x4,
                };
            }
            case "WixSuppressModularization":
            {
                return new WixSuppressModularizationSymbol(SourceLineNumber4(row.SourceLineNumbers))
                {
                    SuppressIdentifier = FieldAsString(row, 0)
                };
            }
            case "WixUI":
                return DefaultSymbolFromRow(typeof(WixUISymbol), row, columnZeroIsId: true);
            case "WixVariable":
                return DefaultSymbolFromRow(typeof(WixVariableSymbol), row, columnZeroIsId: true);
            default:
                return GenericSymbolFromCustomRow(row, columnZeroIsId: false);
            }
        }

        private static CustomActionTargetType DetermineCustomActionTargetType(int type)
        {
            var targetType = default(CustomActionTargetType);

            if ((type & WindowsInstallerConstants.MsidbCustomActionTypeVBScript) == WindowsInstallerConstants.MsidbCustomActionTypeVBScript)
            {
                targetType = CustomActionTargetType.VBScript;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeJScript) == WindowsInstallerConstants.MsidbCustomActionTypeJScript)
            {
                targetType = CustomActionTargetType.JScript;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeTextData) == WindowsInstallerConstants.MsidbCustomActionTypeTextData)
            {
                targetType = CustomActionTargetType.TextData;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeExe) == WindowsInstallerConstants.MsidbCustomActionTypeExe)
            {
                targetType = CustomActionTargetType.Exe;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeDll) == WindowsInstallerConstants.MsidbCustomActionTypeDll)
            {
                targetType = CustomActionTargetType.Dll;
            }

            return targetType;
        }

        private static CustomActionSourceType DetermineCustomActionSourceType(int type)
        {
            var sourceType = CustomActionSourceType.Binary;

            if ((type & WindowsInstallerConstants.MsidbCustomActionTypeProperty) == WindowsInstallerConstants.MsidbCustomActionTypeProperty)
            {
                sourceType = CustomActionSourceType.Property;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeDirectory) == WindowsInstallerConstants.MsidbCustomActionTypeDirectory)
            {
                sourceType = CustomActionSourceType.Directory;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeSourceFile) == WindowsInstallerConstants.MsidbCustomActionTypeSourceFile)
            {
                sourceType = CustomActionSourceType.File;
            }

            return sourceType;
        }

        private static CustomActionExecutionType DetermineCustomActionExecutionType(int type)
        {
            var executionType = CustomActionExecutionType.Immediate;

            if ((type & (WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit)) == (WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit))
            {
                executionType = CustomActionExecutionType.Commit;
            }
            else if ((type & (WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback)) == (WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback))
            {
                executionType = CustomActionExecutionType.Rollback;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeInScript) == WindowsInstallerConstants.MsidbCustomActionTypeInScript)
            {
                executionType = CustomActionExecutionType.Deferred;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat) == WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat)
            {
                executionType = CustomActionExecutionType.ClientRepeat;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess) == WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess)
            {
                executionType = CustomActionExecutionType.OncePerProcess;
            }
            else if ((type & WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence) == WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence)
            {
                executionType = CustomActionExecutionType.FirstSequence;
            }

            return executionType;
        }

        private static IntermediateFieldType ColumnType3ToIntermediateFieldType4(Wix3.ColumnType columnType)
        {
            switch (columnType)
            {
            case Wix3.ColumnType.Number:
                return IntermediateFieldType.Number;
            case Wix3.ColumnType.Object:
                return IntermediateFieldType.Path;
            case Wix3.ColumnType.Unknown:
            case Wix3.ColumnType.String:
            case Wix3.ColumnType.Localized:
            case Wix3.ColumnType.Preserved:
            default:
                return IntermediateFieldType.String;
            }
        }

        private static IntermediateSymbol DefaultSymbolFromRow(Type symbolType, Wix3.Row row, bool columnZeroIsId)
        {
            var id = columnZeroIsId ? GetIdentifierForRow(row) : null;

            var createSymbol = symbolType.GetConstructor(new[] { typeof(SourceLineNumber), typeof(Identifier) });
            var symbol = (IntermediateSymbol)createSymbol.Invoke(new object[] { SourceLineNumber4(row.SourceLineNumbers), id });

            SetSymbolFieldsFromRow(row, symbol, columnZeroIsId);

            return symbol;
        }

        private static IntermediateSymbol GenericSymbolFromCustomRow(Wix3.Row row, bool columnZeroIsId)
        {
            var columnDefinitions = row.Table.Definition.Columns.Cast<Wix3.ColumnDefinition>();
            var fieldDefinitions = columnDefinitions.Select(columnDefinition =>
                new IntermediateFieldDefinition(columnDefinition.Name, ColumnType3ToIntermediateFieldType4(columnDefinition.Type))).ToArray();
            var symbolDefinition = new IntermediateSymbolDefinition(row.Table.Name, fieldDefinitions, null);

            var id = columnZeroIsId ? GetIdentifierForRow(row) : null;

            var createSymbol = typeof(IntermediateSymbol).GetConstructor(new[] { typeof(IntermediateSymbolDefinition), typeof(SourceLineNumber), typeof(Identifier) });
            var symbol = (IntermediateSymbol)createSymbol.Invoke(new object[] { symbolDefinition, SourceLineNumber4(row.SourceLineNumbers), id });

            SetSymbolFieldsFromRow(row, symbol, columnZeroIsId);

            return symbol;
        }

        private static void SetSymbolFieldsFromRow(Wix3.Row row, IntermediateSymbol symbol, bool columnZeroIsId)
        {
            var offset = 0;
            if (columnZeroIsId)
            {
                offset = 1;
            }

            for (var i = offset; i < row.Fields.Length; ++i)
            {
                var column = row.Fields[i].Column;
                switch (column.Type)
                {
                case Wix3.ColumnType.String:
                case Wix3.ColumnType.Localized:
                case Wix3.ColumnType.Object:
                case Wix3.ColumnType.Preserved:
                    symbol.Set(i - offset, FieldAsString(row, i));
                    break;
                case Wix3.ColumnType.Number:
                    int? nullableValue = FieldAsNullableInt(row, i);
                    // TODO: Consider whether null values should be coerced to their default value when
                    // a column is not nullable. For now, just pass through the null.
                    //int value = FieldAsInt(row, i);
                    //symbol.Set(i - offset, column.IsNullable ? nullableValue : value);
                    symbol.Set(i - offset, nullableValue);
                    break;
                case Wix3.ColumnType.Unknown:
                    break;
                }
            }
        }

        private static Identifier GetIdentifierForRow(Wix3.Row row)
        {
            var column = row.Fields[0].Column;
            switch (column.Type)
            {
            case Wix3.ColumnType.String:
            case Wix3.ColumnType.Localized:
            case Wix3.ColumnType.Object:
            case Wix3.ColumnType.Preserved:
                return new Identifier(AccessModifier.Global, (string)row.Fields[0].Data);
            case Wix3.ColumnType.Number:
                return new Identifier(AccessModifier.Global, FieldAsInt(row, 0));
            default:
                return null;
            }
        }

        private static SectionType OutputType3ToSectionType4(Wix3.OutputType outputType)
        {
            switch (outputType)
            {
            case Wix3.OutputType.Bundle:
                return SectionType.Bundle;
            case Wix3.OutputType.Module:
                return SectionType.Module;
            case Wix3.OutputType.Patch:
                return SectionType.Patch;
            case Wix3.OutputType.PatchCreation:
                return SectionType.PatchCreation;
            case Wix3.OutputType.Product:
                return SectionType.Product;
            case Wix3.OutputType.Transform:
            case Wix3.OutputType.Unknown:
            default:
                return SectionType.Unknown;
            }
        }

        private static SourceLineNumber SourceLineNumber4(Wix3.SourceLineNumberCollection source)
        {
            return String.IsNullOrEmpty(source?.EncodedSourceLineNumbers) ? null : SourceLineNumber.CreateFromEncoded(source.EncodedSourceLineNumbers);
        }

        private static string FieldAsString(Wix3.Row row, int column)
        {
            return (string)row[column];
        }

        private static int FieldAsInt(Wix3.Row row, int column)
        {
            return Convert.ToInt32(row[column]);
        }

        private static int? FieldAsNullableInt(Wix3.Row row, int column)
        {
            var field = row.Fields[column];
            if (field.Data == null)
            {
                return null;
            }
            else
            {
                return Convert.ToInt32(field.Data);
            }
        }

        private static string[] SplitDefaultDir(string defaultDir)
        {
            var split1 = defaultDir.Split(':');
            var targetSplit = split1.Length > 1 ? split1[1].Split('|') : split1[0].Split('|');
            var sourceSplit = split1.Length > 1 ? split1[0].Split('|') : new[] { String.Empty };
            return new[]
            {
                targetSplit.Length > 1 ? targetSplit[1] : targetSplit[0],
                targetSplit.Length > 1 ? targetSplit[0] : null,
                sourceSplit.Length > 1 ? sourceSplit[1] : sourceSplit[0],
                sourceSplit.Length > 1 ? sourceSplit[0] : null
            };
        }
    }
}
