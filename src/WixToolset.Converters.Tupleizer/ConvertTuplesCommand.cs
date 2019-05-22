// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters.Tupleizer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using Wix3 = Microsoft.Tools.WindowsInstallerXml;

    public class ConvertTuplesCommand
    {
        public Intermediate Execute(string path)
        {
            var output = Wix3.Output.Load(path, suppressVersionCheck: true, suppressSchema: true);
            return this.Execute(output);
        }

        public Intermediate Execute(Wix3.Output output)
        {
            var section = new IntermediateSection(String.Empty, OutputType3ToSectionType4(output.Type), output.Codepage);

            var wixMediaByDiskId = IndexWixMediaTableByDiskId(output);
            var bindPathsById = IndexById<Wix3.Row>(output, "BindPath");
            var fontsById = IndexById<Wix3.Row>(output, "Font");
            var selfRegById = IndexById<Wix3.Row>(output, "SelfReg");
            var wixDirectoryById = IndexById<Wix3.Row>(output, "WixDirectory");

            foreach (Wix3.Table table in output.Tables)
            {
                foreach (Wix3.Row row in table.Rows)
                {
                    var tuple = GenerateTupleFromRow(row, wixMediaByDiskId, fontsById, bindPathsById, selfRegById, wixDirectoryById);
                    if (tuple != null)
                    {
                        section.Tuples.Add(tuple);
                    }
                }
            }

            return new Intermediate(String.Empty, new[] { section }, localizationsByCulture: null, embedFilePaths: null);
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

        private static IntermediateTuple GenerateTupleFromRow(Wix3.Row row, Dictionary<int, Wix3.WixMediaRow> wixMediaByDiskId, Dictionary<string, Wix3.Row> fontsById, Dictionary<string, Wix3.Row> bindPathsById, Dictionary<string, Wix3.Row> selfRegById, Dictionary<string, Wix3.Row> wixDirectoryById)
        {
            var name = row.Table.Name;
            switch (name)
            {
            case "_SummaryInformation":
                return DefaultTupleFromRow(typeof(SummaryInformationTuple), row, columnZeroIsId: false);
            case "ActionText":
                return DefaultTupleFromRow(typeof(ActionTextTuple), row, columnZeroIsId: false);
            case "AdvtExecuteSequence":
                return DefaultTupleFromRow(typeof(AdvtExecuteSequenceTuple), row, columnZeroIsId: false);
            case "AppId":
                return DefaultTupleFromRow(typeof(AppIdTuple), row, columnZeroIsId: false);
            case "AppSearch":
                return DefaultTupleFromRow(typeof(AppSearchTuple), row, columnZeroIsId: false);
            case "Billboard":
                return DefaultTupleFromRow(typeof(BillboardTuple), row, columnZeroIsId: true);
            case "Binary":
                return DefaultTupleFromRow(typeof(BinaryTuple), row, columnZeroIsId: true);
            case "BindPath":
                return null;
            case "CCPSearch":
                return DefaultTupleFromRow(typeof(CCPSearchTuple), row, columnZeroIsId: true);
            case "Class":
                return DefaultTupleFromRow(typeof(ClassTuple), row, columnZeroIsId: false);
            case "CompLocator":
                return DefaultTupleFromRow(typeof(CompLocatorTuple), row, columnZeroIsId: true);
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

                var keyPathType = ComponentKeyPathType.File;
                if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath) == WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath)
                {
                    keyPathType = ComponentKeyPathType.Registry;
                }
                else if ((attributes & WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource) == WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource)
                {
                    keyPathType = ComponentKeyPathType.OdbcDataSource;
                }

                return new ComponentTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
                {
                    ComponentId = FieldAsString(row, 1),
                    DirectoryRef = FieldAsString(row, 2),
                    Condition = FieldAsString(row, 4),
                    KeyPath = FieldAsString(row, 5),
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
                return DefaultTupleFromRow(typeof(ConditionTuple), row, columnZeroIsId: false);
            case "CreateFolder":
                return DefaultTupleFromRow(typeof(CreateFolderTuple), row, columnZeroIsId: false);
            case "CustomAction":
            {
                var caType = FieldAsInt(row, 1);
                var executionType = DetermineCustomActionExecutionType(caType);
                var sourceType = DetermineCustomActionSourceType(caType);
                var targetType = DetermineCustomActionTargetType(caType);

                return new CustomActionTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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

                var tuple = new DirectoryTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, id))
                {
                    ParentDirectoryRef = FieldAsString(row, 1),
                    Name = splits[0],
                    ShortName = splits[1],
                    SourceName = splits[2],
                    SourceShortName = splits[3]
                };

                if (wixDirectoryById.TryGetValue(id, out var wixDirectoryRow))
                {
                    tuple.ComponentGuidGenerationSeed = FieldAsString(wixDirectoryRow, 1);
                }

                return tuple;
            }
            case "DrLocator":
                return DefaultTupleFromRow(typeof(DrLocatorTuple), row, columnZeroIsId: false);
            case "DuplicateFile":
                return DefaultTupleFromRow(typeof(DuplicateFileTuple), row, columnZeroIsId: true);
            case "Error":
                return DefaultTupleFromRow(typeof(ErrorTuple), row, columnZeroIsId: false);
            case "Extension":
                return DefaultTupleFromRow(typeof(ExtensionTuple), row, columnZeroIsId: false);
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

                return new FeatureTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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
                return DefaultTupleFromRow(typeof(FeatureComponentsTuple), row, columnZeroIsId: false);
            case "File":
            {
                var attributes = FieldAsNullableInt(row, 6);
                var readOnly = (attributes & WindowsInstallerConstants.MsidbFileAttributesReadOnly) == WindowsInstallerConstants.MsidbFileAttributesReadOnly;
                var hidden = (attributes & WindowsInstallerConstants.MsidbFileAttributesHidden) == WindowsInstallerConstants.MsidbFileAttributesHidden;
                var system = (attributes & WindowsInstallerConstants.MsidbFileAttributesSystem) == WindowsInstallerConstants.MsidbFileAttributesSystem;
                var vital = (attributes & WindowsInstallerConstants.MsidbFileAttributesVital) == WindowsInstallerConstants.MsidbFileAttributesVital;
                var checksum = (attributes & WindowsInstallerConstants.MsidbFileAttributesChecksum) == WindowsInstallerConstants.MsidbFileAttributesChecksum;
                bool? compressed = null;
                if ((attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) == WindowsInstallerConstants.MsidbFileAttributesNoncompressed)
                {
                    compressed = false;
                }
                else if ((attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed) == WindowsInstallerConstants.MsidbFileAttributesCompressed)
                {
                    compressed = true;
                }

                var id = FieldAsString(row, 0);

                var tuple = new FileTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, id))
                {
                    ComponentRef = FieldAsString(row, 1),
                    Name = FieldAsString(row, 2),
                    FileSize = FieldAsInt(row, 3),
                    Version = FieldAsString(row, 4),
                    Language = FieldAsString(row, 5),
                    ReadOnly = readOnly,
                    Hidden = hidden,
                    System = system,
                    Vital = vital,
                    Checksum = checksum,
                    Compressed = compressed,
                };

                if (bindPathsById.TryGetValue(id, out var bindPathRow))
                {
                    tuple.BindPath = FieldAsString(bindPathRow, 1) ?? String.Empty;
                }

                if (fontsById.TryGetValue(id, out var fontRow))
                {
                    tuple.FontTitle = FieldAsString(fontRow, 1) ?? String.Empty;
                }

                if (selfRegById.TryGetValue(id, out var selfRegRow))
                {
                    tuple.SelfRegCost = FieldAsNullableInt(selfRegRow, 1) ?? 0;
                }

                return tuple;
            }
            case "Font":
                return null;
            case "Icon":
                return DefaultTupleFromRow(typeof(IconTuple), row, columnZeroIsId: true);
            case "InstallExecuteSequence":
                return DefaultTupleFromRow(typeof(InstallExecuteSequenceTuple), row, columnZeroIsId: false);
            case "LockPermissions":
                return DefaultTupleFromRow(typeof(LockPermissionsTuple), row, columnZeroIsId: false);
            case "Media":
            {
                var diskId = FieldAsInt(row, 0);
                var tuple = new MediaTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, diskId))
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

                    tuple.CompressionLevel = String.IsNullOrEmpty(compressionLevel) ? null : (CompressionLevel?)Enum.Parse(typeof(CompressionLevel), compressionLevel, true);
                    tuple.Layout = wixMediaRow.Layout;
                }

                return tuple;
            }
            case "MIME":
                return DefaultTupleFromRow(typeof(MIMETuple), row, columnZeroIsId: false);
            case "ModuleIgnoreTable":
                return DefaultTupleFromRow(typeof(ModuleIgnoreTableTuple), row, columnZeroIsId: true);
            case "MoveFile":
                return DefaultTupleFromRow(typeof(MoveFileTuple), row, columnZeroIsId: true);
            case "MsiAssembly":
                return DefaultTupleFromRow(typeof(MsiAssemblyTuple), row, columnZeroIsId: false);
            case "MsiLockPermissionsEx":
                return DefaultTupleFromRow(typeof(MsiLockPermissionsExTuple), row, columnZeroIsId: true);
            case "MsiShortcutProperty":
                return DefaultTupleFromRow(typeof(MsiShortcutPropertyTuple), row, columnZeroIsId: true);
            case "ODBCDataSource":
                return DefaultTupleFromRow(typeof(ODBCDataSourceTuple), row, columnZeroIsId: true);
            case "ODBCDriver":
                return DefaultTupleFromRow(typeof(ODBCDriverTuple), row, columnZeroIsId: true);
            case "ODBCTranslator":
                return DefaultTupleFromRow(typeof(ODBCTranslatorTuple), row, columnZeroIsId: true);
            case "ProgId":
                return DefaultTupleFromRow(typeof(ProgIdTuple), row, columnZeroIsId: false);
            case "Property":
                return DefaultTupleFromRow(typeof(PropertyTuple), row, columnZeroIsId: true);
            case "PublishComponent":
                return DefaultTupleFromRow(typeof(PublishComponentTuple), row, columnZeroIsId: false);
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

                return new RegistryTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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

                return new RegLocatorTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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
                var installMode = FieldAsInt(row, 4);
                return new RemoveFileTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
                {
                    ComponentRef = FieldAsString(row, 1),
                    FileName = FieldAsString(row, 2),
                    DirProperty = FieldAsString(row, 3),
                    OnInstall = (installMode & WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall) == WindowsInstallerConstants.MsidbRemoveFileInstallModeOnInstall ? (bool?)true : null,
                    OnUninstall = (installMode & WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove) == WindowsInstallerConstants.MsidbRemoveFileInstallModeOnRemove ? (bool?)true : null
                };
            }
            case "RemoveRegistry":
            {
                return new RemoveRegistryTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
                {
                    Action = RemoveRegistryActionType.RemoveOnInstall,
                    Root = (RegistryRootType)FieldAsInt(row, 1),
                    Key = FieldAsString(row, 2),
                    Name = FieldAsString(row, 3),
                    ComponentRef = FieldAsString(row, 4),
                };
            }

            case "ReserveCost":
                return DefaultTupleFromRow(typeof(ReserveCostTuple), row, columnZeroIsId: true);
            case "SelfReg":
                return null;
            case "ServiceControl":
            {
                var events = FieldAsInt(row, 2);
                var wait = FieldAsNullableInt(row, 4);
                return new ServiceControlTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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
                return DefaultTupleFromRow(typeof(ServiceInstallTuple), row, columnZeroIsId: true);
            case "Shortcut":
            {
                var splitName = FieldAsString(row, 2).Split('|');

                return new ShortcutTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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
                return DefaultTupleFromRow(typeof(SignatureTuple), row, columnZeroIsId: false);
            case "UIText":
                return DefaultTupleFromRow(typeof(UITextTuple), row, columnZeroIsId: true);
            case "Upgrade":
            {
                var attributes = FieldAsInt(row, 4);
                return new UpgradeTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
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
                return DefaultTupleFromRow(typeof(VerbTuple), row, columnZeroIsId: false);
            case "WixAction":
                var sequenceTable = FieldAsString(row, 0);
                return new WixActionTuple(SourceLineNumber4(row.SourceLineNumbers))
                {
                    SequenceTable = (SequenceTable)Enum.Parse(typeof(SequenceTable), sequenceTable == "AdvtExecuteSequence" ? nameof(SequenceTable.AdvertiseExecuteSequence) : sequenceTable),
                    Action = FieldAsString(row, 1),
                    Condition = FieldAsString(row, 2),
                    Sequence = FieldAsNullableInt(row, 3),
                    Before = FieldAsString(row, 4),
                    After = FieldAsString(row, 5),
                    Overridable = FieldAsNullableInt(row, 6) != 0,
                };
            case "WixBootstrapperApplication":
                return DefaultTupleFromRow(typeof(WixBootstrapperApplicationTuple), row, columnZeroIsId: true);
            case "WixBundleContainer":
                return DefaultTupleFromRow(typeof(WixBundleContainerTuple), row, columnZeroIsId: true);
            case "WixBundleVariable":
                return DefaultTupleFromRow(typeof(WixBundleVariableTuple), row, columnZeroIsId: true);
            case "WixChainItem":
                return DefaultTupleFromRow(typeof(WixChainItemTuple), row, columnZeroIsId: true);
            case "WixCustomTable":
                return DefaultTupleFromRow(typeof(WixCustomTableTuple), row, columnZeroIsId: true);
            case "WixDeltaPatchFile":
                return DefaultTupleFromRow(typeof(WixDeltaPatchFileTuple), row, columnZeroIsId: true);
            case "WixDirectory":
                return null;
            case "WixFile":
                var assemblyAttributes3 = FieldAsNullableInt(row, 1);
                return new WixFileTuple(SourceLineNumber4(row.SourceLineNumbers), new Identifier(AccessModifier.Public, FieldAsString(row, 0)))
                {
                    AssemblyType = assemblyAttributes3 == 0 ? FileAssemblyType.DotNetAssembly : assemblyAttributes3 == 1 ? FileAssemblyType.Win32Assembly : FileAssemblyType.NotAnAssembly,
                    AssemblyManifestFileRef = FieldAsString(row, 2),
                    AssemblyApplicationFileRef = FieldAsString(row, 3),
                    DirectoryRef = FieldAsString(row, 4),
                    DiskId = FieldAsNullableInt(row, 5) ?? 0,
                    Source = new IntermediateFieldPathValue() { Path = FieldAsString(row, 6) },
                    ProcessorArchitecture = FieldAsString(row, 7),
                    PatchGroup = FieldAsInt(row, 8),
                    Attributes = FieldAsInt(row, 9),
                    PatchAttributes = (PatchAttributeType)FieldAsInt(row, 10),
                };
            case "WixInstanceTransforms":
                return DefaultTupleFromRow(typeof(WixInstanceTransformsTuple), row, columnZeroIsId: true);
            case "WixMedia":
                return null;
            case "WixMerge":
                return DefaultTupleFromRow(typeof(WixMergeTuple), row, columnZeroIsId: true);
            case "WixPatchBaseline":
                return DefaultTupleFromRow(typeof(WixPatchBaselineTuple), row, columnZeroIsId: true);
            case "WixProperty":
            {
                var attributes = FieldAsInt(row, 1);
                return new WixPropertyTuple(SourceLineNumber4(row.SourceLineNumbers))
                {
                    PropertyRef = FieldAsString(row, 0),
                    Admin = (attributes & 0x1) == 0x1,
                    Hidden = (attributes & 0x2) == 0x2,
                    Secure = (attributes & 0x4) == 0x4,
                };
            }
            case "WixSuppressModularization":
                return DefaultTupleFromRow(typeof(WixSuppressModularizationTuple), row, columnZeroIsId: true);
            case "WixUI":
                return DefaultTupleFromRow(typeof(WixUITuple), row, columnZeroIsId: true);
            case "WixVariable":
                return DefaultTupleFromRow(typeof(WixVariableTuple), row, columnZeroIsId: true);
            default:
                return GenericTupleFromCustomRow(row, columnZeroIsId: false);
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

        private static IntermediateTuple DefaultTupleFromRow(Type tupleType, Wix3.Row row, bool columnZeroIsId)
        {
            var tuple = Activator.CreateInstance(tupleType) as IntermediateTuple;

            SetTupleFieldsFromRow(row, tuple, columnZeroIsId);

            tuple.SourceLineNumbers = SourceLineNumber4(row.SourceLineNumbers);
            return tuple;
        }

        private static IntermediateTuple GenericTupleFromCustomRow(Wix3.Row row, bool columnZeroIsId)
        {
            var columnDefinitions = row.Table.Definition.Columns.Cast<Wix3.ColumnDefinition>();
            var fieldDefinitions = columnDefinitions.Select(columnDefinition =>
                new IntermediateFieldDefinition(columnDefinition.Name, ColumnType3ToIntermediateFieldType4(columnDefinition.Type))).ToArray();
            var tupleDefinition = new IntermediateTupleDefinition(row.Table.Name, fieldDefinitions, null);
            var tuple = new IntermediateTuple(tupleDefinition, SourceLineNumber4(row.SourceLineNumbers));

            SetTupleFieldsFromRow(row, tuple, columnZeroIsId);

            return tuple;
        }

        private static void SetTupleFieldsFromRow(Wix3.Row row, IntermediateTuple tuple, bool columnZeroIsId)
        {
            int offset = 0;
            if (columnZeroIsId)
            {
                tuple.Id = GetIdentifierForRow(row);
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
                    tuple.Set(i - offset, FieldAsString(row, i));
                    break;
                case Wix3.ColumnType.Number:
                    int? nullableValue = FieldAsNullableInt(row, i);
                    // TODO: Consider whether null values should be coerced to their default value when
                    // a column is not nullable. For now, just pass through the null.
                    //int value = FieldAsInt(row, i);
                    //tuple.Set(i - offset, column.IsNullable ? nullableValue : value);
                    tuple.Set(i - offset, nullableValue);
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
                return new Identifier(AccessModifier.Public, (string)row.Fields[0].Data);
            case Wix3.ColumnType.Number:
                return new Identifier(AccessModifier.Public, FieldAsInt(row, 0));
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
