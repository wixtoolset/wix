// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters.Symbolizer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Wix3 = Microsoft.Tools.WindowsInstallerXml;
    using WixToolset.Converters.Symbolizer;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class ConvertSymbolsFixture
    {
        [Fact]
        public void CanLoadWixoutAndConvertToIntermediate()
        {
            var rootFolder = TestData.Get();
            var dataFolder = TestData.Get(@"TestData\Integration");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var path = Path.Combine(dataFolder, "test.wixout");

                var intermediate = ConvertSymbols.ConvertFile(path);

                Assert.NotNull(intermediate);
                Assert.Single(intermediate.Sections);
                Assert.Equal(String.Empty, intermediate.Id);

                // Save and load to guarantee round-tripping support.
                //
                var wixiplFile = Path.Combine(intermediateFolder, "test.wixipl");
                intermediate.Save(wixiplFile);

                intermediate = Intermediate.Load(wixiplFile);

                var output = Wix3.Output.Load(path, suppressVersionCheck: true, suppressSchema: true);
                var wixMediaByDiskId = IndexWixMediaTableByDiskId(output);

                // Dump to text for easy diffing, with some massaging to keep v3 and v4 diffable.
                //
                var tables = output.Tables.Cast<Wix3.Table>();
                var wix3Dump = tables
                    .SelectMany(table => table.Rows.Cast<Wix3.Row>()
                    .SelectMany(row => RowToStrings(row, wixMediaByDiskId)))
                    .Where(s => !String.IsNullOrEmpty(s))
                    .OrderBy(s => s)
                    .ToArray();

                var symbols = intermediate.Sections.SelectMany(s => s.Symbols);

                var assemblySymbolsByFileId = symbols.OfType<AssemblySymbol>().ToDictionary(a => a.Id.Id);

                var wix4Dump = symbols
                    .SelectMany(symbol => SymbolToStrings(symbol, assemblySymbolsByFileId))
                    .OrderBy(s => s)
                    .ToArray();

#if false
                Assert.Equal(wix3Dump, wix4Dump);
#else // useful when you want to diff the outputs with another diff tool.
                var wix3TextDump = String.Join(Environment.NewLine, wix3Dump);
                var wix4TextDump = String.Join(Environment.NewLine, wix4Dump);

                var path3 = Path.Combine(Path.GetTempPath(), "~3.txt");
                var path4 = Path.Combine(Path.GetTempPath(), "~4.txt");

                File.WriteAllText(path3, wix3TextDump);
                File.WriteAllText(path4, wix4TextDump);

                Assert.Equal(wix3TextDump, wix4TextDump);
#endif
            }
        }

        private static Dictionary<int, Wix3.WixMediaRow> IndexWixMediaTableByDiskId(Wix3.Output output)
        {
            var wixMediaByDiskId = new Dictionary<int, Wix3.WixMediaRow>();
            var wixMediaTable = output.Tables["WixMedia"];

            if (wixMediaTable != null)
            {
                foreach (Wix3.WixMediaRow row in wixMediaTable.Rows)
                {
                    wixMediaByDiskId.Add((int)row[0], row);
                }
            }

            return wixMediaByDiskId;
        }

        private static IEnumerable<string> RowToStrings(Wix3.Row row, Dictionary<int, Wix3.WixMediaRow> wixMediaByDiskId)
        {
            string fields = null;

            // Massage output to match WiX v3 rows and v4 symbols.
            //
            switch (row.Table.Name)
            {
            case "Directory":
                var dirs = SplitDefaultDir((string)row[2]);
                fields = String.Join(",", row[0], row[1], dirs[0], dirs[1], dirs[2], dirs[3]);
                break;
            case "File":
            {
                var fieldValues = row.Fields.Take(7).Select(SafeConvertField).ToArray();
                if (fieldValues[3] == null)
                {
                    // "Somebody" sometimes writes out a null field even when the column definition says
                    // it's non-nullable. Not naming names or anything. (SWID tags.)
                    fieldValues[3] = "0";
                }
                fields = String.Join(",", fieldValues);
                break;
            }
            case "Media":
                var compression = wixMediaByDiskId.TryGetValue((int)row[0], out var wixMedia) ? (CompressionLevel?)Enum.Parse(typeof(CompressionLevel), SafeConvertField(wixMedia.Fields[1]), true) : null;

                fields = String.Join(",", row.Fields.Select(SafeConvertField));
                fields = String.Join(",", fields, (int?)compression, SafeConvertField(wixMedia?.Fields[2]));
                break;
            case "RegLocator":
                var type = (int)row[4];
                fields = String.Join(",", row[0], row[1], row[2], row[3], type & 0xF, (type & 0x10) == 0x10);
                break;
            case "RemoveFile":
                var attributes = (int)row[4];
                var onInstall = (attributes & 1) == 1 ? (bool?)true : null;
                var onUninstall = (attributes & 2) == 2 ? (bool?)true : null;
                fields = String.Join(",", row.Fields.Take(4).Select(SafeConvertField));
                fields = String.Join(",", fields, onInstall, onUninstall);
                break;
            case "Shortcut":
                var split = ((string)row[2]).Split('|');
                var afterName = String.Join(",", row.Fields.Skip(3).Select(SafeConvertField));
                fields = String.Join(",", row[0], row[1], split.Length > 1 ? split[1] : split[0], split.Length > 1 ? split[0] : String.Empty, afterName);
                break;
            case "WixAction":
                var table = (int)SequenceStringToSequenceTable(row[0]);
                fields = String.Join(",", table, row[1], row[2], row[3], row[4], row[5], row[6]);
                break;
            case "WixFile":
            {
                var fieldValues = row.Fields.Select(SafeConvertField).ToArray();
                if (fieldValues[8] == null)
                {
                    // "Somebody" sometimes writes out a null field even when the column definition says
                    // it's non-nullable. Not naming names or anything. (SWID tags.)
                    fieldValues[8] = "0";
                }
                if (fieldValues[10] == null)
                {
                    // WixFile rows that come from merge modules will not have the attributes column set
                    // so initilaize with 0.
                    fieldValues[10] = "0";
                }
                fields = String.Join(",", fieldValues);
                break;
            }
            case "WixMedia":
                break;
            default:
                fields = String.Join(",", row.Fields.Select(SafeConvertField));
                break;
            }

            if (fields != null)
            {
                yield return $"{row.Table.Name}:{fields}";
            }
        }

        private static IEnumerable<string> SymbolToStrings(IntermediateSymbol symbol, Dictionary<string, AssemblySymbol> assemblySymbolsByFileId)
        {
            var name = symbol.Definition.Type == SymbolDefinitionType.SummaryInformation ? "_SummaryInformation" : symbol.Definition.Name;
            var id = symbol.Id?.Id ?? String.Empty;

            string fields;
            switch (symbol.Definition.Name)
            {
            // Massage output to match WiX v3 rows and v4 symbols.
            //
            case "Component":
            {
                var componentSymbol = (ComponentSymbol)symbol;
                var attributes = ComponentLocation.Either == componentSymbol.Location ? WindowsInstallerConstants.MsidbComponentAttributesOptional : 0;
                attributes |= ComponentLocation.SourceOnly == componentSymbol.Location ? WindowsInstallerConstants.MsidbComponentAttributesSourceOnly : 0;
                attributes |= ComponentKeyPathType.Registry == componentSymbol.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath : 0;
                attributes |= ComponentKeyPathType.OdbcDataSource == componentSymbol.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource : 0;
                attributes |= componentSymbol.DisableRegistryReflection ? WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection : 0;
                attributes |= componentSymbol.NeverOverwrite ? WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite : 0;
                attributes |= componentSymbol.Permanent ? WindowsInstallerConstants.MsidbComponentAttributesPermanent : 0;
                attributes |= componentSymbol.SharedDllRefCount ? WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount : 0;
                attributes |= componentSymbol.Shared ? WindowsInstallerConstants.MsidbComponentAttributesShared : 0;
                attributes |= componentSymbol.Transitive ? WindowsInstallerConstants.MsidbComponentAttributesTransitive : 0;
                attributes |= componentSymbol.UninstallWhenSuperseded ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;
                attributes |= componentSymbol.Win64 ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;

                fields = String.Join(",",
                    componentSymbol.ComponentId,
                    componentSymbol.DirectoryRef,
                    attributes.ToString(),
                    componentSymbol.Condition,
                    componentSymbol.KeyPath
                    );
                break;
            }
            case "CustomAction":
            {
                var customActionSymbol = (CustomActionSymbol)symbol;
                var type = customActionSymbol.Win64 ? WindowsInstallerConstants.MsidbCustomActionType64BitScript : 0;
                type |= customActionSymbol.TSAware ? WindowsInstallerConstants.MsidbCustomActionTypeTSAware : 0;
                type |= customActionSymbol.Impersonate ? 0 : WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate;
                type |= customActionSymbol.IgnoreResult ? WindowsInstallerConstants.MsidbCustomActionTypeContinue : 0;
                type |= customActionSymbol.Hidden ? WindowsInstallerConstants.MsidbCustomActionTypeHideTarget : 0;
                type |= customActionSymbol.Async ? WindowsInstallerConstants.MsidbCustomActionTypeAsync : 0;
                type |= CustomActionExecutionType.FirstSequence == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence : 0;
                type |= CustomActionExecutionType.OncePerProcess == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess : 0;
                type |= CustomActionExecutionType.ClientRepeat == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat : 0;
                type |= CustomActionExecutionType.Deferred == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript : 0;
                type |= CustomActionExecutionType.Rollback == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback : 0;
                type |= CustomActionExecutionType.Commit == customActionSymbol.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit : 0;
                type |= CustomActionSourceType.File == customActionSymbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeSourceFile : 0;
                type |= CustomActionSourceType.Directory == customActionSymbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeDirectory : 0;
                type |= CustomActionSourceType.Property == customActionSymbol.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeProperty : 0;
                type |= CustomActionTargetType.Dll == customActionSymbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeDll : 0;
                type |= CustomActionTargetType.Exe == customActionSymbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeExe : 0;
                type |= CustomActionTargetType.TextData == customActionSymbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeTextData : 0;
                type |= CustomActionTargetType.JScript == customActionSymbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeJScript : 0;
                type |= CustomActionTargetType.VBScript == customActionSymbol.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeVBScript : 0;

                fields = String.Join(",",
                    type.ToString(),
                    customActionSymbol.Source,
                    customActionSymbol.Target,
                    customActionSymbol.PatchUninstall ? WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall.ToString() : null
                    );
                break;
            }
            case "Directory":
            {
                var directorySymbol = (DirectorySymbol)symbol;

                if (!String.IsNullOrEmpty(directorySymbol.ComponentGuidGenerationSeed))
                {
                    yield return $"WixDirectory:{directorySymbol.Id.Id},{directorySymbol.ComponentGuidGenerationSeed}";
                }

                fields = String.Join(",", directorySymbol.ParentDirectoryRef, directorySymbol.Name, directorySymbol.ShortName, directorySymbol.SourceName, directorySymbol.SourceShortName);
                break;
            }
            case "Feature":
            {
                var featureSymbol = (FeatureSymbol)symbol;
                var attributes = featureSymbol.DisallowAbsent ? WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent : 0;
                attributes |= featureSymbol.DisallowAdvertise ? WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise : 0;
                attributes |= FeatureInstallDefault.FollowParent == featureSymbol.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFollowParent : 0;
                attributes |= FeatureInstallDefault.Source == featureSymbol.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorSource : 0;
                attributes |= FeatureTypicalDefault.Advertise == featureSymbol.TypicalDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise : 0;

                fields = String.Join(",",
                    featureSymbol.ParentFeatureRef,
                    featureSymbol.Title,
                    featureSymbol.Description,
                    featureSymbol.Display.ToString(),
                    featureSymbol.Level.ToString(),
                    featureSymbol.DirectoryRef,
                    attributes.ToString());
                break;
            }
            case "File":
            {
                var fileSymbol = (FileSymbol)symbol;

                if (fileSymbol.BindPath != null)
                {
                    yield return $"BindImage:{fileSymbol.Id.Id},{fileSymbol.BindPath}";
                }

                if (fileSymbol.FontTitle != null)
                {
                    yield return $"Font:{fileSymbol.Id.Id},{fileSymbol.FontTitle}";
                }

                if (fileSymbol.SelfRegCost.HasValue)
                {
                    yield return $"SelfReg:{fileSymbol.Id.Id},{fileSymbol.SelfRegCost}";
                }

                int? assemblyAttributes = null;
                if (assemblySymbolsByFileId.TryGetValue(fileSymbol.Id.Id, out var assemblySymbol))
                {
                    if (assemblySymbol.Type == AssemblyType.DotNetAssembly)
                    {
                        assemblyAttributes = 0;
                    }
                    else if (assemblySymbol.Type == AssemblyType.Win32Assembly)
                    {
                        assemblyAttributes = 1;
                    }
                }

                yield return "WixFile:" + String.Join(",",
                    fileSymbol.Id.Id,
                    assemblyAttributes,
                    assemblySymbol?.ManifestFileRef,
                    assemblySymbol?.ApplicationFileRef,
                    fileSymbol.DirectoryRef,
                    fileSymbol.DiskId,
                    fileSymbol.Source.Path,
                    null, // assembly processor arch
                    fileSymbol.PatchGroup,
                    0,
                    (int)fileSymbol.PatchAttributes,
                    fileSymbol.RetainLengths,
                    fileSymbol.IgnoreOffsets,
                    fileSymbol.IgnoreLengths,
                    fileSymbol.RetainOffsets
                    );

                var fileAttributes = 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.ReadOnly) != 0 ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.Hidden) != 0 ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.System) != 0 ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.Vital) != 0 ? WindowsInstallerConstants.MsidbFileAttributesVital : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.Checksum) != 0 ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.Compressed) != 0 ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0;
                fileAttributes |= (fileSymbol.Attributes & FileSymbolAttributes.Uncompressed) != 0 ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0;

                fields = String.Join(",",
                fileSymbol.ComponentRef,
                fileSymbol.Name,
                fileSymbol.FileSize.ToString(),
                fileSymbol.Version,
                fileSymbol.Language,
                fileAttributes);
                break;
            }

            case "Media":
                fields = String.Join(",", symbol.Fields.Skip(1).Select(SafeConvertField));
                break;

            case "Assembly":
            {
                var assemblySymbol = (AssemblySymbol)symbol;

                id = null;
                name = "MsiAssembly";
                fields = String.Join(",", assemblySymbol.ComponentRef, assemblySymbol.FeatureRef, assemblySymbol.ManifestFileRef, assemblySymbol.ApplicationFileRef, assemblySymbol.Type == AssemblyType.Win32Assembly ? 1 : 0);
                break;
            }
            case "RegLocator":
            {
                var locatorSymbol = (RegLocatorSymbol)symbol;

                fields = String.Join(",", (int)locatorSymbol.Root, locatorSymbol.Key, locatorSymbol.Name, (int)locatorSymbol.Type, locatorSymbol.Win64);
                break;
            }
            case "Registry":
            {
                var registrySymbol = (RegistrySymbol)symbol;
                var value = registrySymbol.Value;

                switch (registrySymbol.ValueType)
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
                    switch (registrySymbol.ValueAction)
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
                            value = String.Concat("[~]", value, "[~]");
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

                fields = String.Join(",",
                    ((int)registrySymbol.Root).ToString(),
                    registrySymbol.Key,
                    registrySymbol.Name,
                    value,
                    registrySymbol.ComponentRef
                    );
                break;
            }

            case "RemoveRegistry":
            {
                var removeRegistrySymbol = (RemoveRegistrySymbol)symbol;
                fields = String.Join(",",
                    ((int)removeRegistrySymbol.Root).ToString(),
                    removeRegistrySymbol.Key,
                    removeRegistrySymbol.Name,
                    removeRegistrySymbol.ComponentRef
                    );
                break;
            }

            case "ServiceControl":
            {
                var serviceControlSymbol = (ServiceControlSymbol)symbol;

                var events = serviceControlSymbol.InstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventDelete : 0;
                events |= serviceControlSymbol.UninstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete : 0;
                events |= serviceControlSymbol.InstallStart ? WindowsInstallerConstants.MsidbServiceControlEventStart : 0;
                events |= serviceControlSymbol.UninstallStart ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStart : 0;
                events |= serviceControlSymbol.InstallStop ? WindowsInstallerConstants.MsidbServiceControlEventStop : 0;
                events |= serviceControlSymbol.UninstallStop ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStop : 0;

                fields = String.Join(",",
                    serviceControlSymbol.Name,
                    events.ToString(),
                    serviceControlSymbol.Arguments,
                    serviceControlSymbol.Wait == true ? "1" : "0",
                    serviceControlSymbol.ComponentRef
                    );
                break;
            }

            case "ServiceInstall":
            {
                var serviceInstallSymbol = (ServiceInstallSymbol)symbol;

                var errorControl = (int)serviceInstallSymbol.ErrorControl;
                errorControl |= serviceInstallSymbol.Vital ? WindowsInstallerConstants.MsidbServiceInstallErrorControlVital : 0;

                var serviceType = (int)serviceInstallSymbol.ServiceType;
                serviceType |= serviceInstallSymbol.Interactive ? WindowsInstallerConstants.MsidbServiceInstallInteractive : 0;

                fields = String.Join(",",
                    serviceInstallSymbol.Name,
                    serviceInstallSymbol.DisplayName,
                    serviceType.ToString(),
                    ((int)serviceInstallSymbol.StartType).ToString(),
                    errorControl.ToString(),
                    serviceInstallSymbol.LoadOrderGroup,
                    serviceInstallSymbol.Dependencies,
                    serviceInstallSymbol.StartName,
                    serviceInstallSymbol.Password,
                    serviceInstallSymbol.Arguments,
                    serviceInstallSymbol.ComponentRef,
                    serviceInstallSymbol.Description
                    );
                break;
            }

            case "Upgrade":
            {
                var upgradeSymbol = (UpgradeSymbol)symbol;

                var attributes = upgradeSymbol.MigrateFeatures ? WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures : 0;
                attributes |= upgradeSymbol.OnlyDetect ? WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect : 0;
                attributes |= upgradeSymbol.IgnoreRemoveFailures ? WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure : 0;
                attributes |= upgradeSymbol.VersionMinInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive : 0;
                attributes |= upgradeSymbol.VersionMaxInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive : 0;
                attributes |= upgradeSymbol.ExcludeLanguages ? WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive : 0;

                fields = String.Join(",",
                    upgradeSymbol.VersionMin,
                    upgradeSymbol.VersionMax,
                    upgradeSymbol.Language,
                    attributes.ToString(),
                    upgradeSymbol.Remove,
                    upgradeSymbol.ActionProperty
                    );
                break;
            }

            case "WixAction":
            {
                var wixActionSymbol = (WixActionSymbol)symbol;
                var data = wixActionSymbol.Fields[(int)WixActionSymbolFields.SequenceTable].AsObject();
                var sequenceTableAsInt = data is string ? (int)SequenceStringToSequenceTable(data) : (int)wixActionSymbol.SequenceTable;

                fields = String.Join(",",
                    sequenceTableAsInt,
                    wixActionSymbol.Action,
                    wixActionSymbol.Condition,
                    wixActionSymbol.Sequence?.ToString() ?? String.Empty,
                    wixActionSymbol.Before,
                    wixActionSymbol.After,
                    wixActionSymbol.Overridable == true ? "1" : "0"
                    );
                break;
            }

            case "WixComplexReference":
            {
                var wixComplexReferenceSymbol = (WixComplexReferenceSymbol)symbol;
                fields = String.Join(",",
                    wixComplexReferenceSymbol.Parent,
                    (int)wixComplexReferenceSymbol.ParentType,
                    wixComplexReferenceSymbol.ParentLanguage,
                    wixComplexReferenceSymbol.Child,
                    (int)wixComplexReferenceSymbol.ChildType,
                    wixComplexReferenceSymbol.IsPrimary ? "1" : "0"
                    );
                break;
            }

            case "WixProperty":
            {
                var wixPropertySymbol = (WixPropertySymbol)symbol;
                var attributes = wixPropertySymbol.Admin ? 0x1 : 0;
                attributes |= wixPropertySymbol.Hidden ? 0x2 : 0;
                attributes |= wixPropertySymbol.Secure ? 0x4 : 0;

                fields = String.Join(",",
                    wixPropertySymbol.PropertyRef,
                    attributes.ToString()
                    );
                break;
            }

            default:
                fields = String.Join(",", symbol.Fields.Select(SafeConvertField));
                break;
            }

            fields = String.IsNullOrEmpty(id) ? fields : String.IsNullOrEmpty(fields) ? id : $"{id},{fields}";
            yield return $"{name}:{fields}";
        }

        private static SequenceTable SequenceStringToSequenceTable(object sequenceString)
        {
            switch (sequenceString)
            {
            case "AdminExecuteSequence":
                return SequenceTable.AdminExecuteSequence;
            case "AdminUISequence":
                return SequenceTable.AdminUISequence;
            case "AdvtExecuteSequence":
                return SequenceTable.AdvertiseExecuteSequence;
            case "InstallExecuteSequence":
                return SequenceTable.InstallExecuteSequence;
            case "InstallUISequence":
                return SequenceTable.InstallUISequence;
            default:
                throw new ArgumentException($"Unknown sequence: {sequenceString}");
            }
        }

        private static string SafeConvertField(Wix3.Field field)
        {
            return field?.Data?.ToString();
        }

        private static string SafeConvertField(IntermediateField field)
        {
            var data = field.AsObject();
            if (data is IntermediateFieldPathValue path)
            {
                return path.Path;
            }

            return data?.ToString();
        }

        private static string[] SplitDefaultDir(string defaultDir)
        {
            var split1 = defaultDir.Split(':');
            var targetSplit = split1.Length > 1 ? split1[1].Split('|') : split1[0].Split('|');
            var sourceSplit = split1.Length > 1 ? split1[0].Split('|') : new[] { String.Empty };
            return new[]
            {
                targetSplit.Length > 1 ? targetSplit[1] : targetSplit[0],
                targetSplit.Length > 1 ? targetSplit[0] : String.Empty,
                sourceSplit.Length > 1 ? sourceSplit[1] : sourceSplit[0],
                sourceSplit.Length > 1 ? sourceSplit[0] : String.Empty
            };
        }
    }
}
