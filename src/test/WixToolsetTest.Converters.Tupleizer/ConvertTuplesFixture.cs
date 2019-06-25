// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters.Tupleizer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Wix3 = Microsoft.Tools.WindowsInstallerXml;
    using WixToolset.Converters.Tupleizer;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class ConvertTuplesFixture
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

                var intermediate = ConvertTuples.ConvertFile(path);

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

                var tuples = intermediate.Sections.SelectMany(s => s.Tuples);

                var assemblyTuplesByFileId = tuples.OfType<AssemblyTuple>().ToDictionary(a => a.Id.Id);

                var wix4Dump = tuples
                    .SelectMany(tuple => TupleToStrings(tuple, assemblyTuplesByFileId))
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

            // Massage output to match WiX v3 rows and v4 tuples.
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

        private static IEnumerable<string> TupleToStrings(IntermediateTuple tuple, Dictionary<string, AssemblyTuple> assemblyTuplesByFileId)
        {
            var name = tuple.Definition.Type == TupleDefinitionType.SummaryInformation ? "_SummaryInformation" : tuple.Definition.Name;
            var id = tuple.Id?.Id ?? String.Empty;

            string fields;
            switch (tuple.Definition.Name)
            {
            // Massage output to match WiX v3 rows and v4 tuples.
            //
            case "Component":
            {
                var componentTuple = (ComponentTuple)tuple;
                var attributes = ComponentLocation.Either == componentTuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesOptional : 0;
                attributes |= ComponentLocation.SourceOnly == componentTuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesSourceOnly : 0;
                attributes |= ComponentKeyPathType.Registry == componentTuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath : 0;
                attributes |= ComponentKeyPathType.OdbcDataSource == componentTuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource : 0;
                attributes |= componentTuple.DisableRegistryReflection ? WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection : 0;
                attributes |= componentTuple.NeverOverwrite ? WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite : 0;
                attributes |= componentTuple.Permanent ? WindowsInstallerConstants.MsidbComponentAttributesPermanent : 0;
                attributes |= componentTuple.SharedDllRefCount ? WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount : 0;
                attributes |= componentTuple.Shared ? WindowsInstallerConstants.MsidbComponentAttributesShared : 0;
                attributes |= componentTuple.Transitive ? WindowsInstallerConstants.MsidbComponentAttributesTransitive : 0;
                attributes |= componentTuple.UninstallWhenSuperseded ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;
                attributes |= componentTuple.Win64 ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;

                fields = String.Join(",",
                    componentTuple.ComponentId,
                    componentTuple.DirectoryRef,
                    attributes.ToString(),
                    componentTuple.Condition,
                    componentTuple.KeyPath
                    );
                break;
            }
            case "CustomAction":
            {
                var customActionTuple = (CustomActionTuple)tuple;
                var type = customActionTuple.Win64 ? WindowsInstallerConstants.MsidbCustomActionType64BitScript : 0;
                type |= customActionTuple.TSAware ? WindowsInstallerConstants.MsidbCustomActionTypeTSAware : 0;
                type |= customActionTuple.Impersonate ? 0 : WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate;
                type |= customActionTuple.IgnoreResult ? WindowsInstallerConstants.MsidbCustomActionTypeContinue : 0;
                type |= customActionTuple.Hidden ? WindowsInstallerConstants.MsidbCustomActionTypeHideTarget : 0;
                type |= customActionTuple.Async ? WindowsInstallerConstants.MsidbCustomActionTypeAsync : 0;
                type |= CustomActionExecutionType.FirstSequence == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence : 0;
                type |= CustomActionExecutionType.OncePerProcess == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess : 0;
                type |= CustomActionExecutionType.ClientRepeat == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat : 0;
                type |= CustomActionExecutionType.Deferred == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript : 0;
                type |= CustomActionExecutionType.Rollback == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback : 0;
                type |= CustomActionExecutionType.Commit == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit : 0;
                type |= CustomActionSourceType.File == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeSourceFile : 0;
                type |= CustomActionSourceType.Directory == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeDirectory : 0;
                type |= CustomActionSourceType.Property == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeProperty : 0;
                type |= CustomActionTargetType.Dll == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeDll : 0;
                type |= CustomActionTargetType.Exe == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeExe : 0;
                type |= CustomActionTargetType.TextData == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeTextData : 0;
                type |= CustomActionTargetType.JScript == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeJScript : 0;
                type |= CustomActionTargetType.VBScript == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeVBScript : 0;

                fields = String.Join(",",
                    type.ToString(),
                    customActionTuple.Source,
                    customActionTuple.Target,
                    customActionTuple.PatchUninstall ? WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall.ToString() : null
                    );
                break;
            }
            case "Directory":
            {
                var directoryTuple = (DirectoryTuple)tuple;

                if (!String.IsNullOrEmpty(directoryTuple.ComponentGuidGenerationSeed))
                {
                    yield return $"WixDirectory:{directoryTuple.Id.Id},{directoryTuple.ComponentGuidGenerationSeed}";
                }

                fields = String.Join(",", directoryTuple.ParentDirectoryRef, directoryTuple.Name, directoryTuple.ShortName, directoryTuple.SourceName, directoryTuple.SourceShortName);
                break;
            }
            case "Feature":
            {
                var featureTuple = (FeatureTuple)tuple;
                var attributes = featureTuple.DisallowAbsent ? WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent : 0;
                attributes |= featureTuple.DisallowAdvertise ? WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise : 0;
                attributes |= FeatureInstallDefault.FollowParent == featureTuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFollowParent : 0;
                attributes |= FeatureInstallDefault.Source == featureTuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorSource : 0;
                attributes |= FeatureTypicalDefault.Advertise == featureTuple.TypicalDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise : 0;

                fields = String.Join(",",
                    featureTuple.ParentFeatureRef,
                    featureTuple.Title,
                    featureTuple.Description,
                    featureTuple.Display.ToString(),
                    featureTuple.Level.ToString(),
                    featureTuple.DirectoryRef,
                    attributes.ToString());
                break;
            }
            case "File":
            {
                var fileTuple = (FileTuple)tuple;

                if (fileTuple.BindPath != null)
                {
                    yield return $"BindImage:{fileTuple.Id.Id},{fileTuple.BindPath}";
                }

                if (fileTuple.FontTitle != null)
                {
                    yield return $"Font:{fileTuple.Id.Id},{fileTuple.FontTitle}";
                }

                if (fileTuple.SelfRegCost.HasValue)
                {
                    yield return $"SelfReg:{fileTuple.Id.Id},{fileTuple.SelfRegCost}";
                }

                int? assemblyAttributes = null;
                if (assemblyTuplesByFileId.TryGetValue(fileTuple.Id.Id, out var assemblyTuple))
                {
                    if (assemblyTuple.Type == AssemblyType.DotNetAssembly)
                    {
                        assemblyAttributes = 0;
                    }
                    else if (assemblyTuple.Type == AssemblyType.Win32Assembly)
                    {
                        assemblyAttributes = 1;
                    }
                }

                yield return "WixFile:" + String.Join(",",
                    fileTuple.Id.Id,
                    assemblyAttributes,
                    assemblyTuple?.ManifestFileRef,
                    assemblyTuple?.ApplicationFileRef,
                    fileTuple.DirectoryRef,
                    fileTuple.DiskId,
                    fileTuple.Source.Path,
                    null, // assembly processor arch
                    fileTuple.PatchGroup,
                    (fileTuple.Attributes & FileTupleAttributes.GeneratedShortFileName) != 0 ? 1 : 0,
                    (int)fileTuple.PatchAttributes,
                    fileTuple.RetainLengths,
                    fileTuple.IgnoreOffsets,
                    fileTuple.IgnoreLengths,
                    fileTuple.RetainOffsets
                    );

                var fileAttributes = 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.ReadOnly) != 0 ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.Hidden) != 0 ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.System) != 0 ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.Vital) != 0 ? WindowsInstallerConstants.MsidbFileAttributesVital : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.Checksum) != 0 ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.Compressed) != 0 ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0;
                fileAttributes |= (fileTuple.Attributes & FileTupleAttributes.Uncompressed) != 0 ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0;

                fields = String.Join(",",
                fileTuple.ComponentRef,
                fileTuple.Name,
                fileTuple.FileSize.ToString(),
                fileTuple.Version,
                fileTuple.Language,
                fileAttributes);
                break;
            }

            case "Media":
                fields = String.Join(",", tuple.Fields.Skip(1).Select(SafeConvertField));
                break;

            case "Assembly":
            {
                var assemblyTuple = (AssemblyTuple)tuple;

                id = null;
                name = "MsiAssembly";
                fields = String.Join(",", assemblyTuple.ComponentRef, assemblyTuple.FeatureRef, assemblyTuple.ManifestFileRef, assemblyTuple.ApplicationFileRef, assemblyTuple.Type == AssemblyType.Win32Assembly ? 1 : 0);
                break;
            }
            case "RegLocator":
            {
                var locatorTuple = (RegLocatorTuple)tuple;

                fields = String.Join(",", (int)locatorTuple.Root, locatorTuple.Key, locatorTuple.Name, (int)locatorTuple.Type, locatorTuple.Win64);
                break;
            }
            case "Registry":
            {
                var registryTuple = (RegistryTuple)tuple;
                var value = registryTuple.Value;

                switch (registryTuple.ValueType)
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
                    switch (registryTuple.ValueAction)
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
                    ((int)registryTuple.Root).ToString(),
                    registryTuple.Key,
                    registryTuple.Name,
                    value,
                    registryTuple.ComponentRef
                    );
                break;
            }

            case "RemoveRegistry":
            {
                var removeRegistryTuple = (RemoveRegistryTuple)tuple;
                fields = String.Join(",",
                    ((int)removeRegistryTuple.Root).ToString(),
                    removeRegistryTuple.Key,
                    removeRegistryTuple.Name,
                    removeRegistryTuple.ComponentRef
                    );
                break;
            }

            case "ServiceControl":
            {
                var serviceControlTuple = (ServiceControlTuple)tuple;

                var events = serviceControlTuple.InstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventDelete : 0;
                events |= serviceControlTuple.UninstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete : 0;
                events |= serviceControlTuple.InstallStart ? WindowsInstallerConstants.MsidbServiceControlEventStart : 0;
                events |= serviceControlTuple.UninstallStart ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStart : 0;
                events |= serviceControlTuple.InstallStop ? WindowsInstallerConstants.MsidbServiceControlEventStop : 0;
                events |= serviceControlTuple.UninstallStop ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStop : 0;

                fields = String.Join(",",
                    serviceControlTuple.Name,
                    events.ToString(),
                    serviceControlTuple.Arguments,
                    serviceControlTuple.Wait == true ? "1" : "0",
                    serviceControlTuple.ComponentRef
                    );
                break;
            }

            case "ServiceInstall":
            {
                var serviceInstallTuple = (ServiceInstallTuple)tuple;

                var errorControl = (int)serviceInstallTuple.ErrorControl;
                errorControl |= serviceInstallTuple.Vital ? WindowsInstallerConstants.MsidbServiceInstallErrorControlVital : 0;

                var serviceType = (int)serviceInstallTuple.ServiceType;
                serviceType |= serviceInstallTuple.Interactive ? WindowsInstallerConstants.MsidbServiceInstallInteractive : 0;

                fields = String.Join(",",
                    serviceInstallTuple.Name,
                    serviceInstallTuple.DisplayName,
                    serviceType.ToString(),
                    ((int)serviceInstallTuple.StartType).ToString(),
                    errorControl.ToString(),
                    serviceInstallTuple.LoadOrderGroup,
                    serviceInstallTuple.Dependencies,
                    serviceInstallTuple.StartName,
                    serviceInstallTuple.Password,
                    serviceInstallTuple.Arguments,
                    serviceInstallTuple.ComponentRef,
                    serviceInstallTuple.Description
                    );
                break;
            }

            case "Upgrade":
            {
                var upgradeTuple = (UpgradeTuple)tuple;

                var attributes = upgradeTuple.MigrateFeatures ? WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures : 0;
                attributes |= upgradeTuple.OnlyDetect ? WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect : 0;
                attributes |= upgradeTuple.IgnoreRemoveFailures ? WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure : 0;
                attributes |= upgradeTuple.VersionMinInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive : 0;
                attributes |= upgradeTuple.VersionMaxInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive : 0;
                attributes |= upgradeTuple.ExcludeLanguages ? WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive : 0;

                fields = String.Join(",",
                    upgradeTuple.VersionMin,
                    upgradeTuple.VersionMax,
                    upgradeTuple.Language,
                    attributes.ToString(),
                    upgradeTuple.Remove,
                    upgradeTuple.ActionProperty
                    );
                break;
            }

            case "WixAction":
            {
                var wixActionTuple = (WixActionTuple)tuple;
                var data = wixActionTuple.Fields[(int)WixActionTupleFields.SequenceTable].AsObject();
                var sequenceTableAsInt = data is string ? (int)SequenceStringToSequenceTable(data) : (int)wixActionTuple.SequenceTable;

                fields = String.Join(",",
                    sequenceTableAsInt,
                    wixActionTuple.Action,
                    wixActionTuple.Condition,
                    wixActionTuple.Sequence?.ToString() ?? String.Empty,
                    wixActionTuple.Before,
                    wixActionTuple.After,
                    wixActionTuple.Overridable == true ? "1" : "0"
                    );
                break;
            }

            case "WixComplexReference":
            {
                var wixComplexReferenceTuple = (WixComplexReferenceTuple)tuple;
                fields = String.Join(",",
                    wixComplexReferenceTuple.Parent,
                    (int)wixComplexReferenceTuple.ParentType,
                    wixComplexReferenceTuple.ParentLanguage,
                    wixComplexReferenceTuple.Child,
                    (int)wixComplexReferenceTuple.ChildType,
                    wixComplexReferenceTuple.IsPrimary ? "1" : "0"
                    );
                break;
            }

            case "WixProperty":
            {
                var wixPropertyTuple = (WixPropertyTuple)tuple;
                var attributes = wixPropertyTuple.Admin ? 0x1 : 0;
                attributes |= wixPropertyTuple.Hidden ? 0x2 : 0;
                attributes |= wixPropertyTuple.Secure ? 0x4 : 0;

                fields = String.Join(",",
                    wixPropertyTuple.PropertyRef,
                    attributes.ToString()
                    );
                break;
            }

            default:
                fields = String.Join(",", tuple.Fields.Select(SafeConvertField));
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
